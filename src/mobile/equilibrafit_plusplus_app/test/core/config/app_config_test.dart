import 'package:equilibrafit_plusplus_app/core/config/app_config.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  tearDown(() => debugDefaultTargetPlatformOverride = null);

  for (final platform in TargetPlatform.values) {
    test('Beta API default and dart-define override on ${platform.name}', () {
      debugDefaultTargetPlatformOverride = platform;
      const configuredUrl = String.fromEnvironment('API_BASE_URL');
      final config = AppConfig.fromEnvironment();

      expect(
        config.apiBaseUrl,
        configuredUrl.isNotEmpty
            ? configuredUrl
            : 'https://equilibrafit-plusplus-api-4lkw.onrender.com',
      );
      expect(config.requestTimeout, const Duration(seconds: 20));
      if (configuredUrl.isEmpty) {
        final uri = Uri.parse(config.apiBaseUrl);
        expect(uri.scheme, 'https');
        expect(uri.host, isNot('localhost'));
        expect(uri.host, isNot('10.0.2.2'));
      }
    });
  }
}
