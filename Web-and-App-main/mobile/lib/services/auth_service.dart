import 'package:flutter/foundation.dart';
import 'package:flutter_appauth/flutter_appauth.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'dart:convert';
import '../api/api_client.dart';
import '../config.dart';

class AuthService extends ChangeNotifier {
  final FlutterAppAuth _appAuth = const FlutterAppAuth();
  final FlutterSecureStorage _secureStorage = const FlutterSecureStorage();

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

  // Keycloak Details (Public Client with PKCE)
  static String keycloakRealmUrl = AppConfig.keycloakRealmUrl;
  static const String _clientId = AppConfig.keycloakClientId;
  static const String _redirectUrl = AppConfig.keycloakRedirectUrl;

  AuthService() {
    checkAuthentication();
  }

  Future<void> checkAuthentication() async {
    _loading = true;
    notifyListeners();

    try {
      final savedToken = await _secureStorage.read(key: 'access_token');
      if (savedToken != null) {
        // Simple JWT decode to extract basic details
        _parseToken(savedToken);
        ApiClient.token = savedToken;
        _isAuthenticated = true;
      }
    } catch (e) {
      debugPrint('Failed to load credentials: $e');
    }

    _loading = false;
    notifyListeners();
  }

  Future<void> login() async {
    _loading = true;
    notifyListeners();

    try {
      final AuthorizationTokenResponse? result = await _appAuth.authorizeAndExchangeCode(
        AuthorizationTokenRequest(
          _clientId,
          _redirectUrl,
          issuer: keycloakRealmUrl,
          scopes: ['openid', 'profile', 'email'],
        ),
      );

      if (result != null && result.accessToken != null) {
        await _secureStorage.write(key: 'access_token', value: result.accessToken);
        await _secureStorage.write(key: 'id_token', value: result.idToken);
        
        _parseToken(result.accessToken!);
        ApiClient.token = result.accessToken;
        _isAuthenticated = true;
      }
    } catch (e) {
      debugPrint('Keycloak auth error: $e');
    }

    _loading = false;
    notifyListeners();
  }

  Future<void> logout() async {
    await _secureStorage.deleteAll();
    ApiClient.token = null;
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
