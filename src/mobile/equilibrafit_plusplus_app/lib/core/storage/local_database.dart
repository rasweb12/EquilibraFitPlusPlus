import 'dart:convert';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:path/path.dart' as path;
import 'package:sqflite/sqflite.dart';

const _databaseVersion = 5;

final databaseProvider = FutureProvider<Database>((ref) async {
  final databasePath = await getDatabasesPath();
  final filePath = path.join(databasePath, 'equilibrafit_plusplus.db');

  return openLocalDatabase(filePath);
});

Future<Database> openLocalDatabase(String filePath) {
  return openDatabase(
    filePath,
    version: _databaseVersion,
    onCreate: _createSchema,
    onUpgrade: _upgradeSchema,
  );
}

Future<void> _createSchema(Database database, int version) async {
  await database.execute('''
    CREATE TABLE sync_queue (
      id TEXT PRIMARY KEY,
      operation TEXT NOT NULL,
      entity_type TEXT NOT NULL,
      payload TEXT NOT NULL,
      status TEXT NOT NULL,
      attempts INTEGER NOT NULL DEFAULT 0,
      created_at TEXT NOT NULL
    )
  ''');

  await database.execute('''
    CREATE TABLE cached_plan (
      id TEXT PRIMARY KEY,
      payload TEXT NOT NULL,
      updated_at TEXT NOT NULL
    )
  ''');

  await _createOfflineFirstTables(database);
  await _addOutboxOwnership(database);
}

Future<void> _upgradeSchema(
  Database database,
  int oldVersion,
  int newVersion,
) async {
  if (oldVersion < 2) {
    await _createOfflineFirstTables(database);
  }

  if (oldVersion < 3) {
    await _createWorkoutSessionTables(database);
  }

  if (oldVersion < 4) {
    await _createAiPendingRequestTables(database);
  }
  if (oldVersion < 5) {
    await _addOutboxOwnership(database);
  }
}

Future<void> _addOutboxOwnership(Database database) async {
  await database.execute('ALTER TABLE sync_outbox ADD COLUMN tenant_id TEXT');
  await database.execute('ALTER TABLE sync_outbox ADD COLUMN usuario_id TEXT');
  final rows = await database.query('sync_outbox');
  for (final row in rows) {
    try {
      final payload = jsonDecode(row['payload_json'] as String);
      if (payload is Map &&
          payload['tenantId'] is String &&
          payload['usuarioId'] is String) {
        await database.update(
            'sync_outbox',
            <String, Object?>{
              'tenant_id': payload['tenantId'],
              'usuario_id': payload['usuarioId'],
            },
            where: 'id = ?',
            whereArgs: [row['id']],);
      }
    } on FormatException {
      // Unidentifiable legacy requests stay quarantined, never sent as another user.
    }
  }
  await database.execute('''
    CREATE INDEX idx_sync_outbox_owner_status
    ON sync_outbox(tenant_id, usuario_id, status, created_at)
  ''');
}

Future<void> _createOfflineFirstTables(Database database) async {
  await database.execute('''
    CREATE TABLE IF NOT EXISTS sync_outbox (
      id TEXT PRIMARY KEY,
      operation_id TEXT NOT NULL,
      entity_type TEXT NOT NULL,
      entity_id TEXT,
      remote_id TEXT,
      operation TEXT NOT NULL,
      payload_json TEXT NOT NULL,
      created_at TEXT NOT NULL,
      updated_at TEXT NOT NULL,
      attempts INTEGER NOT NULL DEFAULT 0,
      last_attempt_at TEXT,
      last_error TEXT,
      status TEXT NOT NULL,
      depends_on_operation_id TEXT
    )
  ''');

  await database.execute('''
    CREATE UNIQUE INDEX IF NOT EXISTS idx_sync_outbox_operation_id
    ON sync_outbox(operation_id)
  ''');
  await database.execute('''
    CREATE INDEX IF NOT EXISTS idx_sync_outbox_status_created
    ON sync_outbox(status, created_at)
  ''');
  await database.execute('''
    CREATE INDEX IF NOT EXISTS idx_sync_outbox_entity_created
    ON sync_outbox(entity_type, created_at)
  ''');
  await database.execute('''
    CREATE INDEX IF NOT EXISTS idx_sync_outbox_remote_id
    ON sync_outbox(remote_id)
  ''');

  await database.execute('''
    CREATE TABLE IF NOT EXISTS workout_cache (
      local_id TEXT PRIMARY KEY,
      remote_id TEXT,
      tenant_id TEXT NOT NULL,
      usuario_id TEXT NOT NULL,
      payload_json TEXT NOT NULL,
      sync_status TEXT NOT NULL,
      local_updated_at TEXT NOT NULL,
      server_updated_at TEXT,
      server_version TEXT
    )
  ''');

  await database.execute('''
    CREATE INDEX IF NOT EXISTS idx_workout_cache_owner
    ON workout_cache(tenant_id, usuario_id)
  ''');
  await database.execute('''
    CREATE INDEX IF NOT EXISTS idx_workout_cache_remote_id
    ON workout_cache(remote_id)
  ''');
  await database.execute('''
    CREATE INDEX IF NOT EXISTS idx_workout_cache_sync_status
    ON workout_cache(sync_status)
  ''');
  await database.execute('''
    CREATE INDEX IF NOT EXISTS idx_workout_cache_local_updated_at
    ON workout_cache(local_updated_at)
  ''');

  await _createWorkoutSessionTables(database);
  await _createAiPendingRequestTables(database);
}

Future<void> _createWorkoutSessionTables(Database database) async {
  await database.execute('''
    CREATE TABLE IF NOT EXISTS workout_session_cache (
      local_id TEXT PRIMARY KEY,
      remote_id TEXT,
      tenant_id TEXT NOT NULL,
      usuario_id TEXT NOT NULL,
      workout_id TEXT NOT NULL,
      training_day INTEGER NOT NULL,
      operation_id TEXT NOT NULL,
      payload_json TEXT NOT NULL,
      summary TEXT NOT NULL,
      sync_status TEXT NOT NULL,
      local_updated_at TEXT NOT NULL,
      server_updated_at TEXT
    )
  ''');

  await database.execute('''
    CREATE UNIQUE INDEX IF NOT EXISTS idx_workout_session_operation_id
    ON workout_session_cache(operation_id)
  ''');
  await database.execute('''
    CREATE INDEX IF NOT EXISTS idx_workout_session_owner
    ON workout_session_cache(tenant_id, usuario_id)
  ''');
  await database.execute('''
    CREATE INDEX IF NOT EXISTS idx_workout_session_workout
    ON workout_session_cache(workout_id, training_day)
  ''');
  await database.execute('''
    CREATE INDEX IF NOT EXISTS idx_workout_session_sync_status
    ON workout_session_cache(sync_status)
  ''');
}

Future<void> _createAiPendingRequestTables(Database database) async {
  await database.execute('''
    CREATE TABLE IF NOT EXISTS ai_pending_request (
      id TEXT PRIMARY KEY,
      operation_id TEXT NOT NULL,
      tenant_id TEXT NOT NULL,
      usuario_id TEXT NOT NULL,
      type TEXT NOT NULL,
      payload_json TEXT NOT NULL,
      result_json TEXT,
      status TEXT NOT NULL,
      created_at TEXT NOT NULL,
      completed_at TEXT
    )
  ''');

  await database.execute('''
    CREATE UNIQUE INDEX IF NOT EXISTS idx_ai_pending_request_operation_id
    ON ai_pending_request(operation_id)
  ''');
  await database.execute('''
    CREATE INDEX IF NOT EXISTS idx_ai_pending_request_owner
    ON ai_pending_request(tenant_id, usuario_id)
  ''');
  await database.execute('''
    CREATE INDEX IF NOT EXISTS idx_ai_pending_request_status_created
    ON ai_pending_request(status, created_at)
  ''');
}
