#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Runs the opt-in S1API weed-variant host, observer, late-join, and reload smoke lane.

.DESCRIPTION
    Builds S1API and the smoke probe, creates separate isolated game installs for
    each network role, and uses an explicitly supplied Goldberg/GSE Steam API plus
    LocalLobby mod. The host waits until it sees another lobby member before it
    loads the disposable save.

    The runner never downloads an emulator and never writes to the source install.
    Test installations, logs, results, and the disposable save stay under OutputRoot.
#>

[CmdletBinding()]
param(
    [ValidateSet("MonoMelon", "Il2CppMelon")]
    [string]$Runtime = "MonoMelon",

    [string]$MonoGamePath = "D:\SteamLibrary\steamapps\common\Schedule I",
    [string]$Il2CppGamePath = "D:\SteamLibrary\steamapps\common\Schedule I_public",

    [Parameter(Mandatory=$true)]
    [string]$GoldbergSteamApiPath,

    [Parameter(Mandatory=$true)]
    [string]$LocalLobbyDllPath,

    [string]$OutputRoot = "",
    [ValidateRange(30, 600)]
    [int]$TimeoutSeconds = 180,
    [switch]$KeepIsolatedInstalls,
    [switch]$KeepGameRunning
)

$ErrorActionPreference = "Stop"
$script:RunStartedAt = Get-Date
$script:Stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

function Write-Step([string]$Message) {
    Write-Host ("[{0:n1}s] {1}" -f $script:Stopwatch.Elapsed.TotalSeconds, $Message) -ForegroundColor Yellow
}

function Assert-Path([string]$Path, [string]$Description) {
    if (-not (Test-Path -LiteralPath $Path)) {
        throw "$Description not found: $Path"
    }
}

function Invoke-Checked {
    param([scriptblock]$Command, [string]$Failure)

    & $Command
    if ($LASTEXITCODE -ne 0) {
        throw $Failure
    }
}

function New-FileLinkOrCopy {
    param([string]$SourcePath, [string]$DestinationPath)

    $destinationParent = Split-Path -Parent $DestinationPath
    New-Item -ItemType Directory -Path $destinationParent -Force | Out-Null
    try {
        New-Item -ItemType HardLink -Path $DestinationPath -Target $SourcePath -Force | Out-Null
    }
    catch {
        Copy-Item -LiteralPath $SourcePath -Destination $DestinationPath -Force
    }
}

function Copy-TreeWithLinks {
    param([string]$SourcePath, [string]$DestinationPath)

    $normalizedSource = [System.IO.Path]::GetFullPath($SourcePath).TrimEnd('\', '/')
    New-Item -ItemType Directory -Path $DestinationPath -Force | Out-Null
    foreach ($directory in Get-ChildItem -LiteralPath $SourcePath -Directory -Recurse -Force) {
        $relative = $directory.FullName.Substring($normalizedSource.Length).TrimStart('\', '/')
        New-Item -ItemType Directory -Path (Join-Path $DestinationPath $relative) -Force | Out-Null
    }

    foreach ($file in Get-ChildItem -LiteralPath $SourcePath -File -Recurse -Force) {
        $relative = $file.FullName.Substring($normalizedSource.Length).TrimStart('\', '/')
        New-FileLinkOrCopy -SourcePath $file.FullName -DestinationPath (Join-Path $DestinationPath $relative)
    }
}

function New-IsolatedInstall {
    param(
        [string]$SourcePath,
        [string]$DestinationPath,
        [string]$S1ApiDll,
        [string]$SmokeDll,
        [string]$LocalLobbyDll,
        [string]$SteamApiDll
    )

    Assert-Path (Join-Path $SourcePath "Schedule I.exe") "Source Schedule I executable"
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
            New-FileLinkOrCopy -SourcePath $sourceFile -DestinationPath (Join-Path $DestinationPath $fileName)
        }
    }

    $monoRuntime = Join-Path $SourcePath "MonoBleedingEdge"
    if (Test-Path -LiteralPath $monoRuntime) {
        New-Item -ItemType Junction -Path (Join-Path $DestinationPath "MonoBleedingEdge") -Target $monoRuntime -Force | Out-Null
    }

    # MelonLoader writes logs and generated state beneath its own directory, so copy
    # it instead of hard-linking mutable files back to the source installation.
    Copy-Item -LiteralPath (Join-Path $SourcePath "MelonLoader") `
        -Destination (Join-Path $DestinationPath "MelonLoader") `
        -Recurse -Force

    $sourceData = Join-Path $SourcePath "Schedule I_Data"
    $destinationData = Join-Path $DestinationPath "Schedule I_Data"
    New-Item -ItemType Directory -Path $destinationData -Force | Out-Null
    foreach ($item in Get-ChildItem -LiteralPath $sourceData -Force) {
        $destination = Join-Path $destinationData $item.Name
        if ($item.PSIsContainer) {
            if ($item.Name -eq "Plugins") {
                Copy-TreeWithLinks -SourcePath $item.FullName -DestinationPath $destination
            }
            else {
                New-Item -ItemType Junction -Path $destination -Target $item.FullName -Force | Out-Null
            }
        }
        else {
            New-FileLinkOrCopy -SourcePath $item.FullName -DestinationPath $destination
        }
    }

    foreach ($directoryName in @("Mods", "Plugins", "UserData", "UserLibs")) {
        New-Item -ItemType Directory -Path (Join-Path $DestinationPath $directoryName) -Force | Out-Null
    }

    Copy-Item -LiteralPath $S1ApiDll -Destination (Join-Path $DestinationPath "Mods\S1API.dll") -Force
    Copy-Item -LiteralPath $SmokeDll -Destination (Join-Path $DestinationPath "Mods\S1API.WeedVariantSmoke.dll") -Force
    Copy-Item -LiteralPath $LocalLobbyDll -Destination (Join-Path $DestinationPath "Mods\LocalLobby.dll") -Force
    $destinationSteamApi = Join-Path $destinationData "Plugins\x86_64\steam_api64.dll"
    if (Test-Path -LiteralPath $destinationSteamApi) {
        Remove-Item -LiteralPath $destinationSteamApi -Force
    }
    Copy-Item -LiteralPath $SteamApiDll -Destination $destinationSteamApi -Force

    $mods = @(Get-ChildItem -LiteralPath (Join-Path $DestinationPath "Mods") -File | Select-Object -ExpandProperty Name | Sort-Object)
    $expectedMods = @("LocalLobby.dll", "S1API.dll", "S1API.WeedVariantSmoke.dll") | Sort-Object
    if (Compare-Object $mods $expectedMods) {
        throw "Isolated Mods mismatch at $DestinationPath. Actual: $($mods -join ', ')"
    }
}

function Write-GoldbergConfig {
    param([string]$GamePath, [string]$AccountName, [string]$SteamId)

    $settingsDirectory = Join-Path $GamePath "Schedule I_Data\Plugins\x86_64\steam_settings"
    New-Item -ItemType Directory -Path $settingsDirectory -Force | Out-Null
    $configPath = Join-Path $settingsDirectory "configs.user.ini"
    if (Test-Path -LiteralPath $configPath) {
        # The plugin tree is linked for speed. Unlink any source config before
        # writing role-specific state so the source installation stays untouched.
        Remove-Item -LiteralPath $configPath -Force
    }
    @"
[user::general]
account_name=$AccountName
account_steamid=$SteamId
language=english
"@ | Set-Content -LiteralPath $configPath -Encoding UTF8
}

function Wait-ForLobbyFile {
    param([string]$GamePath, [int]$Timeout)

    $lobbyFile = Join-Path $GamePath "UserData\LocalLobby\lobby.txt"
    $deadline = (Get-Date).AddSeconds($Timeout)
    while ((Get-Date) -lt $deadline) {
        if (Test-Path -LiteralPath $lobbyFile) {
            $value = (Get-Content -LiteralPath $lobbyFile -Raw).Trim()
            if ($value -match '^\d+$') {
                return $value
            }
        }

        Start-Sleep -Milliseconds 500
    }

    throw "Timed out waiting for LocalLobby host file: $lobbyFile"
}

function Write-LocalLobbyJoinFile {
    param([string]$GamePath, [string]$LobbyId)

    if ($LobbyId -notmatch '^\d+$') {
        throw "Refusing to write an invalid LocalLobby ID: $LobbyId"
    }

    $localLobbyDirectory = Join-Path $GamePath "UserData\LocalLobby"
    New-Item -ItemType Directory -Path $localLobbyDirectory -Force | Out-Null
    Set-Content -LiteralPath (Join-Path $localLobbyDirectory "lobby.txt") -Value $LobbyId -Encoding ASCII
}

function Wait-ForResult {
    param(
        [string]$Path,
        [System.Diagnostics.Process]$Process,
        [int]$Timeout,
        [string]$Role
    )

    $deadline = (Get-Date).AddSeconds($Timeout)
    while ((Get-Date) -lt $deadline) {
        if (Test-Path -LiteralPath $Path) {
            return (Get-Content -LiteralPath $Path -Raw).Trim()
        }

        if ($Process.HasExited) {
            throw "$Role exited with code $($Process.ExitCode) before writing $Path"
        }

        Start-Sleep -Milliseconds 500
    }

    throw "Timed out waiting for $Role result: $Path"
}

function Copy-RoleLogs {
    param([string]$GamePath, [string]$LogsPath, [string]$Role)

    New-Item -ItemType Directory -Path $LogsPath -Force | Out-Null
    foreach ($candidate in @(
        (Join-Path $GamePath "MelonLoader\Latest.log"),
        (Join-Path $GamePath "UserData\MelonLoader\Latest.log")
    )) {
        if (Test-Path -LiteralPath $candidate) {
            Copy-Item -LiteralPath $candidate -Destination (Join-Path $LogsPath "$Role-Latest.log") -Force
        }
    }
}

function Stop-RoleProcess([System.Diagnostics.Process]$Process) {
    if ($null -ne $Process -and -not $Process.HasExited) {
        Stop-Process -Id $Process.Id -Force -ErrorAction SilentlyContinue
        $Process.WaitForExit(5000) | Out-Null
    }
}

function Remove-TestRoot {
    param([string]$RootPath, [string]$AllowedBasePath)

    $resolvedRoot = [System.IO.Path]::GetFullPath($RootPath)
    $resolvedBase = [System.IO.Path]::GetFullPath($AllowedBasePath)
    if (-not $resolvedRoot.StartsWith($resolvedBase, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to clean test directory outside expected base: $resolvedRoot"
    }

    Get-ChildItem -LiteralPath $resolvedRoot -Recurse -Force -Attributes ReparsePoint -ErrorAction SilentlyContinue |
        Sort-Object FullName -Descending |
        ForEach-Object {
            try {
                [System.IO.Directory]::Delete($_.FullName, $false)
            }
            catch {
                Remove-Item -LiteralPath $_.FullName -Force -ErrorAction SilentlyContinue
            }
        }

    $lastError = $null
    for ($attempt = 1; $attempt -le 6; $attempt++) {
        try {
            Get-ChildItem -LiteralPath $resolvedRoot -Recurse -File -Force -ErrorAction SilentlyContinue |
                ForEach-Object { $_.Attributes = [System.IO.FileAttributes]::Normal }
            Remove-Item -LiteralPath $resolvedRoot -Recurse -Force -ErrorAction Stop
            return
        }
        catch {
            $lastError = $_
            Start-Sleep -Milliseconds 500
        }
    }

    throw "Could not clean isolated test root after retries: $resolvedRoot. $($lastError.Exception.Message)"
}

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$sourceGamePath = if ($Runtime -eq "MonoMelon") { $MonoGamePath } else { $Il2CppGamePath }
$expectedLocalLobbyName = if ($Runtime -eq "MonoMelon") { "LocalLobby-Mono.dll" } else { "LocalLobby-IL2CPP.dll" }

Assert-Path $sourceGamePath "Source game install"
Assert-Path $GoldbergSteamApiPath "Goldberg/GSE steam_api64.dll"
Assert-Path $LocalLobbyDllPath "LocalLobby mod"

if ((Split-Path -Leaf $GoldbergSteamApiPath) -ne "steam_api64.dll") {
    throw "GoldbergSteamApiPath must point to steam_api64.dll."
}
$localLobbyName = Split-Path -Leaf $LocalLobbyDllPath
if ($localLobbyName -ne $expectedLocalLobbyName -and $localLobbyName -ne "LocalLobby.dll") {
    throw "Expected $expectedLocalLobbyName or an installed LocalLobby.dll for $Runtime, got $localLobbyName."
}

$emulatorInfo = (Get-Item -LiteralPath $GoldbergSteamApiPath).VersionInfo
if ($emulatorInfo.CompanyName -match "Valve") {
    throw "GoldbergSteamApiPath identifies a Valve Steam Client API, not an emulator."
}
$emulatorHash = (Get-FileHash -LiteralPath $GoldbergSteamApiPath -Algorithm SHA256).Hash
$sourceSteamApi = Join-Path $sourceGamePath "Schedule I_Data\Plugins\x86_64\steam_api64.dll"
if (Test-Path -LiteralPath $sourceSteamApi) {
    $sourceInfo = (Get-Item -LiteralPath $sourceSteamApi).VersionInfo
    $sourceHash = (Get-FileHash -LiteralPath $sourceSteamApi -Algorithm SHA256).Hash
    if ($sourceInfo.CompanyName -match "Valve" -and $sourceHash -eq $emulatorHash) {
        throw "The supplied emulator hash matches the Valve Steam API in the source install."
    }
}

if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path ([System.IO.Path]::GetPathRoot($sourceGamePath)) "S1API.WeedVariantSmoke"
}

$sourceVolume = [System.IO.Path]::GetPathRoot([System.IO.Path]::GetFullPath($sourceGamePath))
$outputVolume = [System.IO.Path]::GetPathRoot([System.IO.Path]::GetFullPath($OutputRoot))
if (-not [string]::Equals($sourceVolume, $outputVolume, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "OutputRoot must be on the same volume as the source game so large immutable game files can be hard-linked. Source=$sourceVolume Output=$outputVolume"
}

$runId = Get-Date -Format "yyyyMMdd-HHmmss"
$runPath = Join-Path $OutputRoot "$runId-$($Runtime.ToLowerInvariant())"
$isolatedRoot = Join-Path $runPath "isolated"
$logsPath = Join-Path $runPath "logs"
$savePath = Join-Path $runPath "SaveGame_WeedVariantSmoke"
$token = [Guid]::NewGuid().ToString("N")
$processes = [System.Collections.Generic.List[System.Diagnostics.Process]]::new()

$hostPath = Join-Path $isolatedRoot "host"
$observerPath = Join-Path $isolatedRoot "observer"
$lateJoinPath = Join-Path $isolatedRoot "latejoin"
$hostResultPath = Join-Path $runPath "result-host.txt"
$observerResultPath = Join-Path $runPath "result-observer.txt"
$lateJoinResultPath = Join-Path $runPath "result-latejoin.txt"
$reloadResultPath = Join-Path $runPath "result-reload.txt"

$s1ApiDll = if ($Runtime -eq "MonoMelon") {
    Join-Path $repoRoot "S1API\bin\MonoMelon\netstandard2.1\S1API.dll"
}
else {
    Join-Path $repoRoot "S1API\bin\Il2CppMelon\net6.0\S1API.dll"
}
$smokeDll = if ($Runtime -eq "MonoMelon") {
    Join-Path $repoRoot "tests\Smoke\WeedVariantSmoke\bin\MonoMelon\netstandard2.1\S1API.WeedVariantSmoke.dll"
}
else {
    Join-Path $repoRoot "tests\Smoke\WeedVariantSmoke\bin\Il2CppMelon\net6.0\S1API.WeedVariantSmoke.dll"
}

New-Item -ItemType Directory -Path $runPath -Force | Out-Null
Write-Host "S1API weed variant smoke" -ForegroundColor Cyan
Write-Host "Runtime: $Runtime"
Write-Host "Run token: $token"
Write-Host "Output: $runPath"
Write-Host "Emulator: $($emulatorInfo.CompanyName) $($emulatorInfo.FileVersion), SHA256=$emulatorHash"

try {
    Write-Step "Build S1API and smoke probe with a matching configuration restore"
    if ($Runtime -eq "MonoMelon") {
        $managedPath = Join-Path $sourceGamePath "Schedule I_Data\Managed"
        $melonPath = Join-Path $sourceGamePath "MelonLoader\net35"
        Invoke-Checked {
            dotnet restore (Join-Path $repoRoot "S1API\S1API.csproj") `
                -p:Configuration=MonoMelon `
                -p:MonoAssembliesPath="$managedPath" `
                -p:MelonLoaderMonoAssembliesPath="$melonPath" `
                -p:AutomateLocalDeployment=false
        } "Mono S1API restore failed."
        Invoke-Checked {
            dotnet build (Join-Path $repoRoot "S1API\S1API.csproj") -c MonoMelon --no-restore `
                -p:MonoAssembliesPath="$managedPath" `
                -p:MelonLoaderMonoAssembliesPath="$melonPath" `
                -p:AutomateLocalDeployment=false
        } "Mono S1API build failed."
    }
    else {
        $managedPath = Join-Path $sourceGamePath "MelonLoader\Il2CppAssemblies"
        $melonPath = Join-Path $sourceGamePath "MelonLoader\net6"
        Invoke-Checked {
            dotnet restore (Join-Path $repoRoot "S1API\S1API.csproj") `
                -p:Configuration=Il2CppMelon `
                -p:Il2CppAssembliesPath="$managedPath" `
                -p:MelonLoaderAssembliesPath="$melonPath" `
                -p:AutomateLocalDeployment=false
        } "IL2CPP S1API restore failed."
        Invoke-Checked {
            dotnet build (Join-Path $repoRoot "S1API\S1API.csproj") -c Il2CppMelon --no-restore `
                -p:Il2CppAssembliesPath="$managedPath" `
                -p:MelonLoaderAssembliesPath="$melonPath" `
                -p:AutomateLocalDeployment=false
        } "IL2CPP S1API build failed."
    }

    Invoke-Checked {
        dotnet restore (Join-Path $repoRoot "tests\Smoke\WeedVariantSmoke\WeedVariantSmoke.csproj") `
            -p:Configuration="$Runtime" -p:GamePath="$sourceGamePath"
    } "Smoke probe restore failed."
    Invoke-Checked {
        dotnet build (Join-Path $repoRoot "tests\Smoke\WeedVariantSmoke\WeedVariantSmoke.csproj") `
            -c "$Runtime" --no-restore -p:GamePath="$sourceGamePath"
    } "Smoke probe build failed."

    Assert-Path $s1ApiDll "S1API build output"
    Assert-Path $smokeDll "Smoke probe build output"

    Write-Step "Prepare separate isolated host, observer, and late-join installs"
    foreach ($path in @($hostPath, $observerPath, $lateJoinPath)) {
        New-IsolatedInstall `
            -SourcePath $sourceGamePath `
            -DestinationPath $path `
            -S1ApiDll $s1ApiDll `
            -SmokeDll $smokeDll `
            -LocalLobbyDll $LocalLobbyDllPath `
            -SteamApiDll $GoldbergSteamApiPath
    }

    Write-GoldbergConfig -GamePath $hostPath -AccountName "S1APIWeedHost" -SteamId "76561198000000101"
    Write-GoldbergConfig -GamePath $observerPath -AccountName "S1APIWeedObserver" -SteamId "76561198000000102"
    Write-GoldbergConfig -GamePath $lateJoinPath -AccountName "S1APIWeedLateJoin" -SteamId "76561198000000103"

    Write-Step "Start host in Menu and wait for the LocalLobby host record"
    # "creator" avoids duplicating LocalLobby's --host launch-option key.
    # The probe normalizes it back to the host role and writes result-host.txt.
    $hostArguments = "--host --s1api-weed-smoke --s1api-weed-smoke-role creator --s1api-weed-smoke-token $token --s1api-weed-smoke-dir `"$runPath`" --s1api-weed-smoke-save `"$savePath`" --s1api-weed-smoke-timeout $TimeoutSeconds"
    $hostProcess = Start-Process -FilePath (Join-Path $hostPath "Schedule I.exe") -ArgumentList $hostArguments -WorkingDirectory $hostPath -WindowStyle Hidden -PassThru
    $processes.Add($hostProcess)
    $lobbyId = Wait-ForLobbyFile -GamePath $hostPath -Timeout $TimeoutSeconds
    Write-Host "LocalLobby host record: $lobbyId"

    Write-Step "Start observer before host load"
    Write-LocalLobbyJoinFile -GamePath $observerPath -LobbyId $lobbyId
    $observerArguments = "--join --s1api-weed-smoke --s1api-weed-smoke-role observer --s1api-weed-smoke-token $token --s1api-weed-smoke-dir `"$runPath`" --s1api-weed-smoke-save `"$savePath`" --s1api-weed-smoke-timeout $TimeoutSeconds --s1api-weed-smoke-exit"
    $observerProcess = Start-Process -FilePath (Join-Path $observerPath "Schedule I.exe") -ArgumentList $observerArguments -WorkingDirectory $observerPath -WindowStyle Hidden -PassThru
    $processes.Add($observerProcess)

    $hostResult = Wait-ForResult -Path $hostResultPath -Process $hostProcess -Timeout $TimeoutSeconds -Role "host"
    $observerResult = Wait-ForResult -Path $observerResultPath -Process $observerProcess -Timeout $TimeoutSeconds -Role "observer"
    if (-not $hostResult.StartsWith("PASS|") -or -not $observerResult.StartsWith("PASS|")) {
        throw "Host/observer phase failed. Host=$hostResult Observer=$observerResult"
    }
    Copy-RoleLogs -GamePath $observerPath -LogsPath $logsPath -Role "observer"
    Stop-RoleProcess $observerProcess

    Write-Step "Start a fresh late-joining client after native creation and save"
    Write-LocalLobbyJoinFile -GamePath $lateJoinPath -LobbyId $lobbyId
    $lateJoinArguments = "--join --s1api-weed-smoke --s1api-weed-smoke-role latejoin --s1api-weed-smoke-token $token --s1api-weed-smoke-dir `"$runPath`" --s1api-weed-smoke-save `"$savePath`" --s1api-weed-smoke-timeout $TimeoutSeconds --s1api-weed-smoke-exit"
    $lateJoinProcess = Start-Process -FilePath (Join-Path $lateJoinPath "Schedule I.exe") -ArgumentList $lateJoinArguments -WorkingDirectory $lateJoinPath -WindowStyle Hidden -PassThru
    $processes.Add($lateJoinProcess)
    $lateJoinResult = Wait-ForResult -Path $lateJoinResultPath -Process $lateJoinProcess -Timeout $TimeoutSeconds -Role "latejoin"
    if (-not $lateJoinResult.StartsWith("PASS|")) {
        throw "Late-join phase failed: $lateJoinResult"
    }
    Copy-RoleLogs -GamePath $lateJoinPath -LogsPath $logsPath -Role "latejoin"
    Stop-RoleProcess $lateJoinProcess

    Write-Step "Stop host and reload its disposable save without calling Build"
    Copy-RoleLogs -GamePath $hostPath -LogsPath $logsPath -Role "host"
    Stop-RoleProcess $hostProcess
    $reloadArguments = "--s1api-weed-smoke --s1api-weed-smoke-role reload --s1api-weed-smoke-token $token --s1api-weed-smoke-dir `"$runPath`" --s1api-weed-smoke-save `"$savePath`" --s1api-weed-smoke-timeout $TimeoutSeconds --s1api-weed-smoke-exit"
    $reloadProcess = Start-Process -FilePath (Join-Path $hostPath "Schedule I.exe") -ArgumentList $reloadArguments -WorkingDirectory $hostPath -WindowStyle Hidden -PassThru
    $processes.Add($reloadProcess)
    $reloadResult = Wait-ForResult -Path $reloadResultPath -Process $reloadProcess -Timeout $TimeoutSeconds -Role "reload"
    if (-not $reloadResult.StartsWith("PASS|")) {
        throw "Reload phase failed: $reloadResult"
    }
    Copy-RoleLogs -GamePath $hostPath -LogsPath $logsPath -Role "reload"

    Write-Host "Host: $hostResult" -ForegroundColor Green
    Write-Host "Observer: $observerResult" -ForegroundColor Green
    Write-Host "Late join: $lateJoinResult" -ForegroundColor Green
    Write-Host "Reload: $reloadResult" -ForegroundColor Green
    Write-Host "S1API weed variant smoke passed. Evidence: $runPath" -ForegroundColor Green
}
finally {
    if (-not $KeepGameRunning) {
        foreach ($process in $processes) {
            Stop-RoleProcess $process
        }
    }

    if (-not $KeepIsolatedInstalls -and (Test-Path -LiteralPath $isolatedRoot)) {
        try {
            Remove-TestRoot -RootPath $isolatedRoot -AllowedBasePath $runPath
        }
        catch {
            Write-Warning $_.Exception.Message
        }
    }
}
