import 'package:dio/dio.dart';
import 'package:equilibrafit_plusplus_app/core/ai/ai_pending_request_service.dart';
import 'package:equilibrafit_plusplus_app/core/http/api_client.dart';
import 'package:equilibrafit_plusplus_app/features/coach/data/coach_repository.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  for (final provider in ['openai', 'gemini']) {
    test('sends only the chosen Coach provider: $provider', () async {
      final dio = Dio();
      RequestOptions? captured;
      dio.interceptors.add(
        InterceptorsWrapper(
          onRequest: (options, handler) {
            captured = options;
            handler.resolve(
              Response<Map<String, Object?>>(
                requestOptions: options,
                statusCode: 200,
                data: {
                  'sessaoId': 'session-1',
                  'mensagemCoach': {'conteudo': 'Resposta'},
                },
              ),
            );
          },
        ),
      );
      await CoachRepository(ApiClient(dio)).sendMessage(
        message: 'Como adaptar minha rotina?',
        sessionId: 'session-1',
        provider: provider,
      );
      expect(captured?.data, {
        'sessaoId': 'session-1',
        'mensagem': 'Como adaptar minha rotina?',
        'provedor': provider,
      });
    });
  }

  for (final fixture in [
    (flag: true, model: 'equilibrafit-coach-rules-v1', expected: true),
    (flag: false, model: 'gpt-4.1-mini', expected: false),
    (flag: null, model: 'equilibrafit-coach-rules-v1', expected: true),
    (flag: null, model: 'gpt-4.1-mini', expected: false),
  ]) {
    test('preserves coach fallback status: $fixture', () async {
      final dio = Dio();
      dio.interceptors.add(
        InterceptorsWrapper(
          onRequest: (options, handler) {
            handler.resolve(
              Response<Map<String, Object?>>(
                requestOptions: options,
                statusCode: 200,
                data: {
                  'sessaoId': 'session-1',
                  'mensagemCoach': {
                    'conteudo': 'Resposta',
                    'modeloIa': fixture.model,
                  },
                  'avisoSaude': 'Aviso de teste',
                  if (fixture.flag != null) 'fallbackUsed': fixture.flag,
                },
              ),
            );
          },
        ),
      );
      final reply =
          await CoachRepository(ApiClient(dio)).sendMessage(message: 'Teste');
      expect(reply.fallbackUsed, fixture.expected);
      expect(reply.content, 'Resposta');
      expect(reply.healthNotice, 'Aviso de teste');
    });
  }

  test('queues one coach request when device is offline', () async {
    final dio = Dio();
    dio.interceptors.add(
      InterceptorsWrapper(
        onRequest: (options, handler) {
          handler.reject(
            DioException(
              requestOptions: options,
              type: DioExceptionType.connectionError,
              error: 'offline',
            ),
          );
        },
      ),
    );
    final pendingStore = _FakePendingStore();
    final repository = CoachRepository(
      ApiClient(dio),
      pendingRequestService: pendingStore,
      tenantId: 'tenant-1',
      usuarioId: 'user-1',
    );

    final reply = await repository.sendMessage(
      message: 'Como treino hoje?',
      provider: 'gemini',
    );

    expect(reply.content, contains('Salvei sua pergunta'));
    expect(pendingStore.calls, 1);
    expect(pendingStore.tenantId, 'tenant-1');
    expect(pendingStore.usuarioId, 'user-1');
    expect(pendingStore.type, 'coach.message');
    expect(pendingStore.payload?['path'], '/api/v1/ia/coach/mensagens');
    expect(pendingStore.payload?['body'], {
      'sessaoId': null,
      'mensagem': 'Como treino hoje?',
      'provedor': 'gemini',
    });
  });
}

class _FakePendingStore implements AiPendingRequestStore {
  int calls = 0;
  String? tenantId;
  String? usuarioId;
  String? type;
  Map<String, Object?>? payload;

  @override
  Future<void> enqueue({
    required String operationId,
    required String tenantId,
    required String usuarioId,
    required String type,
    required Map<String, Object?> payload,
  }) async {
    calls += 1;
    this.tenantId = tenantId;
    this.usuarioId = usuarioId;
    this.type = type;
    this.payload = payload;
  }
}
