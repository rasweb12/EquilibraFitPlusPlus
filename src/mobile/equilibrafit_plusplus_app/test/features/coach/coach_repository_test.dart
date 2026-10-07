import 'package:dio/dio.dart';
import 'package:equilibrafit_plusplus_app/core/ai/ai_pending_request_service.dart';
import 'package:equilibrafit_plusplus_app/core/http/api_client.dart';
import 'package:equilibrafit_plusplus_app/features/coach/data/coach_repository.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
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

    final reply = await repository.sendMessage(message: 'Como treino hoje?');

    expect(reply.content, contains('Salvei sua pergunta'));
    expect(pendingStore.calls, 1);
    expect(pendingStore.tenantId, 'tenant-1');
    expect(pendingStore.usuarioId, 'user-1');
    expect(pendingStore.type, 'coach.message');
    expect(pendingStore.payload?['path'], '/api/v1/ia/coach/mensagens');
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
