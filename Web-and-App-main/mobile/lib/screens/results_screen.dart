import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import '../api/api_client.dart';
import '../config.dart';

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
        final body = jsonDecode(res.body);
        setState(() {
          // Response is { cumulativeGpa, academicStanding, results: [...] }
          _results = (body is Map && body['results'] != null)
              ? body['results'] as List<dynamic>
              : (body is List ? body : []);
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

  Future<Map<String, dynamic>?> _getPrediction(
      double week7, double week12, double prefinal) async {
    try {
      final predictionUrl =
          '${AppConfig.apiBaseUrl.replaceAll('/api', '')}:8001/predict'
              .replaceAll(':5000:8001', ':8001');
      final response = await http.post(
        Uri.parse(predictionUrl),
        headers: {'Content-Type': 'application/json'},
        body: jsonEncode({
          'week7Score': week7,
          'week12Score': week12,
          'prefinalScore': prefinal,
        }),
      );
      if (response.statusCode == 200) {
        return jsonDecode(response.body) as Map<String, dynamic>;
      }
    } catch (e) {
      debugPrint('Prediction service unavailable: $e');
    }
    return null;
  }

  Widget _predictionBadge(Map<String, dynamic> pred) {
    final atRisk = pred['atRisk'] as bool? ?? false;
    final finalRange = pred['finalRange'] as Map<String, dynamic>?;
    final totalRange = pred['totalRange'] as Map<String, dynamic>?;
    final finalLow  = finalRange?['low']  ?? 0;
    final finalHigh = finalRange?['high'] ?? 0;
    final totalLow  = totalRange?['low']  ?? 0;
    final totalHigh = totalRange?['high'] ?? 0;

    return Container(
      margin: const EdgeInsets.only(top: 8),
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
      decoration: BoxDecoration(
        color: atRisk
            ? Colors.red.withOpacity(0.1)
            : Colors.blue.withOpacity(0.1),
        border: Border.all(
            color: atRisk ? Colors.red : Colors.blue, width: 1),
        borderRadius: BorderRadius.circular(8),
      ),
      child: Row(children: [
        Text(atRisk ? '⚠️' : '📊', style: const TextStyle(fontSize: 16)),
        const SizedBox(width: 8),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                'AI Prediction',
                style: TextStyle(
                    fontSize: 12,
                    fontWeight: FontWeight.bold,
                    color: atRisk ? Colors.red : Colors.blue),
              ),
              Text(
                'Predicted Final: $finalLow–$finalHigh / 40',
                style: const TextStyle(fontSize: 12, color: Colors.white70),
              ),
              Text(
                'Predicted Total: $totalLow–$totalHigh / 100',
                style: const TextStyle(fontSize: 12, color: Colors.white70),
              ),
              if (atRisk)
                const Text(
                  '⚠ Risk of Auto-F — predicted final below 12',
                  style: TextStyle(fontSize: 11, color: Colors.red),
                ),
            ],
          ),
        ),
      ]),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFF0F172A),
      appBar: AppBar(
        title: const Text('Academic Results',
            style: TextStyle(color: Colors.white, fontWeight: FontWeight.bold)),
        backgroundColor: const Color(0xFF1E293B),
        elevation: 0,
      ),
      body: _loading
          ? const Center(
              child: CircularProgressIndicator(color: Colors.blueAccent))
          : _error != null
              ? Center(
                  child: Text(_error!,
                      style: const TextStyle(
                          color: Colors.redAccent, fontSize: 16)))
              : _results.isEmpty
                  ? const Center(
                      child: Text(
                          'No results published or enrolled sections.',
                          style: TextStyle(color: Color(0xFF94A3B8))))
                  : RefreshIndicator(
                      onRefresh: _loadResults,
                      child: ListView.builder(
                        padding: const EdgeInsets.all(16.0),
                        itemCount: _results.length,
                        itemBuilder: (context, index) {
                          final r = _results[index] as Map<String, dynamic>;
                          final code  = r['courseCode'] as String? ?? '';
                          final name  = r['courseName'] as String? ?? '';
                          final grade = r['letterGrade'] as String?;
                          final total = r['totalScore'] as num?;

                          // Prediction from backend response (preferred)
                          final predMap =
                              r['prediction'] as Map<String, dynamic>?;

                          return Card(
                            color: const Color(0xFF1E293B),
                            margin: const EdgeInsets.only(bottom: 16),
                            shape: RoundedRectangleBorder(
                                borderRadius: BorderRadius.circular(12)),
                            child: Padding(
                              padding: const EdgeInsets.all(16.0),
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Row(
                                    mainAxisAlignment:
                                        MainAxisAlignment.spaceBetween,
                                    children: [
                                      Expanded(
                                        child: Column(
                                          crossAxisAlignment:
                                              CrossAxisAlignment.start,
                                          children: [
                                            Text(code,
                                                style: const TextStyle(
                                                    color: Color(0xFFeab308),
                                                    fontWeight: FontWeight.bold,
                                                    fontSize: 16)),
                                            const SizedBox(height: 4),
                                            Text(name,
                                                style: const TextStyle(
                                                    color: Colors.white,
                                                    fontWeight: FontWeight.w600,
                                                    fontSize: 14)),
                                          ],
                                        ),
                                      ),
                                      Container(
                                        padding: const EdgeInsets.symmetric(
                                            horizontal: 10, vertical: 6),
                                        decoration: BoxDecoration(
                                          color: grade != null &&
                                                  !grade.startsWith('F')
                                              ? Colors.greenAccent
                                                  .withOpacity(0.1)
                                              : Colors.redAccent
                                                  .withOpacity(0.1),
                                          borderRadius:
                                              BorderRadius.circular(6),
                                          border: Border.all(
                                            color: grade != null &&
                                                    !grade.startsWith('F')
                                                ? Colors.greenAccent
                                                : Colors.redAccent,
                                          ),
                                        ),
                                        child: Text(
                                          grade ?? 'Pending',
                                          style: TextStyle(
                                            color: grade != null &&
                                                    !grade.startsWith('F')
                                                ? Colors.greenAccent
                                                : Colors.redAccent,
                                            fontWeight: FontWeight.bold,
                                          ),
                                        ),
                                      ),
                                    ],
                                  ),
                                  const Divider(
                                      color: Colors.white10, height: 24),
                                  Row(
                                    mainAxisAlignment:
                                        MainAxisAlignment.spaceBetween,
                                    children: [
                                      _buildScoreCol('7th', r['week7Score']),
                                      _buildScoreCol('12th', r['week12Score']),
                                      _buildScoreCol(
                                          'C.Work', r['prefinalScore']),
                                      _buildScoreCol('Final', r['finalScore']),
                                      _buildScoreCol('Total', total,
                                          isBold: true),
                                    ],
                                  ),
                                  // AI Prediction badge
                                  if (predMap != null &&
                                      r['finalScore'] == null)
                                    _predictionBadge(predMap),
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
        Text(label,
            style: const TextStyle(color: Color(0xFF94A3B8), fontSize: 12)),
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
