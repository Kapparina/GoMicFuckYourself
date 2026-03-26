# Distribution Manifests

This folder contains generated package-manager manifests for the MSI installer.

## Generate manifests

Run:

```powershell
.\scripts\Generate-PackageManifests.ps1 `
  -Version 0.1.0 `
  -InstallerUrl https://github.com/YOUR_GITHUB_REPO/releases/download/v0.1.0/GoMicFuckYourself-0.1.0.msi `
  -InstallerSha256 YOUR_SHA256 `
  -ProductCode '{7e383a7a-9580-48a6-818e-b173fef990c8}' `
  -RepositorySlug YOUR_GITHUB_REPO
```

Outputs:

- `distribution/winget/<PackageIdentifier>/<Version>/...`
- `distribution/scoop/gomicfuckyourself.json`

## Required manual values

The generator now requires two release-specific values:

- `ProductCode`
- `RepositorySlug`

Current locally built MSI product code:

- `{7e383a7a-9580-48a6-818e-b173fef990c8}`

## Winget notes

- manifest schema version is generated as `1.10.0`
- publish the MSI first, then generate manifests with the final release URL and SHA256
- submit the generated files to `microsoft/winget-pkgs`

## Scoop notes

- the generated manifest uses `msiexec` explicitly rather than deprecated Scoop MSI fields
- publish it in your own bucket repo or another bucket you control
