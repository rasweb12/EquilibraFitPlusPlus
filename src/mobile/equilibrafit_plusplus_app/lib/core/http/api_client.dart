import 'dart:convert';

import 'package:dio/dio.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../config/app_config.dart';
import '../errors/app_failure.dart';
import '../storage/secure_token_store.dart';

final dioProvider = Provider<Dio>((ref) {
  final config = ref.watch(appConfigProvider);
  final tokenStore = ref.watch(secureTokenStoreProvider);

  final dio = Dio(
    BaseOptions(
      baseUrl: config.apiBaseUrl,
      connectTimeout: config.requestTimeout,
      receiveTimeout: config.requestTimeout,
      sendTimeout: config.requestTimeout,
      contentType: Headers.jsonContentType,
      responseType: ResponseType.json,
    ),
  );
  Future<void>? refreshInFlight;

  Future<void> refreshSession() async {
    final refreshToken = await tokenStore.readRefreshToken();
    if (refreshToken == null || refreshToken.isEmpty) {
      throw const AppFailure(
        'Entre novamente.',
        code: 'auth.invalid_refresh_token',
      );
    }
    final refreshClient = Dio(
      BaseOptions(
        baseUrl: config.apiBaseUrl,
        connectTimeout: config.requestTimeout,
        receiveTimeout: config.requestTimeout,
        sendTimeout: config.requestTimeout,
      ),
    );
    try {
      final response = await refreshClient.post<Map<String, dynamic>>(
        '/api/v1/auth/refresh',
        data: <String, String>{'refreshToken': refreshToken},
      );
      final access = response.data?['accessToken'];
      final rotated = response.data?['refreshToken'];
      if (access is! String ||
          rotated is! String ||
          access.isEmpty ||
          rotated.isEmpty) {
        throw const AppFailure(
          'Entre novamente.',
          code: 'auth.invalid_refresh_token',
        );
      }
      if (await tokenStore.readRefreshToken() != refreshToken) {
        throw const AppFailure('A sessao mudou.', code: 'auth.session_changed');
      }
      await tokenStore.saveTokens(accessToken: access, refreshToken: rotated);
    } finally {
      refreshClient.close();
    }
  }

  dio.interceptors.add(
    InterceptorsWrapper(
      onRequest: (options, handler) async {
        if (kDebugMode) {
          debugPrint(
            '[EquilibraFit API] --> ${options.method} ${options.uri}',
          );
        }

        final token = await tokenStore.readAccessToken();
        final expectedUser = options.headers.remove('X-Local-Expected-User') ??
            options.extra['expectedUser'];
        if (expectedUser != null) {
          options.extra['expectedUser'] = expectedUser;
          if (token == null || _tokenSubject(token) != expectedUser) {
            handler.reject(
              DioException(
                requestOptions: options,
                error: const AppFailure(
                  'A sessao mudou.',
                  code: 'auth.session_changed',
                ),
              ),
            );
            return;
          }
        }

        if (token != null && token.isNotEmpty) {
          options.headers['Authorization'] = 'Bearer $token';
        }

        handler.next(options);
      },
      onResponse: (response, handler) {
        if (kDebugMode) {
          debugPrint(
            '[EquilibraFit API] <-- '
            '${response.statusCode} '
            '${response.requestOptions.uri}',
          );
        }

        handler.next(response);
      },
      onError: (error, handler) async {
        final request = error.requestOptions;
        if (error.response?.statusCode == 401 &&
            !request.path.startsWith('/api/v1/auth/') &&
            request.extra['sessionRetried'] != true) {
          try {
            refreshInFlight ??=
                refreshSession().whenComplete(() => refreshInFlight = null);
            await refreshInFlight;
            request.extra['sessionRetried'] = true;
            request.headers['Authorization'] =
                'Bearer ${await tokenStore.readAccessToken()}';
            handler.resolve(await dio.fetch<Object?>(request));
            return;
          } catch (_) {
            handler.reject(error);
            return;
          }
        }
        if (kDebugMode) {
          debugPrint(
            '[EquilibraFit API] xx '
            '${error.response?.statusCode ?? '-'} '
            '${error.requestOptions.uri} '
            '${error.type}',
          );
        }

        handler.reject(error);
      },
    ),
  );

  return dio;
});

final apiClientProvider = Provider<ApiClient>(
  (ref) => ApiClient(ref.watch(dioProvider)),
);

class ApiClient {
  const ApiClient(this._dio);

  static const aiRequestTimeout = Duration(seconds: 75);

  final Dio _dio;

  Future<Map<String, Object?>> getJson(
    String path, {
    Map<String, Object?>? query,
  }) async {
    final response = await _guard(
      () => _dio.get<Object?>(
        path,
        queryParameters: query,
      ),
    );

    return _asObject(response.data);
  }

  Future<Map<String, Object?>> postJson(
    String path, {
    Map<String, Object?>? body,
    Map<String, Object?>? headers,
    Duration? timeout,
  }) async {
    final response = await _guard(
      () => _dio.post<Object?>(
        path,
        data: body,
        options: _options(headers, timeout: timeout),
      ),
    );

    return _asObject(response.data);
  }

  Future<List<Map<String, Object?>>> postJsonList(
    String path, {
    Map<String, Object?>? body,
    Map<String, Object?>? headers,
    Duration? timeout,
  }) async {
    final response = await _guard(
      () => _dio.post<Object?>(
        path,
        data: body,
        options: _options(headers, timeout: timeout),
      ),
    );

    return _asObjectList(response.data);
  }

  Future<Map<String, Object?>> putJson(
    String path, {
    Map<String, Object?>? body,
    Map<String, Object?>? headers,
  }) async {
    final response = await _guard(
      () => _dio.put<Object?>(
        path,
        data: body,
        options: _options(headers),
      ),
    );

    return _asObject(response.data);
  }

  Future<void> delete(
    String path, {
    Map<String, Object?>? query,
    Map<String, Object?>? headers,
  }) async {
    await _guard(
      () => _dio.delete<Object?>(
        path,
        queryParameters: query,
        options: _options(headers),
      ),
    );
  }

  Future<Map<String, Object?>> deleteJson(
    String path, {
    Map<String, Object?>? query,
    Map<String, Object?>? headers,
  }) async {
    final response = await _guard(
      () => _dio.delete<Object?>(
        path,
        queryParameters: query,
        options: _options(headers),
      ),
    );

    return _asObject(response.data);
  }

  Future<Response<Object?>> _guard(
    Future<Response<Object?>> Function() request,
  ) async {
    try {
      return await request();
    } on DioException catch (error) {
      if (error.error is AppFailure) throw error.error! as AppFailure;
      throw _toFailure(error);
    }
  }

  Options? _options(
    Map<String, Object?>? headers, {
    Duration? timeout,
  }) {
    if ((headers == null || headers.isEmpty) && timeout == null) {
      return null;
    }

    return Options(
      headers: headers,
      sendTimeout: timeout,
      receiveTimeout: timeout,
    );
  }

  Map<String, Object?> _asObject(Object? value) {
    if (value == null) {
      return <String, Object?>{};
    }

    if (value is Map<String, Object?>) {
      return value;
    }

    if (value is Map) {
      return value.map(
        (key, item) => MapEntry(
          key.toString(),
          item,
        ),
      );
    }

    return <String, Object?>{};
  }

  List<Map<String, Object?>> _asObjectList(
    Object? value,
  ) {
    if (value is Iterable) {
      return value.map(_asObject).toList(growable: false);
    }

    return const <Map<String, Object?>>[];
  }

  AppFailure _toFailure(
    DioException error,
  ) {
    final statusCode = error.response?.statusCode;
    final data = error.response?.data;
    final apiMessage = statusCode != null && statusCode >= 500 && data is! Map
        ? null
        : _extractMessage(data);

    if (apiMessage != null && apiMessage.trim().isNotEmpty) {
      return AppFailure(
        apiMessage,
        code: statusCode?.toString(),
      );
    }

    if (error.response == null &&
        error.type == DioExceptionType.receiveTimeout) {
      return AppFailure(
        kDebugMode
            ? 'O servidor demorou para responder. '
                'API configurada: ${_dio.options.baseUrl}.'
            : 'O servidor demorou para responder. Tente novamente em instantes.',
        code: 'network_unavailable',
      );
    }

    if (error.response == null && _isConnectionFailure(error.type)) {
      return AppFailure(
        kDebugMode
            ? 'Não conseguimos conectar com a API agora. '
                'API configurada: ${_dio.options.baseUrl}.'
            : 'Não conseguimos conectar com a API agora. '
                'Confira se ela está iniciada e tente novamente.',
        code: 'network_unavailable',
      );
    }

    if (statusCode == 502 || statusCode == 503 || statusCode == 504) {
      return AppFailure(
        statusCode == 504
            ? 'O servidor demorou para responder. Tente novamente em instantes.'
            : 'O serviço está temporariamente indisponível. '
                'Tente novamente em instantes.',
        code: statusCode.toString(),
      );
    }

    if (statusCode == 401) {
      return const AppFailure(
        'Sua sessão precisa ser atualizada.',
      );
    }

    if (statusCode == 403) {
      return const AppFailure(
        'Você não tem permissão para concluir esta ação.',
      );
    }

    if (statusCode == 404) {
      return const AppFailure(
        'Não encontramos o recurso solicitado.',
        code: '404',
      );
    }

    if (statusCode == 409) {
      return const AppFailure(
        'Não foi possível concluir porque existe um conflito com os dados atuais.',
        code: '409',
      );
    }

    if (statusCode == 422) {
      return const AppFailure(
        'Alguns dados informados precisam ser revisados.',
        code: '422',
      );
    }

    if (statusCode == 429) {
      return const AppFailure(
        'O uso está intenso agora. Vamos tentar novamente em instantes.',
        code: '429',
      );
    }

    if (kDebugMode && statusCode != null) {
      return AppFailure(
        'Não conseguimos concluir agora. '
        'Podemos tentar novamente? '
        'Código HTTP $statusCode.',
        code: statusCode.toString(),
      );
    }

    return const AppFailure(
      'Não conseguimos concluir agora. Podemos tentar novamente.',
    );
  }

  String? _extractMessage(
    Object? data,
  ) {
    if (data is String && data.trim().isNotEmpty) {
      return data;
    }

    if (data is! Map) {
      return null;
    }

    final normalized = data.map(
      (key, value) => MapEntry(
        key.toString(),
        value,
      ),
    );

    final message = normalized['message'] ??
        normalized['Message'] ??
        normalized['mensagem'] ??
        normalized['Mensagem'];

    if (message is String && message.trim().isNotEmpty) {
      return message;
    }

    final detail = normalized['detail'] ??
        normalized['Detail'] ??
        normalized['detalhe'] ??
        normalized['Detalhe'];

    if (detail is String && detail.trim().isNotEmpty) {
      return detail;
    }

    final title = normalized['title'] ?? normalized['Title'];

    if (title is String && title.trim().isNotEmpty) {
      return title;
    }

    final details = normalized['details'] ??
        normalized['Details'] ??
        normalized['detalhes'] ??
        normalized['Detalhes'];

    if (details is Iterable) {
      for (final item in details) {
        if (item is Map) {
          final itemMessage = item['message'] ??
              item['Message'] ??
              item['mensagem'] ??
              item['Mensagem'];

          if (itemMessage is String && itemMessage.trim().isNotEmpty) {
            return itemMessage;
          }
        }
      }
    }

    final errors = normalized['errors'] ??
        normalized['Errors'] ??
        normalized['erros'] ??
        normalized['Erros'];

    if (errors is Map) {
      for (final value in errors.values) {
        if (value is Iterable) {
          for (final item in value) {
            if (item is String && item.trim().isNotEmpty) {
              return item;
            }
          }
        }

        if (value is String && value.trim().isNotEmpty) {
          return value;
        }
      }
    }

    return null;
  }

  bool _isConnectionFailure(
    DioExceptionType type,
  ) {
    return type == DioExceptionType.connectionError ||
        type == DioExceptionType.connectionTimeout ||
        type == DioExceptionType.receiveTimeout ||
        type == DioExceptionType.sendTimeout ||
        type == DioExceptionType.unknown;
  }
}

String? _tokenSubject(String token) {
  try {
    final parts = token.split('.');
    if (parts.length != 3) return null;
    final payload = jsonDecode(
      utf8.decode(base64Url.decode(base64Url.normalize(parts[1]))),
    );
    return payload is Map ? payload['sub']?.toString() : null;
  } on FormatException {
    return null;
  }
}
