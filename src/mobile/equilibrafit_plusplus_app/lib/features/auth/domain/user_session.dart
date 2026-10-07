class UserSession {
  const UserSession({
    required this.id,
    required this.tenantId,
    required this.name,
    required this.email,
    required this.role,
    required this.expiresIn,
  });

  final String id;
  final String tenantId;
  final String name;
  final String email;
  final String role;
  final int expiresIn;

  Map<String, Object?> toJson() {
    return <String, Object?>{
      'id': id,
      'tenantId': tenantId,
      'name': name,
      'email': email,
      'role': role,
      'expiresIn': expiresIn,
    };
  }

  factory UserSession.fromJson(Map<String, Object?> json) {
    return UserSession(
      id: _requiredString(json['id']),
      tenantId: _requiredString(json['tenantId']),
      name: _requiredString(json['name']),
      email: _requiredString(json['email']),
      role: _requiredString(json['role']),
      expiresIn: _intValue(json['expiresIn']),
    );
  }
}

class StoredUserSession {
  const StoredUserSession({
    required this.user,
    required this.requiresOnboarding,
  });

  factory StoredUserSession.fromJson(Map<String, Object?> json) {
    final userJson = json['user'];
    if (userJson is! Map) {
      throw const FormatException('Sessão local inválida.');
    }

    return StoredUserSession(
      user: UserSession.fromJson(
        userJson.map((key, value) => MapEntry(key.toString(), value)),
      ),
      requiresOnboarding: json['requiresOnboarding'] == true,
    );
  }

  final UserSession user;
  final bool requiresOnboarding;

  Map<String, Object?> toJson() {
    return <String, Object?>{
      'user': user.toJson(),
      'requiresOnboarding': requiresOnboarding,
    };
  }
}

String _requiredString(Object? value) {
  final text = value?.toString().trim() ?? '';
  if (text.isEmpty) {
    throw const FormatException('Sessão local incompleta.');
  }

  return text;
}

int _intValue(Object? value) {
  if (value is int) {
    return value;
  }

  return int.tryParse(value?.toString() ?? '') ?? 0;
}
