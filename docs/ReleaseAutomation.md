# Release Automation

This repo includes a GitHub Actions workflow at `.github/workflows/release.yml` that builds the MSI installer and uploads it to a GitHub Release.

## Requirements

Before using the workflow, confirm the repository settings allow it to publish releases.

In GitHub:

1. Open `Settings > Actions > General`
2. Ensure GitHub Actions is enabled for the repository
3. Under `Workflow permissions`, select `Read and write permissions`

The workflow uses the built-in `GITHUB_TOKEN`. No personal access token or custom secret is required for normal use.

The workflow needs:

- `contents: write`

That permission is required to:

- create a GitHub Release
- upload the MSI as a release asset

## How Releases Are Triggered

The workflow runs automatically when a tag matching `v*` is pushed.

Examples:

- `v0.1.0`
- `v1.2.3`

It can also be run manually with `workflow_dispatch`, but manual runs only build and upload the MSI as a workflow artifact. They do not create a GitHub Release unless the workflow is running for a tag.

## Release Process

1. Commit all release-ready changes
2. Create a version tag
3. Push the tag to GitHub

Example:

```powershell
git tag v0.1.0
git push origin v0.1.0
```

## What The Workflow Does

When triggered by a tag, the workflow:

1. checks out the repository with full history
2. installs .NET 10
3. installs WiX 6.0.2
4. installs the required WiX extensions
5. runs `.\scripts\Build-Installer.ps1 -Configuration Release`
6. finds the generated MSI
7. uploads the MSI as a workflow artifact
8. creates a GitHub Release for the tag
9. attaches the MSI to the release
10. generates release notes automatically from the tag diff and commit history

## Versioning

The installer build script uses two modes:

For tagged release builds:

- tag `v0.1.0` becomes installer version `0.1.0`
- tag `v1.2.3` becomes installer version `1.2.3`

For non-tag local builds:

- the MSI gets a numeric dev version derived from UTC time so it does not collide with tagged release installs
- the tray and agent get a matching file version plus an informational version shaped like `<latest-tag>-dev+<branch>.<sha>`

That same version is also passed into the published tray and agent binaries, so their Windows file properties line up with the release tag.

## Manual Local Build

To test the installer locally without creating a release:

```powershell
.\scripts\Build-Installer.ps1 -Configuration Release
```

To override the version manually:

```powershell
.\scripts\Build-Installer.ps1 -Configuration Release -Version 1.2.3
```

## Expected Output

The workflow publishes:

- a GitHub Release named from the pushed tag
- the MSI installer as a release asset
- automatically generated release notes

The workflow is for tagged releases. Local non-tag builds are still the right way to produce ad hoc development installers.

## Troubleshooting

If the workflow fails to create a release:

- verify `Settings > Actions > General > Workflow permissions` is set to `Read and write permissions`
- verify the run was triggered by a tag such as `v0.1.0`
- verify the repository allows GitHub Actions to run

If the workflow builds but no release appears:

- check whether the run came from `workflow_dispatch` instead of a tag push
- the workflow only creates GitHub Releases when `github.ref` is a tag
