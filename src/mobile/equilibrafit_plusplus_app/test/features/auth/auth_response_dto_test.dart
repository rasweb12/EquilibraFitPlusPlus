import 'package:equilibrafit_plusplus_app/features/auth/data/auth_response_dto.dart';
import 'package:test/test.dart';

void main() {
  group('AuthResponseDto', () {
    test('maps camelCase auth response to user session', () {
      final dto = AuthResponseDto.fromJson(
        <String, Object?>{
          'accessToken': 'access-token',
          'refreshToken': 'refresh-token',
          'expiresIn': 3600,
          'usuario': <String, Object?>{
            'id': 'user-id',
            'tenantId': 'tenant-id',
            'nome': 'Ana',
            'email': 'ana@example.com',
            'role': 'Usuario',
          },
        },
      );

      final session = dto.toSession();

      expect(dto.accessToken, 'access-token');
      expect(dto.refreshToken, 'refresh-token');
      expect(session.id, 'user-id');
      expect(session.tenantId, 'tenant-id');
      expect(session.name, 'Ana');
      expect(session.email, 'ana@example.com');
      expect(session.role, 'Usuario');
      expect(session.expiresIn, 3600);
    });

    test('maps PascalCase auth response to user session', () {
      final dto = AuthResponseDto.fromJson(
        <String, Object?>{
          'AccessToken': 'access-token',
          'RefreshToken': 'refresh-token',
          'ExpiresIn': 7200,
          'Usuario': <String, Object?>{
            'Id': 'user-id',
            'TenantId': 'tenant-id',
            'Nome': 'Bruno',
            'Email': 'bruno@example.com',
            'Role': 'Administrador',
          },
        },
      );

      final session = dto.toSession();

      expect(session.name, 'Bruno');
      expect(session.tenantId, 'tenant-id');
      expect(session.role, 'Administrador');
      expect(session.expiresIn, 7200);
    });
  });
}
