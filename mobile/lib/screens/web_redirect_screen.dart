import 'package:flutter/material.dart';

class WebRedirectScreen extends StatelessWidget {
  const WebRedirectScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFF0D1B2A),
      body: SafeArea(
        child: Center(
          child: Padding(
            padding: const EdgeInsets.all(32.0),
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                // Logo
                Container(
                  width: 100,
                  height: 100,
                  decoration: BoxDecoration(
                    color: const Color(0xFF1A3A5C),
                    borderRadius: BorderRadius.circular(50),
                  ),
                  child: const Icon(
                    Icons.school,
                    size: 60,
                    color: Color(0xFF4A90E2),
                  ),
                ),
                const SizedBox(height: 32),
                
                // Title
                const Text(
                  'AAST PORTAL',
                  style: TextStyle(
                    color: Colors.white,
                    fontSize: 28,
                    fontWeight: FontWeight.bold,
                    letterSpacing: 2,
                  ),
                ),
                const SizedBox(height: 8),
                const Text(
                  'Computer Engineering Department',
                  style: TextStyle(
                    color: Color(0xFF8AAAC8),
                    fontSize: 14,
                  ),
                ),
                const SizedBox(height: 48),
                
                // Info card
                Container(
                  padding: const EdgeInsets.all(24),
                  decoration: BoxDecoration(
                    color: const Color(0xFF1A2F45),
                    borderRadius: BorderRadius.circular(16),
                    border: Border.all(
                      color: const Color(0xFF2A4A6A),
                      width: 1,
                    ),
                  ),
                  child: Column(
                    children: [
                      const Icon(
                        Icons.devices,
                        color: Color(0xFF4A90E2),
                        size: 40,
                      ),
                      const SizedBox(height: 16),
                      const Text(
                        'Mobile App Detected',
                        style: TextStyle(
                          color: Colors.white,
                          fontSize: 18,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                      const SizedBox(height: 12),
                      const Text(
                        'You are accessing the mobile version of AAST Portal. '
                        'For the full web experience with all features, '
                        'please use our dedicated web portal.',
                        textAlign: TextAlign.center,
                        style: TextStyle(
                          color: Color(0xFF8AAAC8),
                          fontSize: 14,
                          height: 1.6,
                        ),
                      ),
                      const SizedBox(height: 24),
                      
                      // Web portal button
                      SizedBox(
                        width: double.infinity,
                        child: ElevatedButton.icon(
                          onPressed: () {
                            // Open main web portal
                          },
                          icon: const Icon(Icons.open_in_browser),
                          label: const Text(
                            'Open Web Portal',
                            style: TextStyle(
                              fontSize: 16,
                              fontWeight: FontWeight.w600,
                            ),
                          ),
                          style: ElevatedButton.styleFrom(
                            backgroundColor: const Color(0xFF4A90E2),
                            foregroundColor: Colors.white,
                            padding: const EdgeInsets.symmetric(vertical: 16),
                            shape: RoundedRectangleBorder(
                              borderRadius: BorderRadius.circular(12),
                            ),
                          ),
                        ),
                      ),
                      const SizedBox(height: 12),
                      
                      // URL display
                      Container(
                        padding: const EdgeInsets.symmetric(
                            horizontal: 16, vertical: 10),
                        decoration: BoxDecoration(
                          color: const Color(0xFF0D1B2A),
                          borderRadius: BorderRadius.circular(8),
                        ),
                        child: Row(
                          mainAxisAlignment: MainAxisAlignment.center,
                          children: [
                            const Icon(
                              Icons.link,
                              color: Color(0xFF4A90E2),
                              size: 16,
                            ),
                            const SizedBox(width: 8),
                            const Text(
                              'http://localhost:3000',
                              style: TextStyle(
                                color: Color(0xFF4A90E2),
                                fontSize: 14,
                                fontFamily: 'monospace',
                              ),
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),
                ),
                const SizedBox(height: 24),
                
                // Back button
                TextButton.icon(
                  onPressed: () => Navigator.of(context).pop(),
                  icon: const Icon(
                    Icons.arrow_back,
                    color: Color(0xFF8AAAC8),
                  ),
                  label: const Text(
                    'Back to Login',
                    style: TextStyle(color: Color(0xFF8AAAC8)),
                  ),
                ),
                
                const SizedBox(height: 32),
                
                // Mobile app info
                Row(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    const Icon(
                      Icons.phone_android,
                      color: Color(0xFF4CAF50),
                      size: 16,
                    ),
                    const SizedBox(width: 8),
                    const Text(
                      'Mobile app available for Android',
                      style: TextStyle(
                        color: Color(0xFF4CAF50),
                        fontSize: 12,
                      ),
                    ),
                  ],
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
