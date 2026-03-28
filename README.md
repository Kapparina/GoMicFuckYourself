# GoMicFuckYourself

[![Release Installer](https://github.com/Kapparina/GoMicFuckYourself/actions/workflows/release.yml/badge.svg)](https://github.com/Kapparina/GoMicFuckYourself/actions/workflows/release.yml)

Windows utility that force-locks a chosen microphone as the default input device, default communications device, and target input volume.

If Windows or another app changes the selected microphone or its level, the enforcement agent sets it back.

## Architecture

This repo is split into four projects:

- `GoMicFuckYourself.Contracts`
  Shared DTOs and named-pipe contracts used by the agent and tray app.
- `GoMicFuckYourself.Agent`
  Per-user background process that owns all audio enforcement logic.
- `App`
  WinForms tray app that builds as `GoMicFuckYourself.Tray`.
- `GoMicFuckYourself.Installer`
  WixSharp MSI installer project.

The agent is intentionally a user-session process, not a Windows service. Windows Core Audio APIs do not behave reliably from Session 0, so microphone control lives in the logged-in user's session.

## Features

- Enforce one selected microphone as:
  - default input device
  - default communications device
  - fixed volume level
- Detect and revert changes caused by Windows or other apps
- Persist configuration under `%ProgramData%\GoMicFuckYourself\agent-config.json`
- Run a tray UI for setup, status, and control
- Start the agent automatically for the current user after setup completes
- Install through an MSI built with WixSharp

## Runtime Model

- The tray app never touches Windows audio APIs directly.
- The agent is the source of truth for microphone policy.
- Tray and agent communicate over named pipes with JSON messages.
- The installer can launch the tray after install for first-run setup.
- First-run setup enables autorun only after configuration is saved.

## Repository Layout

```text
.
|-- App/
|-- GoMicFuckYourself.Agent/
|-- GoMicFuckYourself.Contracts/
|-- GoMicFuckYourself.Installer/
|-- docs/
|-- scripts/
`-- .github/workflows/
```

## Development Requirements

- Windows
- .NET 10 SDK
- WiX CLI 6.0.2
- WiX extensions:
  - `WixToolset.UI.wixext/6.0.2`
  - `WixToolset.Util.wixext/6.0.2`

Install WiX locally:

```powershell
dotnet tool install --global wix --version 6.0.2
wix extension add -g WixToolset.UI.wixext/6.0.2
wix extension add -g WixToolset.Util.wixext/6.0.2
```

### Build

Build the solution:

```powershell
dotnet build .\GoMicFuckYourself.sln
```

Build the MSI installer:

```powershell
.\scripts\Build-Installer.ps1 -Configuration Release
```

The installer build script:

- publishes the agent into `artifacts\payload\Agent`
- publishes the tray into `artifacts\payload\Tray`
- derives the installer version from the latest Git tag by default
- builds the MSI with the WixSharp installer project
- prints the MSI path and SHA256 hash

If no Git tag exists, the script falls back to version `0.1.0`.

You can also override the version manually:

```powershell
.\scripts\Build-Installer.ps1 -Configuration Release -Version 1.2.3
```

### Install And Test

1. Build the MSI.
2. Run the generated installer as administrator.
3. On the finish page, optionally launch the tray app for first-run setup.
4. Pick an enabled microphone and target volume, then save.
5. Confirm the agent holds that microphone as the default input and communications device.

Installed layout:

- `%ProgramFiles%\GoMicFuckYourself\Agent\`
- `%ProgramFiles%\GoMicFuckYourself\Tray\`
- `%ProgramData%\GoMicFuckYourself\agent-config.json`

Start Menu:

- the installer adds a Start Menu shortcut for `GoMicFuckYourself`

Runtime notes:

- launching the tray normally will connect to a running agent or start it if needed
- exiting the tray from its tray-menu `Exit` path also shuts down the agent
- the tray only lists enabled microphones

## First-Run Behavior

When setup is started in first-run mode, the tray app will:

- Kill other running tray instances
- Kill running agent instances
- Start a fresh agent for setup
- Save configuration through the pipe
- Restart the agent so the new configuration is live
- Enable current-user autorun after setup completes

## Release Workflow

GitHub Actions builds and publishes the MSI on version tags matching `v*`.

Example:

```powershell
git tag v0.1.0
git push origin v0.1.0
```

The release workflow:

- builds the MSI on GitHub Actions
- uploads it as a workflow artifact
- creates a GitHub Release
- attaches the MSI to the release
- uses GitHub-generated release notes

See:

- [docs/ReleaseAutomation.md](docs/ReleaseAutomation.md)

## Additional Docs

- [docs/InstallerBootstrapContract.md](docs/InstallerBootstrapContract.md)
- [docs/InstallerPackaging.md](docs/InstallerPackaging.md)
- [docs/ReleaseChecklist.md](docs/ReleaseChecklist.md)
- [docs/ReleaseNotesTemplate.md](docs/ReleaseNotesTemplate.md)

## License

This project is licensed under the terms in [./LICENSE](LICENSE).
