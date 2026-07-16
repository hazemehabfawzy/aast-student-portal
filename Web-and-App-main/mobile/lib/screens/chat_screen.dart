import 'dart:async';
import 'dart:convert';
import 'package:flutter/material.dart';
import '../api/api_client.dart';

class ChatScreen extends StatefulWidget {
  const ChatScreen({super.key});

  @override
  State<ChatScreen> createState() => _ChatScreenState();
}

class _ChatScreenState extends State<ChatScreen> {
  final TextEditingController _controller = TextEditingController();
  final ScrollController _scrollController = ScrollController();
  List<Map<String, dynamic>> _sections = [];
  List<Map<String, dynamic>> _messages = [];
  String? _selectedSectionId;
  bool _loadingSections = true;
  bool _loadingMessages = false;
  bool _sending = false;
  Timer? _pollTimer;

  @override
  void initState() {
    super.initState();
    _loadSections();
  }

  @override
  void dispose() {
    _pollTimer?.cancel();
    _controller.dispose();
    _scrollController.dispose();
    super.dispose();
  }

  Future<void> _loadSections() async {
    setState(() => _loadingSections = true);
    try {
      final res = await ApiClient.get('/students/me/schedule');
      if (res.statusCode == 200) {
        final List<dynamic> data = jsonDecode(res.body);
        final sections = data.map((e) => {
          'id': e['sectionId']?.toString() ?? '',
          'courseCode': e['courseCode'] ?? '',
          'courseName': e['courseName'] ?? '',
          'instructorName': e['instructorName'] ?? '',
        }).where((s) => (s['id'] as String).isNotEmpty).toList();

        setState(() {
          _sections = sections.cast<Map<String, dynamic>>();
          if (_sections.isNotEmpty && _selectedSectionId == null) {
            _selectedSectionId = _sections.first['id'] as String;
          }
        });

        if (_selectedSectionId != null) {
          await _loadMessages(_selectedSectionId!);
          _startPolling();
        }
      }
    } catch (e) {
      debugPrint('Failed to load sections: $e');
    } finally {
      if (mounted) setState(() => _loadingSections = false);
    }
  }

  void _startPolling() {
    _pollTimer?.cancel();
    _pollTimer = Timer.periodic(const Duration(seconds: 5), (_) {
      if (_selectedSectionId != null) {
        _loadMessages(_selectedSectionId!, silent: true);
      }
    });
  }

  Future<void> _loadMessages(String sectionId, {bool silent = false}) async {
    if (!silent) setState(() => _loadingMessages = true);
    try {
      final res = await ApiClient.get('/chat/sections/$sectionId');
      if (res.statusCode == 200) {
        final List<dynamic> data = jsonDecode(res.body);
        setState(() {
          _messages = data.map((m) => {
            'id': m['id']?.toString() ?? '',
            'senderName': m['senderName'] ?? '',
            'senderRole': m['senderRole'] ?? 'student',
            'message': m['message'] ?? '',
            'sentAt': m['sentAt'] ?? '',
          }).cast<Map<String, dynamic>>().toList();
        });
        _scrollToBottom();
      }
    } catch (e) {
      debugPrint('Failed to load messages: $e');
    } finally {
      if (mounted && !silent) setState(() => _loadingMessages = false);
    }
  }

  void _scrollToBottom() {
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (_scrollController.hasClients) {
        _scrollController.animateTo(
          _scrollController.position.maxScrollExtent,
          duration: const Duration(milliseconds: 300),
          curve: Curves.easeOut,
        );
      }
    });
  }

  Future<void> _selectSection(String sectionId) async {
    setState(() => _selectedSectionId = sectionId);
    await _loadMessages(sectionId);
    _startPolling();
  }

  Future<void> _sendMessage(String text) async {
    if (text.trim().isEmpty || _selectedSectionId == null || _sending) return;
    setState(() => _sending = true);
    try {
      final res = await ApiClient.post(
        '/chat/sections/$_selectedSectionId',
        {'message': text.trim()},
      );
      if (res.statusCode == 200) {
        _controller.clear();
        await _loadMessages(_selectedSectionId!, silent: true);
      }
    } catch (e) {
      debugPrint('Failed to send message: $e');
    } finally {
      if (mounted) setState(() => _sending = false);
    }
  }

  String _formatTime(String iso) {
    try {
      return DateTime.parse(iso).toLocal().toString().substring(0, 16);
    } catch (_) {
      return iso;
    }
  }

  Map<String, dynamic>? get _selectedSection {
    if (_selectedSectionId == null) return null;
    try {
      return _sections.firstWhere((s) => s['id'] == _selectedSectionId);
    } catch (_) {
      return null;
    }
  }

  @override
  Widget build(BuildContext context) {
    final selected = _selectedSection;

    return Scaffold(
      backgroundColor: const Color(0xFF0F172A),
      appBar: AppBar(
        title: const Text('Course Chat', style: TextStyle(color: Colors.white, fontWeight: FontWeight.bold)),
        backgroundColor: const Color(0xFF1E293B),
      ),
      body: _loadingSections
          ? const Center(child: CircularProgressIndicator(color: Colors.blueAccent))
          : _sections.isEmpty
              ? const Center(child: Text('No enrolled courses to chat in.', style: TextStyle(color: Color(0xFF94A3B8))))
              : Column(
                  children: [
                    SizedBox(
                      height: 100,
                      child: ListView.builder(
                        scrollDirection: Axis.horizontal,
                        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                        itemCount: _sections.length,
                        itemBuilder: (context, index) {
                          final sec = _sections[index];
                          final isSelected = sec['id'] == _selectedSectionId;
                          return GestureDetector(
                            onTap: () => _selectSection(sec['id'] as String),
                            child: Container(
                              width: 140,
                              margin: const EdgeInsets.only(right: 8),
                              padding: const EdgeInsets.all(10),
                              decoration: BoxDecoration(
                                color: isSelected ? const Color(0xFF1E3A5F) : const Color(0xFF1E293B),
                                borderRadius: BorderRadius.circular(10),
                                border: Border.all(
                                  color: isSelected ? Colors.blueAccent : const Color(0xFF334155),
                                ),
                              ),
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Text(
                                    sec['courseCode'] ?? '',
                                    style: const TextStyle(color: Colors.white, fontWeight: FontWeight.bold, fontSize: 13),
                                  ),
                                  Text(
                                    sec['courseName'] ?? '',
                                    maxLines: 2,
                                    overflow: TextOverflow.ellipsis,
                                    style: const TextStyle(color: Color(0xFF94A3B8), fontSize: 11),
                                  ),
                                ],
                              ),
                            ),
                          );
                        },
                      ),
                    ),
                    if (selected != null)
                      Padding(
                        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 4),
                        child: Align(
                          alignment: Alignment.centerLeft,
                          child: Text(
                            '${selected['courseCode']} — ${selected['courseName']}',
                            style: const TextStyle(color: Colors.white, fontWeight: FontWeight.w600),
                          ),
                        ),
                      ),
                    Expanded(
                      child: _loadingMessages && _messages.isEmpty
                          ? const Center(child: CircularProgressIndicator(color: Colors.blueAccent))
                          : ListView.builder(
                              controller: _scrollController,
                              padding: const EdgeInsets.all(12),
                              itemCount: _messages.length,
                              itemBuilder: (context, index) {
                                final m = _messages[index];
                                final isInstructor = m['senderRole'] == 'instructor';
                                return Container(
                                  margin: const EdgeInsets.symmetric(vertical: 6),
                                  padding: const EdgeInsets.all(12),
                                  decoration: BoxDecoration(
                                    color: const Color(0xFF1E293B),
                                    borderRadius: BorderRadius.circular(10),
                                    border: Border(
                                      left: BorderSide(
                                        color: isInstructor ? Colors.blueAccent : Colors.transparent,
                                        width: 3,
                                      ),
                                    ),
                                  ),
                                  child: Column(
                                    crossAxisAlignment: CrossAxisAlignment.start,
                                    children: [
                                      Row(
                                        children: [
                                          Text(
                                            m['senderName'] ?? '',
                                            style: const TextStyle(color: Colors.white, fontWeight: FontWeight.bold, fontSize: 13),
                                          ),
                                          const SizedBox(width: 8),
                                          Container(
                                            padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                                            decoration: BoxDecoration(
                                              color: isInstructor
                                                  ? Colors.blueAccent.withValues(alpha: 0.2)
                                                  : const Color(0xFF334155),
                                              borderRadius: BorderRadius.circular(4),
                                            ),
                                            child: Text(
                                              m['senderRole'] ?? '',
                                              style: TextStyle(
                                                color: isInstructor ? Colors.blueAccent : const Color(0xFF94A3B8),
                                                fontSize: 10,
                                              ),
                                            ),
                                          ),
                                          const Spacer(),
                                          Text(
                                            _formatTime(m['sentAt'] ?? ''),
                                            style: const TextStyle(color: Color(0xFF64748B), fontSize: 10),
                                          ),
                                        ],
                                      ),
                                      const SizedBox(height: 6),
                                      Text(
                                        m['message'] ?? '',
                                        style: const TextStyle(color: Colors.white, fontSize: 14),
                                      ),
                                    ],
                                  ),
                                );
                              },
                            ),
                    ),
                    SafeArea(
                      child: Container(
                        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                        color: const Color(0xFF0F172A),
                        child: Row(
                          children: [
                            Expanded(
                              child: TextField(
                                controller: _controller,
                                style: const TextStyle(color: Colors.white),
                                decoration: InputDecoration(
                                  hintText: 'Type a message...',
                                  hintStyle: const TextStyle(color: Color(0xFF94A3B8)),
                                  filled: true,
                                  fillColor: const Color(0xFF1E293B),
                                  border: OutlineInputBorder(
                                    borderRadius: BorderRadius.circular(12),
                                    borderSide: BorderSide.none,
                                  ),
                                  contentPadding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
                                ),
                                onSubmitted: _sendMessage,
                              ),
                            ),
                            const SizedBox(width: 8),
                            ElevatedButton(
                              onPressed: _sending ? null : () => _sendMessage(_controller.text),
                              style: ElevatedButton.styleFrom(
                                backgroundColor: Colors.blueAccent,
                                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
                              ),
                              child: _sending
                                  ? const SizedBox(width: 20, height: 20, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
                                  : const Icon(Icons.send, color: Colors.white),
                            ),
                          ],
                        ),
                      ),
                    ),
                  ],
                ),
    );
  }
}
