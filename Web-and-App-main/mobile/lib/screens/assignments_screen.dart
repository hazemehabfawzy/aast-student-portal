import 'dart:convert';
import 'package:file_picker/file_picker.dart';
import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import 'package:provider/provider.dart';
import 'package:url_launcher/url_launcher_string.dart';
import '../api/api_client.dart';
import '../services/auth_service.dart';

class AssignmentsScreen extends StatefulWidget {
  const AssignmentsScreen({super.key});

  @override
  State<AssignmentsScreen> createState() => _AssignmentsScreenState();
}

class _AssignmentsScreenState extends State<AssignmentsScreen> {
  bool _loading = true;
  List<dynamic> _assignments = [];
  Map<int, PlatformFile?> _selectedFiles = {};

  @override
  void initState() {
    super.initState();
    _loadAssignments();
  }

  Future<void> _loadAssignments() async {
    setState(() => _loading = true);
    try {
      final res = await ApiClient.get('/assignments');
      if (res.statusCode == 200) {
        _assignments = jsonDecode(res.body);
      }
    } catch (e) {
      debugPrint('Error loading assignments: $e');
    } finally {
      setState(() => _loading = false);
    }
  }

  Future<void> _pickFile(int assignmentId) async {
    final result = await FilePicker.pickFiles(allowMultiple: false);
    if (result == null || result.files.isEmpty) return;

    setState(() {
      _selectedFiles[assignmentId] = result.files.first;
    });
  }

  Future<void> _uploadAttachment(int assignmentId) async {
    final file = _selectedFiles[assignmentId];
    if (file == null || file.path == null) return;

    final multipartFile = await http.MultipartFile.fromPath('file', file.path!, filename: file.name);
    final response = await ApiClient.postMultipart('/assignments/$assignmentId/attachments', [multipartFile]);

    if (response.statusCode == 200) {
      setState(() {
        _selectedFiles[assignmentId] = null;
      });
      await _loadAssignments();
    } else {
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Attachment upload failed')));
    }
  }

  Future<void> _showCreateDialog() async {
    final titleCtrl = TextEditingController();
    final bodyCtrl = TextEditingController();
    DateTime? dueDate;

    await showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Create Assignment'),
        content: SingleChildScrollView(
          child: Column(
            children: [
              TextField(controller: titleCtrl, decoration: const InputDecoration(labelText: 'Title')),
              TextField(controller: bodyCtrl, decoration: const InputDecoration(labelText: 'Body')),
              const SizedBox(height: 8),
              ElevatedButton(
                onPressed: () async {
                  final picked = await showDatePicker(
                    context: ctx,
                    initialDate: DateTime.now(),
                    firstDate: DateTime.now().subtract(const Duration(days: 1)),
                    lastDate: DateTime.now().add(const Duration(days: 365)),
                  );
                  if (picked != null) dueDate = picked;
                },
                child: const Text('Pick Due Date'),
              ),
            ],
          ),
        ),
        actions: [
          TextButton(onPressed: () => Navigator.of(ctx).pop(), child: const Text('Cancel')),
          ElevatedButton(
            onPressed: () async {
              final title = titleCtrl.text.trim();
              if (title.isEmpty) return;
              final payload = {
                'title': title,
                'body': bodyCtrl.text.trim(),
                'dueDate': dueDate?.toIso8601String(),
              };
              try {
                final res = await ApiClient.post('/assignments', payload);
                if (res.statusCode == 200) {
                  Navigator.of(ctx).pop();
                  await _loadAssignments();
                } else {
                  ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Failed to create assignment')));
                }
              } catch (e) {
                ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Network error')));
              }
            },
            child: const Text('Create'),
          ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final auth = Provider.of<AuthService>(context);
    final isInstructor = auth.role == 'instructor';

    return Scaffold(
      backgroundColor: const Color(0xFF0F172A),
      appBar: AppBar(
        title: const Text('Assignments', style: TextStyle(color: Colors.white, fontWeight: FontWeight.bold)),
        backgroundColor: const Color(0xFF1E293B),
        actions: [
          if (isInstructor)
            IconButton(onPressed: _showCreateDialog, icon: const Icon(Icons.add)),
        ],
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator(color: Colors.blueAccent))
          : _assignments.isEmpty
              ? const Center(child: Text('No assignments found.', style: TextStyle(color: Color(0xFF94A3B8))))
              : RefreshIndicator(
                  onRefresh: _loadAssignments,
                  child: ListView.builder(
                    padding: const EdgeInsets.all(16),
                    itemCount: _assignments.length,
                    itemBuilder: (context, index) {
                      final a = _assignments[index];
                      final due = a['dueDate'] != null ? DateTime.parse(a['dueDate']).toLocal().toString().split(' ')[0] : 'No due date';
                      final instructor = a['instructor'] != null ? a['instructor']['fullName'] ?? a['instructor']['FullName'] ?? '' : '';
                      final attachments = a['attachments'] as List<dynamic>? ?? [];

                      return Card(
                        color: const Color(0xFF1E293B),
                        margin: const EdgeInsets.only(bottom: 12),
                        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
                        child: Padding(
                          padding: const EdgeInsets.all(12),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(a['title'] ?? '', style: const TextStyle(color: Colors.white, fontWeight: FontWeight.bold, fontSize: 16)),
                              const SizedBox(height: 6),
                              Text(a['body'] ?? '', style: const TextStyle(color: Color(0xFF94A3B8))),
                              const SizedBox(height: 6),
                              Text('Due: $due', style: const TextStyle(color: Colors.blueAccent)),
                              if (instructor.isNotEmpty) const SizedBox(height: 4),
                              if (instructor.isNotEmpty)
                                Text('By: $instructor', style: const TextStyle(color: Color(0xFF94A3B8), fontSize: 12)),
                              if (attachments.isNotEmpty) ...[
                                const SizedBox(height: 8),
                                const Text('Attachments:', style: TextStyle(color: Colors.white70, fontWeight: FontWeight.w600)),
                                for (var att in attachments)
                                  TextButton(
                                    onPressed: () async {
                                      final url = '${ApiClient.baseUrl}/assignments/${a['id']}/attachments/${att['id']}';
                                      if (await canLaunchUrlString(url)) {
                                        await launchUrlString(url);
                                      }
                                    },
                                    child: Text(att['fileName'] ?? '', style: const TextStyle(color: Colors.lightBlueAccent)),
                                  ),
                              ],
                              if (isInstructor) ...[
                                const Divider(color: Colors.white12),
                                const SizedBox(height: 8),
                                Row(
                                  children: [
                                    Expanded(
                                      child: Text(
                                        _selectedFiles[a['id']]?.name ?? 'No file selected',
                                        style: const TextStyle(color: Colors.white70),
                                      ),
                                    ),
                                    TextButton(
                                      onPressed: () => _pickFile(a['id']),
                                      child: const Text('Choose File'),
                                    ),
                                  ],
                                ),
                                ElevatedButton(
                                  onPressed: _selectedFiles[a['id']] != null ? () => _uploadAttachment(a['id']) : null,
                                  child: const Text('Upload Attachment'),
                                ),
                              ],
                            ],
                          ),
                        ),
                      );
                    },
                  ),
                ),
    );
  }
}
