import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

final appConfigProvider = Provider<AppConfig>(
  (ref) => AppConfig.fromEnvironment(),
);

class AppConfig {
  const AppConfig({required this.apiBaseUrl, required this.requestTimeout});

  factory AppConfig.fromEnvironment() {
    const configuredApiBaseUrl = String.fromEnvironment('API_BASE_URL');
    if (kReleaseMode &&
        (!configuredApiBaseUrl.startsWith('https://') ||
            Uri.tryParse(configuredApiBaseUrl)?.host == 'localhost' ||
            Uri.tryParse(configuredApiBaseUrl)?.host == '10.0.2.2')) {
      throw StateError('Release requires a public HTTPS API_BASE_URL.');
    }

    return AppConfig(
      apiBaseUrl: configuredApiBaseUrl.isNotEmpty
          ? configuredApiBaseUrl
          : 'https://equilibrafit-plusplus-api-4lkw.onrender.com',
      requestTimeout: const Duration(seconds: 20),
    );
  }

  final String apiBaseUrl;
  final Duration requestTimeout;
}
