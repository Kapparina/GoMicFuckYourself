# GoMicFuckYourself

[![Release Installer](https://github.com/Kapparina/GoMicFuckYourself/actions/workflows/release.yml/badge.svg)](https://github.com/Kapparina/GoMicFuckYourself/actions/workflows/release.yml)

GoMicFuckYourself is a Windows utility that keeps one microphone locked as:

- your default input device
- your default communications device
- your chosen input volume

If Windows or another app changes those settings, GoMicFuckYourself changes them back.

## Download

Download the latest `.msi` installer from the [Releases](https://github.com/Kapparina/GoMicFuckYourself/releases) page.

## Install

1. Run the installer.
2. Follow the setup wizard.
3. On the last screen, optionally check the box to launch the app immediately.

## First-Time Setup

When the app opens for the first time:

1. Choose the microphone you want to keep.
2. Set the volume level you want to keep.
3. Leave enforcement enabled.
4. Click `Save and close`.

After that, the app will keep enforcing those settings automatically.

Once first-time setup is complete, GoMicFuckYourself is configured to start automatically when you sign in to Windows.

## Everyday Use

Open `GoMicFuckYourself` from the Start menu or the tray icon.

In the app window:

- `Save` applies your changes immediately
- `Save and close` applies your changes and hides the window
- `Reboot Enforcement Agent` restarts the background process if you need to recover it

## What To Expect

- Only enabled microphones appear in the dropdown.
- Opening the tray app starts the enforcement agent if it is not already running.
- Exiting the tray app from its tray menu also stops the agent.
- If the app is already running, opening it again reuses the existing instance instead of starting another copy.
- After setup is saved, the app is configured to start automatically at Windows sign-in.

## Troubleshooting

If the wrong microphone is active:

1. Open the app.
2. Confirm the correct microphone is selected.
3. Click `Save`.

If enforcement seems stuck:

1. Open the app.
2. Click `Reboot Enforcement Agent`.

If the app is not visible:

- check the system tray
- launch `GoMicFuckYourself` from the Start menu

## Developer Docs

Development and release docs live under [`docs/`](docs/).

Useful entry points:

- [Installer bootstrap contract](docs/InstallerBootstrapContract.md)
- [Installer packaging](docs/InstallerPackaging.md)
- [Release automation](docs/ReleaseAutomation.md)
- [Release checklist](docs/ReleaseChecklist.md)
- [Release notes template](docs/ReleaseNotesTemplate.md)

## License

This project is licensed under the terms in [LICENSE](LICENSE).
