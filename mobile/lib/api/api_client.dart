import 'dart:convert';
import 'package:http/http.dart' as http;
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import '../config.dart';

class ApiClient {
  static final String baseUrl = AppConfig.apiBaseUrl;
  static const _storage = FlutterSecureStorage();

  static Future<String?> _getToken() async {
    return await _storage.read(key: 'access_token');
  }

  static Future<Map<String, String>> _getHeaders() async {
    final token = await _getToken();
    return {
      'Content-Type': 'application/json',
      'X-Client-Platform': 'mobile',
      if (token != null) 'Authorization': 'Bearer $token',
    };
  }

  static Future<http.Response> _sendRequest(
    String method,
    String path, {
    Map<String, dynamic>? body,
  }) async {
    final uri = Uri.parse('$baseUrl$path');
    var headers = await _getHeaders();
    
    http.Response response;
    if (method == 'GET') {
      response = await http.get(uri, headers: headers);
    } else if (method == 'POST') {
      response = await http.post(uri, headers: headers, body: jsonEncode(body));
    } else if (method == 'PUT') {
      response = await http.put(uri, headers: headers, body: jsonEncode(body));
    } else {
      response = await http.delete(uri, headers: headers);
    }

    if (response.statusCode == 401) {
      // Try refresh
      final newToken = await _refreshToken();
      if (newToken != null) {
        // Retry with new token
        headers = {
          'Content-Type': 'application/json',
          'X-Client-Platform': 'mobile',
          'Authorization': 'Bearer $newToken',
        };
        if (method == 'GET') {
          response = await http.get(uri, headers: headers);
        } else if (method == 'POST') {
          response = await http.post(uri, headers: headers, body: jsonEncode(body));
        } else if (method == 'PUT') {
          response = await http.put(uri, headers: headers, body: jsonEncode(body));
        } else {
          response = await http.delete(uri, headers: headers);
        }
      }
    }
    return response;
  }

  static Future<String?> _refreshToken() async {
    try {
      final refreshToken = await _storage.read(key: 'refresh_token');
      if (refreshToken == null) return null;
      
      final response = await http.post(
        Uri.parse('${AppConfig.keycloakUrl}/protocol/openid-connect/token'),
        headers: {'Content-Type': 'application/x-www-form-urlencoded'},
        body: {
          'grant_type': 'refresh_token',
          'client_id': AppConfig.clientId,
          'refresh_token': refreshToken,
        },
      );
      
      if (response.statusCode == 200) {
        final data = json.decode(response.body);
        await _storage.write(
            key: 'access_token', value: data['access_token']);
        await _storage.write(
            key: 'refresh_token', value: data['refresh_token']);
        return data['access_token'];
      }
      
      // Refresh failed - clear and force re-login
      await _storage.deleteAll();
      return null;
    } catch (e) {
      return null;
    }
  }

  static Future<http.Response> get(String path) async {
    return await _sendRequest('GET', path);
  }

  static Future<http.Response> post(String path, Map<String, dynamic> body) async {
    return await _sendRequest('POST', path, body: body);
  }

  static Future<http.Response> put(String path, Map<String, dynamic> body) async {
    return await _sendRequest('PUT', path, body: body);
  }

  static Future<http.Response> delete(String path) async {
    return await _sendRequest('DELETE', path);
  }
}
