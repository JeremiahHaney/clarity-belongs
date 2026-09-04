param(
    [Parameter(Mandatory)]
    [string]$Path,

    [string]$OutputPath = '.\artifacts\markdown-index.md'
)

$ErrorActionPreference = 'Stop'
$modulePath = Join-Path $PSScriptRoot '..\..\product-factory\powershell\ProductFactory.psm1'
Import-Module $modulePath -Force

$root = Resolve-ProductDirectory -Path $Path
$run = Start-ProductRun -Product 'Markdown Index Builder' -Inputs @{
    path = $root
}

$files = @(Get-ProductFiles -Path $root -Recurse |
    Where-Object { $_.Extension -ieq '.md' } |
    Sort-Object FullName)

$entries = foreach ($file in $files)
{
    $firstHeading = Get-Content -LiteralPath $file.FullName -ErrorAction Stop |
        Where-Object { $_ -match '^#\s+' } |
        Select-Object -First 1

    $title = if ($firstHeading)
    {
        $firstHeading -replace '^#\s+', ''
    }
    else
    {
        [IO.Path]::GetFileNameWithoutExtension($file.Name)
    }

    [pscustomobject]@{
        title = $title
        relativePath = [IO.Path]::GetRelativePath($root, $file.FullName).Replace('\', '/')
    }
}

$parent = Split-Path -Parent $OutputPath

if ($parent)
{
    Ensure-ProductOutputDirectory -Path $parent | Out-Null
}

$lines = @(
    '# Markdown Index',
    '',
    "Generated: $([DateTimeOffset]::UtcNow.ToString('u'))",
    ''
)

$lines += @($entries | ForEach-Object { "- [$($_.title)]($($_.relativePath))" })
$lines | Set-Content -LiteralPath $OutputPath -Encoding UTF8

$result = [pscustomobject]@{
    markdownFiles = $entries.Count
    outputPath = (Resolve-Path -LiteralPath $OutputPath).Path
}

Complete-ProductRun -Run $run -Result $result | ConvertTo-Json -Depth 6
