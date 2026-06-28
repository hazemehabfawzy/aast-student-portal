import 'package:flutter/foundation.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:firebase_messaging/firebase_messaging.dart';
import 'dart:convert';
import 'package:http/http.dart' as http;
import '../api/api_client.dart';
import '../config.dart';

class AuthService extends ChangeNotifier {
  final FlutterSecureStorage _secureStorage = const FlutterSecureStorage();

  bool _isAuthenticated = false;
  bool get isAuthenticated => _isAuthenticated;

  bool _loading = false;
  bool get loading => _loading;

  String? _errorMessage;
  String? get errorMessage => _errorMessage;

  String? _fullName;
  String? get fullName => _fullName;

  String? _email;
  String? get email => _email;

  String? _username;
  String? get username => _username;

  static String get keycloakRealmUrl => AppConfig.keycloakUrl;
  static String get _clientId => AppConfig.clientId;

  AuthService() {
    checkAuthentication();
  }

  Future<void> checkAuthentication() async {
    _loading = true;
    notifyListeners();

    try {
      final savedToken = await _secureStorage.read(key: 'access_token');
      if (savedToken != null) {
        _parseToken(savedToken);
        _isAuthenticated = true;
        _registerFcmToken();
      }
    } catch (e) {
      debugPrint('Failed to load credentials: $e');
    }

    _loading = false;
    notifyListeners();
  }

  Future<bool> login(String username, String password) async {
    _loading = true;
    _errorMessage = null;
    notifyListeners();

    try {
      final baseUrl = keycloakRealmUrl.replaceAll(RegExp(r'/+$'), '');
      final response = await http.post(
        Uri.parse('$baseUrl/protocol/openid-connect/token'),
        headers: {'Content-Type': 'application/x-www-form-urlencoded'},
        body: {
          'grant_type': 'password',
          'client_id': _clientId,
          'username': username,
          'password': password,
          'scope': 'openid profile email',
        },
      );

      if (response.statusCode == 200) {
        final data = json.decode(response.body);
        final accessToken = data['access_token'];
        
        await _secureStorage.write(key: 'access_token', value: accessToken);
        print('Token saved: ${accessToken?.substring(0, 20)}...');
        if (data['refresh_token'] != null) {
          await _secureStorage.write(key: 'refresh_token', value: data['refresh_token']);
        }

        _parseToken(accessToken);
        _isAuthenticated = true;
        _loading = false;
        notifyListeners();
        
        await _registerFcmToken();
        return true;
      } else {
        final errorData = json.decode(response.body);
        _errorMessage = errorData['error_description'] ?? 'Invalid username or password';
        _isAuthenticated = false;
        _loading = false;
        notifyListeners();
        return false;
      }
    } catch (e) {
      _errorMessage = 'Connection error. Please check your network.';
      _isAuthenticated = false;
      _loading = false;
      notifyListeners();
      return false;
    }
  }

  Future<void> _registerFcmToken() async {
    try {
      await FirebaseMessaging.instance.requestPermission(
        alert: true,
        badge: true,
        sound: true,
      );

      final token = await FirebaseMessaging.instance.getToken();
      if (token != null) {
        await ApiClient.post('/students/me/fcm-token', {
          'token': token,
          'platform': 'android',
        });
        debugPrint('FCM token registered: ${token.substring(0, 20)}...');
      }

      FirebaseMessaging.instance.onTokenRefresh.listen((newToken) {
        ApiClient.post('/students/me/fcm-token', {
          'token': newToken,
          'platform': 'android',
        });
      });

      FirebaseMessaging.onMessage.listen((RemoteMessage message) {
        debugPrint('Push received: ${message.notification?.title}');
      });

    } catch (e) {
      debugPrint('FCM error (non-fatal): $e');
    }
  }

  Future<void> logout() async {
    await _secureStorage.deleteAll();
    _isAuthenticated = false;
    _fullName = null;
    _email = null;
    _username = null;
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
    } catch (e) {
      debugPrint('Error parsing token: $e');
    }
  }
}
