import 'user_session.dart';

abstract interface class AuthRepository {
  Future<UserSession> login({required String email, required String password});

  Future<UserSession> register({
    required String name,
    required String email,
    required String password,
    required bool analyticsConsent,
  });

  Future<StoredUserSession?> restoreSession();

  Future<void> persistSession(
    UserSession user, {
    required bool requiresOnboarding,
  });

  Future<void> signOut();
}
