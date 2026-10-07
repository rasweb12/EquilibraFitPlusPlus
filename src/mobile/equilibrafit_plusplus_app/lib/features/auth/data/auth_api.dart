import '../../../core/http/api_client.dart';
import 'auth_response_dto.dart';

class AuthApi {
  const AuthApi(this._client);

  final ApiClient _client;

  Future<void> logout() async {
    await _client.postJson('/api/v1/auth/logout');
  }

  Future<AuthResponseDto> login({
    required String email,
    required String password,
  }) async {
    final json = await _client.postJson(
      '/api/v1/auth/login',
      body: <String, Object?>{'email': email.trim(), 'senha': password},
    );

    return AuthResponseDto.fromJson(json);
  }

  Future<AuthResponseDto> register({
    required String name,
    required String email,
    required String password,
    required bool analyticsConsent,
  }) async {
    final consents = <Map<String, Object?>>[
      <String, Object?>{'tipo': 'TermosUso', 'versao': '1.0'},
      <String, Object?>{'tipo': 'Privacidade', 'versao': '1.0'},
      <String, Object?>{'tipo': 'UsoIa', 'versao': '1.0'},
      if (analyticsConsent)
        <String, Object?>{'tipo': 'Analytics', 'versao': '1.0'},
    ];

    final json = await _client.postJson(
      '/api/v1/auth/cadastrar',
      body: <String, Object?>{
        'nome': name.trim(),
        'email': email.trim(),
        'senha': password,
        'aceites': consents,
      },
    );

    return AuthResponseDto.fromJson(json);
  }
}
