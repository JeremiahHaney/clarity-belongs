@echo off
setlocal
cd /d "%~dp0"

echo Pulling latest Clarity Belongs...
git pull --ff-only
if errorlevel 1 (
  echo.
  echo Git pull failed. Fix the repository state, then run this script again.
  exit /b 1
)

echo.
echo Building and launching Clarity Belongs Android app...
dotnet build mobile\ClarityBelongs.Mobile\ClarityBelongs.Mobile.csproj -f net10.0-android -t:Run
exit /b %errorlevel%
