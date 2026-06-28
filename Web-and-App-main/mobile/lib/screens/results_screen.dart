import 'dart:convert';
import 'package:flutter/material.dart';
import '../api/api_client.dart';

class ResultsScreen extends StatefulWidget {
  const ResultsScreen({super.key});

  @override
  State<ResultsScreen> createState() => _ResultsScreenState();
}

class _ResultsScreenState extends State<ResultsScreen> {
  List<dynamic> _results = [];
  bool _loading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _loadResults();
  }

  Future<void> _loadResults() async {
    try {
      final res = await ApiClient.get('/students/me/results');
      if (res.statusCode == 200) {
        setState(() {
          _results = jsonDecode(res.body);
          _error = null;
        });
      } else {
        setState(() {
          _error = 'Failed to load results (${res.statusCode})';
        });
      }
    } catch (e) {
      setState(() {
        _error = 'Failed to connect to the backend server.';
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
        title: const Text('Academic Results', style: TextStyle(color: Colors.white, fontWeight: FontWeight.bold)),
        backgroundColor: const Color(0xFF1E293B),
        elevation: 0,
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator(color: Colors.blueAccent))
          : _error != null
              ? Center(child: Text(_error!, style: const TextStyle(color: Colors.redAccent, fontSize: 16)))
              : _results.isEmpty
                  ? const Center(child: Text('No results published or enrolled sections.', style: TextStyle(color: Color(0xFF94A3B8))))
                  : RefreshIndicator(
                      onRefresh: _loadResults,
                      child: ListView.builder(
                        padding: const EdgeInsets.all(16.0),
                        itemCount: _results.length,
                        itemBuilder: (context, index) {
                          final r = _results[index];
                          final code = r['courseCode'] ?? '';
                          final name = r['courseName'] ?? '';
                          final grade = r['letterGrade'] as String?;
                          final total = r['totalScore'] as num?;

                          return Card(
                            color: const Color(0xFF1E293B),
                            margin: const EdgeInsets.only(bottom: 16),
                            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                            child: Padding(
                              padding: const EdgeInsets.all(16.0),
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Row(
                                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                                    children: [
                                      Expanded(
                                        child: Column(
                                          crossAxisAlignment: CrossAxisAlignment.start,
                                          children: [
                                            Text(code, style: const TextStyle(color: Color(0xFFeab308), fontWeight: FontWeight.bold, fontSize: 16)),
                                            const SizedBox(height: 4),
                                            Text(name, style: const TextStyle(color: Colors.white, fontWeight: FontWeight.w600, fontSize: 14)),
                                          ],
                                        ),
                                      ),
                                      Container(
                                        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 6),
                                        decoration: BoxDecoration(
                                          color: grade != null && !grade.startsWith('F') 
                                              ? Colors.greenAccent.withOpacity(0.1)
                                              : Colors.redAccent.withOpacity(0.1),
                                          borderRadius: BorderRadius.circular(6),
                                          border: Border.all(
                                            color: grade != null && !grade.startsWith('F') 
                                                ? Colors.greenAccent
                                                : Colors.redAccent,
                                          ),
                                        ),
                                        child: Text(
                                          grade ?? 'Pending',
                                          style: TextStyle(
                                            color: grade != null && !grade.startsWith('F') 
                                                ? Colors.greenAccent
                                                : Colors.redAccent,
                                            fontWeight: FontWeight.bold,
                                          ),
                                        ),
                                      ),
                                    ],
                                  ),
                                  const Divider(color: Colors.white10, height: 24),
                                  Row(
                                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                                    children: [
                                      _buildScoreCol('Wk 7', r['week7Score']),
                                      _buildScoreCol('Wk 12', r['week12Score']),
                                      _buildScoreCol('Class', r['prefinalScore']),
                                      _buildScoreCol('Final', r['finalScore']),
                                      _buildScoreCol('Total', total, isBold: true),
                                    ],
                                  ),
                                ],
                              ),
                            ),
                          );
                        },
                      ),
                    ),
    );
  }

  Widget _buildScoreCol(String label, dynamic score, {bool isBold = false}) {
    return Column(
      children: [
        Text(label, style: const TextStyle(color: Color(0xFF94A3B8), fontSize: 12)),
        const SizedBox(height: 4),
        Text(
          score != null ? score.toString() : '-',
          style: TextStyle(
            color: isBold ? Colors.blueAccent : Colors.white,
            fontWeight: isBold ? FontWeight.bold : FontWeight.normal,
            fontSize: 14,
          ),
        ),
      ],
    );
  }
}
