import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../ai/ai_pending_request_service.dart';
import '../errors/app_failure.dart';
import '../http/api_client.dart';
import '../storage/local_database.dart';
import 'sync_service.dart';
import 'sync_status.dart';
import 'sync_owner.dart';

final syncEngineProvider = Provider<SyncEngine>((ref) {
  final engine = SyncEngine(ref);
  ref.onDispose(engine.dispose);
  return engine;
});

class SyncEngine {
  SyncEngine(this._ref);

  final Ref _ref;
  Timer? _timer;
  bool _syncing = false;

  void start() {
    _timer ??= Timer.periodic(
      const Duration(seconds: 45),
      (_) => syncPendingOnce(),
    );
    unawaited(syncPendingOnce());
  }

  void dispose() {
    _timer?.cancel();
    _timer = null;
  }

  Future<void> syncPendingOnce() async {
    if (_syncing || _ref.read(syncOwnerProvider) == null) {
      return;
    }

    _syncing = true;
    try {
      final sync = _ref.read(syncServiceProvider);
      final items = await sync.pendingOperations();

      for (final item in items) {
        if (!_owns(item.payload['tenantId']?.toString(),
            item.payload['usuarioId']?.toString(),)) {
          break;
        }
        final shouldContinue = await _syncItem(sync, item);
        if (!shouldContinue) {
          break;
        }
      }

      await _syncPendingAiRequests();
    } finally {
      _syncing = false;
    }
  }

  Future<void> _syncPendingAiRequests() async {
    final pendingService = _ref.read(aiPendingRequestServiceProvider);
    final pendingRequests = await pendingService.pending();

    for (final request in pendingRequests) {
      if (!_owns(request.tenantId, request.usuarioId)) break;
      await pendingService.markSyncing(request.id);
      try {
        final result = await _ref.read(apiClientProvider).postJson(
              _aiPath(request),
              body: _aiBody(request),
              headers: <String, Object?>{
                'Idempotency-Key': request.operationId,
                'X-Local-Expected-User': request.usuarioId,
              },
              timeout: ApiClient.aiRequestTimeout,
            );
        await pendingService.markCompleted(request.id, result);
      } on AppFailure catch (failure) {
        await pendingService.markFailed(
          request.id,
          retryable: failure.code == 'network_unavailable',
        );
        if (failure.code == 'network_unavailable') {
          break;
        }
      } catch (_) {
        await pendingService.markFailed(request.id);
      }
    }
  }

  Future<bool> _syncItem(SyncService sync, SyncOutboxItem item) async {
    await sync.markSyncing(item.id);

    try {
      switch (item.operation) {
        case 'workout_session.register':
          final response = await _postJson(item);
          await _markWorkoutSessionSynced(item, response);
          break;
        case 'workout_exercise.complete':
          await _putJson(item);
          break;
        case 'workout_exercise.add':
          await _postJson(item);
          break;
        case 'workout_exercise.update':
          await _putJson(item);
          break;
        case 'workout_exercise.remove':
          await _delete(item);
          break;
        default:
          await sync.markFailed(
            item.id,
            'Operação offline desconhecida: ${item.operation}',
            retryable: false,
          );
          return true;
      }

      await sync.markSynced(item.id);
      return true;
    } on AppFailure catch (failure) {
      if (failure.code == '409') {
        await sync.markConflict(item.id, failure.message);
        return true;
      }

      await sync.markFailed(
        item.id,
        failure.message,
        retryable: failure.code == 'network_unavailable',
      );

      return failure.code != 'network_unavailable';
    } catch (error) {
      await sync.markFailed(item.id, error.toString());
      return true;
    }
  }

  Future<Map<String, Object?>> _postJson(SyncOutboxItem item) {
    final client = _ref.read(apiClientProvider);
    return client.postJson(
      _path(item),
      body: _body(item),
      headers: _idempotencyHeaders(item),
    );
  }

  Future<Map<String, Object?>> _putJson(SyncOutboxItem item) {
    final client = _ref.read(apiClientProvider);
    return client.putJson(
      _path(item),
      body: _body(item),
      headers: _idempotencyHeaders(item),
    );
  }

  Future<void> _delete(SyncOutboxItem item) {
    final client = _ref.read(apiClientProvider);
    return client.delete(
      _path(item),
      headers: _idempotencyHeaders(item),
    );
  }

  Future<void> _markWorkoutSessionSynced(
    SyncOutboxItem item,
    Map<String, Object?> response,
  ) async {
    final database = await _ref.read(databaseProvider.future);
    final now = DateTime.now().toUtc().toIso8601String();
    await database.update(
      'workout_session_cache',
      <String, Object?>{
        'remote_id': _remoteId(response),
        'sync_status': SyncStatus.synced.value,
        'server_updated_at': now,
      },
      where: 'operation_id = ?',
      whereArgs: [item.operationId],
    );
  }

  String _path(SyncOutboxItem item) {
    return item.payload['path']?.toString() ?? '';
  }

  Map<String, Object?> _body(SyncOutboxItem item) {
    final body = item.payload['body'];

    if (body is Map<String, Object?>) {
      return body;
    }

    if (body is Map) {
      return body.map((key, value) => MapEntry(key.toString(), value));
    }

    return const <String, Object?>{};
  }

  Map<String, Object?> _idempotencyHeaders(SyncOutboxItem item) {
    return <String, Object?>{
      'Idempotency-Key': item.operationId,
      'X-Local-Expected-User': item.payload['usuarioId'],
    };
  }

  String? _remoteId(Map<String, Object?> response) {
    final value = response['id'] ?? response['Id'];
    final text = value?.toString().trim() ?? '';
    return text.isEmpty ? null : text;
  }

  bool _owns(String? tenantId, String? usuarioId) {
    final owner = _ref.read(syncOwnerProvider);
    return owner != null && owner.tenantId == tenantId && owner.id == usuarioId;
  }

  String _aiPath(AiPendingRequest request) {
    return request.payload['path']?.toString() ?? '/api/v1/ia/coach/mensagens';
  }

  Map<String, Object?> _aiBody(AiPendingRequest request) {
    final body = request.payload['body'];

    if (body is Map<String, Object?>) {
      return body;
    }

    if (body is Map) {
      return body.map((key, value) => MapEntry(key.toString(), value));
    }

    return const <String, Object?>{};
  }
}
