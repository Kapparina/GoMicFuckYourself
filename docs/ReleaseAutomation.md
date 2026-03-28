# Release Automation

This repo includes a GitHub Actions workflow at `.github/workflows/release.yml` that builds the MSI installer, builds the bootstrapper EXE, and uploads both to a GitHub Release.

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
4. installs the required WiX extensions (`Bal`, `Netfx`, `UI`, and `Util`)
5. runs `.\scripts\Build-Installer.ps1 -Configuration Release`
6. downloads the configured .NET Desktop Runtime prerequisite during the build
7. finds the generated MSI and bootstrapper EXE
8. uploads both as workflow artifacts
9. creates a GitHub Release for the tag
10. attaches both the MSI and bootstrapper to the release
11. generates release notes automatically from the tag diff and commit history

## Versioning

The installer build script derives the version from the latest Git tag by default.

For example:

- tag `v0.1.0` becomes installer version `0.1.0`
- tag `v1.2.3` becomes installer version `1.2.3`

If no tag is available, the script falls back to `0.1.0`.

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
- the bootstrapper EXE as a release asset
- the MSI installer as a release asset
- automatically generated release notes

## Troubleshooting

If the workflow fails to create a release:

- verify `Settings > Actions > General > Workflow permissions` is set to `Read and write permissions`
- verify the run was triggered by a tag such as `v0.1.0`
- verify the repository allows GitHub Actions to run

If the workflow builds but no release appears:

- check whether the run came from `workflow_dispatch` instead of a tag push
- the workflow only creates GitHub Releases when `github.ref` is a tag
