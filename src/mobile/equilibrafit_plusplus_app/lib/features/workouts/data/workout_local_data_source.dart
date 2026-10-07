import 'dart:convert';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:sqflite/sqflite.dart';

import '../../../core/storage/local_database.dart';
import '../../../core/sync/sync_status.dart';

final workoutLocalDataSourceProvider = Provider<WorkoutLocalDataSource>(
  WorkoutLocalDataSource.new,
);

abstract interface class WorkoutLocalStore {
  Future<Map<String, Object?>?> readActiveWorkoutJson({
    required String tenantId,
    required String usuarioId,
  });

  Future<void> saveActiveWorkoutJson({
    required String tenantId,
    required String usuarioId,
    required Map<String, Object?> json,
  });

  Future<void> clearActiveWorkout({
    required String tenantId,
    required String usuarioId,
  });

  Future<void> updateExerciseCompletion({
    required String tenantId,
    required String usuarioId,
    required String exerciseId,
    required bool completed,
  });

  Future<void> savePendingSession({
    required String tenantId,
    required String usuarioId,
    required String workoutId,
    required int trainingDay,
    required String operationId,
    required Map<String, Object?> payload,
    required String summary,
  });

  Future<void> clearPrivateData({
    required String tenantId,
    required String usuarioId,
  });
}

class WorkoutLocalDataSource implements WorkoutLocalStore {
  const WorkoutLocalDataSource(this._ref);

  static const activeWorkoutLocalId = 'active_workout';

  final Ref _ref;

  @override
  Future<Map<String, Object?>?> readActiveWorkoutJson({
    required String tenantId,
    required String usuarioId,
  }) async {
    final database = await _ref.read(databaseProvider.future);
    final rows = await database.query(
      'workout_cache',
      columns: ['payload_json'],
      where: 'local_id = ? AND tenant_id = ? AND usuario_id = ?',
      whereArgs: [
        _activeLocalId(tenantId: tenantId, usuarioId: usuarioId),
        tenantId,
        usuarioId,
      ],
      limit: 1,
    );

    if (rows.isEmpty) {
      return null;
    }

    final payload = rows.first['payload_json'];
    if (payload is! String || payload.trim().isEmpty) {
      return null;
    }

    final Object? decoded;
    try {
      decoded = jsonDecode(payload);
    } on FormatException {
      return null;
    }

    if (decoded is Map<String, Object?>) {
      return decoded;
    }

    if (decoded is Map) {
      return decoded.map(
        (key, value) => MapEntry(key.toString(), value),
      );
    }

    return null;
  }

  @override
  Future<void> saveActiveWorkoutJson({
    required String tenantId,
    required String usuarioId,
    required Map<String, Object?> json,
  }) async {
    if (json.isEmpty) {
      return;
    }

    final database = await _ref.read(databaseProvider.future);
    final now = DateTime.now().toUtc().toIso8601String();

    await database.insert(
      'workout_cache',
      <String, Object?>{
        'local_id': _activeLocalId(
          tenantId: tenantId,
          usuarioId: usuarioId,
        ),
        'remote_id': _stringValue(json['id'] ?? json['Id']),
        'tenant_id': tenantId,
        'usuario_id': usuarioId,
        'payload_json': jsonEncode(json),
        'sync_status': SyncStatus.synced.value,
        'local_updated_at': now,
        'server_updated_at': now,
        'server_version': _stringValue(
          json['versao'] ?? json['Versao'],
        ),
      },
      conflictAlgorithm: ConflictAlgorithm.replace,
    );
  }

  @override
  Future<void> clearActiveWorkout({
    required String tenantId,
    required String usuarioId,
  }) async {
    final database = await _ref.read(databaseProvider.future);
    await database.delete(
      'workout_cache',
      where: 'local_id = ? AND tenant_id = ? AND usuario_id = ?',
      whereArgs: [
        _activeLocalId(tenantId: tenantId, usuarioId: usuarioId),
        tenantId,
        usuarioId,
      ],
    );
  }

  @override
  Future<void> updateExerciseCompletion({
    required String tenantId,
    required String usuarioId,
    required String exerciseId,
    required bool completed,
  }) async {
    final json = await readActiveWorkoutJson(
      tenantId: tenantId,
      usuarioId: usuarioId,
    );

    if (json == null) {
      return;
    }

    final exercises = json['exercicios'] ?? json['Exercicios'];
    if (exercises is! List) {
      return;
    }

    for (final item in exercises) {
      if (item is! Map) {
        continue;
      }

      final normalized = item.map(
        (key, value) => MapEntry(key.toString(), value),
      );
      final currentId = _stringValue(normalized['id'] ?? normalized['Id']);

      if (currentId == exerciseId) {
        normalized['concluidoHoje'] = completed;
        normalized['ConcluidoHoje'] = completed;
        item
          ..['concluidoHoje'] = completed
          ..['ConcluidoHoje'] = completed;
        break;
      }
    }

    await saveActiveWorkoutJson(
      tenantId: tenantId,
      usuarioId: usuarioId,
      json: json,
    );
  }

  @override
  Future<void> savePendingSession({
    required String tenantId,
    required String usuarioId,
    required String workoutId,
    required int trainingDay,
    required String operationId,
    required Map<String, Object?> payload,
    required String summary,
  }) async {
    final database = await _ref.read(databaseProvider.future);
    final now = DateTime.now().toUtc().toIso8601String();

    await database.insert(
      'workout_session_cache',
      <String, Object?>{
        'local_id': operationId,
        'remote_id': null,
        'tenant_id': tenantId,
        'usuario_id': usuarioId,
        'workout_id': workoutId,
        'training_day': trainingDay,
        'operation_id': operationId,
        'payload_json': jsonEncode(payload),
        'summary': summary,
        'sync_status': SyncStatus.pending.value,
        'local_updated_at': now,
        'server_updated_at': null,
      },
      conflictAlgorithm: ConflictAlgorithm.replace,
    );
  }

  @override
  Future<void> clearPrivateData({
    required String tenantId,
    required String usuarioId,
  }) async {
    final database = await _ref.read(databaseProvider.future);
    await database.delete(
      'workout_cache',
      where: 'tenant_id = ? AND usuario_id = ?',
      whereArgs: [tenantId, usuarioId],
    );
    await database.delete(
      'workout_session_cache',
      where: 'tenant_id = ? AND usuario_id = ?',
      whereArgs: [tenantId, usuarioId],
    );
    await database.delete(
      'sync_outbox',
      where: 'tenant_id = ? AND usuario_id = ?',
      whereArgs: [tenantId, usuarioId],
    );
    await database.delete(
      'ai_pending_request',
      where: 'tenant_id = ? AND usuario_id = ?',
      whereArgs: [tenantId, usuarioId],
    );
    await database.delete('sync_queue');
    await database.delete('cached_plan');
  }

  String _activeLocalId({
    required String tenantId,
    required String usuarioId,
  }) {
    return '$tenantId:$usuarioId:$activeWorkoutLocalId';
  }

  String? _stringValue(Object? value) {
    if (value == null) {
      return null;
    }

    final text = value.toString().trim();
    return text.isEmpty ? null : text;
  }
}
