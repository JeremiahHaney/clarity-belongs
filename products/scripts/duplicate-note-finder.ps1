param(
    [Parameter(Mandatory)]
    [string]$Path,

    [string[]]$Extensions = @('.md', '.txt'),

    [string]$OutputPath = '.\artifacts\duplicate-note-finder.json'
)

$ErrorActionPreference = 'Stop'
$modulePath = Join-Path $PSScriptRoot '..\..\product-factory\powershell\ProductFactory.psm1'
Import-Module $modulePath -Force

$root = Resolve-ProductDirectory -Path $Path
$normalizedExtensions = @($Extensions | ForEach-Object {
    if ($_.StartsWith('.')) { $_.ToLowerInvariant() } else { ".$($_.ToLowerInvariant())" }
})

$run = Start-ProductRun -Product 'Duplicate Note Finder' -Inputs @{
    path = $root
    extensions = $normalizedExtensions
}

$notes = @(Get-ProductFiles -Path $root -Recurse |
    Where-Object { $normalizedExtensions -contains $_.Extension.ToLowerInvariant() })

$hashed = foreach ($note in $notes)
{
    $content = Get-Content -LiteralPath $note.FullName -Raw -ErrorAction Stop
    $normalized = ($content -replace '\r\n', "`n").Trim()
    $bytes = [Text.Encoding]::UTF8.GetBytes($normalized)
    $sha = [Security.Cryptography.SHA256]::HashData($bytes)
    $hash = [Convert]::ToHexString($sha)

    [pscustomobject]@{
        path = $note.FullName
        relativePath = [IO.Path]::GetRelativePath($root, $note.FullName)
        characters = $normalized.Length
        sha256 = $hash
    }
}

$groups = @($hashed |
    Group-Object sha256 |
    Where-Object Count -gt 1 |
    ForEach-Object {
        [pscustomobject]@{
            count = $_.Count
            sha256 = $_.Name
            files = @($_.Group | Sort-Object relativePath)
        }
    } |
    Sort-Object count -Descending)

$result = [pscustomobject]@{
    scannedNotes = $notes.Count
    duplicateGroups = $groups.Count
    duplicateNotes = @($groups | ForEach-Object { $_.count } | Measure-Object -Sum).Sum
    groups = $groups
}

$envelope = Complete-ProductRun -Run $run -Result $result
$saved = Save-ProductJson -Value $envelope -Path $OutputPath

Write-Host "Scanned $($result.scannedNotes) notes."
Write-Host "Found $($result.duplicateGroups) duplicate groups."
Write-Host "Saved: $saved"
