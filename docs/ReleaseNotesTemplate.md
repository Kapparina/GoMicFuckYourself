# GoMicFuckYourself v0.1.0

## Highlights

- Per-user agent for microphone default-device and volume enforcement
- Tray application for first-run setup and ongoing configuration
- Bootstrapper installer that installs the .NET Desktop Runtime prerequisite and then launches the MSI
- MSI installer built with WixSharp for prerequisite-satisfied environments

## Included Artifacts

- `GoMicFuckYourself-0.1.0-bootstrapper.exe`
- `GoMicFuckYourself-0.1.0.msi`

## Verification

- Product code: `{7e383a7a-9580-48a6-818e-b173fef990c8}`
- Bootstrapper SHA256: `<fill in>`
- MSI SHA256: `<fill in>`

## Notes

- Requires administrator rights for installation
- The bootstrapper is the recommended installer for normal users
- The MSI is intended for machines that already have the required .NET Desktop Runtime installed
- Installs both the enforcement agent and the tray application
