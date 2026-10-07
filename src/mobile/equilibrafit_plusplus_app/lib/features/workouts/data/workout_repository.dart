import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/errors/app_failure.dart';
import '../../../core/http/api_client.dart';
import '../../../core/sync/sync_engine.dart';
import '../../../core/sync/sync_service.dart';
import '../../../core/utils/json_helpers.dart';
import '../../../core/utils/local_id_generator.dart';
import '../../auth/presentation/controllers/session_controller.dart';
import '../domain/workout.dart';
import 'workout_local_data_source.dart';

final workoutRepositoryProvider = Provider<WorkoutRepository>((ref) {
  final usuarioId = ref.watch(
    sessionControllerProvider.select((state) => state.user?.id),
  );
  final tenantId = ref.watch(
    sessionControllerProvider.select((state) => state.user?.tenantId),
  );

  return WorkoutRepository(
    ref.watch(apiClientProvider),
    localDataSource: ref.watch(workoutLocalDataSourceProvider),
    syncService: ref.watch(syncServiceProvider),
    syncEngine: ref.watch(syncEngineProvider),
    tenantId: tenantId,
    usuarioId: usuarioId,
  );
});

final activeWorkoutProvider = FutureProvider.autoDispose<Workout?>((ref) {
  return ref.watch(workoutRepositoryProvider).getActive();
});

class WorkoutRepository {
  const WorkoutRepository(
    this._client, {
    WorkoutLocalStore? localDataSource,
    SyncService? syncService,
    SyncEngine? syncEngine,
    String? tenantId,
    String? usuarioId,
  })  : _localDataSource = localDataSource,
        _syncService = syncService,
        _syncEngine = syncEngine,
        _tenantId = tenantId ?? _defaultTenantId,
        _usuarioId = usuarioId;

  static const _defaultTenantId = 'default';

  final ApiClient _client;
  final WorkoutLocalStore? _localDataSource;
  final SyncService? _syncService;
  final SyncEngine? _syncEngine;
  final String _tenantId;
  final String? _usuarioId;

  Future<Workout?> getActive() async {
    final cachedJson = await _readCachedActiveWorkoutJson();
    final Map<String, Object?> json;

    try {
      json = await _client.getJson('/api/v1/treinos/ativo');
    } on AppFailure catch (failure) {
      if (failure.code == '404') {
        await _clearCachedActiveWorkout();
        return null;
      }

      if (_isNetworkFailure(failure) && cachedJson != null) {
        return _fromJson(cachedJson);
      }

      rethrow;
    }

    if (json.isEmpty) {
      await _clearCachedActiveWorkout();
      return null;
    }

    await _saveActiveWorkoutJson(json);

    return _fromJson(json);
  }

  Future<List<Workout>> listHistory() async {
    final json = await _client.getJson(
      '/api/v1/treinos',
      query: <String, Object?>{
        'page': 1,
        'pageSize': 20,
      },
    );

    return jsonObjectList(json['items'] ?? json['Items'])
        .map(_fromJson)
        .toList(growable: false);
  }

  Future<Workout> generate([
    WorkoutGenerationConfig? config,
  ]) async {
    final json = await _client.postJson(
      '/api/v1/treinos/gerar-ia',
      body: <String, Object?>{
        'objetivo': config?.goal,
        'nivel': config?.level,
        'diasPorSemana': config?.daysPerWeek,
        'duracaoMinutos': config?.durationMinutes,
        'duracaoSemanas': config?.durationWeeks,
        'limitacoes': config?.limitations ?? const <String>[],
        'equipamentos': config?.equipment ?? const <String>[],
        'gruposMuscularesPrioritarios':
            config?.priorityMuscleGroups ?? const <String>[],
      },
      timeout: ApiClient.aiRequestTimeout,
    );

    final workoutJson = jsonObject(json['treino'] ?? json['Treino']);
    await _saveActiveWorkoutJson(workoutJson);

    return _fromJson(workoutJson);
  }

  Future<Workout> addExercise({
    required String workoutId,
    required WorkoutExercise exercise,
  }) async {
    final operationId = newLocalOperationId();
    final path = '/api/v1/treinos/$workoutId/exercicios';
    final body = _exerciseCreateBody(exercise);

    try {
      final json = await _client.postJson(
        path,
        body: body,
        headers: _idempotencyHeaders(operationId),
      );
      await _saveActiveWorkoutJson(json);

      return _fromJson(json);
    } on AppFailure catch (failure) {
      if (!_isNetworkFailure(failure)) {
        rethrow;
      }

      return _applyLocalExerciseAdd(
        workoutId: workoutId,
        exercise: exercise,
        operationId: operationId,
        path: path,
        body: body,
      );
    }
  }

  Future<void> removeExercise({
    required String workoutId,
    required String workoutExerciseId,
  }) async {
    final operationId = newLocalOperationId();
    final path = '/api/v1/treinos/$workoutId/exercicios/$workoutExerciseId';

    try {
      await _client.delete(path, headers: _idempotencyHeaders(operationId));
    } on AppFailure catch (failure) {
      if (!_isNetworkFailure(failure)) {
        rethrow;
      }

      await _applyLocalExerciseRemove(
        workoutId: workoutId,
        workoutExerciseId: workoutExerciseId,
        operationId: operationId,
        path: path,
      );
      return;
    }

    await _removeExerciseFromCachedWorkout(workoutExerciseId);
  }

  Future<Workout> updateWorkout(
    Workout workout,
  ) async {
    final json = await _client.putJson(
      '/api/v1/treinos/${workout.id}',
      body: <String, Object?>{
        'nome': workout.name,
        'objetivo': workout.goal,
        'frequenciaSemanal': workout.weeklyFrequency,
        'dataInicio': _dateOnly(
          workout.startDate ?? DateTime.now(),
        ),
        'duracaoSemanas': workout.durationWeeks,
        'fase': workout.phase,
      },
    );

    await _saveActiveWorkoutJson(json);

    return _fromJson(json);
  }

  Future<Workout> updateExercise({
    required String workoutId,
    required WorkoutExercise exercise,
  }) async {
    final operationId = newLocalOperationId();
    final path = '/api/v1/treinos/$workoutId/exercicios/${exercise.id}';
    final body = _exerciseUpdateBody(exercise);

    try {
      final json = await _client.putJson(
        path,
        body: body,
        headers: _idempotencyHeaders(operationId),
      );
      await _saveActiveWorkoutJson(json);

      return _fromJson(json);
    } on AppFailure catch (failure) {
      if (!_isNetworkFailure(failure)) {
        rethrow;
      }

      return _applyLocalExerciseUpdate(
        workoutId: workoutId,
        exercise: exercise,
        operationId: operationId,
        path: path,
        body: body,
      );
    }
  }

  Future<Workout> replaceExerciseWithSuggestion({
    required String workoutId,
    required String exerciseId,
  }) async {
    final json = await _client.postJson(
      '/api/v1/treinos/$workoutId/exercicios/$exerciseId/substituir',
      body: const <String, Object?>{
        'novoExercicioId': null,
        'usarSugestaoIa': true,
      },
      timeout: ApiClient.aiRequestTimeout,
    );

    await _saveActiveWorkoutJson(json);

    return _fromJson(json);
  }

  Future<WorkoutEvolutionProposal> createEvolutionProposal({
    required String workoutId,
    String? note,
  }) async {
    final json = await _client.postJson(
      '/api/v1/treinos/$workoutId/evolucoes',
      body: <String, Object?>{
        'observacao': note,
      },
      timeout: ApiClient.aiRequestTimeout,
    );

    return _proposalFromJson(json);
  }

  Future<Workout> applyEvolutionProposal({
    required String workoutId,
    required String proposalId,
  }) async {
    final json = await _client.postJson(
      '/api/v1/treinos/$workoutId/evolucoes/$proposalId/aplicar',
    );

    await _saveActiveWorkoutJson(json);

    return _fromJson(json);
  }

  Future<WorkoutSessionResult> registerSession({
    required String workoutId,
    required int trainingDay,
    required List<WorkoutSetEntry> sets,
    String? note,
  }) async {
    final operationId = newLocalOperationId();
    final path = '/api/v1/treinos/$workoutId/sessoes';
    final body = _sessionBody(
      operationId: operationId,
      trainingDay: trainingDay,
      sets: sets,
      note: note,
    );

    await _markExercisesCompletedLocally(sets.map((set) => set.exerciseId));

    try {
      final json = await _client.postJson(
        path,
        body: body,
        headers: _idempotencyHeaders(operationId),
      );

      return WorkoutSessionResult(
        id: jsonString(json['id'] ?? json['Id']),
        summary: jsonString(
          json['resumo'] ?? json['Resumo'],
        ),
      );
    } on AppFailure catch (failure) {
      if (!_isNetworkFailure(failure)) {
        rethrow;
      }

      final summary = _localSessionSummary(sets);
      await _savePendingSession(
        workoutId: workoutId,
        trainingDay: trainingDay,
        operationId: operationId,
        path: path,
        body: body,
        summary: summary,
      );
      await _enqueueOfflineOperation(
        operationId: operationId,
        operation: 'workout_session.register',
        entityType: 'WorkoutSession',
        entityId: operationId,
        remoteId: workoutId,
        path: path,
        body: body,
      );

      return WorkoutSessionResult(
        id: operationId,
        summary: summary,
      );
    }
  }

  Future<List<WorkoutProgressionSuggestion>> suggestProgressions({
    required String workoutId,
  }) async {
    final items = await _client.postJsonList(
      '/api/v1/treinos/$workoutId/progressoes/sugerir',
      timeout: ApiClient.aiRequestTimeout,
    );

    return items.map(_progressionFromJson).toList(growable: false);
  }

  Future<WorkoutProgressionSuggestion> decideProgression({
    required String suggestionId,
    required bool apply,
  }) async {
    final json = await _client.postJson(
      '/api/v1/treinos/progressoes/$suggestionId/decidir',
      body: <String, Object?>{
        'aplicar': apply,
      },
    );

    return _progressionFromJson(json);
  }

  Future<ExerciseHistory?> getExerciseHistory(
    String exerciseId,
  ) async {
    try {
      final json = await _client.getJson(
        '/api/v1/treinos/exercicios/$exerciseId/historico',
      );

      return _historyFromJson(json);
    } on AppFailure catch (failure) {
      if (failure.code == '404') {
        return null;
      }

      rethrow;
    }
  }

  Future<void> setExerciseCompleted({
    required String workoutId,
    required String exerciseId,
    required bool completed,
  }) async {
    final operationId = newLocalOperationId();
    final path = '/api/v1/treinos/$workoutId/exercicios/$exerciseId/conclusao';
    final body = <String, Object?>{
      'concluido': completed,
      'data': _dateOnly(DateTime.now()),
      'operationId': operationId,
    };

    await _updateExerciseCompletionLocally(
      exerciseId: exerciseId,
      completed: completed,
    );

    try {
      await _client.putJson(
        path,
        body: body,
        headers: _idempotencyHeaders(operationId),
      );
    } on AppFailure catch (failure) {
      if (!_isNetworkFailure(failure)) {
        rethrow;
      }

      await _enqueueOfflineOperation(
        operationId: operationId,
        operation: 'workout_exercise.complete',
        entityType: 'WorkoutExerciseCompletion',
        entityId: exerciseId,
        remoteId: exerciseId,
        path: path,
        body: body,
      );
    }
  }

  Future<Map<String, Object?>?> _readCachedActiveWorkoutJson() async {
    final localDataSource = _localDataSource;
    final usuarioId = _usuarioId;

    if (localDataSource == null ||
        usuarioId == null ||
        usuarioId.trim().isEmpty) {
      return null;
    }

    try {
      return await localDataSource.readActiveWorkoutJson(
        tenantId: _tenantId,
        usuarioId: usuarioId,
      );
    } catch (_) {
      return null;
    }
  }

  Future<void> _saveActiveWorkoutJson(
    Map<String, Object?> json,
  ) async {
    final localDataSource = _localDataSource;
    final usuarioId = _usuarioId;

    if (localDataSource == null ||
        usuarioId == null ||
        usuarioId.trim().isEmpty ||
        json.isEmpty) {
      return;
    }

    await localDataSource.saveActiveWorkoutJson(
      tenantId: _tenantId,
      usuarioId: usuarioId,
      json: json,
    );
  }

  Future<void> _clearCachedActiveWorkout() async {
    final localDataSource = _localDataSource;
    final usuarioId = _usuarioId;

    if (localDataSource == null ||
        usuarioId == null ||
        usuarioId.trim().isEmpty) {
      return;
    }

    await localDataSource.clearActiveWorkout(
      tenantId: _tenantId,
      usuarioId: usuarioId,
    );
  }

  bool _isNetworkFailure(AppFailure failure) {
    return failure.code == 'network_unavailable';
  }

  Future<Workout> _applyLocalExerciseAdd({
    required String workoutId,
    required WorkoutExercise exercise,
    required String operationId,
    required String path,
    required Map<String, Object?> body,
  }) async {
    final json = await _requiredCachedWorkout();
    final exercises = _mutableExercises(json);
    exercises.add(_exerciseToJson(exercise, id: 'local-$operationId'));
    json['exercicios'] = exercises;

    await _saveActiveWorkoutJson(json);
    await _enqueueOfflineOperation(
      operationId: operationId,
      operation: 'workout_exercise.add',
      entityType: 'WorkoutExercise',
      entityId: 'local-$operationId',
      remoteId: workoutId,
      path: path,
      body: body,
    );

    return _fromJson(json);
  }

  Future<Workout> _applyLocalExerciseUpdate({
    required String workoutId,
    required WorkoutExercise exercise,
    required String operationId,
    required String path,
    required Map<String, Object?> body,
  }) async {
    final json = await _requiredCachedWorkout();
    final exercises = _mutableExercises(json);
    final index = exercises.indexWhere(
      (item) => _text(item['id'] ?? item['Id']) == exercise.id,
    );

    if (index >= 0) {
      exercises[index] = <String, Object?>{
        ...exercises[index],
        ..._exerciseToJson(exercise),
      };
    }

    json['exercicios'] = exercises;
    await _saveActiveWorkoutJson(json);
    await _enqueueOfflineOperation(
      operationId: operationId,
      operation: 'workout_exercise.update',
      entityType: 'WorkoutExercise',
      entityId: exercise.id,
      remoteId: exercise.id,
      path: path,
      body: body,
    );

    return _fromJson(json);
  }

  Future<void> _applyLocalExerciseRemove({
    required String workoutId,
    required String workoutExerciseId,
    required String operationId,
    required String path,
  }) async {
    await _removeExerciseFromCachedWorkout(workoutExerciseId);
    await _enqueueOfflineOperation(
      operationId: operationId,
      operation: 'workout_exercise.remove',
      entityType: 'WorkoutExercise',
      entityId: workoutExerciseId,
      remoteId: workoutId,
      path: path,
      body: const <String, Object?>{},
    );
  }

  Future<void> _removeExerciseFromCachedWorkout(
    String workoutExerciseId,
  ) async {
    final json = await _readCachedActiveWorkoutJson();
    if (json == null) {
      return;
    }

    final exercises = _mutableExercises(json)
        .where((item) => _text(item['id'] ?? item['Id']) != workoutExerciseId)
        .toList(growable: true);
    json['exercicios'] = exercises;
    await _saveActiveWorkoutJson(json);
  }

  Future<Map<String, Object?>> _requiredCachedWorkout() async {
    final json = await _readCachedActiveWorkoutJson();
    if (json == null) {
      throw const AppFailure(
        'Este treino ainda não está disponível offline. Abra-o uma vez online antes de editar.',
        code: 'offline_cache_miss',
      );
    }

    return Map<String, Object?>.from(json);
  }

  List<Map<String, Object?>> _mutableExercises(Map<String, Object?> json) {
    final source = json['exercicios'] ?? json['Exercicios'];
    if (source is! Iterable) {
      return <Map<String, Object?>>[];
    }

    return source
        .whereType<Map>()
        .map(
          (item) => item.map(
            (key, value) => MapEntry(key.toString(), value),
          ),
        )
        .toList(growable: true);
  }

  Future<void> _markExercisesCompletedLocally(
    Iterable<String> exerciseIds,
  ) async {
    for (final exerciseId in exerciseIds.toSet()) {
      await _updateExerciseCompletionLocally(
        exerciseId: exerciseId,
        completed: true,
      );
    }
  }

  Future<void> _updateExerciseCompletionLocally({
    required String exerciseId,
    required bool completed,
  }) async {
    final localDataSource = _localDataSource;
    final usuarioId = _usuarioId;

    if (localDataSource == null ||
        usuarioId == null ||
        usuarioId.trim().isEmpty) {
      return;
    }

    await localDataSource.updateExerciseCompletion(
      tenantId: _tenantId,
      usuarioId: usuarioId,
      exerciseId: exerciseId,
      completed: completed,
    );
  }

  Future<void> _savePendingSession({
    required String workoutId,
    required int trainingDay,
    required String operationId,
    required String path,
    required Map<String, Object?> body,
    required String summary,
  }) async {
    final localDataSource = _localDataSource;
    final usuarioId = _usuarioId;

    if (localDataSource == null ||
        usuarioId == null ||
        usuarioId.trim().isEmpty) {
      return;
    }

    await localDataSource.savePendingSession(
      tenantId: _tenantId,
      usuarioId: usuarioId,
      workoutId: workoutId,
      trainingDay: trainingDay,
      operationId: operationId,
      payload: <String, Object?>{
        'path': path,
        'body': body,
      },
      summary: summary,
    );
  }

  Future<void> _enqueueOfflineOperation({
    required String operationId,
    required String operation,
    required String entityType,
    required String? entityId,
    required String? remoteId,
    required String path,
    required Map<String, Object?> body,
  }) async {
    final syncService = _syncService;
    final usuarioId = _usuarioId;

    if (syncService == null || usuarioId == null || usuarioId.trim().isEmpty) {
      return;
    }

    await syncService.enqueue(
      id: operationId,
      operationId: operationId,
      operation: operation,
      entityType: entityType,
      entityId: entityId,
      remoteId: remoteId,
      payload: <String, Object?>{
        'tenantId': _tenantId,
        'usuarioId': usuarioId,
        'path': path,
        'body': body,
      },
    );
    await _syncEngine?.syncPendingOnce();
  }

  Map<String, Object?> _sessionBody({
    required String operationId,
    required int trainingDay,
    required List<WorkoutSetEntry> sets,
    String? note,
  }) {
    return <String, Object?>{
      'operationId': operationId,
      'data': _dateOnly(DateTime.now()),
      'diaTreino': trainingDay,
      'observacao': note,
      'series': sets
          .map(
            (set) => <String, Object?>{
              'treinoExercicioId': set.exerciseId,
              'numeroSerie': set.setNumber,
              'cargaKg': set.loadKg,
              'repeticoesRealizadas': set.repetitions,
              'rpe': set.rpe,
              'observacao': set.note,
              'dorDesconforto': set.pain,
              'dorDescricao': set.painDescription,
            },
          )
          .toList(growable: false),
    };
  }

  Map<String, Object?> _exerciseCreateBody(WorkoutExercise exercise) {
    return <String, Object?>{
      'exercicioId':
          exercise.exerciseId.trim().isEmpty ? null : exercise.exerciseId,
      'nome': exercise.name,
      'grupoMuscular': exercise.muscleGroup,
      'nivel': exercise.level,
      'equipamento': exercise.equipment,
      'instrucao': exercise.instruction,
      'diaTreino': exercise.trainingDay,
      'ordem': exercise.order,
      'series': exercise.sets,
      'repeticoes': exercise.repetitions,
      'descansoSegundos': exercise.restSeconds,
      'cargaAlvoKg': exercise.targetLoadKg,
      'rpeAlvo': exercise.targetRpe,
      'repeticoesMin': exercise.minRepetitions,
      'repeticoesMax': exercise.maxRepetitions,
      'observacao': exercise.note,
    };
  }

  Map<String, Object?> _exerciseUpdateBody(WorkoutExercise exercise) {
    return <String, Object?>{
      'diaTreino': exercise.trainingDay,
      'ordem': exercise.order,
      'series': exercise.sets,
      'repeticoes': exercise.repetitions,
      'descansoSegundos': exercise.restSeconds,
      'equipamento': exercise.equipment,
      'observacao': exercise.note,
      'cargaAlvoKg': exercise.targetLoadKg,
      'rpeAlvo': exercise.targetRpe,
      'repeticoesMin': exercise.minRepetitions,
      'repeticoesMax': exercise.maxRepetitions,
      'progressaoMotivo': exercise.progressionReason,
    };
  }

  Map<String, Object?> _exerciseToJson(
    WorkoutExercise exercise, {
    String? id,
  }) {
    return <String, Object?>{
      'id': id ?? exercise.id,
      'exercicioId': exercise.exerciseId,
      'nome': exercise.name,
      'grupoMuscular': exercise.muscleGroup,
      'nivel': exercise.level,
      'equipamento': exercise.equipment,
      'instrucao': exercise.instruction,
      'diaTreino': exercise.trainingDay,
      'concluidoHoje': exercise.completedToday,
      'ordem': exercise.order,
      'observacao': exercise.note,
      'series': exercise.sets,
      'repeticoes': exercise.repetitions,
      'descansoSegundos': exercise.restSeconds,
      'cargaAlvoKg': exercise.targetLoadKg,
      'rpeAlvo': exercise.targetRpe,
      'repeticoesMin': exercise.minRepetitions,
      'repeticoesMax': exercise.maxRepetitions,
      'progressaoMotivo': exercise.progressionReason,
    };
  }

  String _localSessionSummary(List<WorkoutSetEntry> sets) {
    final totalSets = sets.length;
    final totalRepetitions =
        sets.fold<int>(0, (sum, set) => sum + set.repetitions);
    final volume = sets.fold<double>(
      0,
      (sum, set) => sum + ((set.loadKg ?? 0) * set.repetitions),
    );
    final pain = sets.any((set) => set.pain);
    final painMessage = pain
        ? ' Você registrou desconforto; mantenha intensidade moderada até sincronizar.'
        : '';

    return 'Sessão salva neste aparelho com $totalSets séries, '
        '$totalRepetitions repetições e volume estimado de '
        '${volume.toStringAsFixed(1)} kg.$painMessage';
  }

  Map<String, Object?> _idempotencyHeaders(String operationId) {
    return <String, Object?>{
      'Idempotency-Key': operationId,
    };
  }

  String _text(Object? value) {
    return value?.toString() ?? '';
  }

  Workout _fromJson(
    Map<String, Object?> json,
  ) {
    return Workout(
      id: jsonString(json['id'] ?? json['Id']),
      name: jsonString(
        json['nome'] ?? json['Nome'],
      ),
      goal: jsonString(
        json['objetivo'] ?? json['Objetivo'],
      ),
      weeklyFrequency: jsonInt(
        json['frequenciaSemanal'] ?? json['FrequenciaSemanal'],
      ),
      active: jsonBool(
        json['ativo'] ?? json['Ativo'],
      ),
      exercises: jsonObjectList(
        json['exercicios'] ?? json['Exercicios'],
      ).map(_exerciseFromJson).toList(growable: false),
      message: jsonString(
        json['mensagem'] ?? json['Mensagem'],
      ),
      version: jsonInt(
        json['versao'] ?? json['Versao'],
        fallback: 1,
      ),
      previousWorkoutId: _emptyToNull(
        jsonString(
          json['treinoAnteriorId'] ?? json['TreinoAnteriorId'],
        ),
      ),
      startDate: jsonDate(
        json['dataInicio'] ?? json['DataInicio'],
      ),
      endDate: jsonDate(
        json['dataFim'] ?? json['DataFim'],
      ),
      durationWeeks: jsonInt(
        json['duracaoSemanas'] ?? json['DuracaoSemanas'],
        fallback: 6,
      ),
      currentWeek: jsonInt(
        json['semanaAtual'] ?? json['SemanaAtual'],
        fallback: 1,
      ),
      phase: jsonString(
        json['fase'] ?? json['Fase'],
        fallback: 'Fase 1',
      ),
    );
  }

  WorkoutExercise _exerciseFromJson(
    Map<String, Object?> json,
  ) {
    return WorkoutExercise(
      id: _emptyToNull(
        jsonString(json['id'] ?? json['Id']),
      ),
      exerciseId: jsonString(
        json['exercicioId'] ?? json['ExercicioId'],
      ),
      name: jsonString(
        json['nome'] ?? json['Nome'],
      ),
      muscleGroup: jsonString(
        json['grupoMuscular'] ?? json['GrupoMuscular'],
      ),
      level: jsonString(
        json['nivel'] ?? json['Nivel'],
      ),
      equipment: _emptyToNull(
        jsonString(
          json['equipamento'] ?? json['Equipamento'],
        ),
      ),
      instruction: jsonString(
        json['instrucao'] ?? json['Instrucao'],
      ),
      trainingDay: jsonInt(
        json['diaTreino'] ?? json['DiaTreino'],
        fallback: 1,
      ),
      completedToday: jsonBool(
        json['concluidoHoje'] ?? json['ConcluidoHoje'],
      ),
      order: jsonInt(
        json['ordem'] ?? json['Ordem'],
      ),
      note: _emptyToNull(
        jsonString(
          json['observacao'] ?? json['Observacao'],
        ),
      ),
      sets: jsonInt(
        json['series'] ?? json['Series'],
      ),
      repetitions: jsonString(
        json['repeticoes'] ?? json['Repeticoes'],
      ),
      restSeconds: jsonInt(
        json['descansoSegundos'] ?? json['DescansoSegundos'],
      ),
      targetLoadKg: _nullableDouble(
        json['cargaAlvoKg'] ?? json['CargaAlvoKg'],
      ),
      targetRpe: _nullableInt(
        json['rpeAlvo'] ?? json['RpeAlvo'],
      ),
      minRepetitions: _nullableInt(
        json['repeticoesMin'] ?? json['RepeticoesMin'],
      ),
      maxRepetitions: _nullableInt(
        json['repeticoesMax'] ?? json['RepeticoesMax'],
      ),
      progressionReason: _emptyToNull(
        jsonString(
          json['progressaoMotivo'] ?? json['ProgressaoMotivo'],
        ),
      ),
    );
  }

  WorkoutEvolutionProposal _proposalFromJson(
    Map<String, Object?> json,
  ) {
    return WorkoutEvolutionProposal(
      id: jsonString(json['id'] ?? json['Id']),
      summary: jsonString(
        json['mudancasResumo'] ?? json['MudancasResumo'],
      ),
      status: jsonString(
        json['status'] ?? json['Status'],
      ),
      proposedVersion: jsonInt(
        json['versaoProposta'] ?? json['VersaoProposta'],
      ),
      message: jsonString(
        json['mensagem'] ?? json['Mensagem'],
      ),
      changes: jsonObjectList(
        json['mudancas'] ?? json['Mudancas'],
      ).map(_proposalChangeFromJson).toList(growable: false),
    );
  }

  WorkoutEvolutionChange _proposalChangeFromJson(
    Map<String, Object?> json,
  ) {
    return WorkoutEvolutionChange(
      exerciseName: jsonString(
        json['exercicioNome'] ?? json['ExercicioNome'],
      ),
      currentSets: jsonInt(
        json['seriesAtual'] ?? json['SeriesAtual'],
      ),
      proposedSets: jsonInt(
        json['seriesProposta'] ?? json['SeriesProposta'],
      ),
      currentRepetitions: jsonString(
        json['repeticoesAtual'] ?? json['RepeticoesAtual'],
      ),
      proposedRepetitions: jsonString(
        json['repeticoesProposta'] ?? json['RepeticoesProposta'],
      ),
      currentLoadKg: _nullableDouble(
        json['cargaAtualKg'] ?? json['CargaAtualKg'],
      ),
      proposedLoadKg: _nullableDouble(
        json['cargaPropostaKg'] ?? json['CargaPropostaKg'],
      ),
      currentRestSeconds: jsonInt(
        json['descansoAtualSegundos'] ?? json['DescansoAtualSegundos'],
      ),
      proposedRestSeconds: jsonInt(
        json['descansoPropostoSegundos'] ?? json['DescansoPropostoSegundos'],
      ),
      reason: jsonString(
        json['motivo'] ?? json['Motivo'],
      ),
    );
  }

  WorkoutProgressionSuggestion _progressionFromJson(
    Map<String, Object?> json,
  ) {
    return WorkoutProgressionSuggestion(
      id: jsonString(json['id'] ?? json['Id']),
      workoutExerciseId: jsonString(
        json['treinoExercicioId'] ?? json['TreinoExercicioId'],
      ),
      exerciseName: jsonString(
        json['exercicioNome'] ?? json['ExercicioNome'],
      ),
      currentLoadKg: _nullableDouble(
        json['cargaAtualKg'] ?? json['CargaAtualKg'],
      ),
      suggestedLoadKg: _nullableDouble(
        json['cargaSugeridaKg'] ?? json['CargaSugeridaKg'],
      ),
      targetRpe: _nullableInt(
        json['rpeAlvo'] ?? json['RpeAlvo'],
      ),
      minRepetitions: _nullableInt(
        json['repeticoesMin'] ?? json['RepeticoesMin'],
      ),
      maxRepetitions: _nullableInt(
        json['repeticoesMax'] ?? json['RepeticoesMax'],
      ),
      reason: jsonString(
        json['motivo'] ?? json['Motivo'],
      ),
      status: jsonString(
        json['status'] ?? json['Status'],
      ),
      message: jsonString(
        json['mensagem'] ?? json['Mensagem'],
      ),
    );
  }

  ExerciseHistory _historyFromJson(
    Map<String, Object?> json,
  ) {
    return ExerciseHistory(
      exerciseId: jsonString(
        json['exercicioId'] ?? json['ExercicioId'],
      ),
      name: jsonString(
        json['nome'] ?? json['Nome'],
      ),
      muscleGroup: jsonString(
        json['grupoMuscular'] ?? json['GrupoMuscular'],
      ),
      previousLoadKg: _nullableDouble(
        json['cargaAnteriorKg'] ?? json['CargaAnteriorKg'],
      ),
      maxLoadKg: _nullableDouble(
        json['cargaMaximaKg'] ?? json['CargaMaximaKg'],
      ),
      totalVolumeKg: jsonDouble(
        json['volumeTotalKg'] ?? json['VolumeTotalKg'],
      ),
      loadEvolutionKg: jsonDouble(
        json['evolucaoCargaKg'] ?? json['EvolucaoCargaKg'],
      ),
      sessions: jsonObjectList(
        json['ultimasSessoes'] ?? json['UltimasSessoes'],
      ).map(_historySessionFromJson).toList(growable: false),
    );
  }

  ExerciseHistorySession _historySessionFromJson(
    Map<String, Object?> json,
  ) {
    return ExerciseHistorySession(
      date: jsonDate(
        json['data'] ?? json['Data'],
      ),
      maxLoadKg: _nullableDouble(
        json['maiorCargaKg'] ?? json['MaiorCargaKg'],
      ),
      totalRepetitions: jsonInt(
        json['repeticoesTotais'] ?? json['RepeticoesTotais'],
      ),
      volumeKg: jsonDouble(
        json['volumeKg'] ?? json['VolumeKg'],
      ),
      averageRpe: _nullableInt(
        json['rpeMedio'] ?? json['RpeMedio'],
      ),
    );
  }

  String _dateOnly(DateTime value) {
    final year = value.year.toString().padLeft(4, '0');
    final month = value.month.toString().padLeft(2, '0');
    final day = value.day.toString().padLeft(2, '0');

    return '$year-$month-$day';
  }

  String? _emptyToNull(String value) {
    final trimmed = value.trim();

    return trimmed.isEmpty ? null : trimmed;
  }

  double? _nullableDouble(Object? value) {
    if (value == null) {
      return null;
    }

    if (value is num) {
      return value.toDouble();
    }

    return double.tryParse(
      value.toString().replaceAll(',', '.'),
    );
  }

  int? _nullableInt(Object? value) {
    if (value == null) {
      return null;
    }

    if (value is int) {
      return value;
    }

    if (value is num) {
      return value.toInt();
    }

    return int.tryParse(value.toString());
  }
}
