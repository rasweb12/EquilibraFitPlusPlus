import 'package:dio/dio.dart';
import 'package:equilibrafit_plusplus_app/core/ai/ai_pending_request_service.dart';
import 'package:equilibrafit_plusplus_app/core/http/api_client.dart';
import 'package:equilibrafit_plusplus_app/features/meals/data/meal_repository.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('queues meal text estimation when device is offline', () async {
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
    final repository = MealRepository(
      ApiClient(dio),
      pendingRequestService: pendingStore,
      tenantId: 'tenant-1',
      usuarioId: 'user-1',
    );

    final estimation = await repository.estimateText(
      description: 'arroz, feijão e frango',
      type: 'Almoco',
    );

    expect(estimation.items, isEmpty);
    expect(estimation.message, contains('Salvei a solicitação'));
    expect(pendingStore.calls, 1);
    expect(pendingStore.tenantId, 'tenant-1');
    expect(pendingStore.usuarioId, 'user-1');
    expect(pendingStore.type, 'meal.text_estimation');
    expect(
      pendingStore.payload?['path'],
      '/api/v1/alimentacao/estimar-texto',
    );
    expect(
      pendingStore.body?['descricao'],
      'arroz, feijão e frango',
    );
  });
}

class _FakePendingStore implements AiPendingRequestStore {
  int calls = 0;
  String? tenantId;
  String? usuarioId;
  String? type;
  Map<String, Object?>? payload;

  Map<String, Object?>? get body {
    final value = payload?['body'];
    return value is Map<String, Object?> ? value : null;
  }

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
