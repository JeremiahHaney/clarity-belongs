param(
    [Parameter(Mandatory)]
    [string]$Path,

    [string]$OutputPath = '.\artifacts\reading-folder-catalog.md'
)

$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot '..\..\product-factory\powershell\ProductFactory.psm1') -Force
$root = Resolve-ProductDirectory -Path $Path
$run = Start-ProductRun -Product 'Reading Folder Catalog' -Inputs @{ path = $root }
$extensions = @('.pdf', '.epub', '.mobi', '.azw', '.azw3', '.doc', '.docx', '.txt', '.md')

$items = @(Get-ProductFiles -Path $root -Recurse |
    Where-Object { $extensions -contains $_.Extension.ToLowerInvariant() } |
    ForEach-Object {
        [pscustomobject]@{
            title = [IO.Path]::GetFileNameWithoutExtension($_.Name)
            type = $_.Extension.TrimStart('.').ToUpperInvariant()
            relativePath = [IO.Path]::GetRelativePath($root, $_.FullName).Replace('\', '/')
            modifiedUtc = $_.LastWriteTimeUtc
        }
    } |
    Sort-Object title)

$parent = Split-Path -Parent $OutputPath
if ($parent) { Ensure-ProductOutputDirectory -Path $parent | Out-Null }

$lines = @('# Reading Folder Catalog', '', "Generated: $([DateTimeOffset]::UtcNow.ToString('u'))", '')
$lines += @($items | ForEach-Object { "- **$($_.title)** [$($_.type)] — $($_.relativePath)" })
$lines | Set-Content -LiteralPath $OutputPath -Encoding UTF8

$result = [pscustomobject]@{ itemCount = $items.Count; outputPath = (Resolve-Path -LiteralPath $OutputPath).Path }
Complete-ProductRun -Run $run -Result $result | ConvertTo-Json -Depth 6
