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
- a Start menu shortcut for `GoMicFuckYourself`

It also:

- optionally launches the tray after install from the installer finish page
- seeds `%ProgramData%\GoMicFuckYourself\agent-config.json` only if it is missing or incompatible
- preserves compatible existing config during upgrades
- defers tray autorun registration until first-run setup completes
- only registers the tray app for sign-in startup; the tray app starts the agent on demand
- removes the current user's tray autorun registry entries during full uninstall
- grants standard users write access to `%ProgramData%\GoMicFuckYourself` so normal config saves do not require elevation
- removes config and installer-owned machine state on full uninstall where possible

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

- derives release versions from the current exact Git tag when building a tagged release
- derives numeric dev MSI versions for non-tag local builds
- publishes the agent and tray payloads
- builds the MSI
- prints the MSI path and SHA256 at the end

You can override the version manually if needed:

```powershell
.\scripts\Build-Installer.ps1 -Configuration Release -Version 1.2.3
```

## Version Metadata

Tagged release builds use the exact Git tag version. Non-tag builds use a numeric dev version derived from UTC time plus an informational version that includes the branch name and commit hash.

Examples:

- tag `v0.1.9` results in:
  - MSI version `0.1.9`
  - tray `FileVersion` `0.1.9.0`
  - agent `FileVersion` `0.1.9.0`
- non-tag local build results in values shaped like:
  - MSI version `<tag-major>.<month-bucket>.<minute-of-month>`
  - file version `<tag-major>.<month-bucket>.<minute-of-month>.<utc-second>`
  - informational version `<latest-tag>-dev+<branch>.<sha>`

The non-tag MSI version is intended to avoid collisions with tagged releases. It is derived at minute resolution, while the file version includes seconds for extra per-build traceability.

Publisher and product metadata for the binaries are defined in [`Directory.Build.props`](../Directory.Build.props).
