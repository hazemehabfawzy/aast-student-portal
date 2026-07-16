@echo off
title AAST Student Portal — Emulator Only
color 0B
cls
echo Launching Pixel 6 API 35...
echo.

set EMULATOR=C:\Users\Ahmed\AppData\Local\Android\Sdk\emulator\emulator.exe
set ADB=C:\Users\Ahmed\AppData\Local\Android\Sdk\platform-tools\adb.exe
set AVD=Pixel_7
set APK=%~dp0Web-and-App-main\mobile\build\app\outputs\flutter-apk\app-debug.apk

"%ADB%" devices 2>nul | findstr "emulator" >nul
if not errorlevel 1 (
    echo Emulator already running.
    goto INSTALL
)

start "" "%EMULATOR%" -avd %AVD% -memory 3072 ^
    -no-snapshot-load -gpu swiftshader_indirect -no-audio
echo Waiting 40 seconds...
timeout /t 40 /nobreak
"%ADB%" -s emulator-5554 wait-for-device 2>nul
timeout /t 10 /nobreak

:INSTALL
"%ADB%" -s emulator-5554 install -r "%APK%"
"%ADB%" -s emulator-5554 shell monkey -p com.studentportal.app 1 >nul 2>&1
echo.
echo App launched! Login: student.one / hazem123
echo.
pause
