# GoMicFuckYourself

[![Release Installer](https://github.com/Kapparina/GoMicFuckYourself/actions/workflows/release.yml/badge.svg)](https://github.com/Kapparina/GoMicFuckYourself/actions/workflows/release.yml)

GoMicFuckYourself is a Windows utility that keeps one microphone set as your Default and Default Communications Device while also ensuring that input volume changes made by Windows or other apps are reverted.

## Download

Download the latest `.msi` installer from the [Releases](https://github.com/Kapparina/GoMicFuckYourself/releases) page.

## Install

1. Run the installer.
2. Follow the setup wizard.
3. On the last screen, optionally check the box to launch the app immediately.

## First-Time Setup

When the app opens for the first time:

1. Choose the microphone you want to keep as your Default and Default Communications Device.
2. Confirm or adjust the target volume. It should default to the selected microphone's current volume.
3. Leave enforcement enabled unless you explicitly want to disable it.
4. Choose whether the tray app should start automatically when you sign in to Windows.
5. Click `Save and close`.

After that, the app will keep enforcing those settings automatically.

## Everyday Use

Open `GoMicFuckYourself` from the Start menu or the tray icon.

In the app window:

- `Save` applies your changes immediately and refreshes the UI
- `Save and close` applies your changes, refreshes the UI, and hides the window
- `Reboot Enforcement Agent` restarts the background process if it needs recovering
- `Start automatically when I sign in` controls whether the tray app launches at Windows sign-in

## What To Expect

- Only enabled microphones appear in the dropdown.
- Opening the tray app starts the enforcement agent if it is not already running.
- Exiting the tray app from its tray menu also stops the agent.
- If the app is already running, opening it again reuses the existing instance instead of starting another copy.
- Saving refreshes the app so the current configuration and current live device state are shown immediately.
- Whether the app starts at Windows sign-in depends on the `Start automatically when I sign in` checkbox.

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

Development and release docs live under [`docs/`](./docs).

Useful entry points:

- [Installer packaging](docs/InstallerPackaging.md)
- [Release automation](docs/ReleaseAutomation.md)

## License

This project is licensed under the terms in [LICENCE](./LICENCE).
