@echo off
title AAST Student Portal — Restart
color 0E
cls
echo ================================================
echo   Restarting AAST Student Portal...
echo ================================================
echo.
cd /d "%~dp0Web-and-App-main"
docker compose down
echo Stopped. Starting again...
echo.
docker compose up -d
echo Waiting 75 seconds...
timeout /t 75 /nobreak
echo.
python "%~dp0Web-and-App-main\sync_keycloak_ids.py" 2>nul
echo.
start "" "http://localhost:3000"
echo.
echo ================================================
echo   Portal restarted!
echo   Web: http://localhost:3000
echo   Student: student.one / hazem123
echo ================================================
echo.
pause
