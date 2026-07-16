import 'package:firebase_core/firebase_core.dart' show FirebaseOptions;
import 'package:flutter/foundation.dart' show defaultTargetPlatform, TargetPlatform;

class DefaultFirebaseOptions {
  static FirebaseOptions get currentPlatform {
    switch (defaultTargetPlatform) {
      case TargetPlatform.android:
        return android;
      default:
        return android;
    }
  }

  static const FirebaseOptions android = FirebaseOptions(
    apiKey:            'AIzaSyAkjBU403mjwn6C1Q-3uJhHMcsq2kyIAo4',
    appId:             '1:326771545260:android:cd80174d713c9f3fafbb09',
    messagingSenderId: '326771545260',
    projectId:         'aast-portal-7f566',
    storageBucket:     'aast-portal-7f566.firebasestorage.app',
  );
}
