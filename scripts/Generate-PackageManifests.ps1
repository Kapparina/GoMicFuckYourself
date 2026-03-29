param(
    [Parameter(Mandatory = $true)]
    [string]$Version,

    [Parameter(Mandatory = $true)]
    [string]$InstallerUrl,

    [Parameter(Mandatory = $true)]
    [string]$InstallerSha256,

    [Parameter(Mandatory = $true)]
    [string]$ProductCode,

    [string]$PackageIdentifier = "GoMicFuckYourself.GoMicFuckYourself",

    [string]$Publisher = "GoMicFuckYourself",

    [string]$PublisherUrl = "https://github.com/oleg-shilo/wixsharp",

    [string]$PackageName = "GoMicFuckYourself",

    [string]$ShortDescription = "Enforces a selected microphone as the default capture and communications device.",

    [string]$Licence = "Proprietary",

    [string]$LicenceUrl = "",

    [string]$Homepage = "",

    [string]$RepositorySlug = "",

    [string]$ManifestRoot = ""
)

$ErrorActionPreference = "Stop"

if ( [string]::IsNullOrWhiteSpace($ManifestRoot))
{
    $ManifestRoot = Join-Path (Split-Path -Parent $PSScriptRoot) "distribution"
}

$wingetRoot = Join-Path $ManifestRoot "winget\$PackageIdentifier\$Version"
$scoopRoot = Join-Path $ManifestRoot "scoop"

New-Item -ItemType Directory -Force -Path $wingetRoot | Out-Null
New-Item -ItemType Directory -Force -Path $scoopRoot | Out-Null

$wingetVersion = @"
# yaml-language-server: `$schema=https://aka.ms/winget-manifest.version.1.10.0.schema.json

PackageIdentifier: $PackageIdentifier
PackageVersion: $Version
DefaultLocale: en-US
ManifestType: version
ManifestVersion: 1.10.0
"@

$licenceUrlLine = if ( [string]::IsNullOrWhiteSpace($LicenceUrl))
{
    ""
}
else
{
    "LicenceUrl: $LicenceUrl`r`n"
}
$homepageLine = if ( [string]::IsNullOrWhiteSpace($Homepage))
{
    ""
}
else
{
    "PackageUrl: $Homepage`r`n"
}

$wingetLocale = @"
# yaml-language-server: `$schema=https://aka.ms/winget-manifest.defaultLocale.1.10.0.schema.json

PackageIdentifier: $PackageIdentifier
PackageVersion: $Version
PackageLocale: en-US
Publisher: $Publisher
PublisherUrl: $PublisherUrl
PackageName: $PackageName
Licence: $Licence
$licenceUrlLine$homepageLine
ShortDescription: $ShortDescription
ManifestType: defaultLocale
ManifestVersion: 1.10.0
"@

$wingetInstaller = @"
# yaml-language-server: `$schema=https://aka.ms/winget-manifest.installer.1.10.0.schema.json

PackageIdentifier: $PackageIdentifier
PackageVersion: $Version
InstallerType: wix
Scope: machine
UpgradeBehavior: install
InstallModes:
- silent
- silentWithProgress
Commands:
- GoMicFuckYourself.Tray
Installers:
- Architecture: x64
  InstallerUrl: $InstallerUrl
  InstallerSha256: $InstallerSha256
  ProductCode: $ProductCode
ManifestType: installer
ManifestVersion: 1.10.0
"@

$resolvedHomepage = if ( [string]::IsNullOrWhiteSpace($Homepage))
{
    $PublisherUrl
}
else
{
    $Homepage
}

$scoopManifest = @"
{
  "version": "$Version",
  "description": "$ShortDescription",
  "homepage": "$resolvedHomepage",
  "licence": "$Licence",
  "architecture": {
    "64bit": {
      "url": "$InstallerUrl",
      "hash": "$InstallerSha256"
    }
  },
  "installer": {
    "script": [
      "Start-Process msiexec.exe -Wait -ArgumentList @('/i', `"$`dir\\$fname`"", '/qn', '/norestart')"
    ]
  },
  "uninstaller": {
    "script": [
      "Start-Process msiexec.exe -Wait -ArgumentList @('/x', '$ProductCode', '/qn', '/norestart')"
    ]
  },
  "checkver": {
    "github": "$RepositorySlug"
  },
  "autoupdate": {
    "architecture": {
      "64bit": {
        "url": "https://github.com/$RepositorySlug/releases/download/v$version/GoMicFuckYourself-$version.msi"
      }
    }
  }
}
"@

Set-Content -Path (Join-Path $wingetRoot "$PackageIdentifier.yaml") -Value $wingetVersion -Encoding UTF8
Set-Content -Path (Join-Path $wingetRoot "$PackageIdentifier.locale.en-US.yaml") -Value $wingetLocale -Encoding UTF8
Set-Content -Path (Join-Path $wingetRoot "$PackageIdentifier.installer.yaml") -Value $wingetInstaller -Encoding UTF8
Set-Content -Path (Join-Path $scoopRoot "gomicfuckyourself.json") -Value $scoopManifest -Encoding UTF8

Write-Host "Generated winget manifests in $wingetRoot"
Write-Host "Generated scoop manifest in $scoopRoot"
if ( [string]::IsNullOrWhiteSpace($RepositorySlug))
{
    throw "RepositorySlug is required. Example: owner/repo"
}
