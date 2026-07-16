import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../services/auth_service.dart';

class LoginScreen extends StatefulWidget {
  const LoginScreen({super.key});

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  final _usernameController = TextEditingController();
  final _passwordController = TextEditingController();
  bool _obscurePassword = true;
  bool _loading = false;
  String? _error;

  @override
  void dispose() {
    _usernameController.dispose();
    _passwordController.dispose();
    super.dispose();
  }

  Future<void> _handleLogin() async {
    setState(() {
      _error = null;
      _loading = true;
    });

    final authService = Provider.of<AuthService>(context, listen: false);
    final success = await authService.loginWithCredentials(
      _usernameController.text.trim(),
      _passwordController.text,
    );

    if (!mounted) return;

    if (success) {
      // AuthenticationWrapper rebuilds to MainNavigationShell
      setState(() => _loading = false);
    } else {
      setState(() {
        _loading = false;
        _error = 'Invalid username or password';
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFF0D1B2A),
      body: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 32),
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 400),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  const Text(
                    '🎓',
                    textAlign: TextAlign.center,
                    style: TextStyle(fontSize: 48),
                  ),
                  const SizedBox(height: 12),
                  const Text(
                    'AAST Student Portal',
                    textAlign: TextAlign.center,
                    style: TextStyle(
                      color: Colors.white,
                      fontSize: 24,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                  const SizedBox(height: 8),
                  const Text(
                    'Computer Engineering Department',
                    textAlign: TextAlign.center,
                    style: TextStyle(color: Color(0xFF8AAAC8), fontSize: 14),
                  ),
                  const SizedBox(height: 32),

                  const Text(
                    'Username',
                    style: TextStyle(color: Color(0xFF8AAAC8), fontSize: 13),
                  ),
                  const SizedBox(height: 6),
                  TextField(
                    controller: _usernameController,
                    style: const TextStyle(color: Colors.white),
                    decoration: InputDecoration(
                      hintText: 'Enter your username',
                      hintStyle: const TextStyle(color: Color(0xFF4A6A8A)),
                      filled: true,
                      fillColor: const Color(0xFF0D1B2A),
                      enabledBorder: OutlineInputBorder(
                        borderRadius: BorderRadius.circular(8),
                        borderSide: const BorderSide(color: Color(0xFF2A4A6A)),
                      ),
                      focusedBorder: OutlineInputBorder(
                        borderRadius: BorderRadius.circular(8),
                        borderSide: const BorderSide(color: Color(0xFF4A90E2)),
                      ),
                    ),
                  ),
                  const SizedBox(height: 16),

                  const Text(
                    'Password',
                    style: TextStyle(color: Color(0xFF8AAAC8), fontSize: 13),
                  ),
                  const SizedBox(height: 6),
                  TextField(
                    controller: _passwordController,
                    obscureText: _obscurePassword,
                    style: const TextStyle(color: Colors.white),
                    onSubmitted: (_) => _handleLogin(),
                    decoration: InputDecoration(
                      hintText: 'Enter your password',
                      hintStyle: const TextStyle(color: Color(0xFF4A6A8A)),
                      filled: true,
                      fillColor: const Color(0xFF0D1B2A),
                      enabledBorder: OutlineInputBorder(
                        borderRadius: BorderRadius.circular(8),
                        borderSide: const BorderSide(color: Color(0xFF2A4A6A)),
                      ),
                      focusedBorder: OutlineInputBorder(
                        borderRadius: BorderRadius.circular(8),
                        borderSide: const BorderSide(color: Color(0xFF4A90E2)),
                      ),
                      suffixIcon: IconButton(
                        icon: Icon(
                          _obscurePassword ? Icons.visibility_off : Icons.visibility,
                          color: const Color(0xFF8AAAC8),
                        ),
                        onPressed: () => setState(() => _obscurePassword = !_obscurePassword),
                      ),
                    ),
                  ),
                  const SizedBox(height: 24),

                  if (_error != null)
                    Container(
                      margin: const EdgeInsets.only(bottom: 16),
                      padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
                      decoration: BoxDecoration(
                        color: const Color(0x22EF4444),
                        border: Border.all(color: const Color(0xFFEF4444)),
                        borderRadius: BorderRadius.circular(8),
                      ),
                      child: Text(
                        _error!,
                        style: const TextStyle(color: Color(0xFFEF4444), fontSize: 13),
                      ),
                    ),

                  ElevatedButton(
                    onPressed: _loading ? null : _handleLogin,
                    style: ElevatedButton.styleFrom(
                      backgroundColor: _loading ? const Color(0xFF2A4A6A) : const Color(0xFF4A90E2),
                      padding: const EdgeInsets.symmetric(vertical: 14),
                      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
                    ),
                    child: Text(
                      _loading ? 'Signing in...' : 'Sign In →',
                      style: const TextStyle(
                        color: Colors.white,
                        fontSize: 15,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                  ),
                  const SizedBox(height: 24),
                  const Text(
                    'AAST Student Portal v1.0',
                    textAlign: TextAlign.center,
                    style: TextStyle(color: Color(0xFF4A6A8A), fontSize: 12),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}
