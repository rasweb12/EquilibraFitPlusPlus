import 'dart:convert';
import '../../../core/errors/app_failure.dart';

import '../../../core/storage/secure_token_store.dart';
import '../domain/auth_repository.dart';
import '../domain/user_session.dart';
import 'auth_api.dart';

class AuthRepositoryImpl implements AuthRepository {
  const AuthRepositoryImpl({
    required AuthApi api,
    required SecureTokenStore tokenStore,
  })  : _api = api,
        _tokenStore = tokenStore;

  final AuthApi _api;
  final SecureTokenStore _tokenStore;

  @override
  Future<UserSession> login({
    required String email,
    required String password,
  }) async {
    final response = await _api.login(email: email, password: password);

    await _tokenStore.saveTokens(
      accessToken: response.accessToken,
      refreshToken: response.refreshToken,
    );

    final session = response.toSession();
    await persistSession(session, requiresOnboarding: false);
    return session;
  }

  @override
  Future<UserSession> register({
    required String name,
    required String email,
    required String password,
    required bool analyticsConsent,
  }) async {
    final response = await _api.register(
      name: name,
      email: email,
      password: password,
      analyticsConsent: analyticsConsent,
    );

    await _tokenStore.saveTokens(
      accessToken: response.accessToken,
      refreshToken: response.refreshToken,
    );

    final session = response.toSession();
    await persistSession(session, requiresOnboarding: true);
    return session;
  }

  @override
  Future<StoredUserSession?> restoreSession() async {
    final accessToken = await _tokenStore.readAccessToken();
    final sessionJson = await _tokenStore.readSession();
    if (accessToken == null ||
        accessToken.trim().isEmpty ||
        sessionJson == null ||
        sessionJson.trim().isEmpty) {
      return null;
    }

    try {
      final decoded = jsonDecode(sessionJson);
      if (decoded is! Map) {
        return null;
      }

      return StoredUserSession.fromJson(
        decoded.map((key, value) => MapEntry(key.toString(), value)),
      );
    } on FormatException {
      return null;
    }
  }

  @override
  Future<void> persistSession(
    UserSession user, {
    required bool requiresOnboarding,
  }) {
    final stored = StoredUserSession(
      user: user,
      requiresOnboarding: requiresOnboarding,
    );
    return _tokenStore.saveSession(jsonEncode(stored.toJson()));
  }

  @override
  Future<void> signOut() async {
    try {
      await _api.logout();
    } on AppFailure {
      // Offline logout still removes this device's credentials.
    } finally {
      await _tokenStore.clear();
    }
  }
}
