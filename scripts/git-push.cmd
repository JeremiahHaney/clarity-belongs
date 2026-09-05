@echo off
setlocal

cd /d C:\Projects\clarity-belongs

git status
if errorlevel 1 goto :err

git add .
if errorlevel 1 goto :err

git commit -m "update"
if errorlevel 1 goto :err

git push
if errorlevel 1 goto :err

echo.
echo Done.
timeout /t 5 /nobreak >nul
exit /b 0

:err
echo.
echo Git command failed. Fix the error above.
pause
exit /b 1