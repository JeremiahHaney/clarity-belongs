param(
    [Parameter(Mandatory = $true)]
    [string]$DatabasePath,

    [string]$OutputPath = ""
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot "src\ClarityBelongs.DatabaseTool\ClarityBelongs.DatabaseTool.csproj"
$resolvedDatabase = [System.IO.Path]::GetFullPath($DatabasePath)

if (-not (Test-Path -LiteralPath $resolvedDatabase -PathType Leaf)) {
    throw "Clarity database not found: $resolvedDatabase"
}

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $OutputPath = Join-Path (Get-Location) "clarity-database-inventory-$timestamp.json"
}

$resolvedOutput = [System.IO.Path]::GetFullPath($OutputPath)

Write-Host "Clarity database: $resolvedDatabase"
Write-Host "Inventory report: $resolvedOutput"

dotnet run `
    --project $projectPath `
    --configuration Release `
    -- `
    --source $resolvedDatabase `
    --output $resolvedOutput

if ($LASTEXITCODE -ne 0) {
    throw "Clarity database inventory failed with exit code $LASTEXITCODE. Do not continue SQL Server cutover."
}

Write-Host "Clarity pre-cutover database validation passed."
