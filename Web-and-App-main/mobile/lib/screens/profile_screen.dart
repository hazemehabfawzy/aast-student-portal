import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../services/auth_service.dart';
import '../api/api_client.dart';
import 'doctors_screen.dart';

class ProfileScreen extends StatefulWidget {
  const ProfileScreen({super.key});

  @override
  State<ProfileScreen> createState() => _ProfileScreenState();
}

class _ProfileScreenState extends State<ProfileScreen> {
  int _totalCredits = 0;
  double _gpa = 0.0;
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    _loadProfileData();
  }

  Future<void> _loadProfileData() async {
    try {
      final res = await ApiClient.get('/students/me/results');
      if (res.statusCode == 200) {
        final List<dynamic> list = jsonDecode(res.body);
        int totalHours = 0;
        double sumGradePoints = 0.0;
        int gradedHours = 0;

        for (var r in list) {
          final hours = r['creditHours'] ?? 3;
          totalHours += hours as int;

          final grade = r['letterGrade'] as String?;
          if (grade != null) {
            final points = _getGradePoints(grade);
            sumGradePoints += points * hours;
            gradedHours += hours;
          }
        }

        setState(() {
          _totalCredits = totalHours;
          _gpa = gradedHours > 0 ? (sumGradePoints / gradedHours) : 0.0;
        });
      }
    } catch (e) {
      debugPrint('Failed to load profile data: $e');
    } finally {
      setState(() {
        _loading = false;
      });
    }
  }

  double _getGradePoints(String grade) {
    switch (grade.toUpperCase()) {
      case 'A+': case 'A': return 4.0;
      case 'A-': return 3.7;
      case 'B+': return 3.3;
      case 'B': return 3.0;
      case 'B-': return 2.7;
      case 'C+': return 2.3;
      case 'C': return 2.0;
      case 'C-': return 1.7;
      case 'D+': return 1.3;
      case 'D': return 1.0;
      default: return 0.0;
    }
  }

  @override
  Widget build(BuildContext context) {
    final auth = Provider.of<AuthService>(context);

    return Scaffold(
      backgroundColor: const Color(0xFF0F172A),
      appBar: AppBar(
        title: const Text('My Profile', style: TextStyle(color: Colors.white, fontWeight: FontWeight.bold)),
        backgroundColor: const Color(0xFF1E293B),
        elevation: 0,
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator(color: Colors.blueAccent))
          : SingleChildScrollView(
              padding: const EdgeInsets.all(24.0),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  // User Avatar & Name Card
                  Container(
                    padding: const EdgeInsets.all(24),
                    decoration: BoxDecoration(
                      color: const Color(0xFF1E293B),
                      borderRadius: BorderRadius.circular(16),
                      border: Border.all(color: Colors.white10),
                    ),
                    child: Column(
                      children: [
                        CircleAvatar(
                          radius: 40,
                          backgroundColor: Colors.blueAccent.withOpacity(0.2),
                          child: Text(
                            auth.fullName?.substring(0, 1).toUpperCase() ?? 'S',
                            style: const TextStyle(fontSize: 32, fontWeight: FontWeight.bold, color: Colors.blueAccent),
                          ),
                        ),
                        const SizedBox(height: 16),
                        Text(
                          auth.fullName ?? 'Student Name',
                          textAlign: TextAlign.center,
                          style: const TextStyle(fontSize: 22, fontWeight: FontWeight.bold, color: Colors.white),
                        ),
                        const SizedBox(height: 4),
                        Text(
                          'ID: ${auth.username ?? "-"}',
                          style: const TextStyle(fontSize: 14, color: Color(0xFF94A3B8)),
                        ),
                        const SizedBox(height: 8),
                        Text(
                          auth.email ?? '-',
                          style: const TextStyle(fontSize: 14, color: Color(0xFF94A3B8)),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 24),

                  // Academic Summary Card
                  const Text(
                    'ACADEMIC SUMMARY',
                    style: TextStyle(color: Color(0xFFeab308), fontWeight: FontWeight.bold, fontSize: 12, letterSpacing: 1),
                  ),
                  const SizedBox(height: 8),
                  Container(
                    padding: const EdgeInsets.all(20),
                    decoration: BoxDecoration(
                      color: const Color(0xFF1E293B),
                      borderRadius: BorderRadius.circular(16),
                      border: Border.all(color: Colors.white10),
                    ),
                    child: Column(
                      children: [
                        _buildRow('Department', 'Computer Engineering'),
                        const Divider(color: Colors.white10, height: 24),
                        _buildRow('Advisor', 'Dr. Ahmed Khalil'),
                        const Divider(color: Colors.white10, height: 24),
                        _buildRow('Total Hours', '$_totalCredits Credits'),
                        const Divider(color: Colors.white10, height: 24),
                        _buildRow('Est. Cumulative GPA', _gpa.toStringAsFixed(2), color: Colors.greenAccent),
                      ],
                    ),
                  ),
                  const SizedBox(height: 32),

                  // Talk to Doctor Button
                  ElevatedButton.icon(
                    onPressed: () {
                      Navigator.of(context).push(
                        MaterialPageRoute(builder: (_) => const DoctorsScreen()),
                      );
                    },
                    icon: const Icon(Icons.local_hospital_outlined, color: Colors.white),
                    label: const Text('Talk to a Doctor', style: TextStyle(color: Colors.white, fontWeight: FontWeight.bold)),
                    style: ElevatedButton.styleFrom(
                      backgroundColor: Colors.blueAccent.withOpacity(0.12),
                      side: const BorderSide(color: Colors.blueAccent),
                      padding: const EdgeInsets.symmetric(vertical: 16),
                      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                    ),
                  ),

                  const SizedBox(height: 12),

                  // Log Out Button
                  ElevatedButton.icon(
                    onPressed: () => auth.logout(),
                    icon: const Icon(Icons.exit_to_app, color: Colors.white),
                    label: const Text('Log Out', style: TextStyle(color: Colors.white, fontWeight: FontWeight.bold)),
                    style: ElevatedButton.styleFrom(
                      backgroundColor: Colors.redAccent.withOpacity(0.1),
                      side: const BorderSide(color: Colors.redAccent),
                      padding: const EdgeInsets.symmetric(vertical: 16),
                      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                    ),
                  ),
                ],
              ),
            ),
    );
  }

  Widget _buildRow(String label, String value, {Color color = Colors.white}) {
    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: [
        Text(label, style: const TextStyle(color: Color(0xFF94A3B8), fontSize: 15)),
        Text(value, style: TextStyle(color: color, fontWeight: FontWeight.bold, fontSize: 15)),
      ],
    );
  }
}
