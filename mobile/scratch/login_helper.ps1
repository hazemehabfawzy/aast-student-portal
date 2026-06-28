param(
    [string]$username = "student.one",
    [string]$password = "hazem123"
)

$adb = "D:\Projects\Tools\AndroidSdk\platform-tools\adb.exe"

Write-Host "Tapping username field..."
& $adb shell input tap 540 734
Start-Sleep -Milliseconds 500

Write-Host "Clearing username field (backspaces)..."
for ($i = 0; $i -lt 30; $i++) {
    & $adb shell input keyevent 67
}

Write-Host "Typing username..."
& $adb shell input text $username
Start-Sleep -Milliseconds 500

Write-Host "Tapping password field..."
& $adb shell input tap 540 919
Start-Sleep -Milliseconds 500

Write-Host "Clearing password field (backspaces)..."
for ($i = 0; $i -lt 30; $i++) {
    & $adb shell input keyevent 67
}

Write-Host "Typing password..."
& $adb shell input text $password
Start-Sleep -Milliseconds 500

Write-Host "Tapping Sign In button..."
& $adb shell input tap 540 1150
