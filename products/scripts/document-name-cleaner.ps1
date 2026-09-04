param(
    [Parameter(Mandatory)]
    [string]$Path,

    [switch]$Apply,

    [string]$OutputPath = '.\artifacts\document-name-cleaner.json'
)

$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot '..\..\product-factory\powershell\ProductFactory.psm1') -Force
$root = Resolve-ProductDirectory -Path $Path
$run = Start-ProductRun -Product 'Document Name Cleaner' -Inputs @{ path = $root; apply = [bool]$Apply }
$extensions = @('.pdf', '.doc', '.docx', '.txt', '.md', '.rtf', '.odt')
$items = @()

foreach ($file in (Get-ProductFiles -Path $root -Recurse | Where-Object { $extensions -contains $_.Extension.ToLowerInvariant() }))
{
    $base = [IO.Path]::GetFileNameWithoutExtension($file.Name)
    $clean = $base -replace '[_]+', ' '
    $clean = $clean -replace '\s+', ' '
    $clean = $clean.Trim(' ', '.', '-')

    if ([string]::IsNullOrWhiteSpace($clean) -or $clean -eq $base) { continue }

    $newName = "$clean$($file.Extension)"
    $destination = Join-Path $file.DirectoryName $newName
    $status = 'Planned'

    if (Test-Path -LiteralPath $destination) { $status = 'Conflict' }
    elseif ($Apply) { Rename-Item -LiteralPath $file.FullName -NewName $newName; $status = 'Renamed' }

    $items += [pscustomobject]@{ source = $file.FullName; destination = $destination; status = $status }
}

$result = [pscustomobject]@{
    apply = [bool]$Apply
    planned = @($items | Where-Object status -eq 'Planned').Count
    renamed = @($items | Where-Object status -eq 'Renamed').Count
    conflicts = @($items | Where-Object status -eq 'Conflict').Count
    items = $items
}

$saved = Save-ProductJson -Value (Complete-ProductRun -Run $run -Result $result) -Path $OutputPath
Write-Host "Renamed: $($result.renamed); Planned: $($result.planned); Conflicts: $($result.conflicts)"
Write-Host "Saved: $saved"
