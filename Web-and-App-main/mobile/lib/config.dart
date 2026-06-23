class AppConfig {
  // Configured for Android Emulator by default.
  // Change to your host machine's LAN IP (e.g., 'http://192.168.1.50:5000/api') for real devices.
  static const String apiBaseUrl = 'http://10.0.2.2:5000/api';
  static const String keycloakRealmUrl = 'http://10.0.2.2:8080/realms/student-portal';
  static const String keycloakClientId = 'student-portal-mobile';
  static const String keycloakRedirectUrl = 'com.studentportal.app:/oauth2redirect';
}
