import 'package:equilibrafit_plusplus_app/features/auth/auth_providers.dart';
import 'package:equilibrafit_plusplus_app/features/auth/domain/auth_repository.dart';
import 'package:equilibrafit_plusplus_app/features/auth/domain/user_session.dart';
import 'package:equilibrafit_plusplus_app/features/auth/presentation/controllers/session_controller.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('restores persisted owner before repositories are created', () async {
    const stored = StoredUserSession(
      user: UserSession(
        id: 'user-1',
        tenantId: 'tenant-1',
        name: 'Ana',
        email: 'ana@example.com',
        role: 'Usuario',
        expiresIn: 3600,
      ),
      requiresOnboarding: false,
    );
    final container = ProviderContainer(
      overrides: [
        authRepositoryProvider.overrideWithValue(
          _FakeAuthRepository(stored),
        ),
      ],
    );
    addTearDown(container.dispose);

    expect(
      container.read(sessionControllerProvider).status,
      SessionStatus.restoring,
    );
    await pumpEventQueue();

    final state = container.read(sessionControllerProvider);
    expect(state.status, SessionStatus.authenticated);
    expect(state.user?.id, 'user-1');
    expect(state.user?.tenantId, 'tenant-1');
  });
}

class _FakeAuthRepository implements AuthRepository {
  const _FakeAuthRepository(this.stored);

  final StoredUserSession? stored;

  @override
  Future<StoredUserSession?> restoreSession() async => stored;

  @override
  Future<void> persistSession(
    UserSession user, {
    required bool requiresOnboarding,
  }) async {}

  @override
  Future<UserSession> login({
    required String email,
    required String password,
  }) {
    throw UnimplementedError();
  }

  @override
  Future<UserSession> register({
    required String name,
    required String email,
    required String password,
    required bool analyticsConsent,
  }) {
    throw UnimplementedError();
  }

  @override
  Future<void> signOut() async {}
}
