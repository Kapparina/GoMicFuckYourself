# Release Checklist

## Build

1. Install WiX if needed:
   - `dotnet tool install --global wix`
2. Ensure the WiX UI extension is installed:
   - `wix extension add -g WixToolset.UI.wixext/6.0.2`
3. Build the MSI:
   - `.\scripts\Build-Installer.ps1 -Configuration Release`

## Verify

1. Confirm the MSI exists under:
   - `GoMicFuckYourself.Installer\bin\Release\net48\msi\`
2. Record the emitted SHA256 hash.
3. Test install on a clean machine or VM.
4. Verify:
   - service installs and starts
   - tray app launches
   - first-run setup can save config
   - microphone enforcement works
   - uninstall removes binaries and service cleanly

## Publish

1. Create a GitHub release tag matching the version.
2. Upload the MSI as a release asset.
3. Include the SHA256 in the release notes.

## Current Release Values

- Version: `0.1.0`
- Product code: `{7e383a7a-9580-48a6-818e-b173fef990c8}`
- Upgrade code: `{7e383a7a-9580-48a6-818e-b173fee980c8}`
- Last built SHA256: `46399A2B7815DDEBCDEFF750FC9C7BE69EDBBAF512E9B17FAD85C1B7E5C02E82`
