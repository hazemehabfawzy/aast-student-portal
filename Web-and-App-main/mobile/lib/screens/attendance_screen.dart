import 'package:flutter/material.dart';
import 'package:geolocator/geolocator.dart';
import 'package:mobile_scanner/mobile_scanner.dart';
import 'dart:convert';
import '../api/api_client.dart';

class AttendanceScreen extends StatefulWidget {
  const AttendanceScreen({super.key});

  @override
  State<AttendanceScreen> createState() => _AttendanceScreenState();
}

class _AttendanceScreenState extends State<AttendanceScreen> {
  bool _useScanner = true;
  final TextEditingController _pinController = TextEditingController();
  bool _checkingIn = false;
  String? _statusMessage;
  bool _isSuccess = false;

  List<dynamic> _activeSessions = [];
  String? _selectedSessionId;
  bool _loadingSessions = true;

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
          } else {
            _selectedSessionId = null;
          }
        });
      } else {
        setState(() {
          _statusMessage = 'Failed to load active sessions';
        });
      }
    } catch (e) {
      setState(() {
        _statusMessage = 'Failed to connect to server for active sessions.';
      });
    } finally {
      setState(() {
        _loadingSessions = false;
      });
    }
  }

  Future<Position?> _determinePosition() async {
    bool serviceEnabled;
    LocationPermission permission;

    // Check if location services are enabled.
    serviceEnabled = await Geolocator.isLocationServiceEnabled();
    if (!serviceEnabled) {
      setState(() {
        _statusMessage = 'Location services are disabled. Please enable them.';
        _isSuccess = false;
      });
      return null;
    }

    permission = await Geolocator.checkPermission();
    if (permission == LocationPermission.denied) {
      permission = await Geolocator.requestPermission();
      if (permission == LocationPermission.denied) {
        setState(() {
          _statusMessage = 'Location permissions are denied';
          _isSuccess = false;
        });
        return null;
      }
    }

    if (permission == LocationPermission.deniedForever) {
      setState(() {
        _statusMessage = 'Location permissions are permanently denied, cannot request permissions.';
        _isSuccess = false;
      });
      return null;
    }

    return await Geolocator.getCurrentPosition();
  }

  Future<void> _submitCheckIn(String code) async {
    if (_selectedSessionId == null) {
      setState(() {
        _statusMessage = 'No active class session selected. Please refresh active sessions.';
        _isSuccess = false;
      });
      return;
    }

    setState(() {
      _checkingIn = true;
      _statusMessage = 'Fetching location...';
    });

    final position = await _determinePosition();
    if (position == null) {
      setState(() {
        _checkingIn = false;
      });
      return;
    }

    setState(() {
      _statusMessage = 'Submitting check-in...';
    });

    try {
      final payload = {
        'sessionId': _selectedSessionId,
        'code': code.trim(),
        'lat': position.latitude,
        'lng': position.longitude,
      };

      final response = await ApiClient.post('/attendance/check-in', payload);
      final responseBody = jsonDecode(response.body);

      if (response.statusCode == 200) {
        setState(() {
          _isSuccess = true;
          _statusMessage = responseBody['message'] ?? 'Check-in successful!';
        });
        _pinController.clear();
      } else {
        setState(() {
          _isSuccess = false;
          _statusMessage = responseBody['message'] ?? 'Check-in failed.';
        });
      }
    } catch (e) {
      setState(() {
        _isSuccess = false;
        _statusMessage = 'Network error: Failed to connect to server.';
      });
    } finally {
      setState(() {
        _checkingIn = false;
      });
    }
  }

  @override
  void dispose() {
    _pinController.dispose();
    super.dispose();
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
            // Instructions Card
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
                      'Ensure you are within the classroom geofence before scanning the QR or inputting the session PIN.',
                      style: TextStyle(color: Colors.white, fontSize: 13, height: 1.4),
                    ),
                  )
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
                            const Expanded(
                              child: Text(
                                'No active sessions found.',
                                style: TextStyle(color: Color(0xFF94A3B8), fontSize: 14),
                              ),
                            ),
                            IconButton(
                              icon: const Icon(Icons.refresh, color: Colors.blueAccent),
                              onPressed: _loadActiveSessions,
                            ),
                          ],
                        )
                      : Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Row(
                              mainAxisAlignment: MainAxisAlignment.spaceBetween,
                              children: [
                                const Text(
                                  'Select Active Class:',
                                  style: TextStyle(color: Color(0xFF94A3B8), fontSize: 13),
                                ),
                                IconButton(
                                  icon: const Icon(Icons.refresh, color: Colors.blueAccent, size: 20),
                                  onPressed: _loadActiveSessions,
                                ),
                              ],
                            ),
                            const SizedBox(height: 8),
                            DropdownButtonHideUnderline(
                              child: DropdownButton<String>(
                                value: _selectedSessionId,
                                dropdownColor: const Color(0xFF1E293B),
                                isExpanded: true,
                                style: const TextStyle(color: Colors.white, fontWeight: FontWeight.bold),
                                items: _activeSessions.map<DropdownMenuItem<String>>((session) {
                                  return DropdownMenuItem<String>(
                                    value: session['sessionId'] as String,
                                    child: Text(
                                      '${session['courseCode']} - ${session['courseName']}',
                                      overflow: TextOverflow.ellipsis,
                                    ),
                                  );
                                }).toList(),
                                onChanged: (value) {
                                  setState(() {
                                    _selectedSessionId = value;
                                  });
                                },
                              ),
                            ),
                          ],
                        ),
            ),
            const SizedBox(height: 24),

            // Tab Buttons
            Row(
              children: [
                Expanded(
                  child: ElevatedButton.icon(
                    onPressed: () => setState(() {
                      _useScanner = true;
                      _statusMessage = null;
                    }),
                    icon: const Icon(Icons.qr_code_scanner),
                    label: const Text('QR Scanner'),
                    style: ElevatedButton.styleFrom(
                      backgroundColor: _useScanner ? Colors.blueAccent : const Color(0xFF1E293B),
                      foregroundColor: Colors.white,
                      padding: const EdgeInsets.symmetric(vertical: 14),
                      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
                    ),
                  ),
                ),
                const SizedBox(width: 16),
                Expanded(
                  child: ElevatedButton.icon(
                    onPressed: () => setState(() {
                      _useScanner = false;
                      _statusMessage = null;
                    }),
                    icon: const Icon(Icons.pin),
                    label: const Text('Enter PIN'),
                    style: ElevatedButton.styleFrom(
                      backgroundColor: !_useScanner ? Colors.blueAccent : const Color(0xFF1E293B),
                      foregroundColor: Colors.white,
                      padding: const EdgeInsets.symmetric(vertical: 14),
                      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
                    ),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 24),

            // Main Action Panel
            if (_useScanner)
              Container(
                height: 280,
                decoration: BoxDecoration(
                  color: const Color(0xFF1E293B),
                  borderRadius: BorderRadius.circular(16),
                  border: Border.all(color: Colors.white10),
                ),
                overflow: BoxOverflow.clip,
                child: _checkingIn
                    ? const Center(child: CircularProgressIndicator(color: Colors.blueAccent))
                    : ClipRRect(
                        borderRadius: BorderRadius.circular(16),
                        child: MobileScanner(
                          onDetect: (capture) {
                            final List<Barcode> barcodes = capture.barcodes;
                            if (barcodes.isNotEmpty && barcodes.first.rawValue != null) {
                              final String scannedCode = barcodes.first.rawValue!;
                              _submitCheckIn(scannedCode);
                            }
                          },
                        ),
                      ),
              )
            else
              Container(
                padding: const EdgeInsets.all(24),
                decoration: BoxDecoration(
                  color: const Color(0xFF1E293B),
                  borderRadius: BorderRadius.circular(16),
                  border: Border.all(color: Colors.white10),
                ),
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
                    ),
                    const SizedBox(height: 24),
                    ElevatedButton(
                      onPressed: _checkingIn || _pinController.text.length < 6
                          ? null
                          : () => _submitCheckIn(_pinController.text),
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
              ),

            const SizedBox(height: 24),

            // Status Message Display
            if (_statusMessage != null)
              Container(
                padding: const EdgeInsets.all(16),
                decoration: BoxDecoration(
                  color: _isSuccess ? Colors.emeraldAccent.withOpacity(0.08) : Colors.redAccent.withOpacity(0.08),
                  border: Border.all(color: _isSuccess ? Colors.emeraldAccent : Colors.redAccent),
                  borderRadius: BorderRadius.circular(12),
                ),
                child: Text(
                  _statusMessage!,
                  textAlign: TextAlign.center,
                  style: TextStyle(
                    color: _isSuccess ? Colors.emeraldAccent : Colors.redAccent,
                    fontWeight: FontWeight.w600,
                  ),
                ),
              ),
          ],
        ),
      ),
    );
  }
}
