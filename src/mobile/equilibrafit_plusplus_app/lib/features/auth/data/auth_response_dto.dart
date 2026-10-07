import '../domain/user_session.dart';

class AuthResponseDto {
  const AuthResponseDto({
    required this.accessToken,
    required this.refreshToken,
    required this.expiresIn,
    required this.user,
  });

  factory AuthResponseDto.fromJson(Map<String, Object?> json) {
    final userJson = _object(json['usuario'] ?? json['Usuario']);

    return AuthResponseDto(
      accessToken: _string(json['accessToken'] ?? json['AccessToken']),
      refreshToken: _string(json['refreshToken'] ?? json['RefreshToken']),
      expiresIn: _int(json['expiresIn'] ?? json['ExpiresIn']),
      user: AuthUserDto.fromJson(userJson),
    );
  }

  final String accessToken;
  final String refreshToken;
  final int expiresIn;
  final AuthUserDto user;

  UserSession toSession() {
    return UserSession(
      id: user.id,
      tenantId: user.tenantId.isEmpty ? 'default' : user.tenantId,
      name: user.name,
      email: user.email,
      role: user.role,
      expiresIn: expiresIn,
    );
  }
}

class AuthUserDto {
  const AuthUserDto({
    required this.id,
    required this.tenantId,
    required this.name,
    required this.email,
    required this.role,
  });

  factory AuthUserDto.fromJson(Map<String, Object?> json) {
    return AuthUserDto(
      id: _string(json['id'] ?? json['Id']),
      tenantId: _string(json['tenantId'] ?? json['TenantId']),
      name: _string(json['nome'] ?? json['Nome']),
      email: _string(json['email'] ?? json['Email']),
      role: _string(json['role'] ?? json['Role']),
    );
  }

  final String id;
  final String tenantId;
  final String name;
  final String email;
  final String role;
}

Map<String, Object?> _object(Object? value) {
  if (value is Map<String, Object?>) {
    return value;
  }

  if (value is Map) {
    return value.map((key, item) => MapEntry(key.toString(), item));
  }

  return <String, Object?>{};
}

String _string(Object? value) => value?.toString() ?? '';

int _int(Object? value) {
  if (value is int) {
    return value;
  }

  return int.tryParse(value?.toString() ?? '') ?? 0;
}
