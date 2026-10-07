import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/ai/ai_pending_request_service.dart';
import '../../../core/errors/app_failure.dart';
import '../../../core/http/api_client.dart';
import '../../../core/utils/json_helpers.dart';
import '../../../core/utils/local_id_generator.dart';
import '../../auth/presentation/controllers/session_controller.dart';
import '../domain/coach_reply.dart';

final coachRepositoryProvider = Provider<CoachRepository>((ref) {
  final usuarioId = ref.watch(
    sessionControllerProvider.select((state) => state.user?.id),
  );
  final tenantId = ref.watch(
    sessionControllerProvider.select((state) => state.user?.tenantId),
  );

  return CoachRepository(
    ref.watch(apiClientProvider),
    pendingRequestService: ref.watch(aiPendingRequestServiceProvider),
    tenantId: tenantId,
    usuarioId: usuarioId,
  );
});

class CoachRepository {
  const CoachRepository(
    this._client, {
    AiPendingRequestStore? pendingRequestService,
    String? tenantId,
    String? usuarioId,
  })  : _pendingRequestService = pendingRequestService,
        _tenantId = tenantId ?? 'default',
        _usuarioId = usuarioId;

  final ApiClient _client;
  final AiPendingRequestStore? _pendingRequestService;
  final String _tenantId;
  final String? _usuarioId;

  Future<CoachReply> sendMessage({
    required String message,
    String? sessionId,
  }) async {
    final body = <String, Object?>{'sessaoId': sessionId, 'mensagem': message};
    final Map<String, Object?> json;

    try {
      json = await _client.postJson(
        '/api/v1/ia/coach/mensagens',
        body: body,
        timeout: ApiClient.aiRequestTimeout,
      );
    } on AppFailure catch (failure) {
      if (failure.code != 'network_unavailable') {
        rethrow;
      }

      return _savePendingCoachMessage(message: message, body: body);
    }

    final coachMessage = jsonObject(
      json['mensagemCoach'] ?? json['MensagemCoach'],
    );

    return CoachReply(
      sessionId: jsonString(json['sessaoId'] ?? json['SessaoId']),
      content: jsonString(coachMessage['conteudo'] ?? coachMessage['Conteudo']),
      healthNotice: jsonString(json['avisoSaude'] ?? json['AvisoSaude']),
    );
  }

  Future<CoachReply> _savePendingCoachMessage({
    required String message,
    required Map<String, Object?> body,
  }) async {
    final usuarioId = _usuarioId;
    final pendingRequestService = _pendingRequestService;
    final operationId = newLocalOperationId();

    if (usuarioId != null &&
        usuarioId.trim().isNotEmpty &&
        pendingRequestService != null) {
      await pendingRequestService.enqueue(
        operationId: operationId,
        tenantId: _tenantId,
        usuarioId: usuarioId,
        type: 'coach.message',
        payload: <String, Object?>{
          'path': '/api/v1/ia/coach/mensagens',
          'body': body,
        },
      );
    }

    return CoachReply(
      sessionId: operationId,
      content:
          'Essa análise precisa de internet. Salvei sua pergunta para processar quando a conexão voltar.',
      healthNotice: message.trim().isEmpty
          ? ''
          : 'Enquanto isso, evite mudanças agressivas sem orientação.',
    );
  }
}
