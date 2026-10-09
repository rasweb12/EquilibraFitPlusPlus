import 'package:dio/dio.dart';
import 'package:equilibrafit_plusplus_app/core/errors/app_failure.dart';
import 'package:equilibrafit_plusplus_app/core/http/api_client.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  for (final status in <int>[502, 503, 504]) {
    test('upstream HTML $status is not shown or automatically retried',
        () async {
      final dio = Dio(BaseOptions(baseUrl: 'https://api.example.test'));
      var requests = 0;
      dio.interceptors.add(
        InterceptorsWrapper(
          onRequest: (options, handler) {
            requests++;
            handler.reject(
              DioException(
                requestOptions: options,
                type: DioExceptionType.badResponse,
                response: Response<Object?>(
                  requestOptions: options,
                  statusCode: status,
                  data: '<html>private-user-and-key</html>',
                ),
              ),
            );
          },
        ),
      );
      addTearDown(() => dio.close(force: true));
      await expectLater(
        ApiClient(dio).postJson('/api/v1/ia/coach/mensagens'),
        throwsA(
          isA<AppFailure>()
              .having((failure) => failure.code, 'status', '$status')
              .having(
                (failure) => failure.message,
                'safe message',
                isNot(contains('private-user-and-key')),
              )
              .having(
                (failure) => failure.message,
                'no HTML',
                isNot(contains('<html>')),
              ),
        ),
      );
      expect(requests, 1);
    });
  }

  test('safe structured AI error preserves HTTP status and message', () async {
    final dio = Dio(BaseOptions(baseUrl: 'https://api.example.test'));
    const message = 'IA temporariamente indisponivel. Tente novamente.';
    dio.interceptors.add(
      InterceptorsWrapper(
        onRequest: (options, handler) => handler.reject(
          DioException(
            requestOptions: options,
            type: DioExceptionType.badResponse,
            response: Response<Object?>(
              requestOptions: options,
              statusCode: 502,
              data: <String, Object?>{
                'code': 'ia.upstream_authentication_failed',
                'message': message,
                'traceId': 'fixture',
                'details': <Object?>[],
              },
            ),
          ),
        ),
      ),
    );
    addTearDown(() => dio.close(force: true));
    await expectLater(
      ApiClient(dio).postJson('/api/v1/ia/coach/mensagens'),
      throwsA(
        isA<AppFailure>()
            .having((failure) => failure.code, 'not user 401', '502')
            .having((failure) => failure.message, 'message', message),
      ),
    );
  });

  test('response timeout is distinguished from connection failure', () async {
    final dio = Dio(BaseOptions(baseUrl: 'https://api.example.test'));
    dio.interceptors.add(
      InterceptorsWrapper(
        onRequest: (options, handler) => handler.reject(
          DioException(
            requestOptions: options,
            type: DioExceptionType.receiveTimeout,
          ),
        ),
      ),
    );
    addTearDown(() => dio.close(force: true));

    await expectLater(
      ApiClient(dio).getJson('/health'),
      throwsA(
        isA<AppFailure>()
            .having((failure) => failure.code, 'code', 'network_unavailable')
            .having(
              (failure) => failure.message,
              'timeout',
              contains('demorou para responder'),
            ),
      ),
    );
  });

  test('remote connection failure does not suggest a local IP', () async {
    const apiUrl = 'https://equilibrafit-plusplus-api-4lkw.onrender.com';
    final dio = Dio(BaseOptions(baseUrl: apiUrl));
    dio.interceptors.add(
      InterceptorsWrapper(
        onRequest: (options, handler) => handler.reject(
          DioException(
            requestOptions: options,
            type: DioExceptionType.connectionError,
          ),
        ),
      ),
    );
    addTearDown(() => dio.close(force: true));

    await expectLater(
      ApiClient(dio).getJson('/health'),
      throwsA(
        isA<AppFailure>()
            .having((failure) => failure.code, 'code', 'network_unavailable')
            .having((failure) => failure.message, 'URL', contains(apiUrl))
            .having(
              (failure) => failure.message,
              'local IP hint',
              isNot(contains('IP do computador')),
            ),
      ),
    );
  });

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
