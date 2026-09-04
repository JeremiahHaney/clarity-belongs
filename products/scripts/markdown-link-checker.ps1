param(
    [Parameter(Mandatory)]
    [string]$Path,

    [string]$OutputPath = '.\artifacts\markdown-link-checker.json'
)

$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot '..\..\product-factory\powershell\ProductFactory.psm1') -Force
$root = Resolve-ProductDirectory -Path $Path
$run = Start-ProductRun -Product 'Markdown Link Checker' -Inputs @{ path = $root }
$items = @()

foreach ($file in (Get-ProductFiles -Path $root -Recurse | Where-Object { $_.Extension -ieq '.md' }))
{
    $content = Get-Content -LiteralPath $file.FullName -Raw
    $matches = [regex]::Matches($content, '!?' + '\[[^\]]*\]\(([^)]+)\)')

    foreach ($match in $matches)
    {
        $target = $match.Groups[1].Value.Trim()

        if ($target -match '^(https?://|mailto:|#)') { continue }

        $pathOnly = ($target -split '#', 2)[0]
        if ([string]::IsNullOrWhiteSpace($pathOnly)) { continue }

        $decoded = [Uri]::UnescapeDataString($pathOnly)
        $candidate = Join-Path $file.DirectoryName $decoded
        $exists = Test-Path -LiteralPath $candidate

        $items += [pscustomobject]@{
            source = [IO.Path]::GetRelativePath($root, $file.FullName)
            target = $target
            exists = $exists
        }
    }
}

$result = [pscustomobject]@{
    linksChecked = $items.Count
    brokenLinks = @($items | Where-Object { -not $_.exists }).Count
    items = @($items | Sort-Object source, target)
}

$saved = Save-ProductJson -Value (Complete-ProductRun -Run $run -Result $result) -Path $OutputPath
Write-Host "Checked $($result.linksChecked) local links; broken: $($result.brokenLinks)."
Write-Host "Saved: $saved"
