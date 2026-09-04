param(
    [Parameter(Mandatory)]
    [string]$Path,

    [string]$OutputDirectory = '.\artifacts\cleaned-transcripts'
)

$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot '..\..\product-factory\powershell\ProductFactory.psm1') -Force
$root = Resolve-ProductDirectory -Path $Path
$out = Ensure-ProductOutputDirectory -Path $OutputDirectory
$run = Start-ProductRun -Product 'Transcript Batch Cleaner' -Inputs @{ path = $root; outputDirectory = $out }
$files = @(Get-ProductFiles -Path $root -Recurse | Where-Object { $_.Extension -in @('.txt', '.md') })
$items = @()

foreach ($file in $files)
{
    $text = Get-Content -LiteralPath $file.FullName -Raw
    $text = $text -replace '\r\n', "`n"
    $text = $text -replace '[ \t]+\n', "`n"
    $text = $text -replace "`n{3,}", "`n`n"
    $text = $text.Trim()

    $relative = [IO.Path]::GetRelativePath($root, $file.FullName)
    $safeName = $relative -replace '[\\/:*?"<>|]', '_'
    $destination = Join-Path $out $safeName
    $text | Set-Content -LiteralPath $destination -Encoding UTF8

    $items += [pscustomobject]@{
        source = $file.FullName
        output = $destination
        characters = $text.Length
    }
}

$result = [pscustomobject]@{ cleanedFiles = $items.Count; outputDirectory = $out; items = $items }
Complete-ProductRun -Run $run -Result $result | ConvertTo-Json -Depth 6
