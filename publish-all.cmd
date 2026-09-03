@echo off
setlocal

cd /d "%~dp0"

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\publish.ps1"
set EXITCODE=%ERRORLEVEL%

echo.
if not "%EXITCODE%"=="0" (
    echo Clarity publish completed with errors.
) else (
    echo Clarity publish complete.
)

pause
exit /b %EXITCODE%
