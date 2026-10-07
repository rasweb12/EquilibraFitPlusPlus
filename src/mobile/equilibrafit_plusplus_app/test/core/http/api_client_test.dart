import 'package:dio/dio.dart';
import 'package:equilibrafit_plusplus_app/core/http/api_client.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  test('custom timeout preserves request headers', () async {
    final dio = Dio(
      BaseOptions(
        sendTimeout: const Duration(seconds: 20),
        receiveTimeout: const Duration(seconds: 20),
      ),
    );
    RequestOptions? captured;
    dio.interceptors.add(
      InterceptorsWrapper(
        onRequest: (options, handler) {
          captured = options;
          handler.resolve(
            Response<Object?>(
              requestOptions: options,
              statusCode: 200,
              data: <String, Object?>{'ok': true},
            ),
          );
        },
      ),
    );

    await ApiClient(dio).postJson(
      '/api/v1/ia/coach/mensagens',
      headers: const <String, Object?>{'Idempotency-Key': 'operation-1'},
      timeout: ApiClient.aiRequestTimeout,
    );

    expect(captured?.headers['Idempotency-Key'], 'operation-1');
    expect(captured?.sendTimeout, ApiClient.aiRequestTimeout);
    expect(captured?.receiveTimeout, ApiClient.aiRequestTimeout);
  });
}
