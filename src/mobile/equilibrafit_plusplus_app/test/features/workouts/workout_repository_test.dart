import 'package:dio/dio.dart';
import 'package:equilibrafit_plusplus_app/core/http/api_client.dart';
import 'package:equilibrafit_plusplus_app/features/workouts/data/workout_local_data_source.dart';
import 'package:equilibrafit_plusplus_app/features/workouts/data/workout_repository.dart';
import 'package:equilibrafit_plusplus_app/features/workouts/domain/workout.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  group('WorkoutRepository', () {
    test('parses active workout with progression fields', () async {
      final fixture = _DioFixture((_) => _workoutJson());
      final repository = WorkoutRepository(ApiClient(fixture.dio));

      final workout = await repository.getActive();

      expect(workout, isNotNull);
      expect(workout!.name, 'Treino base');
      expect(workout.exercises.single.targetLoadKg, 72.5);
      expect(workout.exercises.single.targetRpe, 8);
      expect(workout.exercises.single.minRepetitions, 8);
      expect(workout.exercises.single.maxRepetitions, 10);
      expect(workout.exercises.single.progressionReason, 'Topo da faixa.');
    });

    test('stores active workout in local cache after successful refresh',
        () async {
      final fixture = _DioFixture((_) => _workoutJson());
      final localStore = _FakeWorkoutLocalStore();
      final repository = WorkoutRepository(
        ApiClient(fixture.dio),
        localDataSource: localStore,
        usuarioId: 'user-1',
      );

      final workout = await repository.getActive();

      expect(workout, isNotNull);
      expect(localStore.savedJson?['id'], 'workout-1');
      expect(localStore.lastTenantId, 'default');
      expect(localStore.lastUsuarioId, 'user-1');
    });

    test('returns cached active workout when API is unavailable', () async {
      final fixture = _FailingDioFixture();
      final localStore = _FakeWorkoutLocalStore(savedJson: _workoutJson());
      final repository = WorkoutRepository(
        ApiClient(fixture.dio),
        localDataSource: localStore,
        usuarioId: 'user-1',
      );

      final workout = await repository.getActive();

      expect(workout, isNotNull);
      expect(workout!.name, 'Treino base');
      expect(workout.exercises.single.name, 'Supino reto');
      expect(localStore.lastUsuarioId, 'user-1');
    });

    test('saves workout session locally when API is unavailable', () async {
      final fixture = _FailingDioFixture();
      final localStore = _FakeWorkoutLocalStore(savedJson: _workoutJson());
      final repository = WorkoutRepository(
        ApiClient(fixture.dio),
        localDataSource: localStore,
        usuarioId: 'user-1',
      );

      final result = await repository.registerSession(
        workoutId: 'workout-1',
        trainingDay: 1,
        sets: const [
          WorkoutSetEntry(
            exerciseId: 'prescribed-1',
            setNumber: 1,
            loadKg: 70,
            repetitions: 12,
            rpe: 8,
            pain: true,
            painDescription: 'Ombro sensível.',
          ),
        ],
      );

      expect(result.id, isNotEmpty);
      expect(result.summary, contains('Sessão salva neste aparelho'));
      expect(localStore.pendingSessionOperationId, result.id);
      expect(localStore.completedExercises['prescribed-1'], isTrue);
      final pendingBody = localStore.pendingSessionPayload?['body'] as Map?;
      final series = pendingBody?['series'] as List?;
      final firstSeries = series?.single as Map?;
      expect(firstSeries?['cargaKg'], 70);
      expect(firstSeries?['repeticoesRealizadas'], 12);
      expect(firstSeries?['rpe'], 8);
      expect(firstSeries?['dorDesconforto'], isTrue);
      expect(firstSeries?['dorDescricao'], 'Ombro sensível.');
    });

    test('adds exercise to cached workout when API is unavailable', () async {
      final fixture = _FailingDioFixture();
      final localStore = _FakeWorkoutLocalStore(savedJson: _workoutJson());
      final repository = WorkoutRepository(
        ApiClient(fixture.dio),
        localDataSource: localStore,
        usuarioId: 'user-1',
      );

      final workout = await repository.addExercise(
        workoutId: 'workout-1',
        exercise: const WorkoutExercise(
          exerciseId: 'exercise-2',
          name: 'Remada baixa',
          muscleGroup: 'Costas',
          level: 'Iniciante',
          instruction: 'Execute com controle.',
          trainingDay: 2,
          completedToday: false,
          order: 1,
          sets: 3,
          repetitions: '10-12',
          restSeconds: 60,
        ),
      );

      expect(workout.exercises, hasLength(2));
      expect(workout.exercises.last.name, 'Remada baixa');
      expect(workout.exercises.last.id, startsWith('local-'));
    });

    test('updates cached exercise when API is unavailable', () async {
      final fixture = _FailingDioFixture();
      final localStore = _FakeWorkoutLocalStore(savedJson: _workoutJson());
      final repository = WorkoutRepository(
        ApiClient(fixture.dio),
        localDataSource: localStore,
        usuarioId: 'user-1',
      );

      final workout = await repository.updateExercise(
        workoutId: 'workout-1',
        exercise: const WorkoutExercise(
          id: 'prescribed-1',
          exerciseId: 'exercise-1',
          name: 'Supino reto',
          muscleGroup: 'Peitoral',
          level: 'Intermediário',
          instruction: 'Execute com controle.',
          trainingDay: 2,
          completedToday: false,
          order: 1,
          sets: 4,
          repetitions: '8-10',
          restSeconds: 90,
          targetLoadKg: 75,
          targetRpe: 8,
        ),
      );

      expect(workout.exercises.single.trainingDay, 2);
      expect(workout.exercises.single.sets, 4);
      expect(workout.exercises.single.targetLoadKg, 75);
    });

    test('removes exercise from cached workout when API is unavailable',
        () async {
      final fixture = _FailingDioFixture();
      final localStore = _FakeWorkoutLocalStore(savedJson: _workoutJson());
      final repository = WorkoutRepository(
        ApiClient(fixture.dio),
        localDataSource: localStore,
        usuarioId: 'user-1',
      );

      await repository.removeExercise(
        workoutId: 'workout-1',
        workoutExerciseId: 'prescribed-1',
      );
      final workout = await repository.getActive();

      expect(workout?.exercises, isEmpty);
    });

    test('marks cached exercise completed when API is unavailable', () async {
      final fixture = _FailingDioFixture();
      final localStore = _FakeWorkoutLocalStore(savedJson: _workoutJson());
      final repository = WorkoutRepository(
        ApiClient(fixture.dio),
        localDataSource: localStore,
        usuarioId: 'user-1',
      );

      await repository.setExerciseCompleted(
        workoutId: 'workout-1',
        exerciseId: 'prescribed-1',
        completed: true,
      );

      expect(localStore.completedExercises['prescribed-1'], isTrue);
    });

    test('parses registered workout session result', () async {
      final fixture = _DioFixture(
        (_) => <String, Object?>{
          'id': 'session-1',
          'resumo': 'Sessão registrada.',
        },
      );
      final repository = WorkoutRepository(ApiClient(fixture.dio));

      final result = await repository.registerSession(
        workoutId: 'workout-1',
        trainingDay: 1,
        sets: const [
          WorkoutSetEntry(
            exerciseId: 'prescribed-1',
            setNumber: 1,
            loadKg: 70,
            repetitions: 10,
            rpe: 7,
            note: 'Boa técnica.',
            pain: false,
          ),
        ],
      );

      expect(result.id, 'session-1');
      expect(result.summary, 'Sessão registrada.');
    });

    test('parses exercise history', () async {
      final fixture = _DioFixture(
        (_) => <String, Object?>{
          'exercicioId': 'exercise-1',
          'nome': 'Supino reto',
          'grupoMuscular': 'Peitoral',
          'cargaAnteriorKg': 70,
          'cargaMaximaKg': 72.5,
          'volumeTotalKg': 2100,
          'evolucaoCargaKg': 2.5,
          'ultimasSessoes': [
            {
              'data': '2026-08-08',
              'maiorCargaKg': 72.5,
              'repeticoesTotais': 30,
              'volumeKg': 2175,
              'rpeMedio': 8,
            },
          ],
        },
      );
      final repository = WorkoutRepository(ApiClient(fixture.dio));

      final history = await repository.getExerciseHistory('exercise-1');

      expect(history, isNotNull);
      expect(history!.sessions.single.maxLoadKg, 72.5);
      expect(history.sessions.single.averageRpe, 8);
    });

    test('parses evolution proposal comparison', () async {
      final fixture = _DioFixture((_) => _proposalJson());
      final repository = WorkoutRepository(ApiClient(fixture.dio));

      final proposal = await repository.createEvolutionProposal(
        workoutId: 'workout-1',
      );

      expect(proposal.proposedVersion, 2);
      expect(proposal.changes.single.exerciseName, 'Supino reto');
      expect(proposal.changes.single.proposedLoadKg, 72.5);
    });

    test('sends update exercise endpoint and structured progression body',
        () async {
      final fixture = _DioFixture((_) => _workoutJson());
      final repository = WorkoutRepository(ApiClient(fixture.dio));
      const exercise = WorkoutExercise(
        id: 'prescribed-1',
        exerciseId: 'exercise-1',
        name: 'Supino reto',
        muscleGroup: 'Peitoral',
        level: 'Intermediário',
        instruction: 'Execute com controle.',
        trainingDay: 2,
        completedToday: false,
        order: 3,
        sets: 4,
        repetitions: '8-10',
        restSeconds: 90,
        equipment: 'Barra',
        note: 'Controle total.',
        targetLoadKg: 72.5,
        targetRpe: 8,
        minRepetitions: 8,
        maxRepetitions: 10,
        progressionReason: 'Topo da faixa.',
      );

      await repository.updateExercise(
        workoutId: 'workout-1',
        exercise: exercise,
      );

      expect(
        fixture.requests.single.path,
        '/api/v1/treinos/workout-1/exercicios/prescribed-1',
      );
      expect(fixture.requests.single.data['cargaAlvoKg'], 72.5);
      expect(fixture.requests.single.data['rpeAlvo'], 8);
      expect(fixture.requests.single.data['repeticoesMin'], 8);
      expect(fixture.requests.single.data['repeticoesMax'], 10);
    });
  });
}

Map<String, Object?> _workoutJson() {
  return <String, Object?>{
    'id': 'workout-1',
    'nome': 'Treino base',
    'objetivo': 'Hipertrofia',
    'frequenciaSemanal': 3,
    'ativo': true,
    'mensagem': 'Vamos adaptar.',
    'versao': 1,
    'dataInicio': '2026-08-01',
    'duracaoSemanas': 6,
    'semanaAtual': 2,
    'fase': 'Fase 1',
    'exercicios': [
      {
        'id': 'prescribed-1',
        'exercicioId': 'exercise-1',
        'nome': 'Supino reto',
        'grupoMuscular': 'Peitoral',
        'nivel': 'Intermediário',
        'equipamento': 'Barra',
        'instrucao': 'Execute com controle.',
        'diaTreino': 1,
        'concluidoHoje': false,
        'ordem': 1,
        'observacao': 'Controle total.',
        'series': 3,
        'repeticoes': '8-10',
        'descansoSegundos': 90,
        'cargaAlvoKg': 72.5,
        'rpeAlvo': 8,
        'repeticoesMin': 8,
        'repeticoesMax': 10,
        'progressaoMotivo': 'Topo da faixa.',
      }
    ],
  };
}

Map<String, Object?> _proposalJson() {
  return <String, Object?>{
    'id': 'proposal-1',
    'treinoUsuarioId': 'workout-1',
    'versaoAtual': 1,
    'versaoProposta': 2,
    'status': 'Pendente',
    'mudancasResumo': 'Progressão segura.',
    'planoProposto': <String, Object?>{},
    'mudancas': [
      {
        'exercicioNome': 'Supino reto',
        'seriesAtual': 3,
        'seriesProposta': 4,
        'repeticoesAtual': '8-10',
        'repeticoesProposta': '8-10',
        'cargaAtualKg': 70,
        'cargaPropostaKg': 72.5,
        'descansoAtualSegundos': 60,
        'descansoPropostoSegundos': 90,
        'motivo': 'Topo da faixa.',
      }
    ],
    'mensagem': 'Revise antes de aplicar.',
  };
}

final class _DioFixture {
  _DioFixture(this._responseFactory) {
    dio = Dio();
    dio.interceptors.add(
      InterceptorsWrapper(
        onRequest: (options, handler) {
          requests.add(_CapturedRequest(options.path, options.data));
          handler.resolve(
            Response<Object?>(
              requestOptions: options,
              statusCode: 200,
              data: _responseFactory(options),
            ),
          );
        },
      ),
    );
  }

  final Object? Function(RequestOptions options) _responseFactory;
  final List<_CapturedRequest> requests = [];
  late final Dio dio;
}

final class _FailingDioFixture {
  _FailingDioFixture() {
    dio = Dio();
    dio.interceptors.add(
      InterceptorsWrapper(
        onRequest: (options, handler) {
          handler.reject(
            DioException(
              requestOptions: options,
              type: DioExceptionType.connectionError,
              error: 'offline',
            ),
          );
        },
      ),
    );
  }

  late final Dio dio;
}

final class _FakeWorkoutLocalStore implements WorkoutLocalStore {
  _FakeWorkoutLocalStore({this.savedJson});

  Map<String, Object?>? savedJson;
  String? lastTenantId;
  String? lastUsuarioId;
  String? pendingSessionOperationId;
  Map<String, Object?>? pendingSessionPayload;
  final Map<String, bool> completedExercises = <String, bool>{};

  @override
  Future<Map<String, Object?>?> readActiveWorkoutJson({
    required String tenantId,
    required String usuarioId,
  }) async {
    lastTenantId = tenantId;
    lastUsuarioId = usuarioId;
    return savedJson;
  }

  @override
  Future<void> saveActiveWorkoutJson({
    required String tenantId,
    required String usuarioId,
    required Map<String, Object?> json,
  }) async {
    lastTenantId = tenantId;
    lastUsuarioId = usuarioId;
    savedJson = Map<String, Object?>.from(json);
  }

  @override
  Future<void> clearActiveWorkout({
    required String tenantId,
    required String usuarioId,
  }) async {
    lastTenantId = tenantId;
    lastUsuarioId = usuarioId;
    savedJson = null;
  }

  @override
  Future<void> updateExerciseCompletion({
    required String tenantId,
    required String usuarioId,
    required String exerciseId,
    required bool completed,
  }) async {
    lastTenantId = tenantId;
    lastUsuarioId = usuarioId;
    completedExercises[exerciseId] = completed;
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
    lastTenantId = tenantId;
    lastUsuarioId = usuarioId;
    pendingSessionOperationId = operationId;
    pendingSessionPayload = Map<String, Object?>.from(payload);
  }

  @override
  Future<void> clearPrivateData({
    required String tenantId,
    required String usuarioId,
  }) async {
    lastTenantId = tenantId;
    lastUsuarioId = usuarioId;
    savedJson = null;
    pendingSessionOperationId = null;
    pendingSessionPayload = null;
    completedExercises.clear();
  }
}

final class _CapturedRequest {
  const _CapturedRequest(this.path, Object? data)
      : data = data is Map ? data : const <String, Object?>{};

  final String path;
  final Map<dynamic, dynamic> data;
}
