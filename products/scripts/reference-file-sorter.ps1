param(
    [Parameter(Mandatory)]
    [string]$Path,

    [switch]$Apply,

    [string]$OutputPath = '.\artifacts\reference-file-sorter.json'
)

$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot '..\..\product-factory\powershell\ProductFactory.psm1') -Force
$root = Resolve-ProductDirectory -Path $Path
$run = Start-ProductRun -Product 'Reference File Sorter' -Inputs @{ path = $root; apply = [bool]$Apply }

$categories = @{
    Documents = @('.pdf', '.doc', '.docx', '.rtf', '.odt')
    Notes = @('.md', '.txt')
    Data = @('.csv', '.tsv', '.json', '.xml', '.yaml', '.yml')
    Images = @('.jpg', '.jpeg', '.png', '.gif', '.webp', '.svg')
    Books = @('.epub', '.mobi', '.azw', '.azw3')
}

$items = @()
foreach ($file in (Get-ChildItem -LiteralPath $root -File -Force))
{
    $category = 'Other'
    $extension = $file.Extension.ToLowerInvariant()

    foreach ($entry in $categories.GetEnumerator())
    {
        if ($entry.Value -contains $extension) { $category = $entry.Key; break }
    }

    $destinationDirectory = Join-Path $root $category
    $destination = Join-Path $destinationDirectory $file.Name
    $status = 'Planned'

    if (Test-Path -LiteralPath $destination) { $status = 'Conflict' }
    elseif ($Apply) { Ensure-ProductOutputDirectory -Path $destinationDirectory | Out-Null; Move-Item -LiteralPath $file.FullName -Destination $destination; $status = 'Moved' }

    $items += [pscustomobject]@{ source = $file.FullName; destination = $destination; category = $category; status = $status }
}

$result = [pscustomobject]@{
    apply = [bool]$Apply
    planned = @($items | Where-Object status -eq 'Planned').Count
    moved = @($items | Where-Object status -eq 'Moved').Count
    conflicts = @($items | Where-Object status -eq 'Conflict').Count
    items = $items
}

$saved = Save-ProductJson -Value (Complete-ProductRun -Run $run -Result $result) -Path $OutputPath
Write-Host "Moved: $($result.moved); Planned: $($result.planned); Conflicts: $($result.conflicts)"
Write-Host "Saved: $saved"
