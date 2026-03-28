param(
    [string]$Configuration = "Release",
    [string]$Version,
    [string]$DotNetDesktopRuntimeVersion = "10.0.3",
    [string]$DotNetDesktopRuntimeUrl
)

$ErrorActionPreference = "Stop"

$root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$artifacts = Join-Path $root "artifacts"
$payloadRoot = Join-Path $artifacts "payload"
$prerequisiteRoot = Join-Path $artifacts "prerequisites"
$agentPublish = Join-Path $payloadRoot "Agent"
$trayPublish = Join-Path $payloadRoot "Tray"
$msiOutput = Join-Path $root "GoMicFuckYourself.Installer\bin\$Configuration\net48\msi"
$agentProject = Join-Path $root "GoMicFuckYourself.Agent\GoMicFuckYourself.Agent.csproj"
$trayProject = Join-Path $root "App\App.csproj"
$installerProject = Join-Path $root "GoMicFuckYourself.Installer\GoMicFuckYourself.Installer.csproj"

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

if (-not $DotNetDesktopRuntimeUrl) {
    $DotNetDesktopRuntimeUrl = "https://builds.dotnet.microsoft.com/dotnet/WindowsDesktop/$DotNetDesktopRuntimeVersion/windowsdesktop-runtime-$DotNetDesktopRuntimeVersion-win-x64.exe"
}

if (-not (Get-Command wix.exe -ErrorAction SilentlyContinue)) {
    throw "wix.exe cannot be found. Install WiX with: dotnet tool install --global wix"
}

foreach ($requiredPath in @($agentProject, $trayProject, $installerProject)) {
    if (-not (Test-Path -LiteralPath $requiredPath)) {
        throw "Required project file was not found: $requiredPath"
    }
}

New-Item -ItemType Directory -Force -Path $agentPublish | Out-Null
New-Item -ItemType Directory -Force -Path $trayPublish | Out-Null
New-Item -ItemType Directory -Force -Path $prerequisiteRoot | Out-Null

$dotNetDesktopRuntimeInstaller = Join-Path $prerequisiteRoot "windowsdesktop-runtime-$DotNetDesktopRuntimeVersion-win-x64.exe"

if (-not (Test-Path -LiteralPath $dotNetDesktopRuntimeInstaller)) {
    Write-Host "Downloading .NET Desktop Runtime $DotNetDesktopRuntimeVersion..."
    Invoke-WebRequest -Uri $DotNetDesktopRuntimeUrl -OutFile $dotNetDesktopRuntimeInstaller
}

Push-Location $root
try {
    & dotnet publish $agentProject `
    -c $Configuration `
    -o $agentPublish

    & dotnet publish $trayProject `
    -c $Configuration `
    -o $trayPublish

    & dotnet run --project $installerProject -c $Configuration -- `
    --payload-root $payloadRoot `
    --dotnet-runtime-installer $dotNetDesktopRuntimeInstaller `
    --version $Version
}
finally {
    Pop-Location
}

$msi = Get-ChildItem -Path $msiOutput -Filter *.msi | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
$bootstrapper = Get-ChildItem -Path $msiOutput -Filter *-bootstrapper.exe | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
if ($null -ne $msi) {
    $hash = (Get-FileHash -Path $msi.FullName -Algorithm SHA256).Hash
    Write-Host "Version: $Version"
    Write-Host "MSI: $($msi.FullName)"
    Write-Host "SHA256: $hash"
}

if ($null -ne $bootstrapper) {
    $bootstrapperHash = (Get-FileHash -Path $bootstrapper.FullName -Algorithm SHA256).Hash
    Write-Host "Bootstrapper: $($bootstrapper.FullName)"
    Write-Host "Bootstrapper SHA256: $bootstrapperHash"
}
