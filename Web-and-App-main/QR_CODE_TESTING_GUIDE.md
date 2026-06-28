# QR Code Scanning Testing & Troubleshooting Guide

## ✅ Setup Verification Checklist

### iOS Configuration
- [x] Camera permission added to Info.plist (`NSCameraUsageDescription`)
- [x] Location permission added to Info.plist (`NSLocationWhenInUseUsageDescription`)
- [x] MobileScanner package installed (v3.5.6)

### Android Configuration  
- [x] Camera permission in AndroidManifest.xml
- [x] Location permissions (FINE & COARSE) in AndroidManifest.xml
- [x] Internet permission in AndroidManifest.xml
- [x] MobileScanner package installed (v3.5.6)

### API Configuration
- [x] X-Client-Platform header set to "mobile" in ApiClient
- [x] JWT token handling in ApiClient
- [x] Check-in endpoint requires mobile platform header (HTTP 403 if not mobile)

## 🧪 Testing Procedure

### 1. Test Environment Setup
```bash
cd mobile
flutter clean
flutter pub get
```

### 2. iOS Testing (Physical Device)
```bash
# Run on iPhone
flutter run -d <device_id>

# Or in Xcode:
# 1. Open ios/Runner.xcworkspace (NOT Runner.xcodeproj)
# 2. Select physical device
# 3. Run the app
```

**iOS Permission Prompts:**
- First time app opens, you'll see "Allow Camera Access?" → Tap **Allow**
- Then you'll see "Allow Location Access?" → Tap **Allow While Using App**

### 3. Android Testing (Physical Device or Emulator)
```bash
# Run on Android
flutter run -d <device_id>
```

**Android Permissions:**
- Grant Camera permission when prompted
- Grant Location permission when prompted
- For emulator, ensure camera is enabled in AVD settings

### 4. Manual QR Code Testing
1. Start a session as instructor (web app: http://localhost:3000/instructor/attendance)
2. Generate a QR code (select "Rotating Token (QR Code)" method)
3. On mobile app:
   - Log in with student account
   - Navigate to "Check-in" tab
   - Ensure a class session appears in dropdown
   - Tap "QR Scanner" button
   - Point camera at QR code on screen
   - App should detect and submit automatically

### 5. Expected Flow
```
QR Scanner → Code Detected → Location Fetched → API Submission → Success/Error Message
```

## 🔧 Troubleshooting

### Issue: Camera not opening
**Solutions:**
1. Check iPhone Settings → Student Portal → Camera is enabled
2. Check Android Settings → App permissions → Camera is allowed
3. Restart the app
4. Rebuild: `flutter clean && flutter pub get && flutter run`

### Issue: "Camera Permission Required" message on screen
**Solutions:**
1. Open app settings
2. Grant camera permission explicitly
3. Toggle camera permission off/on
4. Restart app

### Issue: QR code detected but no API response
**Solutions:**
1. Verify backend is running: `docker-compose logs StudentPortal.Api | tail -20`
2. Check network connectivity: Ensure emulator/device can reach API
3. For emulator: Use `http://10.0.2.2:5000` (not `localhost`)
4. For physical device: Use your machine's LAN IP (e.g., `http://192.168.1.50:5000`)
5. Check token validity: Log out and log back in

### Issue: "Location services are disabled" message
**Solutions:**
1. iPhone: Settings → Privacy → Location Services → Enable
2. Android: Settings → Location → Turn on

### Issue: "No active class session selected"
**Solutions:**
1. Instructor hasn't started a session yet
2. Try refreshing active sessions (pull down or navigate away/back)
3. Check browser console for any session creation errors

### Issue: Duplicate scans happening
**Solution:** 
- Code now has 2-second debounce built-in
- If you still see duplicates, check if you're holding camera on QR code for 2+ seconds

## 📊 Backend Validation

### Check-in Endpoint Details
```
POST /attendance/check-in
Headers: X-Client-Platform: mobile, Authorization: Bearer <token>
Body: {
  "sessionId": "UUID",
  "code": "QR_or_PIN_value",
  "lat": 30.0,
  "lng": 31.0
}
Response: 
- 200: { "message": "Successfully checked in" }
- 403: { "message": "Attendance check-in is only available from the mobile app." }
- 429: { "message": "You've already checked in for this session." }
```

### Verify Mobile Platform Header
```bash
# Test with curl (should fail - no mobile header)
curl -X POST http://localhost:5000/api/attendance/check-in \
  -H "Authorization: Bearer <token>" \
  -H "Content-Type: application/json" \
  -d '{"sessionId":"...", "code":"...", "lat":30, "lng":31}'
# Result: 403 Forbidden

# Test with mobile header (should work)
curl -X POST http://localhost:5000/api/attendance/check-in \
  -H "Authorization: Bearer <token>" \
  -H "X-Client-Platform: mobile" \
  -H "Content-Type: application/json" \
  -d '{"sessionId":"...", "code":"...", "lat":30, "lng":31}'
# Result: 200 OK
```

## 📱 Device Configuration Reference

### Android Emulator Setup
```bash
# When creating AVD, enable:
- Camera: Front & Back (or at least Front)
- Location: FINE & COARSE permissions in manifest

# Run emulator with:
flutter run -d emulator-5554
```

### iOS Simulator Limitations
- **Camera scanning does NOT work on simulator** (iOS limitation)
- **Must test on physical iPhone**
- Simulator can test PIN code method instead

## ✨ Key Improvements Made

1. **Permissions Handling:**
   - Added NSCameraUsageDescription to iOS Info.plist
   - Added NSLocationWhenInUseUsageDescription to iOS Info.plist
   - Created complete AndroidManifest.xml with all required permissions

2. **Error Handling:**
   - Added duplicate scan prevention (2-second debounce)
   - Added camera permission denied UI feedback
   - Better error messages with HTTP status codes
   - Network error details displayed to user

3. **Code Quality:**
   - Added `_lastScannedCode` and `_lastScanTime` to track scans
   - Added `_cameraPermissionDenied` flag for permission status
   - Improved error handling in `_submitCheckIn()`
   - Added debug logging for troubleshooting

## 🚀 Next Steps

- [ ] Test on physical iOS device (required for camera)
- [ ] Test on Android device or emulator
- [ ] Verify location geofence validation works
- [ ] Test QR code rotation every 15 seconds (web app)
- [ ] Test PIN code method as fallback
- [ ] Test error scenarios (invalid QR, out of geofence, etc.)
