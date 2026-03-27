# Installer Bootstrap Contract

## Scope

This document defines the installation and first-run contract for:

- `GoMicFuckYourself.Agent`
- `GoMicFuckYourself.Tray`
- the future installer/bootstrapper

The goal is to make initial setup deterministic and keep runtime behavior simple.

## Install Targets

### Agent

- Installed as a regular executable under `%ProgramFiles%`
- Started at user logon via `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` after first-run setup completes
- Runs in the interactive user session
- Owns all Core Audio and enforcement logic

Rationale:

- Core Audio enforcement must run in the interactive user session
- the agent can access endpoint notifications and volume control reliably
- the tray app remains UI-only and talks to the agent over named pipes

### Tray

- Installed separately from the agent binaries
- Started at user logon via `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` after first-run setup completes
- May be launched manually with `GoMicFuckYourself.Tray.exe --first-run` during setup and testing

Rationale:

- the tray app is per-user UI
- the agent is per-user enforcement
- machine-wide autorun keeps both pieces available after install

## Filesystem Layout

### Program Files

Agent binaries:

- `%ProgramFiles%\GoMicFuckYourself\Agent\`

Tray binaries:

- `%ProgramFiles%\GoMicFuckYourself\Tray\`

### ProgramData

Shared machine config root:

- `%ProgramData%\GoMicFuckYourself\`

Agent config file:

- `%ProgramData%\GoMicFuckYourself\agent-config.json`

Optional logs directory:

- `%ProgramData%\GoMicFuckYourself\Logs\`

Installer requirements:

- create `%ProgramData%\GoMicFuckYourself\`
- create `agent-config.json` if it does not exist
- preserve existing config on upgrade

## Config Schema

The installer and agent must both treat this file as the canonical initial config.

```json
{
  "selectedCaptureDeviceId": null,
  "targetVolumePercent": 100,
  "enforcementEnabled": true
}
```

Rules:

- `selectedCaptureDeviceId`
  - `null` means no device has been chosen yet
  - once set, it must be a Core Audio endpoint ID
- `targetVolumePercent`
  - valid range is `0` to `100`
  - installer default is `100`
- `enforcementEnabled`
  - installer default is `true`
  - if `false`, the agent stays alive but does not force changes

Runtime rules:

- the agent must tolerate a missing file by treating it as default config
- the agent must tolerate invalid JSON by falling back to default config and logging an error
- the installer must not overwrite a non-empty existing config during upgrade

## Named Pipe Contract

Pipe name:

- `GoMicFuckYourself.Agent`

Transport:

- `NamedPipeServerStream`
- UTF-8 JSON request/response messages

Installer usage:

- the installer does not call audio APIs directly
- the installer may verify agent health through the pipe
- the tray app is responsible for device selection UI and `SaveConfig`

## First-Run Flow

### Fresh Install

1. Install agent binaries.
2. Install tray binaries.
3. Create `%ProgramData%\GoMicFuckYourself\`.
4. Create default `agent-config.json` if missing.
5. Do not register normal autorun entries yet.
6. Optionally launch `GoMicFuckYourself.Tray.exe --first-run`.

### First-Run Tray Behavior

When launched with `--first-run`, the tray app should:

1. call `GetStatus`
2. call `ListCaptureDevices`
3. prompt the user to select a microphone and volume
4. call `SaveConfig`
5. call `ForceEnforce`
6. display success or failure status from the agent
7. register current-user autorun entries for the tray and agent

The tray app must not write `%ProgramData%` directly or call audio APIs directly.

## Agent Startup Requirements

At runtime the agent must:

1. load `agent-config.json`
2. start named pipe server
3. initialize audio watchers
4. enforce config immediately if a device is selected
5. continue running even if no device is configured

If `selectedCaptureDeviceId` is `null`, the agent status should report:

- agent healthy
- no selected device
- enforcement idle

This avoids turning first boot into an agent failure state.

## Installer Verification

After installation, the installer should verify:

1. agent binaries exist under `%ProgramFiles%\GoMicFuckYourself\Agent\`
2. tray binaries exist under `%ProgramFiles%\GoMicFuckYourself\Tray\`
3. `%ProgramData%\GoMicFuckYourself\agent-config.json` exists
4. named pipe responds to `GetStatus`

If agent startup fails, the installer or troubleshooting flow should show:

- whether `GoMicFuckYourself.Agent.exe` is running in the user session
- last known pipe or startup error if available
- path to the config file

## Upgrade Rules

- preserve `%ProgramData%\GoMicFuckYourself\agent-config.json`
- preserve any existing current-user autorun entries for the user who completed setup
- do not auto-launch first-run UI during upgrade if a selected device already exists
- re-register tray autorun only if the tray app is installed

## Uninstall Rules

- remove the agent autorun entry
- remove tray autorun entry
- remove binaries from `%ProgramFiles%\GoMicFuckYourself\`
- leave `%ProgramData%\GoMicFuckYourself\agent-config.json` in place by default

Rationale:

- preserving config makes reinstall and repair safer
- deleting user intent automatically is the wrong default

## Security Notes

- named pipe access should default to local users, with write operations authorized by local machine context
- the tray app should be treated as untrusted input to the agent
- all agent-side config writes must validate device ID and volume range

## Implementation Implications

This contract drives the next code tasks:

1. keep `ServiceConfig` and `ConfigStore` targeting `%ProgramData%\GoMicFuckYourself\agent-config.json`
2. keep `MicPolicyEngine` startup enforcement and periodic fallback enforcement in the agent
3. keep pipe handlers for `GetStatus`, `GetConfig`, `SaveConfig`, `ListCaptureDevices`, and `ForceEnforce`
4. keep tray `--first-run` bootstrap flow
5. keep installer packaging aligned to the agent-based startup model
