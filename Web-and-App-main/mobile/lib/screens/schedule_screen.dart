import 'dart:convert';
import 'package:flutter/material.dart';
import '../api/api_client.dart';

class ScheduleScreen extends StatefulWidget {
  const ScheduleScreen({super.key});

  @override
  State<ScheduleScreen> createState() => _ScheduleScreenState();
}

class _ScheduleScreenState extends State<ScheduleScreen> {
  Map<String, List<dynamic>> _groupedSchedule = {};
  bool _loading = true;
  String? _error;

  final List<String> _daysOrder = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];

  @override
  void initState() {
    super.initState();
    _loadSchedule();
  }

  Future<void> _loadSchedule() async {
    try {
      final res = await ApiClient.get('/students/me/schedule');
      if (res.statusCode == 200) {
        final List<dynamic> enrolled = jsonDecode(res.body);

        final Map<String, List<dynamic>> tempGrouped = {};

        for (var sec in enrolled) {
          try {
            final List<dynamic> scheduleItems = jsonDecode(sec['scheduleJson'] ?? '[]');
            for (var item in scheduleItems) {
              final String? dayStr = item['day'] ?? item['Day'] as String?;
              final int? dayIndex = item['dayOfWeek'] ?? item['DayOfWeek'] as int?;
              String? dayName;

              if (dayIndex != null && dayIndex >= 0 && dayIndex < _daysOrder.length) {
                dayName = _daysOrder[dayIndex];
              } else if (dayStr != null) {
                final normalized = dayStr.trim().toLowerCase();
                if (normalized.startsWith('sun')) dayName = 'Sunday';
                else if (normalized.startsWith('mon')) dayName = 'Monday';
                else if (normalized.startsWith('tue')) dayName = 'Tuesday';
                else if (normalized.startsWith('wed')) dayName = 'Wednesday';
                else if (normalized.startsWith('thu')) dayName = 'Thursday';
                else if (normalized.startsWith('fri')) dayName = 'Friday';
                else if (normalized.startsWith('sat')) dayName = 'Saturday';
              }

              if (dayName != null) {
                tempGrouped.putIfAbsent(dayName, () => []);
                tempGrouped[dayName]!.add({
                  'code': sec['courseCode'] ?? '',
                  'name': sec['courseName'] ?? '',
                  'instructor': sec['instructorName'] ?? '',
                  'start': (item['startTime'] ?? item['StartTime'] as String? ?? '00:00').substring(0, 5),
                  'end': (item['endTime'] ?? item['EndTime'] as String? ?? '00:00').substring(0, 5),
                });
              }
            }
          } catch (e) {
            debugPrint('Failed to parse schedule JSON: $e');
          }
        }

        // Sort lectures in each day by start time
        tempGrouped.forEach((day, lectures) {
          lectures.sort((a, b) => (a['start'] as String).compareTo(b['start'] as String));
        });

        setState(() {
          _groupedSchedule = tempGrouped;
          _error = null;
        });
      } else {
        setState(() {
          _error = 'Failed to load schedule';
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

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFF0F172A),
      appBar: AppBar(
        title: const Text('My Class Timetable', style: TextStyle(color: Colors.white, fontWeight: FontWeight.bold)),
        backgroundColor: const Color(0xFF1E293B),
        elevation: 0,
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator(color: Colors.blueAccent))
          : _error != null
              ? Center(child: Text(_error!, style: const TextStyle(color: Colors.redAccent, fontSize: 16)))
              : _groupedSchedule.isEmpty
                  ? const Center(child: Text('You are not registered in any sections.', style: TextStyle(color: Color(0xFF94A3B8))))
                  : RefreshIndicator(
                      onRefresh: _loadSchedule,
                      child: ListView.builder(
                        padding: const EdgeInsets.all(16.0),
                        itemCount: _daysOrder.length,
                        itemBuilder: (context, index) {
                          final day = _daysOrder[index];
                          final lectures = _groupedSchedule[day] ?? [];
                          if (lectures.isEmpty) return const SizedBox.shrink();

                          return Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Padding(
                                padding: const EdgeInsets.only(left: 8.0, top: 8.0, bottom: 12.0),
                                child: Text(
                                  day,
                                  style: const TextStyle(color: Color(0xFFeab308), fontSize: 18, fontWeight: FontWeight.bold),
                                ),
                              ),
                              ...lectures.map((lec) => Card(
                                    color: const Color(0xFF1E293B),
                                    margin: const EdgeInsets.only(bottom: 12),
                                    shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
                                    child: ListTile(
                                      contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
                                      title: Text(
                                        '${lec['code']} - ${lec['name']}',
                                        style: const TextStyle(color: Colors.white, fontWeight: FontWeight.bold),
                                      ),
                                      subtitle: Padding(
                                        padding: const EdgeInsets.only(top: 6.0),
                                        child: Text(
                                          '👤 ${lec['instructor']}',
                                          style: const TextStyle(color: Color(0xFF94A3B8), fontSize: 13),
                                        ),
                                      ),
                                      trailing: Container(
                                        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
                                        decoration: BoxDecoration(
                                          color: Colors.blueAccent.withOpacity(0.1),
                                          borderRadius: BorderRadius.circular(6),
                                        ),
                                        child: Text(
                                          '${lec['start']} - ${lec['end']}',
                                          style: const TextStyle(color: Colors.blueAccent, fontWeight: FontWeight.bold, fontSize: 13),
                                        ),
                                      ),
                                    ),
                                  )),
                              const SizedBox(height: 16),
                            ],
                          );
                        },
                      ),
                    ),
    );
  }
}
