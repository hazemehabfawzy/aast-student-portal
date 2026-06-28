# Launch emulator if not running
$running = & "D:\Projects\Tools\AndroidSdk\platform-tools\adb.exe" devices | Select-String "emulator"
if (-not $running) {
    Write-Host "Starting emulator..."
    cmd /c "D:\Projects\Tools\AndroidSdk\emulator\emulator.exe" -avd StudentPortal_Emulator -memory 2048 -gpu swiftshader_indirect &
    Write-Host "Waiting 90 seconds for emulator to boot..."
    Start-Sleep -Seconds 90
    
    # Wait for boot
    $boot = ""
    $tries = 0
    while ($boot -ne "1" -and $tries -lt 20) {
        $boot = & "D:\Projects\Tools\AndroidSdk\platform-tools\adb.exe" shell getprop sys.boot_completed 2>$null
        $boot = $boot.Trim()
        Start-Sleep -Seconds 5
        $tries++
        Write-Host "Boot status: $boot (attempt $tries)"
    }
} else {
    Write-Host "Emulator already running"
}

Write-Host "Running Flutter app..."
Set-Location $PSScriptRoot
$env:GRADLE_USER_HOME = "D:\Projects\Tools\gradle-home"
flutter run -d emulator
