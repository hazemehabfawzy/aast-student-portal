import 'package:flutter/foundation.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:http/http.dart' as http;
import 'dart:convert';
import '../api/api_client.dart';
import '../config.dart';

class AuthService extends ChangeNotifier {
  final FlutterSecureStorage _storage = const FlutterSecureStorage();

  bool _isAuthenticated = false;
  bool get isAuthenticated => _isAuthenticated;

  bool _loading = false;
  bool get loading => _loading;

  String? _fullName;
  String? get fullName => _fullName;

  String? _email;
  String? get email => _email;

  String? _username;
  String? get username => _username;

  String? _role;
  String? get role => _role;

  AuthService() {
    checkAuthentication();
  }

  Future<void> checkAuthentication() async {
    _loading = true;
    notifyListeners();

    try {
      final savedToken = await _storage.read(key: 'access_token');
      if (savedToken != null) {
        _parseToken(savedToken);
        ApiClient.instance.setToken(savedToken);
        _isAuthenticated = true;
      }
    } catch (e) {
      debugPrint('Failed to load credentials: $e');
    }

    _loading = false;
    notifyListeners();
  }

  Future<bool> loginWithCredentials(String username, String password) async {
    _loading = true;
    notifyListeners();

    try {
      final response = await http.post(
        Uri.parse(AppConfig.tokenEndpoint),
        headers: {'Content-Type': 'application/x-www-form-urlencoded'},
        body: {
          'grant_type': 'password',
          'client_id': AppConfig.keycloakClientId,
          'username': username,
          'password': password,
        },
      );

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);
        await _storage.write(key: 'access_token', value: data['access_token']);
        await _storage.write(key: 'refresh_token', value: data['refresh_token']);
        _parseToken(data['access_token']);
        ApiClient.instance.setToken(data['access_token']);
        _isAuthenticated = true;
        _loading = false;
        notifyListeners();
        return true;
      }
    } catch (e) {
      debugPrint('Login error: $e');
    }

    _loading = false;
    notifyListeners();
    return false;
  }

  Future<void> logout() async {
    await _storage.delete(key: 'access_token');
    await _storage.delete(key: 'refresh_token');
    ApiClient.instance.clearToken();
    _isAuthenticated = false;
    _fullName = null;
    _email = null;
    _username = null;
    _role = null;
    notifyListeners();
  }

  void _parseToken(String token) {
    try {
      final parts = token.split('.');
      if (parts.length != 3) return;

      final payload = utf8.decode(base64Url.decode(base64Url.normalize(parts[1])));
      final map = jsonDecode(payload);

      _fullName = map['name'] ?? map['preferred_username'] ?? 'Student';
      _email = map['email'];
      _username = map['preferred_username'];

      if (map['role'] != null) {
        _role = map['role'];
      } else if (map['roles'] != null && map['roles'] is List && (map['roles'] as List).isNotEmpty) {
        _role = (map['roles'] as List).first.toString();
      } else if (map['realm_access'] != null && map['realm_access']['roles'] != null) {
        final roles = List<String>.from(map['realm_access']['roles']);
        if (roles.isNotEmpty) _role = roles.first;
      }
    } catch (e) {
      debugPrint('Error parsing token: $e');
    }
  }
}
