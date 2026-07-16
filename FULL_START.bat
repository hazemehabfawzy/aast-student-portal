@echo off
title AAST Student Portal — Full Start
color 0A
cls
echo ================================================
echo   AAST Student Portal — Starting Everything
echo ================================================
echo.

set EMULATOR=C:\Users\Ahmed\AppData\Local\Android\Sdk\emulator\emulator.exe
set ADB=C:\Users\Ahmed\AppData\Local\Android\Sdk\platform-tools\adb.exe
set AVD=Pixel_7
set APK=%~dp0Web-and-App-main\mobile\build\app\outputs\flutter-apk\app-debug.apk
set PROJECT=%~dp0Web-and-App-main
set SYNC=%~dp0Web-and-App-main\sync_keycloak_ids.py

:: ── 1. Check Docker ──────────────────────────────────────────
docker ps >nul 2>&1
if errorlevel 1 (
    echo [ERROR] Docker Desktop is not running.
    echo Please open Docker Desktop and wait for it to load fully.
    echo Then run this script again.
    pause
    exit /b 1
)
echo [1/5] Docker is running... OK
echo.

:: ── 2. Start containers ──────────────────────────────────────
echo [2/5] Starting all Docker containers...
cd /d "%PROJECT%"
docker compose up -d
echo Containers started.
echo.

:: ── 3. Wait and sync ─────────────────────────────────────────
echo [3/5] Waiting 90 seconds for Keycloak to be ready...
timeout /t 90 /nobreak
echo.
echo Syncing Keycloak user IDs...
python "%SYNC%" 2>nul
if errorlevel 1 (
    echo Sync skipped - data may appear empty, try RESTART_PORTAL.bat
) else (
    echo Sync complete!
)
echo.

:: ── 4. Open browser ──────────────────────────────────────────
echo [4/5] Opening web portal...
start "" "http://localhost:3000"
timeout /t 3 /nobreak
echo.

:: ── 5. Launch emulator ───────────────────────────────────────
echo [5/5] Launching Pixel 6 API 35 emulator...

"%ADB%" devices 2>nul | findstr "emulator" >nul
if not errorlevel 1 (
    echo Emulator already running — skipping launch.
    goto INSTALL
)

start "" "%EMULATOR%" -avd %AVD% -memory 3072 ^
    -no-snapshot-load -gpu swiftshader_indirect -no-audio
echo Waiting 40 seconds for emulator to boot...
timeout /t 40 /nobreak
"%ADB%" -s emulator-5554 wait-for-device 2>nul
timeout /t 10 /nobreak

:INSTALL
echo Installing mobile app on emulator...
"%ADB%" -s emulator-5554 install -r "%APK%" 2>nul
if errorlevel 1 (
    echo APK install failed - emulator may still be booting.
    echo Run INSTALL_MOBILE.bat in 30 seconds.
) else (
    echo App installed!
    "%ADB%" -s emulator-5554 shell monkey -p com.studentportal.app 1 >nul 2>&1
    echo App launched!
)

echo.
echo ================================================
echo   EVERYTHING IS RUNNING!
echo ================================================
echo.
echo   Web App:    http://localhost:3000
echo   API Docs:   http://localhost:5000/swagger
echo   Keycloak:   http://localhost:8080
echo   Emulator:   Pixel 6 API 35
echo.
echo   CREDENTIALS:
echo   Student:    student.one     / hazem123
echo   Instructor: instructor.one  / Instructor@123
echo   Admin:      admin.portal    / Admin@123
echo.
echo   To stop everything run STOP_PORTAL.bat
echo ================================================
echo.
pause
