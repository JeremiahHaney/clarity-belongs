param(
    [Parameter(Mandatory)]
    [string]$Path,

    [switch]$Apply,

    [string]$OutputPath = '.\artifacts\note-file-organizer.json'
)

$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot '..\..\product-factory\powershell\ProductFactory.psm1') -Force
$root = Resolve-ProductDirectory -Path $Path
$run = Start-ProductRun -Product 'Note File Organizer' -Inputs @{ path = $root; apply = [bool]$Apply }
$extensions = @('.md', '.txt')
$items = @()

foreach ($file in (Get-ChildItem -LiteralPath $root -File -Force | Where-Object { $extensions -contains $_.Extension.ToLowerInvariant() }))
{
    $year = $file.LastWriteTime.ToString('yyyy')
    $month = $file.LastWriteTime.ToString('MM - MMMM')
    $destinationDirectory = Join-Path (Join-Path $root $year) $month
    $destination = Join-Path $destinationDirectory $file.Name
    $status = 'Planned'

    if (Test-Path -LiteralPath $destination)
    {
        $status = 'Conflict'
    }
    elseif ($Apply)
    {
        Ensure-ProductOutputDirectory -Path $destinationDirectory | Out-Null
        Move-Item -LiteralPath $file.FullName -Destination $destination
        $status = 'Moved'
    }

    $items += [pscustomobject]@{ source = $file.FullName; destination = $destination; status = $status }
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
