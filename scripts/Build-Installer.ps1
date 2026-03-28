param(
    [string]$Configuration = "Release",
    [string]$Version
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$artifacts = Join-Path $root "artifacts"
$payloadRoot = Join-Path $artifacts "payload"
$agentPublish = Join-Path $payloadRoot "Agent"
$trayPublish = Join-Path $payloadRoot "Tray"
$msiOutput = Join-Path $root "GoMicFuckYourself.Installer\bin\$Configuration\net48\msi"

if (-not $Version) {
    $latestTag = ""
    try {
        $latestTag = (git -C $root describe --tags --abbrev=0 2>$null).Trim()
    }
    catch {
    }

    if ($latestTag) {
        $Version = $latestTag.TrimStart('v', 'V')
    }
    else {
        $Version = "0.1.0"
    }
}

if (-not (Get-Command wix.exe -ErrorAction SilentlyContinue)) {
    throw "wix.exe cannot be found. Install WiX with: dotnet tool install --global wix"
}

New-Item -ItemType Directory -Force -Path $agentPublish | Out-Null
New-Item -ItemType Directory -Force -Path $trayPublish | Out-Null

dotnet publish (Join-Path $root "GoMicFuckYourself.Agent\GoMicFuckYourself.Agent.csproj") `
    -c $Configuration `
    -o $agentPublish

dotnet publish (Join-Path $root "App\App.csproj") `
    -c $Configuration `
    -o $trayPublish

dotnet run --project (Join-Path $root "GoMicFuckYourself.Installer\GoMicFuckYourself.Installer.csproj") -c $Configuration -- `
    --payload-root $payloadRoot `
    --version $Version

$msi = Get-ChildItem -Path $msiOutput -Filter *.msi | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
if ($null -ne $msi) {
    $hash = (Get-FileHash -Path $msi.FullName -Algorithm SHA256).Hash
    Write-Host "Version: $Version"
    Write-Host "MSI: $($msi.FullName)"
    Write-Host "SHA256: $hash"
}
