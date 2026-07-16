@echo off
title AAST Student Portal — Install Mobile App
color 0B
cls
echo ================================================
echo   AAST Student Portal — Mobile App Installer
echo ================================================
echo.

set ADB=C:\Users\Ahmed\AppData\Local\Android\Sdk\platform-tools\adb.exe
set EMULATOR=C:\Users\Ahmed\AppData\Local\Android\Sdk\emulator\emulator.exe
set AVD=Pixel_7
set APK=%~dp0Web-and-App-main\mobile\build\app\outputs\flutter-apk\app-debug.apk
set MOBILE=%~dp0Web-and-App-main\mobile

:: Check APK exists
if not exist "%APK%" (
    echo [ERROR] APK not found. Building now (takes ~5 minutes)...
    echo.
    cd /d "%MOBILE%"
    flutter build apk --debug
)

:: Show connected devices
echo Connected devices:
"%ADB%" devices
echo.

:: Check emulator running
"%ADB%" devices 2>nul | findstr "emulator" >nul
if errorlevel 1 (
    echo No emulator running. Launching Pixel 6...
    start "" "%EMULATOR%" -avd %AVD% -memory 3072 ^
        -no-snapshot-load -gpu swiftshader_indirect -no-audio
    echo Waiting 40 seconds...
    timeout /t 40 /nobreak
    "%ADB%" -s emulator-5554 wait-for-device 2>nul
    timeout /t 10 /nobreak
)

:: Install on emulator
echo Installing on emulator...
"%ADB%" -s emulator-5554 install -r "%APK%"
if not errorlevel 1 (
    "%ADB%" -s emulator-5554 shell monkey -p com.studentportal.app 1 >nul 2>&1
    echo Launched on emulator!
)

:: Install on real phone if connected
for /f "skip=1 tokens=1,2" %%a in ('"%ADB%" devices') do (
    if "%%b"=="device" if not "%%a"=="emulator-5554" (
        echo Installing on real phone: %%a
        "%ADB%" -s %%a install -r "%APK%"
        "%ADB%" -s %%a shell monkey -p com.studentportal.app 1 >nul 2>&1
        echo Launched on phone!
    )
)

echo.
echo ================================================
echo   Installation complete!
echo ================================================
echo.
echo Login: student.one / hazem123
echo Make sure phone/emulator is on same WiFi as PC.
echo.
pause
