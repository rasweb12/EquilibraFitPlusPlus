import 'package:equilibrafit_plusplus_app/features/auth/domain/user_session.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('stored session preserves tenant and user identity', () {
    const original = StoredUserSession(
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

    final restored = StoredUserSession.fromJson(original.toJson());

    expect(restored.user.id, 'user-1');
    expect(restored.user.tenantId, 'tenant-1');
    expect(restored.requiresOnboarding, isFalse);
  });

  test('stored session rejects missing owner identity', () {
    expect(
      () => StoredUserSession.fromJson(
        <String, Object?>{
          'user': <String, Object?>{
            'id': '',
            'tenantId': 'tenant-1',
            'name': 'Ana',
            'email': 'ana@example.com',
            'role': 'Usuario',
            'expiresIn': 3600,
          },
        },
      ),
      throwsFormatException,
    );
  });
}
