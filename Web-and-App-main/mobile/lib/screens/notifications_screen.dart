import 'dart:convert';
import 'package:flutter/material.dart';
import '../api/api_client.dart';

class NotificationsScreen extends StatefulWidget {
  const NotificationsScreen({super.key});

  @override
  State<NotificationsScreen> createState() => _NotificationsScreenState();
}

class _NotificationsScreenState extends State<NotificationsScreen> {
  List<dynamic> _notifications = [];
  bool _loading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _loadNotifications();
  }

  Future<void> _loadNotifications() async {
    try {
      final res = await ApiClient.get('/students/me/notifications');
      if (res.statusCode == 200) {
        final List<dynamic> data = jsonDecode(res.body);
        // Sort newest first
        data.sort((a, b) => (b['createdAt'] as String).compareTo(a['createdAt'] as String));
        setState(() {
          _notifications = data;
          _error = null;
        });
      } else {
        setState(() {
          _error = 'Failed to load notifications';
        });
      }
    } catch (e) {
      setState(() {
        _error = 'Failed to connect to backend server.';
      });
    } finally {
      setState(() {
        _loading = false;
      });
    }
  }

  Future<void> _markAsRead(String id) async {
    try {
      final res = await ApiClient.put('/notifications/$id/read', {});
      if (res.statusCode == 200) {
        setState(() {
          _notifications = _notifications.map((n) {
            if (n['id'] == id) {
              return {...n, 'isRead': true};
            }
            return n;
          }).toList();
        });
      }
    } catch (e) {
      debugPrint('Error marking as read: $e');
    }
  }

  IconData _getIcon(String type) {
    switch (type) {
      case 'result': return Icons.analytics_outlined;
      case 'attendance_warning': return Icons.warning_amber_rounded;
      default: return Icons.notifications_none;
    }
  }

  Color _getIconColor(String type) {
    switch (type) {
      case 'result': return Colors.emeraldAccent;
      case 'attendance_warning': return Colors.amberAccent;
      default: return Colors.blueAccent;
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFF0F172A),
      appBar: AppBar(
        title: const Text('My Alerts', style: TextStyle(color: Colors.white, fontWeight: FontWeight.bold)),
        backgroundColor: const Color(0xFF1E293B),
        elevation: 0,
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator(color: Colors.blueAccent))
          : _error != null
              ? Center(child: Text(_error!, style: const TextStyle(color: Colors.redAccent, fontSize: 16)))
              : _notifications.isEmpty
                  ? const Center(child: Text('No alerts found.', style: TextStyle(color: Color(0xFF94A3B8))))
                  : RefreshIndicator(
                      onRefresh: _loadNotifications,
                      child: ListView.builder(
                        padding: const EdgeInsets.all(16.0),
                        itemCount: _notifications.length,
                        itemBuilder: (context, index) {
                          final n = _notifications[index];
                          final id = n['id'] as String;
                          final isRead = n['isRead'] as bool? ?? false;
                          final type = n['type'] ?? 'general';

                          return Opacity(
                            opacity: isRead ? 0.6 : 1.0,
                            child: Card(
                              color: const Color(0xFF1E293B),
                              margin: const EdgeInsets.bottom(12),
                              shape: RoundedRectangleBorder(
                                borderRadius: BorderRadius.circular(10),
                                side: BorderSide(
                                  color: isRead ? Colors.transparent : Colors.blueAccent.withOpacity(0.3),
                                ),
                              ),
                              child: ListTile(
                                leading: CircleAvatar(
                                  backgroundColor: _getIconColor(type).withOpacity(0.1),
                                  child: Icon(_getIcon(type), color: _getIconColor(type)),
                                ),
                                title: Row(
                                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                                  children: [
                                    Expanded(
                                      child: Text(
                                        n['title'] ?? '',
                                        style: TextStyle(
                                          color: isRead ? Colors.white70 : Colors.white,
                                          fontWeight: isRead ? FontWeight.normal : FontWeight.bold,
                                        ),
                                      ),
                                    ),
                                    if (!isRead)
                                      IconButton(
                                        icon: const Icon(Icons.check_circle_outline, color: Colors.blueAccent, size: 20),
                                        onPressed: () => _markAsRead(id),
                                      ),
                                  ],
                                ),
                                subtitle: Padding(
                                  padding: const EdgeInsets.only(top: 8.0, bottom: 4.0),
                                  child: Text(
                                    n['body'] ?? '',
                                    style: const TextStyle(color: Color(0xFF94A3B8), fontSize: 14, height: 1.3),
                                  ),
                                ),
                              ),
                            ),
                          );
                        },
                      ),
                    ),
    );
  }
}
