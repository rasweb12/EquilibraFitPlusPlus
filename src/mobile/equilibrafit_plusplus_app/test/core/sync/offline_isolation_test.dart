import 'dart:io';

import 'package:equilibrafit_plusplus_app/core/ai/ai_pending_request_service.dart';
import 'package:equilibrafit_plusplus_app/core/storage/local_database.dart';
import 'package:equilibrafit_plusplus_app/core/sync/sync_owner.dart';
import 'package:equilibrafit_plusplus_app/core/sync/sync_service.dart';
import 'package:equilibrafit_plusplus_app/features/auth/domain/user_session.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:sqflite_common_ffi/sqflite_ffi.dart';

void main() {
  setUpAll(() {
    sqfliteFfiInit();
    databaseFactory = databaseFactoryFfi;
  });

  test('restart preserves outbox; switching users scopes sync and pending AI',
      () async {
    final folder =
        await Directory.systemTemp.createTemp('equilibrafit-plusplus-');
    addTearDown(() => folder.delete(recursive: true));
    final file = '${folder.path}/offline.db';
    var database = await openLocalDatabase(file);
    UserSession? owner = _user('a');
    ProviderContainer createContainer() => ProviderContainer(overrides: [
          databaseProvider.overrideWith((ref) async => database),
          syncOwnerProvider.overrideWith((ref) => owner),
        ],);
    var container = createContainer();
    var sync = container.read(syncServiceProvider);
    await sync.enqueue(
        id: 'op-a',
        operation: 'workout_session.register',
        entityType: 'workout',
        payload: <String, Object?>{
          'tenantId': 'tenant',
          'usuarioId': 'a',
          'body': <String, Object?>{'repeticoes': 12},
        },);
    await sync.enqueue(
        id: 'op-b',
        operation: 'workout_session.register',
        entityType: 'workout',
        payload: <String, Object?>{'tenantId': 'tenant', 'usuarioId': 'b'},);
    await container.read(aiPendingRequestServiceProvider).enqueue(
        operationId: 'ai-a',
        tenantId: 'tenant',
        usuarioId: 'a',
        type: 'coach',
        payload: const {},);
    await container.read(aiPendingRequestServiceProvider).enqueue(
        operationId: 'ai-b',
        tenantId: 'tenant',
        usuarioId: 'b',
        type: 'coach',
        payload: const {},);
    await sync.markSyncing('op-a');
    container.dispose();
    await database.close();
    database = await openLocalDatabase(file);
    container = createContainer();
    addTearDown(container.dispose);
    addTearDown(() => database.close());
    sync = container.read(syncServiceProvider);
    expect((await sync.pendingOperations()).single.operationId, 'op-a');
    expect((await sync.pendingOperations()).single.payload['body'],
        {'repeticoes': 12},);
    await sync.markConflict('op-a', '409');
    expect((await sync.summary()).conflict, 1);
    await sync.retry('op-a');
    expect((await sync.pendingOperations()).single.attempts, 0);
    await sync.markSynced('op-a');
    await sync.enqueue(
        id: 'op-a',
        operation: 'workout_session.register',
        entityType: 'workout',
        payload: <String, Object?>{'tenantId': 'tenant', 'usuarioId': 'a'},);
    expect(await sync.pendingOperations(), isEmpty);
    expect(
        (await container.read(aiPendingRequestServiceProvider).pending())
            .single
            .id,
        'ai-a',);
    await container
        .read(aiPendingRequestServiceProvider)
        .markCompleted('ai-a', {'message': 'ok'});
    owner = null;
    container.invalidate(syncOwnerProvider);
    expect(await sync.operations(), isEmpty);
    expect(await container.read(aiPendingRequestServiceProvider).pending(),
        isEmpty,);
    owner = _user('b');
    container.invalidate(syncOwnerProvider);
    expect((await sync.pendingOperations()).single.operationId, 'op-b');
    expect(
        (await container.read(aiPendingRequestServiceProvider).pending())
            .single
            .id,
        'ai-b',);
    expect((await sync.operations()).any((item) => item.id == 'op-a'), isFalse);
  });
}

UserSession _user(String id) => UserSession(
    id: id,
    tenantId: 'tenant',
    name: id,
    email: '$id@example.test',
    role: 'Usuario',
    expiresIn: 3600,);
