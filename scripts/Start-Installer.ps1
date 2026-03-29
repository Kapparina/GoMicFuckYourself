param(
    [string]$Configuration = "Release"
)

$root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
$msiOutput = Join-Path $root "GoMicFuckYourself.Installer\bin\$Configuration\net48\msi"

Push-Location $msiOutput
try
{
    $msiFile = (Get-ChildItem -Filter "*.msi" | Sort-Object LastWriteTimeUtc | Select-Object -First 1)
    if (-not $msiFile)
    {
        throw "No MSI file found in output directory: $msiOutput"
    }
    Write-Host "Found MSI file: $( $msiFile.FullName )"
    $argArray = @(
        '/i', $msiFile.FullName
    )
   
    Write-Host "Starting installer with elevated privileges..."
    Start-Process msiexec.exe -ArgumentList $argArray -Verb RunAs -Wait
    Write-Host "Installer process completed with exit code: $LASTEXITCODE"
}
finally
{
    Pop-Location
    [System.Environment]::Exit($LASTEXITCODE)
}

