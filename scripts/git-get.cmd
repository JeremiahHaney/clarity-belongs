@echo off
setlocal

cd /d C:\Projects\clarity-belongs

git status
if errorlevel 1 goto :err

git fetch origin
if errorlevel 1 goto :err

git switch main
if errorlevel 1 goto :err

git pull
if errorlevel 1 goto :err

echo.
echo Download complete.
echo Current branch:
git branch --show-current
echo.
pause
exit /b 0

:err
echo.
echo Git command failed. Fix the error above.
pause
exit /b 1