import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:firebase_core/firebase_core.dart';
import 'package:firebase_messaging/firebase_messaging.dart';
import 'services/auth_service.dart';
import 'screens/login_screen.dart';
import 'screens/profile_screen.dart';
import 'screens/results_screen.dart';
import 'screens/attendance_screen.dart';
import 'screens/schedule_screen.dart';
import 'screens/notifications_screen.dart';
import 'screens/home_screen.dart';

import 'package:flutter/foundation.dart';

@pragma('vm:entry-point')
Future<void> _firebaseMessagingBackgroundHandler(
    RemoteMessage message) async {
  if (!kIsWeb) {
    await Firebase.initializeApp();
  }
}

void main() async {
  WidgetsFlutterBinding.ensureInitialized();
  if (!kIsWeb) {
    try {
      await Firebase.initializeApp().timeout(const Duration(seconds: 10));
      FirebaseMessaging.onBackgroundMessage(
          _firebaseMessagingBackgroundHandler);
    } catch (e) {
      debugPrint('Firebase initialization failed: $e');
    }
  }
  runApp(
    ChangeNotifierProvider(
      create: (_) => AuthService(),
      child: const StudentPortalApp(),
    ),
  );
}

class StudentPortalApp extends StatelessWidget {
  const StudentPortalApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'AAST Student Portal',
      debugShowCheckedModeBanner: false,
      theme: ThemeData(
        brightness: Brightness.dark,
        primarySwatch: Colors.blue,
        fontFamily: 'Outfit',
      ),
      home: const AuthenticationWrapper(),
    );
  }
}

class AuthenticationWrapper extends StatelessWidget {
  const AuthenticationWrapper({super.key});

  @override
  Widget build(BuildContext context) {
    final authService = Provider.of<AuthService>(context);

    if (authService.loading) {
      return const Scaffold(
        backgroundColor: Color(0xFF0F172A),
        body: Center(
          child: CircularProgressIndicator(color: Colors.blueAccent),
        ),
      );
    }

    if (authService.isAuthenticated) {
      return const MainNavigationShell();
    }

    return const LoginScreen();
  }
}

class MainNavigationShell extends StatefulWidget {
  const MainNavigationShell({super.key});

  @override
  State<MainNavigationShell> createState() => _MainNavigationShellState();
}

class _MainNavigationShellState extends State<MainNavigationShell> {
  int _currentIndex = 0;

  late final List<Widget> _screens;

  @override
  void initState() {
    super.initState();
    _screens = [
      HomeScreen(onNavigate: (index) {
        setState(() {
          _currentIndex = index;
        });
      }),
      const ResultsScreen(),
      const AttendanceScreen(),
      const ScheduleScreen(),
      const NotificationsScreen(),
      const ProfileScreen(),
    ];
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: IndexedStack(
        index: _currentIndex,
        children: _screens,
      ),
      bottomNavigationBar: BottomNavigationBar(
        currentIndex: _currentIndex >= 5 ? 0 : _currentIndex,
        onTap: (index) {
          setState(() {
            _currentIndex = index;
          });
        },
        type: BottomNavigationBarType.fixed,
        backgroundColor: const Color(0xFF1E293B),
        selectedItemColor: Colors.blueAccent,
        unselectedItemColor: const Color(0xFF94A3B8),
        items: const [
          BottomNavigationBarItem(
            icon: Icon(Icons.home_outlined),
            activeIcon: Icon(Icons.home),
            label: 'Home',
          ),
          BottomNavigationBarItem(
            icon: Icon(Icons.bar_chart_outlined),
            activeIcon: Icon(Icons.bar_chart),
            label: 'Results',
          ),
          BottomNavigationBarItem(
            icon: Icon(Icons.qr_code_scanner),
            label: 'Check-in',
          ),
          BottomNavigationBarItem(
            icon: Icon(Icons.calendar_today_outlined),
            activeIcon: Icon(Icons.calendar_today),
            label: 'Schedule',
          ),
          BottomNavigationBarItem(
            icon: Icon(Icons.notifications_outlined),
            activeIcon: Icon(Icons.notifications),
            label: 'Alerts',
          ),
        ],
      ),
    );
  }
}
