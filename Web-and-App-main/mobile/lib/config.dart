import 'dart:io';
import 'package:device_info_plus/device_info_plus.dart';

class AppConfig {
  static const String pcLocalIp  = '192.168.1.14';
  static const String _emulatorHost = '10.0.2.2';
  static const String _realm     = 'student-portal';
  static const String _clientId  = 'student-portal-mobile';

  static bool? _isEmulator;

  static Future<void> init() async {
    try {
      if (Platform.isAndroid) {
        final info = await DeviceInfoPlugin().androidInfo;
        _isEmulator = !info.isPhysicalDevice;
      } else {
        _isEmulator = false;
      }
    } catch (e) {
      _isEmulator = true;
    }
  }

  static String get _host =>
      (_isEmulator ?? true) ? _emulatorHost : pcLocalIp;

  static String get apiBaseUrl       => 'http://$_host:5000/api';
  static String get keycloakRealmUrl => 'http://$_host:8080/realms/$_realm';
  static String get tokenEndpoint    => '$keycloakRealmUrl/protocol/openid-connect/token';

  // These never change per-device so they stay const-compatible
  static const String keycloakClientId    = 'student-portal-mobile';
  static const String keycloakRedirectUrl = 'com.studentportal.app:/oauth2redirect';
}
