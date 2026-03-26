# Installer Bootstrap Contract

## Scope

This document defines the installation and first-run contract for:

- `GoMicFuckYourself.Service`
- `GoMicFuckYourself.Tray`
- the future installer/bootstrapper

The goal is to make initial setup deterministic and keep runtime behavior simple.

## Install Targets

### Service

- Service name: `GoMicFuckYourself.Service`
- Display name: `GoMicFuckYourself Service`
- Startup type: `Automatic`
- Recovery: restart on first, second, and subsequent failure
- Account: `LocalSystem`

Rationale:

- audio endpoint policy and device enforcement are machine-level concerns
- `LocalSystem` avoids permission issues with `%ProgramData%` and service startup
- the tray app remains unprivileged and talks to the service over named pipes

### Tray

- Installed separately from the service binaries
- Started at user logon via `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`
- Command line on first launch after install: `GoMicFuckYourself.Tray.exe --first-run`

Rationale:

- the tray app is per-user UI
- the service is machine-wide enforcement
- using `HKCU\Run` avoids forcing a machine-wide interactive startup model

## Filesystem Layout

### Program Files

Service binaries:

- `%ProgramFiles%\GoMicFuckYourself\Service\`

Tray binaries:

- `%ProgramFiles%\GoMicFuckYourself\Tray\`

### ProgramData

Shared machine config root:

- `%ProgramData%\GoMicFuckYourself\`

Service config file:

- `%ProgramData%\GoMicFuckYourself\service-config.json`

Optional logs directory:

- `%ProgramData%\GoMicFuckYourself\Logs\`

Installer requirements:

- create `%ProgramData%\GoMicFuckYourself\`
- create `service-config.json` if it does not exist
- preserve existing config on upgrade

## Config Schema

The installer and service must both treat this file as the canonical initial config.

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
  - if `false`, the service stays alive but does not force changes

Runtime rules:

- the service must tolerate a missing file by treating it as default config
- the service must tolerate invalid JSON by falling back to default config and logging an error
- the installer must not overwrite a non-empty existing config during upgrade

## Named Pipe Contract

Pipe name:

- `GoMicFuckYourself.Service`

Transport:

- `NamedPipeServerStream`
- UTF-8 JSON request/response messages

Installer usage:

- the installer does not call audio APIs directly
- the installer may verify service health through the pipe
- the tray app is responsible for device selection UI and `SaveConfig`

## First-Run Flow

### Fresh Install

1. Install service binaries.
2. Install tray binaries.
3. Create `%ProgramData%\GoMicFuckYourself\`.
4. Create default `service-config.json` if missing.
5. Register and start `GoMicFuckYourself.Service`.
6. Register tray autorun for the installing user.
7. Launch `GoMicFuckYourself.Tray.exe --first-run`.

### First-Run Tray Behavior

When launched with `--first-run`, the tray app should:

1. call `GetStatus`
2. call `ListCaptureDevices`
3. prompt the user to select a microphone and volume
4. call `SaveConfig`
5. call `ForceEnforce`
6. display success or failure status from the service

The tray app must not write `%ProgramData%` directly.

## Service Startup Requirements

At runtime the service must:

1. load `service-config.json`
2. start named pipe server
3. initialize audio watchers
4. enforce config immediately if a device is selected
5. continue running even if no device is configured

If `selectedCaptureDeviceId` is `null`, the service status should report:

- service healthy
- no selected device
- enforcement idle

This avoids turning first boot into a service failure state.

## Installer Verification

After installation, the installer should verify:

1. service registration exists
2. service status is `Running`
3. `%ProgramData%\GoMicFuckYourself\service-config.json` exists
4. named pipe responds to `GetStatus`

If service startup fails, the installer should show:

- Windows service status
- last known error if available
- path to the config file

## Upgrade Rules

- preserve `%ProgramData%\GoMicFuckYourself\service-config.json`
- restart the service after binary replacement
- do not auto-launch first-run UI during upgrade if a selected device already exists
- re-register tray autorun only if the tray app is installed

## Uninstall Rules

- stop and remove the service
- remove tray autorun entry
- remove binaries from `%ProgramFiles%\GoMicFuckYourself\`
- leave `%ProgramData%\GoMicFuckYourself\service-config.json` in place by default

Rationale:

- preserving config makes reinstall and repair safer
- deleting user intent automatically is the wrong default

## Security Notes

- named pipe access should default to local users, with write operations authorized by local machine context
- the tray app should be treated as untrusted input to the service
- all service-side config writes must validate device ID and volume range

## Implementation Implications

This contract drives the next code tasks:

1. add `ServiceConfig` and `ConfigStore` targeting `%ProgramData%\GoMicFuckYourself\service-config.json`
2. add `MicPolicyEngine` startup enforcement and periodic fallback enforcement
3. add pipe handlers for `GetStatus`, `GetConfig`, `SaveConfig`, `ListCaptureDevices`, and `ForceEnforce`
4. add tray `--first-run` bootstrap flow
5. add installer project or packaging definition implementing the steps above
