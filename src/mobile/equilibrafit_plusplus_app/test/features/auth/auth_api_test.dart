import 'dart:convert';
import 'dart:io';

import 'package:dio/dio.dart';
import 'package:equilibrafit_plusplus_app/core/errors/app_failure.dart';
import 'package:equilibrafit_plusplus_app/core/http/api_client.dart';
import 'package:equilibrafit_plusplus_app/features/auth/data/auth_api.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  late Dio dio;
  late AuthApi api;
  late List<RequestOptions> requests;

  setUp(() {
    requests = [];
    dio = Dio(
      BaseOptions(
        baseUrl: 'https://api.example.test',
        connectTimeout: const Duration(seconds: 20),
        sendTimeout: const Duration(seconds: 20),
        receiveTimeout: const Duration(seconds: 20),
      ),
    );
    dio.interceptors.add(
      InterceptorsWrapper(
        onRequest: (options, handler) {
          requests.add(options);
          handler.resolve(
            Response<Object?>(requestOptions: options, data: _response),
          );
        },
      ),
    );
    api = AuthApi(ApiClient(dio));
  });

  tearDown(() => dio.close(force: true));

  test('signup allows cold start without changing its contract', () async {
    await api.register(
      name: ' Ana ',
      email: ' ana@example.test ',
      password: 'synthetic-test-password',
      analyticsConsent: false,
    );

    expect(requests, hasLength(1));
    final request = requests.single;
    expect(request.path, '/api/v1/auth/cadastrar');
    expect(request.method, 'POST');
    expect(request.connectTimeout, const Duration(seconds: 20));
    expect(request.receiveTimeout, const Duration(seconds: 90));
    expect(request.sendTimeout, AuthApi.authenticationTimeout);
    final body = request.data as Map<String, Object?>;
    expect(body['nome'], 'Ana');
    expect(body['email'], 'ana@example.test');
    expect(body['aceites'], hasLength(3));
  });

  test('login allows cold start and preserves response parsing', () async {
    final response = await api.login(
      email: ' ana@example.test ',
      password: 'synthetic-test-password',
    );

    expect(requests, hasLength(1));
    expect(requests.single.path, '/api/v1/auth/login');
    expect(requests.single.receiveTimeout, AuthApi.authenticationTimeout);
    expect(response.user.id, 'user-1');
    expect(response.accessToken, 'synthetic-access-token');
  });

  test('logout keeps the normal response timeout', () async {
    await api.logout();

    expect(requests, hasLength(1));
    expect(requests.single.path, '/api/v1/auth/logout');
    expect(requests.single.receiveTimeout, const Duration(seconds: 20));
    expect(dio.options.receiveTimeout, const Duration(seconds: 20));
  });

  for (final failure in [
    DioExceptionType.receiveTimeout,
    DioExceptionType.badResponse,
  ]) {
    test('signup is not automatically retried after ${failure.name}', () async {
      dio.interceptors.clear();
      dio.interceptors.add(
        InterceptorsWrapper(
          onRequest: (options, handler) {
            requests.add(options);
            handler.reject(
              DioException(
                requestOptions: options,
                type: failure,
                response: failure == DioExceptionType.badResponse
                    ? Response<Object?>(
                        requestOptions: options,
                        statusCode: 503,
                      )
                    : null,
              ),
            );
          },
        ),
      );

      await expectLater(
        api.register(
          name: 'Ana',
          email: 'ana@example.test',
          password: 'synthetic-test-password',
          analyticsConsent: false,
        ),
        throwsA(isA<AppFailure>()),
      );
      expect(requests, hasLength(1));
    });
  }

  test('signup accepts a real HTTP response beyond the normal deadline',
      () async {
    final server = await HttpServer.bind(InternetAddress.loopbackIPv4, 0);
    var received = 0;
    final listener = server.listen((request) async {
      received++;
      await request.drain<void>();
      await Future<void>.delayed(const Duration(milliseconds: 150));
      request.response.headers.contentType = ContentType.json;
      request.response.write(jsonEncode(_response));
      await request.response.close();
    });
    final http = Dio(
      BaseOptions(
        baseUrl: 'http://127.0.0.1:${server.port}',
        connectTimeout: const Duration(seconds: 5),
        receiveTimeout: const Duration(milliseconds: 20),
      ),
    );
    try {
      final response = await AuthApi(ApiClient(http)).register(
        name: 'Ana',
        email: 'ana@example.test',
        password: 'synthetic-test-password',
        analyticsConsent: false,
      );
      expect(response.user.id, 'user-1');
      expect(received, 1);
      expect(http.options.receiveTimeout, const Duration(milliseconds: 20));
    } finally {
      http.close(force: true);
      await listener.cancel();
      await server.close(force: true);
    }
  });
}

const _response = <String, Object?>{
  'accessToken': 'synthetic-access-token',
  'refreshToken': 'synthetic-refresh-token',
  'expiresIn': 3600,
  'usuario': <String, Object?>{
    'id': 'user-1',
    'tenantId': 'tenant-1',
    'nome': 'Ana',
    'email': 'ana@example.test',
    'role': 'Usuario',
  },
};
