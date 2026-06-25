import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../services/auth_service.dart';
import '../api/api_client.dart';

class HomeScreen extends StatefulWidget {
  final Function(int) onNavigate;
  const HomeScreen({super.key, required this.onNavigate});

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  bool _isDarkMode = true;
  String _language = 'EN';
  List<dynamic> _todaySchedule = [];
  bool _isLoading = true;

  @override
  void initState() {
    super.initState();
    _loadTodaySchedule();
  }

  Future<void> _loadTodaySchedule() async {
    try {
      final res = await ApiClient.get('/students/me/schedule');
      if (res.statusCode == 200) {
        final List<dynamic> schedule = jsonDecode(res.body);
        final today = _getTodayName();
        
        final todaySections = schedule.where((section) {
          final schedJson = section['scheduleJson'];
          if (schedJson == null) return false;
          try {
            final slots = schedJson is String
                ? jsonDecode(schedJson)
                : schedJson;
            return (slots as List).any((slot) =>
                slot['day']?.toString().toLowerCase() ==
                today.toLowerCase() ||
                slot['Day']?.toString().toLowerCase() ==
                today.toLowerCase());
          } catch (_) {
            return false;
          }
        }).toList();

        setState(() {
          _todaySchedule = todaySections;
          _isLoading = false;
        });
      } else {
        setState(() => _isLoading = false);
      }
    } catch (e) {
      setState(() => _isLoading = false);
    }
  }

  String _getTodayName() {
    const days = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];
    return days[DateTime.now().weekday - 1];
  }

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthService>();
    final studentName = auth.fullName ?? 'Student';
    final bgColor = _isDarkMode 
        ? const Color(0xFF0D1B2A) 
        : const Color(0xFFF5F7FA);
    final cardColor = _isDarkMode 
        ? const Color(0xFF1A2F45) 
        : Colors.white;
    final textColor = _isDarkMode ? Colors.white : Colors.black87;
    final subColor = _isDarkMode 
        ? const Color(0xFF8AAAC8) 
        : Colors.black54;

    return Scaffold(
      backgroundColor: bgColor,
      appBar: AppBar(
        backgroundColor: _isDarkMode 
            ? const Color(0xFF0D1B2A) 
            : const Color(0xFF1A3A8C),
        elevation: 0,
        title: Row(
          children: [
            // Avatar
            GestureDetector(
              onTap: () => _showProfile(context, auth),
              child: CircleAvatar(
                radius: 18,
                backgroundColor: const Color(0xFF2A4A8A),
                child: Text(
                  studentName.isNotEmpty ? studentName[0].toUpperCase() : 'S',
                  style: const TextStyle(
                    color: Colors.white,
                    fontWeight: FontWeight.bold,
                  ),
                ),
              ),
            ),
            const SizedBox(width: 10),
            Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  'Hi, $studentName',
                  style: const TextStyle(
                    color: Colors.white,
                    fontSize: 14,
                    fontWeight: FontWeight.bold,
                  ),
                ),
              ],
            ),
          ],
        ),
        actions: [
          // Dark mode toggle
          IconButton(
            icon: Icon(
              _isDarkMode ? Icons.light_mode : Icons.dark_mode,
              color: Colors.white,
            ),
            onPressed: () => setState(
              () => _isDarkMode = !_isDarkMode
            ),
          ),
          // Language toggle
          GestureDetector(
            onTap: () => setState(() => 
              _language = _language == 'EN' ? 'AR' : 'EN'
            ),
            child: Container(
              margin: const EdgeInsets.only(right: 12),
              padding: const EdgeInsets.symmetric(
                horizontal: 8, vertical: 4
              ),
              decoration: BoxDecoration(
                border: Border.all(color: Colors.white54),
                borderRadius: BorderRadius.circular(6),
              ),
              child: Text(
                _language,
                style: const TextStyle(
                  color: Colors.white,
                  fontSize: 12,
                  fontWeight: FontWeight.bold,
                ),
              ),
            ),
          ),
        ],
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Activities Section
            Text(
              'Activities',
              style: TextStyle(
                color: textColor,
                fontSize: 18,
                fontWeight: FontWeight.bold,
              ),
            ),
            const SizedBox(height: 12),
            
            // Activity buttons grid
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                _activityButton(
                  icon: Icons.bar_chart,
                  label: 'Results',
                  color: const Color(0xFF4A90E2),
                  cardColor: cardColor,
                  textColor: textColor,
                  onTap: () => widget.onNavigate(1),
                ),
                _activityButton(
                  icon: Icons.calendar_today,
                  label: 'Schedule',
                  color: const Color(0xFF4CAF50),
                  cardColor: cardColor,
                  textColor: textColor,
                  onTap: () => widget.onNavigate(3),
                ),
                _activityButton(
                  icon: Icons.qr_code_scanner,
                  label: 'Attendance',
                  color: const Color(0xFFFF9800),
                  cardColor: cardColor,
                  textColor: textColor,
                  onTap: () => widget.onNavigate(2),
                ),
                _activityButton(
                  icon: Icons.notifications_outlined,
                  label: 'Alerts',
                  color: const Color(0xFFF44336),
                  cardColor: cardColor,
                  textColor: textColor,
                  onTap: () => widget.onNavigate(4),
                ),
              ],
            ),
            const SizedBox(height: 24),
            
            // Today's Schedule
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(
                  'Today\'s Schedule',
                  style: TextStyle(
                    color: textColor,
                    fontSize: 18,
                    fontWeight: FontWeight.bold,
                  ),
                ),
                Text(
                  _getFormattedDate(),
                  style: const TextStyle(
                    color: Color(0xFF4A90E2),
                    fontSize: 13,
                    fontWeight: FontWeight.w500,
                  ),
                ),
              ],
            ),
            const SizedBox(height: 12),
            
            if (_isLoading)
              const Center(child: CircularProgressIndicator())
            else if (_todaySchedule.isEmpty)
              Container(
                padding: const EdgeInsets.all(20),
                decoration: BoxDecoration(
                  color: cardColor,
                  borderRadius: BorderRadius.circular(12),
                ),
                child: Center(
                  child: Text(
                    'No classes today',
                    style: TextStyle(color: subColor),
                  ),
                ),
              )
            else
              ..._todaySchedule.map((section) => 
                _scheduleCard(section, cardColor, textColor, subColor)
              ).toList(),
          ],
        ),
      ),
    );
  }

  Widget _activityButton({
    required IconData icon,
    required String label,
    required Color color,
    required Color cardColor,
    required Color textColor,
    required VoidCallback onTap,
  }) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        width: 78,
        padding: const EdgeInsets.symmetric(vertical: 16),
        decoration: BoxDecoration(
          color: cardColor,
          borderRadius: BorderRadius.circular(12),
          boxShadow: [
            BoxShadow(
              color: Colors.black.withOpacity(0.1),
              blurRadius: 4,
              offset: const Offset(0, 2),
            ),
          ],
        ),
        child: Column(
          children: [
            Container(
              padding: const EdgeInsets.all(10),
              decoration: BoxDecoration(
                color: color.withOpacity(0.15),
                borderRadius: BorderRadius.circular(10),
              ),
              child: Icon(icon, color: color, size: 26),
            ),
            const SizedBox(height: 8),
            Text(
              label,
              style: TextStyle(
                color: textColor,
                fontSize: 11,
                fontWeight: FontWeight.w500,
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _scheduleCard(
    dynamic section,
    Color cardColor,
    Color textColor,
    Color subColor,
  ) {
    final schedJson = section['scheduleJson'];
    String timeStr = '';
    try {
      final slots = schedJson is String ? jsonDecode(schedJson) : schedJson;
      final today = _getTodayName();
      final todaySlot = (slots as List).firstWhere(
        (s) => s['day']?.toString().toLowerCase() == today.toLowerCase() ||
               s['Day']?.toString().toLowerCase() == today.toLowerCase(),
        orElse: () => null,
      );
      if (todaySlot != null) {
        timeStr = '${(todaySlot['startTime'] ?? todaySlot['StartTime'] ?? '').substring(0, 5)} - ${(todaySlot['endTime'] ?? todaySlot['EndTime'] ?? '').substring(0, 5)}';
      }
    } catch (_) {}

    return Container(
      margin: const EdgeInsets.only(bottom: 12),
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: cardColor,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(
          color: const Color(0xFF4A90E2).withOpacity(0.3)
        ),
      ),
      child: Row(
        children: [
          // Time indicator
          Column(
            children: [
              Container(
                width: 8,
                height: 8,
                decoration: const BoxDecoration(
                  color: Color(0xFF4A90E2),
                  shape: BoxShape.circle,
                ),
              ),
              Container(
                width: 2,
                height: 40,
                color: const Color(0xFF4A90E2).withOpacity(0.3),
              ),
            ],
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  timeStr,
                  style: const TextStyle(
                    color: Color(0xFF4A90E2),
                    fontSize: 12,
                    fontWeight: FontWeight.w500,
                  ),
                ),
                const SizedBox(height: 4),
                Text(
                  '${section['courseCode'] ?? ''} - ${section['courseName'] ?? ''}',
                  style: TextStyle(
                    color: textColor,
                    fontSize: 14,
                    fontWeight: FontWeight.bold,
                  ),
                ),
                Text(
                  'Instructor: ${section['instructorName'] ?? ''}',
                  style: TextStyle(
                    color: subColor,
                    fontSize: 12,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  String _getFormattedDate() {
    final now = DateTime.now();
    const months = ['Jan','Feb','Mar','Apr','May','Jun',
                    'Jul','Aug','Sep','Oct','Nov','Dec'];
    const days = ['Monday','Tuesday','Wednesday','Thursday',
                  'Friday','Saturday','Sunday'];
    return '${days[now.weekday-1]}, ${now.day} ${months[now.month-1]}';
  }

  void _showProfile(BuildContext context, AuthService auth) {
    showDialog(
      context: context,
      builder: (_) => Dialog(
        backgroundColor: const Color(0xFF1A2F45),
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(16),
        ),
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              CircleAvatar(
                radius: 40,
                backgroundColor: const Color(0xFF1A3A5C),
                child: Text(
                  auth.fullName != null && auth.fullName!.isNotEmpty
                      ? auth.fullName![0].toUpperCase()
                      : 'S',
                  style: const TextStyle(
                    color: Color(0xFF4A90E2),
                    fontSize: 36,
                    fontWeight: FontWeight.bold,
                  ),
                ),
              ),
              const SizedBox(height: 16),
              Text(
                auth.fullName ?? 'Student Name',
                style: const TextStyle(
                  color: Colors.white,
                  fontSize: 18,
                  fontWeight: FontWeight.bold,
                ),
              ),
              const SizedBox(height: 4),
              Text(
                auth.email ?? '',
                style: const TextStyle(
                  color: Color(0xFF8AAAC8),
                  fontSize: 13,
                ),
              ),
              const SizedBox(height: 16),
              const Divider(color: Color(0xFF2A4A6A)),
              const SizedBox(height: 16),
              _infoRow(Icons.badge, 'Student ID', auth.username ?? '-'),
              _infoRow(Icons.school, 'Department', 'Computer Engineering'),
              _infoRow(Icons.calendar_today, 'Year Level', 'Year 3'),
              _infoRow(Icons.phone, 'Phone', 'N/A'),
              const SizedBox(height: 16),
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  TextButton(
                    onPressed: () => Navigator.pop(context),
                    child: const Text('Close'),
                  ),
                  TextButton(
                    onPressed: () {
                      Navigator.pop(context);
                      auth.logout();
                    },
                    style: TextButton.styleFrom(foregroundColor: Colors.red),
                    child: const Text('Logout'),
                  ),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _infoRow(IconData icon, String label, String value) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 6),
      child: Row(
        children: [
          Icon(icon, color: const Color(0xFF4A90E2), size: 18),
          const SizedBox(width: 12),
          Text(
            label,
            style: const TextStyle(
              color: Color(0xFF8AAAC8),
              fontSize: 13,
            ),
          ),
          const Spacer(),
          Text(
            value,
            style: const TextStyle(
              color: Colors.white,
              fontSize: 13,
              fontWeight: FontWeight.w500,
            ),
          ),
        ],
      ),
    );
  }
}
