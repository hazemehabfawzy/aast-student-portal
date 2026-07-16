import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:url_launcher/url_launcher.dart';
import '../api/api_client.dart';
import 'chat_screen.dart';

class DoctorsScreen extends StatefulWidget {
  const DoctorsScreen({super.key});

  @override
  State<DoctorsScreen> createState() => _DoctorsScreenState();
}

class _DoctorsScreenState extends State<DoctorsScreen> {
  bool _loading = true;
  List<Map<String, dynamic>> _doctors = [];

  @override
  void initState() {
    super.initState();
    _fetchDoctors();
  }

  Future<void> _fetchDoctors() async {
    setState(() => _loading = true);
    try {
      final res = await ApiClient.get('/doctors');
      if (res.statusCode == 200) {
        final List<dynamic> list = jsonDecode(res.body);
        _doctors = list.map((e) => Map<String, dynamic>.from(e)).toList();
      } else {
        debugPrint('Failed to fetch doctors: ${res.statusCode}');
      }
    } catch (e) {
      debugPrint('Error fetching doctors: $e');
    } finally {
      setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFF0F172A),
      appBar: AppBar(
        title: const Text('Talk to a Doctor', style: TextStyle(color: Colors.white, fontWeight: FontWeight.bold)),
        backgroundColor: const Color(0xFF1E293B),
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator(color: Colors.blueAccent))
          : _doctors.isEmpty
              ? Center(
                  child: TextButton(
                    onPressed: _fetchDoctors,
                    child: const Text('No doctors found. Retry', style: TextStyle(color: Colors.white)),
                  ),
                )
              : ListView.separated(
                  padding: const EdgeInsets.all(16),
                  itemCount: _doctors.length,
                  separatorBuilder: (_, __) => const SizedBox(height: 12),
                  itemBuilder: (context, index) {
                    final doc = _doctors[index];
                    final name = (doc['fullName'] ?? doc['FullName'] ?? doc['Fullname'] ?? doc['fullname'] ?? doc['name']) as String? ?? 'Doctor';
                    final title = (doc['title'] ?? '') as String;
                    final email = (doc['email'] ?? '') as String?;
                    final phone = (doc['phone'] ?? doc['Phone'] ?? '') as String?;

                    return Container(
                      decoration: BoxDecoration(
                        color: const Color(0xFF1E293B),
                        borderRadius: BorderRadius.circular(12),
                        border: Border.all(color: Colors.white10),
                      ),
                      child: ListTile(
                        contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
                        leading: CircleAvatar(
                          backgroundColor: Colors.blueAccent.withOpacity(0.2),
                          child: Text(name.substring(0, 1), style: const TextStyle(color: Colors.blueAccent, fontWeight: FontWeight.bold)),
                        ),
                        title: Text('$title $name', style: const TextStyle(color: Colors.white, fontWeight: FontWeight.bold)),
                        subtitle: Text(email ?? '', style: const TextStyle(color: Color(0xFF94A3B8))),
                        trailing: Row(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            ElevatedButton.icon(
                              onPressed: () {
                                Navigator.of(context).push(
                                  MaterialPageRoute(builder: (_) => const ChatScreen()),
                                );
                              },
                              icon: const Icon(Icons.chat_bubble_outline, color: Colors.white),
                              label: const Text('Chat', style: TextStyle(color: Colors.white)),
                              style: ElevatedButton.styleFrom(
                                backgroundColor: Colors.blueAccent,
                                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
                              ),
                            ),
                            const SizedBox(width: 8),
                            if (phone != null && phone.isNotEmpty)
                              IconButton(
                                tooltip: 'Call',
                                onPressed: () async {
                                  final uri = Uri(scheme: 'tel', path: phone);
                                  if (await canLaunchUrl(uri)) {
                                    await launchUrl(uri);
                                  } else {
                                    ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Cannot launch phone dialer')));
                                  }
                                },
                                icon: const Icon(Icons.phone, color: Colors.greenAccent),
                              ),
                            if (email != null && email.isNotEmpty)
                              IconButton(
                                tooltip: 'Email',
                                onPressed: () async {
                                  final uri = Uri(scheme: 'mailto', path: email);
                                  if (await canLaunchUrl(uri)) {
                                    await launchUrl(uri);
                                  } else {
                                    ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Cannot open mail client')));
                                  }
                                },
                                icon: const Icon(Icons.email, color: Colors.orangeAccent),
                              ),
                          ],
                        ),
                      ),
                    );
                  },
                ),
    );
  }
}
