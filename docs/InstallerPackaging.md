# Installer Packaging

## Installer Technology

The installer is built with `WixSharp` on top of WiX v4.

Project:

- `GoMicFuckYourself.Installer`

NuGet package:

- `WixSharp_wix4`
- version aligned to the latest GitHub release currently in use here: `2.12.0`

WiX tool prerequisite:

- `dotnet tool install --global wix`

## Payload Layout

The installer project expects published binaries next to the installer output or under an explicitly supplied payload root:

```text
payload/
  Agent/
    GoMicFuckYourself.Agent.exe
    ...
  Tray/
    GoMicFuckYourself.Tray.exe
    ...
```

Override the payload location when generating the MSI:

```powershell
dotnet run --project .\GoMicFuckYourself.Installer -- --payload-root F:\artifacts\publish
```

## Installed Layout

The MSI installs:

- agent files to `%ProgramFiles%\GoMicFuckYourself\Agent\`
- tray files to `%ProgramFiles%\GoMicFuckYourself\Tray\`
- default config to `%ProgramData%\GoMicFuckYourself\service-config.json`

It also:

- creates a machine-wide agent autorun entry in `HKLM\Software\Microsoft\Windows\CurrentVersion\Run`
- creates a machine-wide tray autorun entry in `HKLM\Software\Microsoft\Windows\CurrentVersion\Run`

## Winget And Scoop

This MSI route is compatible with both `winget` and `scoop`, but the package managers need distribution metadata in addition to the MSI itself.

### Winget

You will need:

- a stable MSI URL
- SHA256 hash for the MSI
- package identifier, version, and locale metadata

Typical next step:

1. publish the MSI as a GitHub Release asset
2. generate a winget manifest with `.\scripts\Generate-PackageManifests.ps1`
3. submit it to the `microsoft/winget-pkgs` repository

### Scoop

You will need:

- a stable MSI or zip URL
- SHA256 hash
- a Scoop manifest JSON file

Typical next step:

1. publish the MSI
2. generate the Scoop manifest with `.\scripts\Generate-PackageManifests.ps1`
3. add it to your bucket repo

## Current Limitation

The MSI and distribution manifests still need final release-specific values at publish time:

- MSI product code
- final release URL
- final SHA256
- GitHub repository slug used for Scoop autoupdate
