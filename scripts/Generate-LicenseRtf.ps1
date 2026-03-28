param(
    [Parameter(Mandatory = $true)]
    [string]$LicensePath,

    [Parameter(Mandatory = $true)]
    [string]$OutputPath
)

$licenseText = Get-Content -Path $LicensePath -Raw -Encoding UTF8

$escapedText = $licenseText `
    -replace '\\', '\\\\' `
    -replace '\{', '\{' `
    -replace '\}', '\}'

$paragraphs = $escapedText -split "\r?\n\r?\n"
$rtfParagraphs = foreach ($paragraph in $paragraphs) {
    $lineText = ($paragraph -split "\r?\n") -join '\line '
    "$lineText\par"
}

$rtf = @"
{\rtf1\ansi\deff0
{\fonttbl{\f0 Segoe UI;}}
\fs22
$(($rtfParagraphs -join "`r`n"))
}
"@

$outputDirectory = Split-Path -Path $OutputPath -Parent
if ($outputDirectory) {
    New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
}

Set-Content -Path $OutputPath -Value $rtf -Encoding ASCII
