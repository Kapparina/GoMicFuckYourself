# Installer Packaging

## Installer Technology

The installer is built with `WixSharp` on top of WiX v4.

The release output now has two layers:

- an MSI containing the application payload
- a Burn bootstrapper EXE that installs the .NET Desktop Runtime prerequisite and then launches the MSI

Project:

- `GoMicFuckYourself.Installer`

NuGet package:

- `WixSharp_wix4`
- version aligned to the latest GitHub release currently in use here: `2.12.0`

WiX tool prerequisite:

- `dotnet tool install --global wix`
- `wix extension add -g WixToolset.Bal.wixext/6.0.2`
- `wix extension add -g WixToolset.Netfx.wixext/6.0.2`
- `wix extension add -g WixToolset.UI.wixext/6.0.2`
- `wix extension add -g WixToolset.Util.wixext/6.0.2`

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
- default config to `%ProgramData%\GoMicFuckYourself\agent-config.json`

It also:

- optionally launches the tray in `--first-run` mode from the installer finish page
- defers tray and agent autorun registration until first-run setup completes

## Bootstrapper Prerequisite

The bootstrapper includes a `.NET Desktop Runtime` prerequisite for `x64` machines.

Current configured prerequisite:

- runtime family: `.NET Desktop Runtime`
- architecture: `x64`
- version: `10.0.3`

The build script downloads the runtime installer from Microsoft's official distribution point before generating the bootstrapper.

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
