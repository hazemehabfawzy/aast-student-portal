import 'package:flutter/material.dart';
import 'package:geolocator/geolocator.dart';
import 'package:mobile_scanner/mobile_scanner.dart';
import 'package:image_picker/image_picker.dart';
import 'dart:convert';
import '../api/api_client.dart';

class AttendanceScreen extends StatefulWidget {
  const AttendanceScreen({super.key});

  @override
  State<AttendanceScreen> createState() => _AttendanceScreenState();
}

enum CheckInMode { qr, pin, face }

class _AttendanceScreenState extends State<AttendanceScreen> {
  CheckInMode _mode = CheckInMode.qr;
  final TextEditingController _pinController = TextEditingController();
  bool _checkingIn = false;
  String? _statusMessage;
  bool _isSuccess = false;
  String? _lastScannedCode;
  DateTime? _lastScanTime;

  List<dynamic> _activeSessions = [];
  String? _selectedSessionId;
  String? _selectedSessionMethod;
  bool _loadingSessions = true;
  bool _cameraPermissionDenied = false;

  @override
  void initState() {
    super.initState();
    _loadActiveSessions();
  }

  Future<void> _loadActiveSessions() async {
    setState(() {
      _loadingSessions = true;
      _statusMessage = null;
    });
    try {
      final res = await ApiClient.get('/attendance/active-sessions');
      if (res.statusCode == 200) {
        final List<dynamic> sessions = jsonDecode(res.body);
        setState(() {
          _activeSessions = sessions;
          if (sessions.isNotEmpty) {
            _selectedSessionId = sessions.first['sessionId'] as String?;
            _selectedSessionMethod = sessions.first['method'] as String?;
            // Auto-switch mode based on session method
            _autoSwitchMode(_selectedSessionMethod);
          } else {
            _selectedSessionId = null;
            _selectedSessionMethod = null;
          }
        });
      } else {
        setState(() => _statusMessage = 'Failed to load active sessions');
      }
    } catch (e) {
      setState(() => _statusMessage = 'Failed to connect to server for active sessions.');
    } finally {
      setState(() => _loadingSessions = false);
    }
  }

  void _autoSwitchMode(String? method) {
    if (method == null) return;
    if (method == 'face') {
      _mode = CheckInMode.face;
    } else if (method == 'pin') {
      _mode = CheckInMode.pin;
    } else {
      _mode = CheckInMode.qr;
    }
  }

  Future<Position?> _determinePosition() async {
    bool serviceEnabled = await Geolocator.isLocationServiceEnabled();
    if (!serviceEnabled) {
      setState(() { _statusMessage = 'Location services are disabled.'; _isSuccess = false; });
      return null;
    }

    LocationPermission permission = await Geolocator.checkPermission();
    if (permission == LocationPermission.denied) {
      permission = await Geolocator.requestPermission();
      if (permission == LocationPermission.denied) {
        setState(() { _statusMessage = 'Location permissions are denied.'; _isSuccess = false; });
        return null;
      }
    }
    if (permission == LocationPermission.deniedForever) {
      setState(() { _statusMessage = 'Location permissions are permanently denied.'; _isSuccess = false; });
      return null;
    }
    return await Geolocator.getCurrentPosition();
  }

  Future<void> _submitCheckIn(String code) async {
    final trimmedCode = code.trim();
    if (trimmedCode.isEmpty) {
      setState(() { _statusMessage = 'Code is empty.'; _isSuccess = false; });
      return;
    }

    if (_lastScannedCode == trimmedCode && _lastScanTime != null) {
      if (DateTime.now().difference(_lastScanTime!).inSeconds < 2) return;
    }

    if (_selectedSessionId == null) {
      setState(() { _statusMessage = 'No active session selected.'; _isSuccess = false; });
      return;
    }

    setState(() {
      _checkingIn = true;
      _statusMessage = 'Fetching location...';
      _lastScannedCode = trimmedCode;
      _lastScanTime = DateTime.now();
    });

    final position = await _determinePosition();
    if (position == null) { setState(() => _checkingIn = false); return; }

    setState(() => _statusMessage = 'Submitting check-in...');

    try {
      final payload = {
        'sessionId': _selectedSessionId,
        'code': trimmedCode,
        'lat': position.latitude,
        'lng': position.longitude,
      };

      final response = await ApiClient.post('/attendance/check-in', payload);
      final body = jsonDecode(response.body);

      setState(() {
        _isSuccess = response.statusCode == 200;
        _statusMessage = body['message'] ?? (response.statusCode == 200 ? 'Check-in successful!' : 'Check-in failed.');
      });
      if (response.statusCode == 200) _pinController.clear();
    } catch (e) {
      setState(() { _isSuccess = false; _statusMessage = 'Network error: $e'; });
    } finally {
      setState(() => _checkingIn = false);
    }
  }

  Future<void> _submitFaceCheckIn() async {
    if (_selectedSessionId == null) {
      setState(() { _statusMessage = 'No active session selected.'; _isSuccess = false; });
      return;
    }

    setState(() { _checkingIn = true; _statusMessage = 'Opening camera...'; _isSuccess = false; });

    try {
      final picker = ImagePicker();
      final XFile? photo = await picker.pickImage(
        source: ImageSource.camera,
        preferredCameraDevice: CameraDevice.front,
        imageQuality: 70,
        maxWidth: 640,
        maxHeight: 480,
      );

      if (photo == null) {
        setState(() { _checkingIn = false; _statusMessage = 'Camera cancelled.'; });
        return;
      }

      setState(() => _statusMessage = 'Processing face...');

      final bytes = await photo.readAsBytes();
      final base64Image = base64Encode(bytes);

      final payload = {
        'image': base64Image,
        'sessionId': _selectedSessionId,
      };

      final response = await ApiClient.post('/attendance/check-in/face', payload);
      final body = jsonDecode(response.body);

      final status = body['status'] as String?;
      setState(() {
        _isSuccess = status == 'success';
        _statusMessage = body['message'] ??
            (status == 'success' ? 'Face recognized — Checked in!' :
             status == 'no_face' ? 'No face detected. Try again.' :
             'Face not recognized. Try again.');
      });
    } catch (e) {
      setState(() { _isSuccess = false; _statusMessage = 'Error: $e'; });
      debugPrint('Face check-in error: $e');
    } finally {
      setState(() => _checkingIn = false);
    }
  }

  @override
  void dispose() {
    _pinController.dispose();
    super.dispose();
  }

  Widget _modeButton(CheckInMode mode, IconData icon, String label) {
    final active = _mode == mode;
    return Expanded(
      child: ElevatedButton.icon(
        onPressed: () => setState(() { _mode = mode; _statusMessage = null; }),
        icon: Icon(icon, size: 18),
        label: Text(label, overflow: TextOverflow.ellipsis),
        style: ElevatedButton.styleFrom(
          backgroundColor: active ? Colors.blueAccent : const Color(0xFF1E293B),
          foregroundColor: Colors.white,
          padding: const EdgeInsets.symmetric(vertical: 12),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFF0F172A),
      appBar: AppBar(
        title: const Text('Check-in Attendance', style: TextStyle(color: Colors.white, fontWeight: FontWeight.bold)),
        backgroundColor: const Color(0xFF1E293B),
        elevation: 0,
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(24.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            // Instructions
            Container(
              padding: const EdgeInsets.all(16),
              decoration: BoxDecoration(
                color: Colors.blueAccent.withOpacity(0.08),
                border: Border.all(color: Colors.blueAccent.withOpacity(0.3)),
                borderRadius: BorderRadius.circular(12),
              ),
              child: const Row(
                children: [
                  Icon(Icons.info_outline, color: Colors.blueAccent, size: 24),
                  SizedBox(width: 12),
                  Expanded(
                    child: Text(
                      'Ensure you are within the classroom geofence. Use the check-in method your instructor selected.',
                      style: TextStyle(color: Colors.white, fontSize: 13, height: 1.4),
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 24),

            // Active Sessions Selector
            const Text(
              'ACTIVE SESSIONS',
              style: TextStyle(color: Color(0xFFeab308), fontWeight: FontWeight.bold, fontSize: 12, letterSpacing: 1),
            ),
            const SizedBox(height: 8),
            Container(
              padding: const EdgeInsets.all(16),
              decoration: BoxDecoration(
                color: const Color(0xFF1E293B),
                borderRadius: BorderRadius.circular(12),
                border: Border.all(color: Colors.white10),
              ),
              child: _loadingSessions
                  ? const Center(child: SizedBox(width: 24, height: 24, child: CircularProgressIndicator(color: Colors.blueAccent, strokeWidth: 2)))
                  : _activeSessions.isEmpty
                      ? Row(
                          mainAxisAlignment: MainAxisAlignment.spaceBetween,
                          children: [
                            const Expanded(child: Text('No active sessions found.', style: TextStyle(color: Color(0xFF94A3B8), fontSize: 14))),
                            IconButton(icon: const Icon(Icons.refresh, color: Colors.blueAccent), onPressed: _loadActiveSessions),
                          ],
                        )
                      : Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Row(
                              mainAxisAlignment: MainAxisAlignment.spaceBetween,
                              children: [
                                const Text('Select Active Class:', style: TextStyle(color: Color(0xFF94A3B8), fontSize: 13)),
                                IconButton(icon: const Icon(Icons.refresh, color: Colors.blueAccent, size: 20), onPressed: _loadActiveSessions),
                              ],
                            ),
                            const SizedBox(height: 8),
                            DropdownButtonHideUnderline(
                              child: DropdownButton<String>(
                                value: _selectedSessionId,
                                dropdownColor: const Color(0xFF1E293B),
                                isExpanded: true,
                                style: const TextStyle(color: Colors.white, fontWeight: FontWeight.bold),
                                items: _activeSessions.map<DropdownMenuItem<String>>((s) {
                                  final method = s['method'] as String? ?? '';
                                  final methodIcon = method == 'face' ? '🤖' : method == 'qr' ? '📷' : '🔢';
                                  return DropdownMenuItem<String>(
                                    value: s['sessionId'] as String,
                                    child: Text('$methodIcon ${s['courseCode']} - ${s['courseName']}', overflow: TextOverflow.ellipsis),
                                  );
                                }).toList(),
                                onChanged: (value) {
                                  final session = _activeSessions.firstWhere((s) => s['sessionId'] == value, orElse: () => null);
                                  setState(() {
                                    _selectedSessionId = value;
                                    _selectedSessionMethod = session?['method'] as String?;
                                    _autoSwitchMode(_selectedSessionMethod);
                                    _statusMessage = null;
                                  });
                                },
                              ),
                            ),
                            if (_selectedSessionMethod != null)
                              Padding(
                                padding: const EdgeInsets.only(top: 8),
                                child: Text(
                                  'Session method: ${_selectedSessionMethod == 'face' ? '🤖 Face Recognition' : _selectedSessionMethod == 'qr' ? '📷 QR Code' : '🔢 PIN'}',
                                  style: const TextStyle(color: Color(0xFF94A3B8), fontSize: 12),
                                ),
                              ),
                          ],
                        ),
            ),
            const SizedBox(height: 24),

            // Mode Tab Buttons
            Row(
              children: [
                _modeButton(CheckInMode.qr, Icons.qr_code_scanner, 'QR Scan'),
                const SizedBox(width: 8),
                _modeButton(CheckInMode.pin, Icons.pin, 'PIN'),
                const SizedBox(width: 8),
                _modeButton(CheckInMode.face, Icons.face_retouching_natural, 'Face Scan'),
              ],
            ),
            const SizedBox(height: 24),

            // Main Panel
            if (_mode == CheckInMode.qr)
              Container(
                height: 280,
                decoration: BoxDecoration(color: const Color(0xFF1E293B), borderRadius: BorderRadius.circular(16), border: Border.all(color: Colors.white10)),
                clipBehavior: Clip.hardEdge,
                child: _checkingIn
                    ? const Center(child: CircularProgressIndicator(color: Colors.blueAccent))
                    : _cameraPermissionDenied
                        ? const Center(child: Padding(
                            padding: EdgeInsets.all(24),
                            child: Text('Camera permission required for QR scanning.', style: TextStyle(color: Colors.white), textAlign: TextAlign.center),
                          ))
                        : ClipRRect(
                            borderRadius: BorderRadius.circular(16),
                            child: MobileScanner(
                              onDetect: (capture) {
                                final barcodes = capture.barcodes;
                                if (barcodes.isNotEmpty && barcodes.first.rawValue != null) {
                                  _submitCheckIn(barcodes.first.rawValue!);
                                }
                              },
                            ),
                          ),
              )
            else if (_mode == CheckInMode.pin)
              Container(
                padding: const EdgeInsets.all(24),
                decoration: BoxDecoration(color: const Color(0xFF1E293B), borderRadius: BorderRadius.circular(16), border: Border.all(color: Colors.white10)),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    TextField(
                      controller: _pinController,
                      keyboardType: TextInputType.number,
                      textAlign: TextAlign.center,
                      maxLength: 6,
                      style: const TextStyle(color: Colors.white, fontSize: 24, letterSpacing: 8, fontWeight: FontWeight.bold),
                      decoration: const InputDecoration(
                        counterText: '',
                        hintText: '000000',
                        hintStyle: TextStyle(color: Colors.white24),
                        focusedBorder: UnderlineInputBorder(borderSide: BorderSide(color: Colors.blueAccent)),
                        enabledBorder: UnderlineInputBorder(borderSide: BorderSide(color: Colors.white24)),
                      ),
                      onChanged: (_) => setState(() {}),
                    ),
                    const SizedBox(height: 24),
                    ElevatedButton(
                      onPressed: _checkingIn || _pinController.text.length < 6 ? null : () => _submitCheckIn(_pinController.text),
                      style: ElevatedButton.styleFrom(
                        backgroundColor: Colors.blueAccent,
                        padding: const EdgeInsets.symmetric(vertical: 16),
                        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                      ),
                      child: _checkingIn
                          ? const SizedBox(width: 20, height: 20, child: CircularProgressIndicator(color: Colors.white, strokeWidth: 2))
                          : const Text('Submit PIN Code', style: TextStyle(color: Colors.white, fontWeight: FontWeight.bold, fontSize: 15)),
                    ),
                  ],
                ),
              )
            else
              // Face Scan Panel
              Container(
                padding: const EdgeInsets.all(28),
                decoration: BoxDecoration(
                  color: const Color(0xFF1E293B),
                  borderRadius: BorderRadius.circular(16),
                  border: Border.all(color: Colors.indigo.withOpacity(0.4)),
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    const Icon(Icons.face_retouching_natural, size: 64, color: Colors.indigoAccent),
                    const SizedBox(height: 16),
                    const Text(
                      'Face Recognition Check-In',
                      textAlign: TextAlign.center,
                      style: TextStyle(color: Colors.white, fontWeight: FontWeight.bold, fontSize: 16),
                    ),
                    const SizedBox(height: 8),
                    const Text(
                      'Take a selfie with your front camera. Make sure your face is well-lit and clearly visible.',
                      textAlign: TextAlign.center,
                      style: TextStyle(color: Color(0xFF94A3B8), fontSize: 13, height: 1.5),
                    ),
                    const SizedBox(height: 24),
                    ElevatedButton.icon(
                      onPressed: _checkingIn ? null : _submitFaceCheckIn,
                      icon: _checkingIn
                          ? const SizedBox(width: 18, height: 18, child: CircularProgressIndicator(color: Colors.white, strokeWidth: 2))
                          : const Icon(Icons.camera_alt),
                      label: Text(_checkingIn ? _statusMessage ?? 'Processing...' : 'Open Camera & Check In'),
                      style: ElevatedButton.styleFrom(
                        backgroundColor: Colors.indigoAccent,
                        foregroundColor: Colors.white,
                        padding: const EdgeInsets.symmetric(vertical: 16),
                        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                      ),
                    ),
                  ],
                ),
              ),

            const SizedBox(height: 24),

            // Status Message
            if (_statusMessage != null && !_checkingIn)
              Container(
                padding: const EdgeInsets.all(16),
                decoration: BoxDecoration(
                  color: _isSuccess ? Colors.greenAccent.withOpacity(0.08) : Colors.redAccent.withOpacity(0.08),
                  border: Border.all(color: _isSuccess ? Colors.greenAccent : Colors.redAccent),
                  borderRadius: BorderRadius.circular(12),
                ),
                child: Text(
                  _statusMessage!,
                  textAlign: TextAlign.center,
                  style: TextStyle(color: _isSuccess ? Colors.greenAccent : Colors.redAccent, fontWeight: FontWeight.w600),
                ),
              ),
          ],
        ),
      ),
    );
  }
}
