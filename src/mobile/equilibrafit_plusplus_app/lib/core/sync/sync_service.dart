import 'dart:convert';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:sqflite/sqflite.dart';

import '../storage/local_database.dart';
import 'sync_status.dart';
import 'sync_owner.dart';

final syncServiceProvider = Provider<SyncService>((ref) => SyncService(ref));

final syncSummaryProvider = FutureProvider<SyncSummary>((ref) {
  ref.watch(syncOwnerProvider);
  return ref.watch(syncServiceProvider).summary();
});

final syncOperationsProvider = FutureProvider<List<SyncOutboxItem>>((ref) {
  ref.watch(syncOwnerProvider);
  return ref.watch(syncServiceProvider).operations(limit: 100);
});

class SyncService {
  const SyncService(this._ref);

  final Ref _ref;

  Future<void> enqueue({
    required String id,
    required String operation,
    required String entityType,
    required Map<String, Object?> payload,
    String? operationId,
    String? entityId,
    String? remoteId,
    String? dependsOnOperationId,
  }) async {
    final database = await _ref.read(databaseProvider.future);
    final now = DateTime.now().toUtc().toIso8601String();

    final tenantId = payload['tenantId']?.toString();
    final usuarioId = payload['usuarioId']?.toString();
    if (tenantId == null ||
        tenantId.isEmpty ||
        usuarioId == null ||
        usuarioId.isEmpty) {
      throw ArgumentError('An offline operation requires its tenant and user.');
    }

    await database.insert(
      'sync_outbox',
      <String, Object?>{
        'id': id,
        'operation_id': operationId ?? id,
        'tenant_id': tenantId,
        'usuario_id': usuarioId,
        'operation': operation,
        'entity_type': entityType,
        'entity_id': entityId,
        'remote_id': remoteId,
        'payload_json': jsonEncode(payload),
        'status': SyncStatus.pending.value,
        'attempts': 0,
        'created_at': now,
        'updated_at': now,
        'depends_on_operation_id': dependsOnOperationId,
      },
      conflictAlgorithm: ConflictAlgorithm.ignore,
    );

    await database.insert(
      'sync_queue',
      <String, Object?>{
        'id': id,
        'operation': operation,
        'entity_type': entityType,
        'payload': jsonEncode(payload),
        'status': SyncStatus.pending.value,
        'attempts': 0,
        'created_at': now,
      },
      conflictAlgorithm: ConflictAlgorithm.ignore,
    );
  }

  Future<List<SyncOutboxItem>> pendingOperations({
    int limit = 20,
  }) async {
    final owner = _ref.read(syncOwnerProvider);
    if (owner == null) return const [];
    final database = await _ref.read(databaseProvider.future);
    final rows = await database.query(
      'sync_outbox',
      where:
          'tenant_id = ? AND usuario_id = ? AND status IN (?, ?) AND attempts < ?',
      whereArgs: [
        owner.tenantId,
        owner.id,
        SyncStatus.pending.value,
        SyncStatus.syncing.value,
        5,
      ],
      orderBy: 'created_at ASC',
      limit: limit,
    );

    return rows.map(SyncOutboxItem.fromRow).toList(growable: false);
  }

  Future<List<SyncOutboxItem>> operations({
    int limit = 100,
  }) async {
    final owner = _ref.read(syncOwnerProvider);
    if (owner == null) return const [];
    final database = await _ref.read(databaseProvider.future);
    final rows = await database.query(
      'sync_outbox',
      where: 'tenant_id = ? AND usuario_id = ?',
      whereArgs: [owner.tenantId, owner.id],
      orderBy: 'created_at DESC',
      limit: limit,
    );

    return rows.map(SyncOutboxItem.fromRow).toList(growable: false);
  }

  Future<SyncSummary> summary() async {
    final owner = _ref.read(syncOwnerProvider);
    final database = await _ref.read(databaseProvider.future);
    final rows = await database.rawQuery(
      '''
      SELECT status, COUNT(*) AS total
      FROM sync_outbox
      WHERE tenant_id = ? AND usuario_id = ?
      GROUP BY status
      ''',
      [owner?.tenantId, owner?.id],
    );

    final counts = <String, int>{};
    for (final row in rows) {
      final total = row['total'];
      counts[row['status']?.toString() ?? ''] =
          total is int ? total : int.tryParse(total?.toString() ?? '') ?? 0;
    }

    return SyncSummary(
      pending: counts[SyncStatus.pending.value] ?? 0,
      syncing: counts[SyncStatus.syncing.value] ?? 0,
      synced: counts[SyncStatus.synced.value] ?? 0,
      failed: counts[SyncStatus.failed.value] ?? 0,
      conflict: counts[SyncStatus.conflict.value] ?? 0,
    );
  }

  Future<void> markSyncing(String id) {
    return _updateStatus(id, SyncStatus.syncing);
  }

  Future<void> markSynced(String id) {
    return _updateStatus(id, SyncStatus.synced);
  }

  Future<void> markConflict(String id, String message) {
    return _updateStatus(id, SyncStatus.conflict, error: message);
  }

  Future<void> retry(String id) async {
    final database = await _ref.read(databaseProvider.future);
    final now = DateTime.now().toUtc().toIso8601String();
    await database.update(
      'sync_outbox',
      <String, Object?>{
        'status': SyncStatus.pending.value,
        'attempts': 0,
        'last_attempt_at': null,
        'last_error': null,
        'updated_at': now,
      },
      where: 'id = ?',
      whereArgs: [id],
    );
  }

  Future<void> markFailed(
    String id,
    String message, {
    bool retryable = true,
  }) async {
    final database = await _ref.read(databaseProvider.future);
    final now = DateTime.now().toUtc().toIso8601String();
    await database.rawUpdate(
      '''
      UPDATE sync_outbox
      SET status = ?,
          attempts = attempts + 1,
          last_attempt_at = ?,
          last_error = ?,
          updated_at = ?
      WHERE id = ?
      ''',
      [
        retryable ? SyncStatus.pending.value : SyncStatus.failed.value,
        now,
        message,
        now,
        id,
      ],
    );
  }

  Future<void> _updateStatus(
    String id,
    SyncStatus status, {
    String? error,
  }) async {
    final database = await _ref.read(databaseProvider.future);
    final now = DateTime.now().toUtc().toIso8601String();
    await database.update(
      'sync_outbox',
      <String, Object?>{
        'status': status.value,
        'last_attempt_at': now,
        'last_error': error,
        'updated_at': now,
      },
      where: 'id = ?',
      whereArgs: [id],
    );
  }
}

class SyncSummary {
  const SyncSummary({
    required this.pending,
    required this.syncing,
    required this.synced,
    required this.failed,
    required this.conflict,
  });

  final int pending;
  final int syncing;
  final int synced;
  final int failed;
  final int conflict;

  int get attentionCount => pending + syncing + failed + conflict;
  int get issueCount => failed + conflict;
  bool get hasWork => attentionCount > 0;
}

class SyncOutboxItem {
  const SyncOutboxItem({
    required this.id,
    required this.operationId,
    required this.operation,
    required this.entityType,
    required this.payload,
    required this.attempts,
    required this.status,
    this.entityId,
    this.remoteId,
    this.lastError,
    this.dependsOnOperationId,
  });

  factory SyncOutboxItem.fromRow(Map<String, Object?> row) {
    final attemptsValue = row['attempts'];

    return SyncOutboxItem(
      id: row['id']?.toString() ?? '',
      operationId: row['operation_id']?.toString() ?? '',
      operation: row['operation']?.toString() ?? '',
      entityType: row['entity_type']?.toString() ?? '',
      entityId: row['entity_id']?.toString(),
      remoteId: row['remote_id']?.toString(),
      payload: _decodePayload(row['payload_json']),
      attempts: attemptsValue is int
          ? attemptsValue
          : int.tryParse(attemptsValue?.toString() ?? '') ?? 0,
      status: row['status']?.toString() ?? SyncStatus.pending.value,
      lastError: row['last_error']?.toString(),
      dependsOnOperationId: row['depends_on_operation_id']?.toString(),
    );
  }

  final String id;
  final String operationId;
  final String operation;
  final String entityType;
  final String? entityId;
  final String? remoteId;
  final Map<String, Object?> payload;
  final int attempts;
  final String status;
  final String? lastError;
  final String? dependsOnOperationId;
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
