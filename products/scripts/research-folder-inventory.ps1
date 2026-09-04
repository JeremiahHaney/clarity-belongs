param(
    [Parameter(Mandatory)]
    [string]$Path,

    [string]$OutputPath = '.\artifacts\research-folder-inventory.json'
)

$ErrorActionPreference = 'Stop'
$modulePath = Join-Path $PSScriptRoot '..\..\product-factory\powershell\ProductFactory.psm1'
Import-Module $modulePath -Force

$root = Resolve-ProductDirectory -Path $Path
$run = Start-ProductRun -Product 'Research Folder Inventory' -Inputs @{
    path = $root
}

$files = @(Get-ProductFiles -Path $root -Recurse |
    ForEach-Object {
        [pscustomobject]@{
            relativePath = [IO.Path]::GetRelativePath($root, $_.FullName)
            extension = if ([string]::IsNullOrWhiteSpace($_.Extension)) { '(none)' } else { $_.Extension.ToLowerInvariant() }
            bytes = $_.Length
            modifiedUtc = $_.LastWriteTimeUtc
        }
    })

$byExtension = @($files |
    Group-Object extension |
    ForEach-Object {
        [pscustomobject]@{
            extension = $_.Name
            count = $_.Count
            bytes = @($_.Group | Measure-Object -Property bytes -Sum).Sum
        }
    } |
    Sort-Object count -Descending)

$result = [pscustomobject]@{
    fileCount = $files.Count
    totalBytes = @($files | Measure-Object -Property bytes -Sum).Sum
    byExtension = $byExtension
    files = @($files | Sort-Object relativePath)
}

$envelope = Complete-ProductRun -Run $run -Result $result
$saved = Save-ProductJson -Value $envelope -Path $OutputPath

Write-Host "Inventoried $($result.fileCount) research files."
Write-Host "Saved: $saved"
