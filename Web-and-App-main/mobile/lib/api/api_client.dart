import 'dart:convert';
import 'package:http/http.dart' as http;
import '../config.dart';

class ApiClient {
  static String baseUrl = AppConfig.apiBaseUrl;
  static String? token;

  static Map<String, String> get _headers => {
    'Content-Type': 'application/json',
    'X-Client-Platform': 'mobile',
    if (token != null) 'Authorization': 'Bearer $token',
  };

  static Map<String, String> get _multipartHeaders {
    return {
      'X-Client-Platform': 'mobile',
      if (token != null) 'Authorization': 'Bearer $token',
    };
  }

  static Future<http.Response> get(String path) async {
    final uri = Uri.parse('$baseUrl$path');
    return await http.get(uri, headers: _headers);
  }

  static Future<http.Response> post(String path, Map<String, dynamic> body) async {
    final uri = Uri.parse('$baseUrl$path');
    return await http.post(
      uri,
      headers: _headers,
      body: jsonEncode(body),
    );
  }

  static Future<http.StreamedResponse> postMultipart(String path, List<http.MultipartFile> files) async {
    final uri = Uri.parse('$baseUrl$path');
    final request = http.MultipartRequest('POST', uri);
    request.headers.addAll(_multipartHeaders);
    request.files.addAll(files);
    return await request.send();
  }

  static Future<http.Response> put(String path, Map<String, dynamic> body) async {
    final uri = Uri.parse('$baseUrl$path');
    return await http.put(
      uri,
      headers: _headers,
      body: jsonEncode(body),
    );
  }

  static Future<http.Response> delete(String path) async {
    final uri = Uri.parse('$baseUrl$path');
    return await http.delete(uri, headers: _headers);
  }
}
