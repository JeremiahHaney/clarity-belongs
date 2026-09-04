param(
    [Parameter(Mandatory)]
    [string]$Path,

    [string[]]$Extensions = @('.md', '.txt'),

    [string]$OutputPath = '.\artifacts\merged-text.txt'
)

$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot '..\..\product-factory\powershell\ProductFactory.psm1') -Force
$root = Resolve-ProductDirectory -Path $Path
$normalized = @($Extensions | ForEach-Object { if ($_.StartsWith('.')) { $_.ToLowerInvariant() } else { ".$($_.ToLowerInvariant())" } })
$run = Start-ProductRun -Product 'Text File Merger' -Inputs @{ path = $root; extensions = $normalized }

$files = @(Get-ProductFiles -Path $root -Recurse |
    Where-Object { $normalized -contains $_.Extension.ToLowerInvariant() } |
    Sort-Object FullName)

$parent = Split-Path -Parent $OutputPath
if ($parent) { Ensure-ProductOutputDirectory -Path $parent | Out-Null }

$builder = [Text.StringBuilder]::new()
foreach ($file in $files)
{
    $relative = [IO.Path]::GetRelativePath($root, $file.FullName)
    [void]$builder.AppendLine("===== $relative =====")
    [void]$builder.AppendLine((Get-Content -LiteralPath $file.FullName -Raw))
    [void]$builder.AppendLine()
}

$builder.ToString() | Set-Content -LiteralPath $OutputPath -Encoding UTF8
$result = [pscustomobject]@{ mergedFiles = $files.Count; outputPath = (Resolve-Path -LiteralPath $OutputPath).Path }
Complete-ProductRun -Run $run -Result $result | ConvertTo-Json -Depth 6
