import 'package:flutter/foundation.dart';

class AppConfig {
  // Use localhost for web browser, 10.0.2.2 for Android emulator
  static String get keycloakUrl {
    if (kIsWeb) {
      return 'http://localhost:8080/realms/student-portal';
    }
    return 'http://10.0.2.2:8080/realms/student-portal';
  }

  static String get apiBaseUrl {
    if (kIsWeb) {
      return 'http://localhost:5000/api';
    }
    return 'http://10.0.2.2:5000/api';
  }

  static String get clientId {
    if (kIsWeb) {
      return 'web-app';
    }
    return 'student-portal-mobile';
  }

  static String get redirectUrl {
    if (kIsWeb) {
      return 'http://localhost:8081/';
    }
    return 'com.studentportal.app://oauth2redirect';
  }
}
