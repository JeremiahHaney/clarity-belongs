$ErrorActionPreference = "Stop"

Clear-Host
Set-Location -LiteralPath $PSScriptRoot

Write-Host "Pulling latest Clarity Belongs..."
git pull --ff-only
if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "Git pull failed. Fix the repository state, then run this script again."
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "Building and launching Clarity Belongs Android app..."
dotnet build "mobile\ClarityBelongs.Mobile\ClarityBelongs.Mobile.csproj" -f net10.0-android -t:Run
exit $LASTEXITCODE
