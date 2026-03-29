param(
    [string]$Configuration = "Release",
    [string]$Version
)

$ErrorActionPreference = "Stop"

$root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$artifacts = Join-Path $root "artifacts"
$payloadRoot = Join-Path $artifacts "payload"
$agentPublish = Join-Path $payloadRoot "Agent"
$trayPublish = Join-Path $payloadRoot "Tray"
$msiOutput = Join-Path $root "GoMicFuckYourself.Installer\bin\$Configuration\net48\msi"
$agentProject = Join-Path $root "GoMicFuckYourself.Agent\GoMicFuckYourself.Agent.csproj"
$trayProject = Join-Path $root "GoMicFuckYourself.Tray\GoMicFuckYourself.Tray.csproj"
$installerProject = Join-Path $root "GoMicFuckYourself.Installer\GoMicFuckYourself.Installer.csproj"

if (-not $Version)
{
    $latestTag = ""
    try
    {
        $latestTag = (git -C $root describe --tags --abbrev=0 2> $null).Trim()
    }
    catch
    {
    }

    if ($latestTag)
    {
        $Version = $latestTag.TrimStart('v', 'V')
    }
    else
    {
        $Version = "dev"
    }
}

if (-not (Get-Command wix.exe -ErrorAction SilentlyContinue))
{
    throw "wix.exe cannot be found. Install WiX with: dotnet tool install --global wix"
}

foreach ($requiredPath in @($agentProject, $trayProject, $installerProject))
{
    if (-not (Test-Path -LiteralPath $requiredPath))
    {
        throw "Required project file was not found: $requiredPath"
    }
}

New-Item -ItemType Directory -Force -Path $agentPublish | Out-Null
New-Item -ItemType Directory -Force -Path $trayPublish | Out-Null

Push-Location $root
try
{
    & dotnet publish $agentProject `
    -c $Configuration `
    -o $agentPublish

    & dotnet publish $trayProject `
    -c $Configuration `
    -o $trayPublish

    & dotnet run --project $installerProject -c $Configuration -- `
    --payload-root $payloadRoot `
    --version $Version
}
finally
{
    Pop-Location
}

$msi = Get-ChildItem -Path $msiOutput -Filter *.msi | Sort-Object LastWriteTimeUtc | Select-Object -Last 1
if ($null -ne $msi)
{
    $hash = (Get-FileHash -Path $msi.FullName -Algorithm SHA256).Hash
    Write-Host "Version: $Version"
    Write-Host "MSI: $( $msi.FullName )"
    Write-Host "SHA256: $hash"
}
