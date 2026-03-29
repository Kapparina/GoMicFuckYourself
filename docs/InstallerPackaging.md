# Installer Packaging

## Installer Technology

The installer is built with `WixSharp` on top of WiX v4.

Project:

- `GoMicFuckYourself.Installer`

NuGet package:

- `WixSharp_wix4`
- version currently in use here: `2.12.0`

## Payload Layout

The installer expects published binaries under the payload root. The standard local build uses `artifacts\payload`:

```text
artifacts/
  payload/
    Agent/
      GoMicFuckYourself.Agent.exe
      ...
    Tray/
      GoMicFuckYourself.Tray.exe
      ...
```

You can override the payload location when generating the MSI:

```powershell
dotnet run --project .\GoMicFuckYourself.Installer -- --payload-root F:\artifacts\publish
```

## Installed Layout

The MSI installs:

- agent files to `%ProgramFiles%\GoMicFuckYourself\Agent\`
- tray files to `%ProgramFiles%\GoMicFuckYourself\Tray\`
- default config to `%ProgramData%\GoMicFuckYourself\agent-config.json`
- a Start menu shortcut for `GoMicFuckYourself`

It also:

- optionally launches the tray in `--first-run` mode from the installer finish page
- defers tray autorun registration until first-run setup completes
- removes the current user's tray autorun registry entries during uninstall
- grants standard users write access to `%ProgramData%\GoMicFuckYourself` so normal config saves do not require elevation

## Build

Install WiX once:

```powershell
dotnet tool install --global wix --version 6.0.2
wix extension add -g WixToolset.UI.wixext/6.0.2
wix extension add -g WixToolset.Util.wixext/6.0.2
```

Build the installer from the repo root:

```powershell
.\scripts\Build-Installer.ps1 -Configuration Release
```

The script:

- derives the version from the latest Git tag by default
- publishes the agent and tray payloads
- builds the MSI
- prints the MSI path and SHA256 at the end

You can override the version manually if needed:

```powershell
.\scripts\Build-Installer.ps1 -Configuration Release -Version 1.2.3
```

## Version Metadata

The installer version comes from the latest Git tag, and that same version is passed into the published tray and agent binaries.

For example, tag `v0.1.7` results in:

- MSI version `0.1.7`
- tray `FileVersion` `0.1.7.0`
- agent `FileVersion` `0.1.7.0`

Publisher and product metadata for the binaries are defined in [`Directory.Build.props`](../Directory.Build.props).
