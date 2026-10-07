import 'dart:convert';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:sqflite/sqflite.dart';

import '../storage/local_database.dart';
import '../sync/sync_status.dart';
import '../sync/sync_owner.dart';

final aiPendingRequestServiceProvider = Provider<AiPendingRequestService>(
  AiPendingRequestService.new,
);

abstract interface class AiPendingRequestStore {
  Future<void> enqueue({
    required String operationId,
    required String tenantId,
    required String usuarioId,
    required String type,
    required Map<String, Object?> payload,
  });
}

class AiPendingRequestService implements AiPendingRequestStore {
  const AiPendingRequestService(this._ref);

  final Ref _ref;

  @override
  Future<void> enqueue({
    required String operationId,
    required String tenantId,
    required String usuarioId,
    required String type,
    required Map<String, Object?> payload,
  }) async {
    final database = await _ref.read(databaseProvider.future);
    final now = DateTime.now().toUtc().toIso8601String();

    await database.insert(
      'ai_pending_request',
      <String, Object?>{
        'id': operationId,
        'operation_id': operationId,
        'tenant_id': tenantId,
        'usuario_id': usuarioId,
        'type': type,
        'payload_json': jsonEncode(payload),
        'result_json': null,
        'status': SyncStatus.pending.value,
        'created_at': now,
        'completed_at': null,
      },
      conflictAlgorithm: ConflictAlgorithm.ignore,
    );
  }

  Future<List<AiPendingRequest>> pending({
    int limit = 10,
  }) async {
    final owner = _ref.read(syncOwnerProvider);
    if (owner == null) return const [];
    final database = await _ref.read(databaseProvider.future);
    final rows = await database.query(
      'ai_pending_request',
      where: 'tenant_id = ? AND usuario_id = ? AND status IN (?, ?)',
      whereArgs: [
        owner.tenantId,
        owner.id,
        SyncStatus.pending.value,
        SyncStatus.syncing.value,
      ],
      orderBy: 'created_at ASC',
      limit: limit,
    );

    return rows.map(AiPendingRequest.fromRow).toList(growable: false);
  }

  Future<void> markSyncing(String id) {
    return _updateStatus(id, SyncStatus.syncing);
  }

  Future<void> markFailed(
    String id, {
    bool retryable = false,
  }) {
    return _updateStatus(
      id,
      retryable ? SyncStatus.pending : SyncStatus.failed,
    );
  }

  Future<void> markCompleted(
    String id,
    Map<String, Object?> result,
  ) async {
    final database = await _ref.read(databaseProvider.future);
    await database.update(
      'ai_pending_request',
      <String, Object?>{
        'status': SyncStatus.synced.value,
        'result_json': jsonEncode(result),
        'completed_at': DateTime.now().toUtc().toIso8601String(),
      },
      where: 'id = ?',
      whereArgs: [id],
    );
  }

  Future<void> _updateStatus(String id, SyncStatus status) async {
    final database = await _ref.read(databaseProvider.future);
    await database.update(
      'ai_pending_request',
      <String, Object?>{'status': status.value},
      where: 'id = ?',
      whereArgs: [id],
    );
  }
}

class AiPendingRequest {
  const AiPendingRequest({
    required this.id,
    required this.operationId,
    required this.type,
    required this.payload,
    this.tenantId = '',
    this.usuarioId = '',
  });

  factory AiPendingRequest.fromRow(Map<String, Object?> row) {
    return AiPendingRequest(
      id: row['id']?.toString() ?? '',
      operationId: row['operation_id']?.toString() ?? '',
      type: row['type']?.toString() ?? '',
      tenantId: row['tenant_id']?.toString() ?? '',
      usuarioId: row['usuario_id']?.toString() ?? '',
      payload: _decodePayload(row['payload_json']),
    );
  }

  final String id;
  final String operationId;
  final String type;
  final String tenantId;
  final String usuarioId;
  final Map<String, Object?> payload;
}

Map<String, Object?> _decodePayload(Object? value) {
  if (value is! String || value.trim().isEmpty) {
    return const <String, Object?>{};
  }

  final decoded = jsonDecode(value);
  if (decoded is Map<String, Object?>) {
    return decoded;
  }

  if (decoded is Map) {
    return decoded.map((key, item) => MapEntry(key.toString(), item));
  }

  return const <String, Object?>{};
}
