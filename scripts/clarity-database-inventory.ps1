param(
    [string]$DatabasePath = "",
    [string]$OutputPath = "",
    [string]$BaselineReport = "",
    [string]$CandidateReport = ""
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot "src\ClarityBelongs.DatabaseTool\ClarityBelongs.DatabaseTool.csproj"

if (-not [string]::IsNullOrWhiteSpace($BaselineReport) -or
    -not [string]::IsNullOrWhiteSpace($CandidateReport)) {
    if ([string]::IsNullOrWhiteSpace($BaselineReport) -or
        [string]::IsNullOrWhiteSpace($CandidateReport)) {
        throw "Reconciliation requires both -BaselineReport and -CandidateReport."
    }

    $resolvedBaseline = [System.IO.Path]::GetFullPath($BaselineReport)
    $resolvedCandidate = [System.IO.Path]::GetFullPath($CandidateReport)

    dotnet run `
        --project $projectPath `
        --configuration Release `
        -- `
        --compare `
        --baseline $resolvedBaseline `
        --candidate $resolvedCandidate

    if ($LASTEXITCODE -ne 0) {
        throw "Clarity database reconciliation failed with exit code $LASTEXITCODE. Do not continue cutover."
    }

    Write-Host "Clarity database reconciliation passed."
    exit 0
}

if ([string]::IsNullOrWhiteSpace($DatabasePath)) {
    throw "Inventory mode requires -DatabasePath. Run against a verified SQLite backup, not the live writable production file."
}

$resolvedDatabase = [System.IO.Path]::GetFullPath($DatabasePath)

if (-not (Test-Path -LiteralPath $resolvedDatabase -PathType Leaf)) {
    throw "Clarity database not found: $resolvedDatabase"
}

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
    $OutputPath = Join-Path (Get-Location) "clarity-database-inventory-$timestamp.json"
}

$resolvedOutput = [System.IO.Path]::GetFullPath($OutputPath)

Write-Warning "Run this inventory against a verified SQLite backup captured for cutover, not the live writable production file."
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
