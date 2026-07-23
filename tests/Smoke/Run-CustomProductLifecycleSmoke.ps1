#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Runs the opt-in S1API custom-product repeat-load smoke.

.DESCRIPTION
    Builds S1API and the smoke probe, creates one isolated linked game install,
    launches a disposable save, and waits for three completed Main loads. The
    source game install and user saves are never modified.
#>

[CmdletBinding()]
param(
    [ValidateSet("MonoMelon", "Il2CppMelon")]
    [string]$Runtime = "MonoMelon",

    [string]$MonoGamePath = "D:\SteamLibrary\steamapps\common\Schedule I_alternate",
    [string]$Il2CppGamePath = "D:\SteamLibrary\steamapps\common\Schedule I_public",
    [string]$OutputRoot = "",

    [ValidateRange(45, 600)]
    [int]$TimeoutSeconds = 210,

    [switch]$KeepIsolatedInstall,
    [switch]$KeepGameRunning
)

$ErrorActionPreference = "Stop"
$script:Stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
$script:RunRoot = ""

function Write-Step([string]$Message) {
    Write-Host ("[{0:n1}s] {1}" -f $script:Stopwatch.Elapsed.TotalSeconds, $Message) `
        -ForegroundColor Yellow
}

function Assert-Path([string]$Path, [string]$Description) {
    if (-not (Test-Path -LiteralPath $Path)) {
        throw "$Description not found: $Path"
    }
}

function Invoke-Checked {
    param(
        [scriptblock]$Command,
        [string]$Failure
    )

    & $Command
    if ($LASTEXITCODE -ne 0) {
        throw $Failure
    }
}

function New-FileLinkOrCopy {
    param(
        [string]$SourcePath,
        [string]$DestinationPath
    )

    $destinationParent = Split-Path -Parent $DestinationPath
    New-Item -ItemType Directory -Path $destinationParent -Force | Out-Null
    try {
        New-Item -ItemType HardLink -Path $DestinationPath -Target $SourcePath -Force |
            Out-Null
    }
    catch {
        Copy-Item -LiteralPath $SourcePath -Destination $DestinationPath -Force
    }
}

function Copy-TreeWithLinks {
    param(
        [string]$SourcePath,
        [string]$DestinationPath
    )

    $normalizedSource = [System.IO.Path]::GetFullPath($SourcePath).TrimEnd('\', '/')
    New-Item -ItemType Directory -Path $DestinationPath -Force | Out-Null

    foreach ($directory in Get-ChildItem -LiteralPath $SourcePath -Directory -Recurse -Force) {
        $relative = $directory.FullName.Substring($normalizedSource.Length).TrimStart('\', '/')
        New-Item -ItemType Directory -Path (Join-Path $DestinationPath $relative) -Force |
            Out-Null
    }

    foreach ($file in Get-ChildItem -LiteralPath $SourcePath -File -Recurse -Force) {
        $relative = $file.FullName.Substring($normalizedSource.Length).TrimStart('\', '/')
        New-FileLinkOrCopy `
            -SourcePath $file.FullName `
            -DestinationPath (Join-Path $DestinationPath $relative)
    }
}

function New-IsolatedInstall {
    param(
        [string]$SourcePath,
        [string]$DestinationPath,
        [string]$S1ApiDll,
        [string]$SmokeDll
    )

    Assert-Path (Join-Path $SourcePath "Schedule I.exe") "Schedule I executable"
    Assert-Path (Join-Path $SourcePath "MelonLoader") "MelonLoader directory"
    Assert-Path (Join-Path $SourcePath "Schedule I_Data") "Schedule I data directory"

    New-Item -ItemType Directory -Path $DestinationPath -Force | Out-Null
    foreach ($fileName in @(
        "Schedule I.exe",
        "UnityCrashHandler64.exe",
        "UnityPlayer.dll",
        "steam_appid.txt",
        "version.dll",
        "GameAssembly.dll",
        "baselib.dll"
    )) {
        $sourceFile = Join-Path $SourcePath $fileName
        if (Test-Path -LiteralPath $sourceFile) {
            New-FileLinkOrCopy `
                -SourcePath $sourceFile `
                -DestinationPath (Join-Path $DestinationPath $fileName)
        }
    }

    $monoRuntime = Join-Path $SourcePath "MonoBleedingEdge"
    if (Test-Path -LiteralPath $monoRuntime) {
        New-Item `
            -ItemType Junction `
            -Path (Join-Path $DestinationPath "MonoBleedingEdge") `
            -Target $monoRuntime `
            -Force |
            Out-Null
    }

    Copy-Item `
        -LiteralPath (Join-Path $SourcePath "MelonLoader") `
        -Destination (Join-Path $DestinationPath "MelonLoader") `
        -Recurse `
        -Force

    $sourceData = Join-Path $SourcePath "Schedule I_Data"
    $destinationData = Join-Path $DestinationPath "Schedule I_Data"
    New-Item -ItemType Directory -Path $destinationData -Force | Out-Null

    foreach ($item in Get-ChildItem -LiteralPath $sourceData -Force) {
        $destination = Join-Path $destinationData $item.Name
        if ($item.PSIsContainer) {
            if ($item.Name -eq "Plugins") {
                Copy-TreeWithLinks `
                    -SourcePath $item.FullName `
                    -DestinationPath $destination
            }
            else {
                New-Item `
                    -ItemType Junction `
                    -Path $destination `
                    -Target $item.FullName `
                    -Force |
                    Out-Null
            }
        }
        else {
            New-FileLinkOrCopy `
                -SourcePath $item.FullName `
                -DestinationPath $destination
        }
    }

    foreach ($directoryName in @("Mods", "Plugins", "UserData", "UserLibs")) {
        New-Item `
            -ItemType Directory `
            -Path (Join-Path $DestinationPath $directoryName) `
            -Force |
            Out-Null
    }

    Copy-Item `
        -LiteralPath $S1ApiDll `
        -Destination (Join-Path $DestinationPath "Mods\S1API.dll") `
        -Force
    Copy-Item `
        -LiteralPath $SmokeDll `
        -Destination (Join-Path $DestinationPath "Mods\S1API.CustomProductLifecycleSmoke.dll") `
        -Force

    $actualMods = @(
        Get-ChildItem -LiteralPath (Join-Path $DestinationPath "Mods") -File |
            Select-Object -ExpandProperty Name |
            Sort-Object
    )
    $expectedMods = @(
        "S1API.CustomProductLifecycleSmoke.dll",
        "S1API.dll"
    ) | Sort-Object

    if (Compare-Object $actualMods $expectedMods) {
        throw "Isolated Mods mismatch. Actual: $($actualMods -join ', ')"
    }
}

function Wait-ForResult {
    param(
        [string]$Path,
        [System.Diagnostics.Process]$Process,
        [int]$Timeout
    )

    $deadline = (Get-Date).AddSeconds($Timeout)
    while ((Get-Date) -lt $deadline) {
        if (Test-Path -LiteralPath $Path) {
            return (Get-Content -LiteralPath $Path -Raw).Trim()
        }

        if ($Process.HasExited) {
            throw "Game exited with code $($Process.ExitCode) before writing $Path"
        }

        Start-Sleep -Milliseconds 500
    }

    throw "Timed out waiting for lifecycle smoke result: $Path"
}

function Copy-SmokeLogs {
    param(
        [string]$GamePath,
        [string]$OutputPath
    )

    foreach ($candidate in @(
        (Join-Path $GamePath "MelonLoader\Latest.log"),
        (Join-Path $GamePath "UserData\MelonLoader\Latest.log")
    )) {
        if (Test-Path -LiteralPath $candidate) {
            Copy-Item `
                -LiteralPath $candidate `
                -Destination (Join-Path $OutputPath "MelonLoader-Latest.log") `
                -Force
        }
    }
}

function Remove-CheckedRunChild([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) {
        return
    }

    $resolvedRunRoot = [System.IO.Path]::GetFullPath($script:RunRoot).TrimEnd('\', '/')
    $resolvedTarget = [System.IO.Path]::GetFullPath($Path).TrimEnd('\', '/')
    $requiredPrefix = $resolvedRunRoot + [System.IO.Path]::DirectorySeparatorChar
    if (-not $resolvedTarget.StartsWith(
            $requiredPrefix,
            [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to remove path outside run root: $resolvedTarget"
    }

    Remove-Item -LiteralPath $resolvedTarget -Recurse -Force
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$gamePath = if ($Runtime -eq "MonoMelon") {
    [System.IO.Path]::GetFullPath($MonoGamePath)
}
else {
    [System.IO.Path]::GetFullPath($Il2CppGamePath)
}

if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $runName = "{0}-{1}-{2}" -f (
        Get-Date -Format "yyyyMMdd-HHmmss"
    ), $Runtime, ([Guid]::NewGuid().ToString("N").Substring(0, 8))
    $OutputRoot = Join-Path (
        [System.IO.Path]::GetTempPath()
    ) (Join-Path "S1API.CustomProductLifecycleSmoke" $runName)
}

$script:RunRoot = [System.IO.Path]::GetFullPath($OutputRoot)
$installPath = Join-Path $script:RunRoot "install"
$evidencePath = Join-Path $script:RunRoot "evidence"
$savePath = Join-Path $script:RunRoot "save"
$resultPath = Join-Path $evidencePath "result.txt"
$playerLogPath = Join-Path $evidencePath "Player.log"
$token = [Guid]::NewGuid().ToString("N")

$existingGameProcesses = @(
    Get-Process -Name "Schedule I" -ErrorAction SilentlyContinue
)
if ($existingGameProcesses.Count -gt 0) {
    throw "Schedule I is already running. Close it before starting the isolated smoke."
}

New-Item -ItemType Directory -Path $evidencePath -Force | Out-Null
Write-Step "Restoring and building $Runtime"
Push-Location $repoRoot
try {
    Invoke-Checked `
        -Command { dotnet restore S1API.sln "-p:Configuration=$Runtime" } `
        -Failure "$Runtime restore failed."
    Invoke-Checked `
        -Command {
            dotnet build S1API.sln `
                -c $Runtime `
                --no-restore `
                -p:AutomateLocalDeployment=false
        } `
        -Failure "$Runtime solution build failed."
    Invoke-Checked `
        -Command {
            dotnet build `
                tests\Smoke\CustomProductLifecycleSmoke\CustomProductLifecycleSmoke.csproj `
                -c $Runtime `
                "-p:GamePath=$gamePath"
        } `
        -Failure "$Runtime lifecycle smoke build failed."
}
finally {
    Pop-Location
}

$s1ApiDll = if ($Runtime -eq "MonoMelon") {
    Join-Path $repoRoot "S1API\bin\MonoMelon\netstandard2.1\S1API.dll"
}
else {
    Join-Path $repoRoot "S1API\bin\Il2CppMelon\net6.0\S1API.dll"
}
$smokeDll = if ($Runtime -eq "MonoMelon") {
    Join-Path $repoRoot (
        "tests\Smoke\CustomProductLifecycleSmoke\bin\MonoMelon\" +
        "netstandard2.1\S1API.CustomProductLifecycleSmoke.dll"
    )
}
else {
    Join-Path $repoRoot (
        "tests\Smoke\CustomProductLifecycleSmoke\bin\Il2CppMelon\" +
        "net6.0\S1API.CustomProductLifecycleSmoke.dll"
    )
}

Assert-Path $s1ApiDll "S1API build output"
Assert-Path $smokeDll "Lifecycle smoke build output"

Write-Step "Creating isolated $Runtime install"
New-IsolatedInstall `
    -SourcePath $gamePath `
    -DestinationPath $installPath `
    -S1ApiDll $s1ApiDll `
    -SmokeDll $smokeDll

$gameProcess = $null
try {
    Write-Step "Launching lifecycle smoke"
    $gameProcess = Start-Process `
        -FilePath (Join-Path $installPath "Schedule I.exe") `
        -WorkingDirectory $installPath `
        -ArgumentList @(
            "-screen-fullscreen", "0",
            "-screen-width", "1280",
            "-screen-height", "720",
            "-logFile", "`"$playerLogPath`"",
            "--s1api-product-lifecycle-smoke",
            "--s1api-product-lifecycle-token", $token,
            "--s1api-product-lifecycle-dir", "`"$evidencePath`"",
            "--s1api-product-lifecycle-save", "`"$savePath`"",
            "--s1api-product-lifecycle-timeout", $TimeoutSeconds
        ) `
        -WindowStyle Hidden `
        -PassThru

    $result = Wait-ForResult `
        -Path $resultPath `
        -Process $gameProcess `
        -Timeout ($TimeoutSeconds + 30)
    Copy-SmokeLogs -GamePath $installPath -OutputPath $evidencePath

    if (-not $result.StartsWith("PASS|", [System.StringComparison]::Ordinal)) {
        throw "Lifecycle smoke failed: $result"
    }

    Write-Step "Lifecycle smoke passed"
    Write-Host $result -ForegroundColor Green
    Write-Host "Evidence: $evidencePath"
}
finally {
    if ($null -ne $gameProcess -and -not $gameProcess.HasExited) {
        if (-not $KeepGameRunning) {
            Stop-Process -Id $gameProcess.Id -Force -ErrorAction SilentlyContinue
            $gameProcess.WaitForExit(5000) | Out-Null
        }
        else {
            $KeepIsolatedInstall = $true
        }
    }

    Copy-SmokeLogs -GamePath $installPath -OutputPath $evidencePath
    if (-not $KeepIsolatedInstall) {
        Remove-CheckedRunChild $installPath
    }
}
