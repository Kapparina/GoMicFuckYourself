param(
    [string]$Configuration = "Release",
    [string]$Version
)

$ErrorActionPreference = "Stop"

$root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$artifacts = Join-Path $root "artifacts"
$payloadRoot = Join-Path $artifacts "payload"
$agentPublish = Join-Path $payloadRoot "Agent"
$trayPublish = Join-Path $payloadRoot "Tray"
$msiOutput = Join-Path $root "GoMicFuckYourself.Installer\bin\$Configuration\net48\msi"
$agentProject = Join-Path $root "GoMicFuckYourself.Agent\GoMicFuckYourself.Agent.csproj"
$trayProject = Join-Path $root "GoMicFuckYourself.Tray\GoMicFuckYourself.Tray.csproj"
$installerProject = Join-Path $root "GoMicFuckYourself.Installer\GoMicFuckYourself.Installer.csproj"
$informationalVersion = $null
$fileVersion = $null

if (-not $Version)
{
    $exactTag = ""
    try
    {
        $exactTag = (git -C $root describe --tags --exact-match 2> $null).Trim()
    }
    catch
    {
    }

    if ($exactTag)
    {
        $Version = $exactTag.TrimStart('v', 'V')
        $informationalVersion = $Version
        $versionParts = $Version.Split('.')
        switch ($versionParts.Length)
        {
            2 { $fileVersion = "$Version.0.0" }
            3 { $fileVersion = "$Version.0" }
            default { $fileVersion = $Version }
        }
    }
    else
    {
        $latestTag = ""
        $branchName = "unknown"
        $commitSha = "unknown"

        try
        {
            $latestTag = (git -C $root describe --tags --abbrev=0 2> $null).Trim()
        }
        catch
        {
        }

        try
        {
            $branchName = (git -C $root rev-parse --abbrev-ref HEAD 2> $null).Trim()
        }
        catch
        {
        }

        try
        {
            $commitSha = (git -C $root rev-parse --short HEAD 2> $null).Trim()
        }
        catch
        {
        }

        if ($latestTag)
        {
            $baseVersion = $latestTag.TrimStart('v', 'V')
        }
        else
        {
            $baseVersion = "0.1.0"
        }

        $safeBranchName = [System.Text.RegularExpressions.Regex]::Replace($branchName.ToLowerInvariant(), "[^0-9a-z\-]+", "-").Trim('-')
        if ([string]::IsNullOrWhiteSpace($safeBranchName))
        {
            $safeBranchName = "local"
        }

        if ([string]::IsNullOrWhiteSpace($commitSha))
        {
            $commitSha = "unknown"
        }

        $baseVersionParts = $baseVersion.Split('.')
        if ($baseVersionParts.Length -lt 1)
        {
            throw "Base version '$baseVersion' must have at least a major component."
        }

        $majorVersion = [int]$baseVersionParts[0]

        $utcNow = [DateTime]::UtcNow
        $monthBucket = (($utcNow.Year - 2020) * 12) + $utcNow.Month
        $minuteOfMonth = (($utcNow.Day - 1) * 1440) + ($utcNow.Hour * 60) + $utcNow.Minute
        $revisionComponent = $utcNow.Second

        if ($monthBucket -gt 255)
        {
            throw "Computed month bucket '$monthBucket' exceeds MSI version limits."
        }

        if ($minuteOfMonth -gt 65535)
        {
            throw "Computed minute-of-month '$minuteOfMonth' exceeds MSI version limits."
        }

        $Version = "$majorVersion.$monthBucket.$minuteOfMonth"
        $fileVersion = "$Version.$revisionComponent"
        $informationalVersion = "$baseVersion-dev+$safeBranchName.$commitSha"
    }
}
else
{
    $informationalVersion = $Version
    $versionParts = $Version.Split('.')
    switch ($versionParts.Length)
    {
        2 { $fileVersion = "$Version.0.0" }
        3 { $fileVersion = "$Version.0" }
        default { $fileVersion = $Version }
    }
}

if (-not (Get-Command wix.exe -ErrorAction SilentlyContinue))
{
    throw "wix.exe cannot be found. Install WiX with: dotnet tool install --global wix"
}

foreach ($requiredPath in @($agentProject, $trayProject, $installerProject))
{
    if (-not (Test-Path -LiteralPath $requiredPath))
    {
        throw "Required project file was not found: $requiredPath"
    }
}

New-Item -ItemType Directory -Force -Path $agentPublish | Out-Null
New-Item -ItemType Directory -Force -Path $trayPublish | Out-Null

Push-Location $root
try
{
    & dotnet publish $agentProject `
    -c $Configuration `
    -p:Version=$Version `
    -p:AssemblyVersion=$fileVersion `
    -p:FileVersion=$fileVersion `
    -p:InformationalVersion=$informationalVersion `
    -o $agentPublish

    & dotnet publish $trayProject `
    -c $Configuration `
    -p:Version=$Version `
    -p:AssemblyVersion=$fileVersion `
    -p:FileVersion=$fileVersion `
    -p:InformationalVersion=$informationalVersion `
    -o $trayPublish

    & dotnet run --project $installerProject -c $Configuration -- `
    --payload-root $payloadRoot `
    --version $Version
}
finally
{
    Pop-Location
}

$msi = Get-ChildItem -Path $msiOutput -Filter *.msi | Sort-Object LastWriteTimeUtc | Select-Object -Last 1
if ($null -ne $msi)
{
    $hash = (Get-FileHash -Path $msi.FullName -Algorithm SHA256).Hash
    Write-Host "Version: $Version"
    Write-Host "FileVersion: $fileVersion"
    Write-Host "InformationalVersion: $informationalVersion"
    Write-Host "MSI: $( $msi.FullName )"
    Write-Host "SHA256: $hash"
}
