@echo off
title AAST Student Portal — Stop
color 0C
cls
echo ================================================
echo   Stopping AAST Student Portal...
echo ================================================
echo.
cd /d "%~dp0Web-and-App-main"
docker compose down
echo.
echo All containers stopped.
echo Run FULL_START.bat to start again.
echo.
pause
