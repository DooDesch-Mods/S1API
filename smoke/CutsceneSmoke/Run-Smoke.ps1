param(
    [ValidateSet("Mono", "Il2Cpp", "Both")]
    [string]$Configuration = "Both",
    [string]$MonoGamePath = "D:\SteamLibrary\steamapps\common\Schedule I_alternate",
    [string]$Il2CppGamePath = "D:\SteamLibrary\steamapps\common\Schedule I_public",
    [int]$TimeoutSeconds = 240,
    [switch]$KeepInstalled,
    [switch]$ShowWindow,
    [string]$RecoverInterruptedRun,
    [ValidateSet("Mono", "Il2Cpp")]
    [string]$RecoverRuntime = "Mono"
)

$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$projectPath = Join-Path $PSScriptRoot "CutsceneSmoke.csproj"
$runId = "{0}-{1}" -f (Get-Date -Format "yyyyMMdd-HHmmss"), ([Guid]::NewGuid().ToString("N").Substring(0, 8))
$artifactRoot = Join-Path $PSScriptRoot "artifacts\$runId"
$temporaryRoot = Join-Path ([System.IO.Path]::GetTempPath()) "S1API-CutsceneSmoke-$runId"

function Assert-TemporaryChild {
    param([string]$Path)

    $resolvedPath = [System.IO.Path]::GetFullPath($Path)
    $resolvedTemporaryRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
    if (-not $resolvedPath.StartsWith($resolvedTemporaryRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing cleanup outside the temporary root: $resolvedPath"
    }
}

function Remove-TemporaryTree {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        return
    }

    Assert-TemporaryChild -Path $Path
    Remove-Item -LiteralPath $Path -Recurse -Force
}

function Get-RuntimeContext {
    param([string]$Runtime)

    if ($Runtime -eq "Mono") {
        return [pscustomobject]@{
            Runtime = "Mono"
            SolutionConfiguration = "MonoMelon"
            GamePath = $MonoGamePath
            S1ApiDll = Join-Path $repoRoot "S1API\bin\MonoMelon\netstandard2.1\S1API.dll"
            SmokeDll = Join-Path $PSScriptRoot "bin\Mono\netstandard2.1\CutsceneSmoke.dll"
        }
    }

    return [pscustomobject]@{
        Runtime = "Il2Cpp"
        SolutionConfiguration = "Il2CppMelon"
        GamePath = $Il2CppGamePath
        S1ApiDll = Join-Path $repoRoot "S1API\bin\Il2CppMelon\net6.0\S1API.dll"
        SmokeDll = Join-Path $PSScriptRoot "bin\Il2Cpp\net6.0\CutsceneSmoke.dll"
    }
}

function Restore-InterruptedRun {
    param(
        [string]$RunRoot,
        [string]$Runtime
    )

    Assert-TemporaryChild -Path $RunRoot
    $resolvedRunRoot = [System.IO.Path]::GetFullPath($RunRoot)
    if (-not (Test-Path -LiteralPath $resolvedRunRoot -PathType Container) -or
        -not (Split-Path $resolvedRunRoot -Leaf).StartsWith("S1API-CutsceneSmoke-", [System.StringComparison]::Ordinal)) {
        throw "The interrupted run root is missing or invalid: $resolvedRunRoot"
    }

    $context = Get-RuntimeContext -Runtime $Runtime
    $gamePath = (Resolve-Path $context.GamePath).Path
    $exePath = Join-Path $gamePath "Schedule I.exe"
    $modsPath = Join-Path $gamePath "Mods"
    $latestLogPath = Join-Path $gamePath "MelonLoader\Latest.log"
    $runtimeName = if ($Runtime -eq "Mono") { "MonoMelon" } else { "Il2CppMelon" }
    $token = (Split-Path $resolvedRunRoot -Leaf).Substring("S1API-CutsceneSmoke-".Length)
    $recoveryArtifactPath = Join-Path $PSScriptRoot "artifacts\$token\$runtimeName"
    $statePath = Join-Path $resolvedRunRoot "$Runtime-state"
    $backupPath = Join-Path $resolvedRunRoot "$Runtime-backup"

    Get-CimInstance Win32_Process -Filter "Name = 'Schedule I.exe'" -ErrorAction SilentlyContinue |
        Where-Object {
            $_.ExecutablePath -and
            $_.ExecutablePath.Equals($exePath, [System.StringComparison]::OrdinalIgnoreCase) -and
            $_.CommandLine -and
            $_.CommandLine.Contains($resolvedRunRoot)
        } |
        ForEach-Object {
            Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue
        }

    New-Item -ItemType Directory -Force -Path $recoveryArtifactPath | Out-Null
    foreach ($fileName in @("result.txt", "cutscene-title-skip.png")) {
        $source = Join-Path $statePath $fileName
        if (Test-Path -LiteralPath $source -PathType Leaf) {
            Copy-Item -LiteralPath $source -Destination (Join-Path $recoveryArtifactPath $fileName) -Force
        }
    }
    if (Test-Path -LiteralPath $latestLogPath -PathType Leaf) {
        Copy-Item -LiteralPath $latestLogPath -Destination (Join-Path $recoveryArtifactPath "MelonLoader-Latest.log") -Force
    }

    $modsRestoredMarker = Join-Path $backupPath "mods-restored.marker"
    $modsBackupPath = Join-Path $backupPath "Mods"
    if (-not (Test-Path -LiteralPath $modsRestoredMarker -PathType Leaf)) {
        foreach ($deployedName in @("CutsceneSmoke.dll", "S1API.dll")) {
            $deployedPath = Join-Path $modsPath $deployedName
            if (Test-Path -LiteralPath $deployedPath -PathType Leaf) {
                Remove-Item -LiteralPath $deployedPath -Force
            }
        }

        Get-ChildItem -LiteralPath $modsBackupPath -Filter "*.dll" -File | ForEach-Object {
            Move-Item -LiteralPath $_.FullName -Destination (Join-Path $modsPath $_.Name) -Force
        }
        Set-Content -LiteralPath $modsRestoredMarker -Value "restored"
    }

    $userLibBackups = @(Get-ChildItem -LiteralPath $backupPath -Filter "file-*.bak" -File)
    $userLibPath = Join-Path $gamePath "UserLibs\S1API.dll"
    if ($userLibBackups.Count -gt 1) {
        throw "Expected at most one UserLib S1API backup, found $($userLibBackups.Count)."
    }
    if ($userLibBackups.Count -eq 1) {
        Copy-Item `
            -LiteralPath $userLibBackups[0].FullName `
            -Destination $userLibPath `
            -Force
    }
    elseif (Test-Path -LiteralPath $userLibPath -PathType Leaf) {
        Remove-Item -LiteralPath $userLibPath -Force
    }

    $latestLogBackup = Join-Path $backupPath "Latest.log"
    if (Test-Path -LiteralPath $latestLogBackup -PathType Leaf) {
        Copy-Item -LiteralPath $latestLogBackup -Destination $latestLogPath -Force
    }
    elseif (Test-Path -LiteralPath $latestLogPath -PathType Leaf) {
        Remove-Item -LiteralPath $latestLogPath -Force
    }

    Remove-TemporaryTree -Path $resolvedRunRoot
    Write-Host "PASS: restored interrupted $Runtime smoke run '$token'."
    Write-Host "Failed-run evidence directory: $recoveryArtifactPath"
}

if ($RecoverInterruptedRun) {
    Restore-InterruptedRun -RunRoot $RecoverInterruptedRun -Runtime $RecoverRuntime
    exit 0
}

New-Item -ItemType Directory -Force -Path $artifactRoot, $temporaryRoot | Out-Null

function Invoke-Build {
    param($Context)

    Write-Host "Restoring S1API for $($Context.SolutionConfiguration)..."
    & dotnet restore (Join-Path $repoRoot "S1API.sln") `
        "-p:Configuration=$($Context.SolutionConfiguration)" `
        "-p:AutomateLocalDeployment=false"
    if ($LASTEXITCODE -ne 0) {
        throw "$($Context.SolutionConfiguration) restore failed."
    }

    Write-Host "Building full S1API solution for $($Context.SolutionConfiguration)..."
    & dotnet build (Join-Path $repoRoot "S1API.sln") `
        -c $Context.SolutionConfiguration `
        --no-restore `
        "-p:AutomateLocalDeployment=false"
    if ($LASTEXITCODE -ne 0) {
        throw "$($Context.SolutionConfiguration) solution build failed."
    }

    Write-Host "Building CutsceneSmoke for $($Context.Runtime)..."
    & dotnet build $projectPath -c $Context.Runtime
    if ($LASTEXITCODE -ne 0) {
        throw "$($Context.Runtime) CutsceneSmoke build failed."
    }

    foreach ($path in @($Context.S1ApiDll, $Context.SmokeDll)) {
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
            throw "Expected build output was not found: $path"
        }
    }
}

function Invoke-RuntimeSmoke {
    param($Context)

    $gamePath = (Resolve-Path $Context.GamePath).Path
    $exePath = Join-Path $gamePath "Schedule I.exe"
    $modsPath = Join-Path $gamePath "Mods"
    $userLibsPath = Join-Path $gamePath "UserLibs"
    $latestLogPath = Join-Path $gamePath "MelonLoader\Latest.log"
    $runtimeArtifactPath = Join-Path $artifactRoot $Context.SolutionConfiguration
    $statePath = Join-Path $temporaryRoot "$($Context.Runtime)-state"
    $backupPath = Join-Path $temporaryRoot "$($Context.Runtime)-backup"
    $deployedFiles = New-Object System.Collections.Generic.List[object]
    $quarantinedMods = New-Object System.Collections.Generic.List[object]
    $process = $null
    $resultText = $null
    $hadLatestLog = Test-Path -LiteralPath $latestLogPath -PathType Leaf

    if (-not (Test-Path -LiteralPath $exePath -PathType Leaf)) {
        throw "Schedule I executable was not found: $exePath"
    }

    $runningGame = Get-CimInstance Win32_Process -Filter "Name = 'Schedule I.exe'" -ErrorAction SilentlyContinue |
        Where-Object { $_.ExecutablePath -and $_.ExecutablePath.Equals($exePath, [System.StringComparison]::OrdinalIgnoreCase) }
    if ($runningGame) {
        throw "A pre-existing Schedule I process is using '$gamePath'. Close it before running this smoke."
    }

    New-Item -ItemType Directory -Force -Path `
        $modsPath, $userLibsPath, $runtimeArtifactPath, $statePath, $backupPath | Out-Null

    function Backup-And-Copy {
        param(
            [string]$Source,
            [string]$Destination
        )

        $entry = [pscustomobject]@{
            Destination = $Destination
            Backup = $null
            Existed = Test-Path -LiteralPath $Destination -PathType Leaf
        }
        if ($entry.Existed) {
            $entry.Backup = Join-Path $backupPath ("file-" + [Guid]::NewGuid().ToString("N") + ".bak")
            Copy-Item -LiteralPath $Destination -Destination $entry.Backup -Force
        }

        Copy-Item -LiteralPath $Source -Destination $Destination -Force
        $deployedFiles.Add($entry)

        $sourceHash = (Get-FileHash -LiteralPath $Source -Algorithm SHA256).Hash
        $destinationHash = (Get-FileHash -LiteralPath $Destination -Algorithm SHA256).Hash
        if ($sourceHash -ne $destinationHash) {
            throw "Deployment hash mismatch for '$Destination'."
        }
    }

    function Restore-Environment {
        if ($KeepInstalled) {
            return
        }

        foreach ($entry in $deployedFiles) {
            if ($entry.Existed) {
                Copy-Item -LiteralPath $entry.Backup -Destination $entry.Destination -Force
            }
            elseif (Test-Path -LiteralPath $entry.Destination -PathType Leaf) {
                Remove-Item -LiteralPath $entry.Destination -Force
            }
        }

        foreach ($entry in $quarantinedMods) {
            if (Test-Path -LiteralPath $entry.Original -PathType Leaf) {
                Remove-Item -LiteralPath $entry.Original -Force
            }
            Move-Item -LiteralPath $entry.Backup -Destination $entry.Original -Force
        }
    }

    try {
        $modsBackupPath = Join-Path $backupPath "Mods"
        New-Item -ItemType Directory -Force -Path $modsBackupPath | Out-Null
        Get-ChildItem -LiteralPath $modsPath -Filter "*.dll" -File | ForEach-Object {
            $backup = Join-Path $modsBackupPath $_.Name
            Move-Item -LiteralPath $_.FullName -Destination $backup -Force
            $quarantinedMods.Add([pscustomobject]@{
                Original = $_.FullName
                Backup = $backup
            })
        }

        if ($hadLatestLog) {
            Copy-Item -LiteralPath $latestLogPath -Destination (Join-Path $backupPath "Latest.log") -Force
            Remove-Item -LiteralPath $latestLogPath -Force
        }

        Backup-And-Copy -Source $Context.S1ApiDll -Destination (Join-Path $modsPath "S1API.dll")
        Backup-And-Copy -Source $Context.S1ApiDll -Destination (Join-Path $userLibsPath "S1API.dll")
        Backup-And-Copy -Source $Context.SmokeDll -Destination (Join-Path $modsPath "CutsceneSmoke.dll")

        $actualMods = @(Get-ChildItem -LiteralPath $modsPath -Filter "*.dll" -File |
            Select-Object -ExpandProperty Name |
            Sort-Object)
        $expectedMods = @("CutsceneSmoke.dll", "S1API.dll")
        if (($actualMods -join "|") -ne ($expectedMods -join "|")) {
            throw "Unexpected Mods DLL set: $($actualMods -join ', ')."
        }

        $arguments = @(
            "--s1api-cutscene-smoke",
            "--s1api-cutscene-smoke-exit",
            "--s1api-cutscene-smoke-dir",
            ('"' + $statePath + '"')
        )
        $windowStyle = if ($ShowWindow) { "Normal" } else { "Hidden" }
        Write-Host "Launching $($Context.Runtime) cutscene smoke..."
        $process = Start-Process `
            -FilePath $exePath `
            -ArgumentList $arguments `
            -WorkingDirectory $gamePath `
            -PassThru `
            -WindowStyle $windowStyle

        $resultPath = Join-Path $statePath "result.txt"
        $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
        $nextProgress = (Get-Date).AddSeconds(10)
        while ((Get-Date) -lt $deadline) {
            if (Test-Path -LiteralPath $resultPath -PathType Leaf) {
                $resultText = (Get-Content -LiteralPath $resultPath -Raw).Trim()
                break
            }

            if ($process.HasExited) {
                break
            }

            if ((Get-Date) -ge $nextProgress) {
                Write-Host "Waiting for $($Context.Runtime) result; pid=$($process.Id)"
                $nextProgress = (Get-Date).AddSeconds(10)
            }

            Start-Sleep -Milliseconds 500
        }

        Start-Sleep -Seconds 2
        foreach ($fileName in @("result.txt", "cutscene-title-skip.png")) {
            $source = Join-Path $statePath $fileName
            if (Test-Path -LiteralPath $source -PathType Leaf) {
                Copy-Item -LiteralPath $source -Destination (Join-Path $runtimeArtifactPath $fileName) -Force
            }
        }
        if (Test-Path -LiteralPath $latestLogPath -PathType Leaf) {
            Copy-Item -LiteralPath $latestLogPath -Destination (Join-Path $runtimeArtifactPath "MelonLoader-Latest.log") -Force
        }

        if (-not $resultText) {
            $exitState = if ($process.HasExited) { "exited=$($process.ExitCode)" } else { "still running" }
            $logTail = if (Test-Path -LiteralPath $latestLogPath) {
                (Get-Content -LiteralPath $latestLogPath -Tail 100) -join [Environment]::NewLine
            }
            else {
                "No fresh MelonLoader log was produced."
            }
            throw "$($Context.Runtime) produced no result ($exitState).`n$logTail"
        }

        if (-not $resultText.StartsWith("PASS|", [System.StringComparison]::Ordinal)) {
            throw "$($Context.Runtime) cutscene smoke failed: $resultText"
        }

        $presentationFailures = @(
            Select-String `
                -LiteralPath $latestLogPath `
                -Pattern @(
                    "CutscenePresentationRuntime\.",
                    "S1API\.Cutscenes\.",
                    "Method unstripping failed"
                ) `
                -CaseSensitive:$false |
                Where-Object {
                    $_.Line -match "Exception|Error|failed"
                } |
                Select-Object -ExpandProperty Line -Unique
        )
        if ($presentationFailures.Count -gt 0) {
            Set-Content `
                -LiteralPath (Join-Path $runtimeArtifactPath "presentation-failures.txt") `
                -Value $presentationFailures
            throw "$($Context.Runtime) logged a cutscene presentation failure: $($presentationFailures[0])"
        }

        $screenshotPath = Join-Path $runtimeArtifactPath "cutscene-title-skip.png"
        if (-not (Test-Path -LiteralPath $screenshotPath -PathType Leaf) -or
            (Get-Item -LiteralPath $screenshotPath).Length -eq 0) {
            throw "$($Context.Runtime) did not produce a non-empty presentation screenshot."
        }

        Set-Content -LiteralPath (Join-Path $runtimeArtifactPath "summary.txt") -Value @(
            "runtime=$($Context.Runtime)",
            "gamePath=$gamePath",
            "s1apiSha256=$((Get-FileHash -LiteralPath $Context.S1ApiDll -Algorithm SHA256).Hash)",
            "smokeSha256=$((Get-FileHash -LiteralPath $Context.SmokeDll -Algorithm SHA256).Hash)",
            "result=$resultText"
        )
        Write-Host "$($Context.Runtime): $resultText"
    }
    finally {
        if ($process -and -not $process.HasExited) {
            Stop-Process -Id $process.Id -Force
            Wait-Process -Id $process.Id -Timeout 20 -ErrorAction SilentlyContinue
        }
        Start-Sleep -Seconds 2

        Restore-Environment
        if (-not $KeepInstalled) {
            $latestLogBackup = Join-Path $backupPath "Latest.log"
            if ($hadLatestLog -and (Test-Path -LiteralPath $latestLogBackup)) {
                Copy-Item -LiteralPath $latestLogBackup -Destination $latestLogPath -Force
            }
            elseif (Test-Path -LiteralPath $latestLogPath -PathType Leaf) {
                Remove-Item -LiteralPath $latestLogPath -Force
            }
        }
    }
}

$runtimes = if ($Configuration -eq "Both") { @("Mono", "Il2Cpp") } else { @($Configuration) }

try {
    foreach ($runtime in $runtimes) {
        $context = Get-RuntimeContext -Runtime $runtime
        Invoke-Build -Context $context
        Invoke-RuntimeSmoke -Context $context
    }

    Write-Host "PASS: cutscene smoke completed for $($runtimes -join ', ')."
    Write-Host "Evidence directory: $artifactRoot"
}
finally {
    if (-not $KeepInstalled) {
        Remove-TemporaryTree -Path $temporaryRoot
    }
}
