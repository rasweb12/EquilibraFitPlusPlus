import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/errors/app_failure.dart';
import '../../../../design_system/tokens/app_tokens.dart';
import '../../../../design_system/widgets/ef_button.dart';
import '../../../../design_system/widgets/ef_empty_state.dart';
import '../../../../design_system/widgets/ef_feedback_banner.dart';
import '../../../../design_system/widgets/ef_scaffold.dart';
import '../../../../design_system/widgets/ef_text_field.dart';
import '../../data/workout_repository.dart';
import '../../domain/workout.dart';

class WorkoutsPage extends ConsumerStatefulWidget {
  const WorkoutsPage({super.key});

  @override
  ConsumerState<WorkoutsPage> createState() => _WorkoutsPageState();
}

class _WorkoutsPageState extends ConsumerState<WorkoutsPage> {
  bool _isGenerating = false;
  bool _isBusy = false;
  int _selectedDay = 1;

  final Map<int, int> _exerciseTargetByDay = <int, int>{};

  @override
  Widget build(BuildContext context) {
    final workout = ref.watch(activeWorkoutProvider);

    return EfScaffold(
      title: 'Treinos',
      subtitle: 'Movimento com segurança e constância.',
      currentIndex: 3,
      onRefresh: () async {
        ref.invalidate(activeWorkoutProvider);
      },
      actions: [
        IconButton(
          tooltip: 'Histórico',
          icon: const Icon(Icons.history),
          onPressed: _isBusy ? null : _showWorkoutHistory,
        ),
        TextButton(
          onPressed: () => context.go('/dashboard'),
          child: const Text('Voltar'),
        ),
      ],
      body: workout.when(
        data: (active) {
          if (active == null) {
            return Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                const EfEmptyState(
                  icon: Icons.fitness_center,
                  title: 'Nenhum treino ativo',
                  message:
                      'Podemos montar um treino inicial seguro com base no seu questionário.',
                ),
                const SizedBox(height: AppTokens.space16),
                EfButton(
                  label: 'Novo plano',
                  icon: Icons.add_circle_outline,
                  isLoading: _isGenerating,
                  onPressed: _showNewPlanFlow,
                ),
              ],
            );
          }

          final totalDays =
              active.weeklyFrequency <= 0 ? 1 : active.weeklyFrequency;
          final selectedDay = _selectedDay < 1
              ? 1
              : (_selectedDay > totalDays ? totalDays : _selectedDay);

          final target = _exerciseTarget(active, selectedDay);

          return _WorkoutContent(
            workout: active,
            selectedDay: selectedDay,
            exerciseTarget: target,
            isBusy: _isBusy || _isGenerating,
            onPreviousDay: () => _previousDay(active),
            onNextDay: () => _nextDay(active),
            onSelectDay: (day) => _selectDay(active, day),
            onDecreaseTarget: () =>
                _decreaseExerciseTarget(active, selectedDay),
            onIncreaseTarget: () =>
                _increaseExerciseTarget(active, selectedDay),
            onAddExercise: () => _showAddExercise(active, selectedDay),
            onToggleExercise: _toggleExercise,
            onEditWorkout: () => _showEditWorkout(active),
            onNewPlan: _showNewPlanFlow,
            onEvolveWorkout: () => _createEvolutionProposal(active),
            onSuggestProgression: () => _suggestProgressions(active),
            onExecuteDay: (day, exercises) =>
                _showExecutionSheet(active, day, exercises),
            onEditExercise: (exercise) => _showEditExercise(active, exercise),
            onReplaceExercise: (exercise) =>
                _replaceExerciseWithSuggestion(active, exercise),
            onRemoveExercise: (exercise) => _removeExercise(active, exercise),
            onExerciseHistory: _showExerciseHistory,
          );
        },
        loading: () => const Center(
          child: Padding(
            padding: EdgeInsets.all(AppTokens.space32),
            child: CircularProgressIndicator(),
          ),
        ),
        error: (error, stackTrace) => const EfEmptyState(
          icon: Icons.cloud_off_outlined,
          title: 'Treino indisponível agora',
          message:
              'Sem problemas. Vamos tentar novamente quando a conexão estiver estável.',
        ),
      ),
    );
  }

  void _selectDay(Workout workout, int day) {
    final totalDays =
        workout.weeklyFrequency <= 0 ? 1 : workout.weeklyFrequency;

    final safeDay = day < 1 ? 1 : (day > totalDays ? totalDays : day);

    setState(() {
      _selectedDay = safeDay;
    });
  }

  void _previousDay(Workout workout) {
    _selectDay(workout, _selectedDay - 1);
  }

  void _nextDay(Workout workout) {
    _selectDay(workout, _selectedDay + 1);
  }

  int _exerciseTarget(Workout workout, int day) {
    return _exerciseTargetByDay.putIfAbsent(
      day,
      () {
        final current = workout.exercises
            .where((exercise) => exercise.trainingDay == day)
            .length;

        return current == 0 ? 5 : current;
      },
    );
  }

  void _increaseExerciseTarget(Workout workout, int day) {
    final current = _exerciseTarget(workout, day);

    setState(() {
      _exerciseTargetByDay[day] = current + 1;
    });
  }

  void _decreaseExerciseTarget(Workout workout, int day) {
    final current = _exerciseTarget(workout, day);

    if (current <= 1) {
      return;
    }

    setState(() {
      _exerciseTargetByDay[day] = current - 1;
    });
  }

  Future<void> _showNewPlanFlow() async {
    final config = await showModalBottomSheet<WorkoutGenerationConfig>(
      context: context,
      isScrollControlled: true,
      useSafeArea: true,
      builder: (_) => const _NewWorkoutPlanSheet(),
    );

    if (!mounted || config == null) {
      return;
    }

    setState(() {
      _isGenerating = true;
    });

    try {
      await ref.read(workoutRepositoryProvider).generate(config);

      if (!mounted) {
        return;
      }

      _exerciseTargetByDay.clear();
      _selectedDay = 1;
      ref.invalidate(activeWorkoutProvider);
      _showSnack('Treino gerado. Vamos adaptar no seu ritmo.');
    } on AppFailure catch (failure) {
      if (mounted) {
        _showSnack(failure.message);
      }
    } finally {
      if (mounted) {
        setState(() {
          _isGenerating = false;
        });
      }
    }
  }

  Future<void> _showAddExercise(
    Workout workout,
    int trainingDay,
  ) async {
    final currentExercises = workout.exercises
        .where((exercise) => exercise.trainingDay == trainingDay)
        .toList();

    final exercise = await showModalBottomSheet<WorkoutExercise>(
      context: context,
      isScrollControlled: true,
      useSafeArea: true,
      builder: (_) => _AddExerciseSheet(
        trainingDay: trainingDay,
        nextOrder: currentExercises.length + 1,
      ),
    );

    if (!mounted || exercise == null) {
      return;
    }

    await _runBusy(() async {
      await ref.read(workoutRepositoryProvider).addExercise(
            workoutId: workout.id,
            exercise: exercise,
          );

      if (!mounted) {
        return;
      }

      ref.invalidate(activeWorkoutProvider);
      _showSnack('${exercise.name} adicionado ao Dia $trainingDay.');
    });
  }

  Future<void> _removeExercise(
    Workout workout,
    WorkoutExercise exercise,
  ) async {
    if (exercise.id == null || exercise.id!.isEmpty) {
      return;
    }

    final confirmed = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: const Text('Remover exercício'),
        content: Text(
          'Remover ${exercise.name} deste plano?\n\n'
          'O histórico das sessões anteriores deve continuar preservado no backend.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(dialogContext).pop(false),
            child: const Text('Cancelar'),
          ),
          FilledButton(
            onPressed: () => Navigator.of(dialogContext).pop(true),
            child: const Text('Remover'),
          ),
        ],
      ),
    );

    if (!mounted || confirmed != true) {
      return;
    }

    await _runBusy(() async {
      await ref.read(workoutRepositoryProvider).removeExercise(
            workoutId: workout.id,
            workoutExerciseId: exercise.id!,
          );

      if (!mounted) {
        return;
      }

      ref.invalidate(activeWorkoutProvider);
      _showSnack('Exercício removido do plano.');
    });
  }

  Future<void> _showEditWorkout(Workout workout) async {
    final updated = await showModalBottomSheet<Workout>(
      context: context,
      isScrollControlled: true,
      useSafeArea: true,
      builder: (_) => _EditWorkoutSheet(workout: workout),
    );

    if (!mounted || updated == null) {
      return;
    }

    await _runBusy(() async {
      await ref.read(workoutRepositoryProvider).updateWorkout(updated);

      if (!mounted) {
        return;
      }

      ref.invalidate(activeWorkoutProvider);
      _showSnack('Treino atualizado. Podemos ajustar de novo quando precisar.');
    });
  }

  Future<void> _showEditExercise(
    Workout workout,
    WorkoutExercise exercise,
  ) async {
    final updated = await showModalBottomSheet<WorkoutExercise>(
      context: context,
      isScrollControlled: true,
      useSafeArea: true,
      builder: (_) => _EditExerciseSheet(exercise: exercise),
    );

    if (!mounted || updated == null || exercise.id == null) {
      return;
    }

    await _runBusy(() async {
      await ref.read(workoutRepositoryProvider).updateExercise(
            workoutId: workout.id,
            exercise: updated,
          );

      if (!mounted) {
        return;
      }

      ref.invalidate(activeWorkoutProvider);

      if (updated.trainingDay != _selectedDay) {
        setState(() {
          _selectedDay = updated.trainingDay;
        });
      }

      _showSnack('Exercício atualizado.');
    });
  }

  Future<void> _createEvolutionProposal(Workout workout) async {
    await _runBusy(() async {
      final proposal = await ref
          .read(workoutRepositoryProvider)
          .createEvolutionProposal(workoutId: workout.id);

      if (!mounted) {
        return;
      }

      final apply = await showDialog<bool>(
        context: context,
        builder: (dialogContext) => AlertDialog(
          title: Text('Versão ${proposal.proposedVersion}'),
          content: SizedBox(
            width: 520,
            child: SingleChildScrollView(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(proposal.summary),
                  if (proposal.changes.isNotEmpty) ...[
                    const SizedBox(height: AppTokens.space16),
                    for (final change in proposal.changes) ...[
                      _ProposalChangeCard(change: change),
                      const SizedBox(height: AppTokens.space12),
                    ],
                  ],
                ],
              ),
            ),
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.of(dialogContext).pop(false),
              child: const Text('Manter depois'),
            ),
            FilledButton(
              onPressed: () => Navigator.of(dialogContext).pop(true),
              child: const Text('Aplicar'),
            ),
          ],
        ),
      );

      if (!mounted || apply != true) {
        return;
      }

      await ref.read(workoutRepositoryProvider).applyEvolutionProposal(
            workoutId: workout.id,
            proposalId: proposal.id,
          );

      if (!mounted) {
        return;
      }

      ref.invalidate(activeWorkoutProvider);
      _showSnack(
        'Nova versão aplicada. O histórico anterior foi preservado.',
      );
    });
  }

  Future<void> _suggestProgressions(Workout workout) async {
    await _runBusy(() async {
      final suggestions = await ref
          .read(workoutRepositoryProvider)
          .suggestProgressions(workoutId: workout.id);

      if (!mounted) {
        return;
      }

      if (suggestions.isEmpty) {
        _showSnack(
          'Ainda não há progressão sugerida. Vamos seguir acompanhando.',
        );
        return;
      }

      await showModalBottomSheet<void>(
        context: context,
        isScrollControlled: true,
        useSafeArea: true,
        builder: (sheetContext) => _FormSheet(
          title: 'Progressões',
          child: Column(
            children: [
              for (final suggestion in suggestions) ...[
                ListTile(
                  contentPadding: EdgeInsets.zero,
                  title: Text(suggestion.exerciseName),
                  subtitle: Text(suggestion.reason),
                ),
                Row(
                  children: [
                    Expanded(
                      child: OutlinedButton(
                        onPressed: () async {
                          await ref
                              .read(workoutRepositoryProvider)
                              .decideProgression(
                                suggestionId: suggestion.id,
                                apply: false,
                              );

                          if (sheetContext.mounted) {
                            Navigator.of(sheetContext).pop();
                          }
                        },
                        child: const Text('Manter'),
                      ),
                    ),
                    const SizedBox(width: AppTokens.space12),
                    Expanded(
                      child: FilledButton(
                        onPressed: () async {
                          await ref
                              .read(workoutRepositoryProvider)
                              .decideProgression(
                                suggestionId: suggestion.id,
                                apply: true,
                              );

                          ref.invalidate(activeWorkoutProvider);

                          if (sheetContext.mounted) {
                            Navigator.of(sheetContext).pop();
                          }
                        },
                        child: const Text('Aplicar'),
                      ),
                    ),
                  ],
                ),
                const Divider(height: AppTokens.space24),
              ],
            ],
          ),
        ),
      );
    });
  }

  Future<void> _showExecutionSheet(
    Workout workout,
    int trainingDay,
    List<WorkoutExercise> exercises,
  ) async {
    final sessionSets = await showModalBottomSheet<List<WorkoutSetEntry>>(
      context: context,
      isScrollControlled: true,
      useSafeArea: true,
      builder: (_) => _WorkoutExecutionSheet(
        trainingDay: trainingDay,
        exercises: exercises,
      ),
    );

    if (!mounted || sessionSets == null || sessionSets.isEmpty) {
      return;
    }

    await _runBusy(() async {
      final result = await ref.read(workoutRepositoryProvider).registerSession(
            workoutId: workout.id,
            trainingDay: trainingDay,
            sets: sessionSets,
          );

      if (!mounted) {
        return;
      }

      ref.invalidate(activeWorkoutProvider);
      _showSnack(result.summary);
    });
  }

  Future<void> _replaceExerciseWithSuggestion(
    Workout workout,
    WorkoutExercise exercise,
  ) async {
    if (exercise.id == null) {
      return;
    }

    final confirmed = await showDialog<bool>(
      context: context,
      builder: (dialogContext) => AlertDialog(
        title: const Text('Trocar exercício'),
        content: Text(
          'Vamos buscar uma alternativa para ${exercise.name}, '
          'preservando ${exercise.muscleGroup} quando possível.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(dialogContext).pop(false),
            child: const Text('Cancelar'),
          ),
          FilledButton(
            onPressed: () => Navigator.of(dialogContext).pop(true),
            child: const Text('Sugerir troca'),
          ),
        ],
      ),
    );

    if (!mounted || confirmed != true) {
      return;
    }

    await _runBusy(() async {
      await ref.read(workoutRepositoryProvider).replaceExerciseWithSuggestion(
            workoutId: workout.id,
            exerciseId: exercise.id!,
          );

      if (!mounted) {
        return;
      }

      ref.invalidate(activeWorkoutProvider);
      _showSnack(
        'Exercício substituído. Podemos ajustar novamente se precisar.',
      );
    });
  }

  Future<void> _showExerciseHistory(WorkoutExercise exercise) async {
    final history = await ref
        .read(workoutRepositoryProvider)
        .getExerciseHistory(exercise.exerciseId);

    if (!mounted) {
      return;
    }

    await showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      useSafeArea: true,
      builder: (_) => _FormSheet(
        title: 'Histórico',
        child: history == null
            ? const Text(
                'Ainda não há sessões registradas para este exercício.',
              )
            : Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    history.name,
                    style: Theme.of(context).textTheme.titleMedium,
                  ),
                  const SizedBox(height: AppTokens.space12),
                  Text('Carga anterior: ${_kg(history.previousLoadKg)}'),
                  Text('Carga máxima: ${_kg(history.maxLoadKg)}'),
                  Text(
                    'Volume total: '
                    '${history.totalVolumeKg.toStringAsFixed(1)} kg',
                  ),
                  const Divider(height: AppTokens.space24),
                  for (final session in history.sessions)
                    ListTile(
                      contentPadding: EdgeInsets.zero,
                      title: Text(_dateLabel(session.date)),
                      subtitle: Text(
                        '${session.totalRepetitions} reps · '
                        '${session.volumeKg.toStringAsFixed(1)} kg volume',
                      ),
                      trailing: Text(_kg(session.maxLoadKg)),
                    ),
                ],
              ),
      ),
    );
  }

  Future<void> _showWorkoutHistory() async {
    await _runBusy(() async {
      final history = await ref.read(workoutRepositoryProvider).listHistory();

      if (!mounted) {
        return;
      }

      await showModalBottomSheet<void>(
        context: context,
        isScrollControlled: true,
        useSafeArea: true,
        builder: (_) => _FormSheet(
          title: 'Planos anteriores',
          child: history.isEmpty
              ? const Text('Ainda não existem planos anteriores.')
              : Column(
                  children: [
                    for (final workout in history)
                      ListTile(
                        contentPadding: EdgeInsets.zero,
                        leading: Icon(
                          workout.active ? Icons.play_circle : Icons.history,
                        ),
                        title: Text(workout.name),
                        subtitle: Text(
                          '${workout.phase} · '
                          'semana ${workout.currentWeek}/${workout.durationWeeks}',
                        ),
                        trailing: Text('v${workout.version}'),
                      ),
                  ],
                ),
        ),
      );
    });
  }

  Future<void> _toggleExercise(
    Workout workout,
    WorkoutExercise exercise,
    bool completed,
  ) async {
    if (exercise.id == null || exercise.id!.isEmpty) {
      return;
    }

    try {
      await ref.read(workoutRepositoryProvider).setExerciseCompleted(
            workoutId: workout.id,
            exerciseId: exercise.id!,
            completed: completed,
          );

      if (!mounted) {
        return;
      }

      ref.invalidate(activeWorkoutProvider);
    } on AppFailure catch (failure) {
      if (mounted) {
        _showSnack(failure.message);
      }
    }
  }

  Future<void> _runBusy(Future<void> Function() action) async {
    if (_isBusy || !mounted) {
      return;
    }

    setState(() {
      _isBusy = true;
    });

    try {
      await action();
    } on AppFailure catch (failure) {
      if (mounted) {
        _showSnack(failure.message);
      }
    } finally {
      if (mounted) {
        setState(() {
          _isBusy = false;
        });
      }
    }
  }

  void _showSnack(String message) {
    if (!mounted) {
      return;
    }

    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(content: Text(message)),
    );
  }
}

class _WorkoutContent extends StatelessWidget {
  const _WorkoutContent({
    required this.workout,
    required this.selectedDay,
    required this.exerciseTarget,
    required this.isBusy,
    required this.onPreviousDay,
    required this.onNextDay,
    required this.onSelectDay,
    required this.onDecreaseTarget,
    required this.onIncreaseTarget,
    required this.onAddExercise,
    required this.onToggleExercise,
    required this.onEditWorkout,
    required this.onNewPlan,
    required this.onEvolveWorkout,
    required this.onSuggestProgression,
    required this.onExecuteDay,
    required this.onEditExercise,
    required this.onReplaceExercise,
    required this.onRemoveExercise,
    required this.onExerciseHistory,
  });

  final Workout workout;
  final int selectedDay;
  final int exerciseTarget;
  final bool isBusy;

  final VoidCallback onPreviousDay;
  final VoidCallback onNextDay;
  final ValueChanged<int> onSelectDay;
  final VoidCallback onDecreaseTarget;
  final VoidCallback onIncreaseTarget;
  final VoidCallback onAddExercise;

  final Future<void> Function(
    Workout workout,
    WorkoutExercise exercise,
    bool completed,
  ) onToggleExercise;

  final VoidCallback onEditWorkout;
  final VoidCallback onNewPlan;
  final VoidCallback onEvolveWorkout;
  final VoidCallback onSuggestProgression;

  final void Function(
    int day,
    List<WorkoutExercise> exercises,
  ) onExecuteDay;

  final void Function(WorkoutExercise exercise) onEditExercise;
  final void Function(WorkoutExercise exercise) onReplaceExercise;
  final void Function(WorkoutExercise exercise) onRemoveExercise;
  final void Function(WorkoutExercise exercise) onExerciseHistory;

  @override
  Widget build(BuildContext context) {
    final exercises = workout.exercises
        .where((exercise) => exercise.trainingDay == selectedDay)
        .toList()
      ..sort((a, b) => a.order.compareTo(b.order));

    final difference = exercises.length - exerciseTarget;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        EfFeedbackBanner(
          title: workout.name,
          message: '${workout.phase} · Semana ${workout.currentWeek} de '
              '${workout.durationWeeks}. ${workout.message}',
        ),
        const SizedBox(height: AppTokens.space20),
        _WorkoutDaySelector(
          currentDay: selectedDay,
          totalDays: workout.weeklyFrequency <= 0 ? 1 : workout.weeklyFrequency,
          onPrevious: onPreviousDay,
          onNext: onNextDay,
          onSelect: onSelectDay,
        ),
        const SizedBox(height: AppTokens.space20),
        Row(
          children: [
            Expanded(
              child: Text(
                'Dia $selectedDay',
                style: Theme.of(context).textTheme.headlineSmall,
              ),
            ),
            TextButton.icon(
              onPressed: isBusy || exercises.isEmpty
                  ? null
                  : () => onExecuteDay(selectedDay, exercises),
              icon: const Icon(Icons.play_arrow),
              label: const Text('Executar'),
            ),
          ],
        ),
        const SizedBox(height: AppTokens.space8),
        Card(
          child: Padding(
            padding: const EdgeInsets.all(AppTokens.space16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                Text(
                  'Quantidade planejada',
                  style: Theme.of(context).textTheme.titleMedium,
                ),
                const SizedBox(height: AppTokens.space12),
                Row(
                  children: [
                    IconButton(
                      tooltip: 'Diminuir meta',
                      onPressed: exerciseTarget > 1 ? onDecreaseTarget : null,
                      icon: const Icon(Icons.remove_circle_outline),
                    ),
                    Text(
                      '$exerciseTarget',
                      style: Theme.of(context).textTheme.headlineSmall,
                    ),
                    IconButton(
                      tooltip: 'Aumentar meta',
                      onPressed: onIncreaseTarget,
                      icon: const Icon(Icons.add_circle_outline),
                    ),
                    const SizedBox(width: AppTokens.space12),
                    Expanded(
                      child: Text(
                        '${exercises.length} exercício'
                        '${exercises.length == 1 ? '' : 's'} atualmente',
                      ),
                    ),
                  ],
                ),
                if (difference < 0)
                  Text(
                    'Você escolheu $exerciseTarget exercícios. '
                    'Pode adicionar ${difference.abs()} para chegar à meta.',
                  )
                else if (difference > 0)
                  Text(
                    'Você está $difference exercício'
                    '${difference == 1 ? '' : 's'} acima da meta escolhida. '
                    'O app não bloqueia a quantidade.',
                  )
                else
                  const Text(
                    'Quantidade atual igual à meta escolhida.',
                  ),
                const SizedBox(height: AppTokens.space12),
                EfButton(
                  label: 'Adicionar exercício',
                  icon: Icons.add,
                  variant: EfButtonVariant.secondary,
                  onPressed: isBusy ? null : onAddExercise,
                ),
              ],
            ),
          ),
        ),
        const SizedBox(height: AppTokens.space20),
        if (exercises.isEmpty)
          const EfEmptyState(
            icon: Icons.fitness_center,
            title: 'Nenhum exercício neste dia',
            message: 'Adicione um exercício ou mova um exercício de outro dia.',
          )
        else
          for (final exercise in exercises) ...[
            _ExerciseTile(
              exercise: exercise,
              onChanged: (value) {
                onToggleExercise(
                  workout,
                  exercise,
                  value ?? false,
                );
              },
              onEdit: () => onEditExercise(exercise),
              onReplace: () => onReplaceExercise(exercise),
              onRemove: () => onRemoveExercise(exercise),
              onHistory: () => onExerciseHistory(exercise),
            ),
            const SizedBox(height: AppTokens.space12),
          ],
        const SizedBox(height: AppTokens.space20),
        Wrap(
          spacing: AppTokens.space8,
          runSpacing: AppTokens.space8,
          children: [
            EfButton(
              label: 'Editar plano',
              icon: Icons.edit_outlined,
              variant: EfButtonVariant.secondary,
              onPressed: isBusy ? null : onEditWorkout,
            ),
            EfButton(
              label: 'Novo plano',
              icon: Icons.add_circle_outline,
              variant: EfButtonVariant.secondary,
              onPressed: isBusy ? null : onNewPlan,
            ),
            EfButton(
              label: 'Evoluir treino',
              icon: Icons.trending_up,
              onPressed: isBusy ? null : onEvolveWorkout,
            ),
            EfButton(
              label: 'Progressão',
              icon: Icons.insights_outlined,
              variant: EfButtonVariant.secondary,
              onPressed: isBusy ? null : onSuggestProgression,
            ),
          ],
        ),
      ],
    );
  }
}

class _WorkoutDaySelector extends StatelessWidget {
  const _WorkoutDaySelector({
    required this.currentDay,
    required this.totalDays,
    required this.onPrevious,
    required this.onNext,
    required this.onSelect,
  });

  final int currentDay;
  final int totalDays;
  final VoidCallback onPrevious;
  final VoidCallback onNext;
  final ValueChanged<int> onSelect;

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Row(
          children: [
            IconButton(
              tooltip: 'Dia anterior',
              onPressed: currentDay > 1 ? onPrevious : null,
              icon: const Icon(Icons.chevron_left),
            ),
            Expanded(
              child: Text(
                'Dia $currentDay de $totalDays',
                textAlign: TextAlign.center,
                style: Theme.of(context).textTheme.titleLarge,
              ),
            ),
            IconButton(
              tooltip: 'Próximo dia',
              onPressed: currentDay < totalDays ? onNext : null,
              icon: const Icon(Icons.chevron_right),
            ),
          ],
        ),
        const SizedBox(height: AppTokens.space8),
        SingleChildScrollView(
          scrollDirection: Axis.horizontal,
          child: Row(
            children: [
              for (var day = 1; day <= totalDays; day++)
                Padding(
                  padding: const EdgeInsets.only(right: AppTokens.space8),
                  child: ChoiceChip(
                    label: Text('Dia $day'),
                    selected: currentDay == day,
                    onSelected: (_) => onSelect(day),
                  ),
                ),
            ],
          ),
        ),
      ],
    );
  }
}

class _ExerciseTile extends StatelessWidget {
  const _ExerciseTile({
    required this.exercise,
    required this.onChanged,
    required this.onEdit,
    required this.onReplace,
    required this.onRemove,
    required this.onHistory,
  });

  final WorkoutExercise exercise;
  final ValueChanged<bool?> onChanged;
  final VoidCallback onEdit;
  final VoidCallback onReplace;
  final VoidCallback onRemove;
  final VoidCallback onHistory;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(AppTokens.space16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Checkbox(
                  value: exercise.completedToday,
                  onChanged: onChanged,
                ),
                Expanded(
                  child: Text(
                    exercise.name,
                    style: Theme.of(context).textTheme.titleMedium,
                  ),
                ),
                PopupMenuButton<_ExerciseAction>(
                  tooltip: 'Ações',
                  onSelected: (action) {
                    switch (action) {
                      case _ExerciseAction.edit:
                        onEdit();
                        break;
                      case _ExerciseAction.replace:
                        onReplace();
                        break;
                      case _ExerciseAction.move:
                        onEdit();
                        break;
                      case _ExerciseAction.history:
                        onHistory();
                        break;
                      case _ExerciseAction.remove:
                        onRemove();
                        break;
                    }
                  },
                  itemBuilder: (_) => const [
                    PopupMenuItem(
                      value: _ExerciseAction.edit,
                      child: Text('Editar'),
                    ),
                    PopupMenuItem(
                      value: _ExerciseAction.replace,
                      child: Text('Trocar exercício'),
                    ),
                    PopupMenuItem(
                      value: _ExerciseAction.move,
                      child: Text('Mover para outro dia'),
                    ),
                    PopupMenuItem(
                      value: _ExerciseAction.history,
                      child: Text('Histórico'),
                    ),
                    PopupMenuDivider(),
                    PopupMenuItem(
                      value: _ExerciseAction.remove,
                      child: Text('Remover do plano'),
                    ),
                  ],
                ),
              ],
            ),
            const SizedBox(height: AppTokens.space8),
            Text(
              '${exercise.sets} séries · ${exercise.repetitions} · '
              '${exercise.restSeconds}s descanso',
            ),
            if (exercise.equipment != null &&
                exercise.equipment!.trim().isNotEmpty) ...[
              const SizedBox(height: AppTokens.space4),
              Text('Equipamento: ${exercise.equipment}'),
            ],
            if (exercise.targetLoadKg != null ||
                exercise.targetRpe != null) ...[
              const SizedBox(height: AppTokens.space4),
              Text(
                'Alvo: ${_kg(exercise.targetLoadKg)}'
                '${exercise.targetRpe == null ? '' : ' · RPE ${exercise.targetRpe}'}',
              ),
            ],
            if (exercise.note != null && exercise.note!.trim().isNotEmpty) ...[
              const SizedBox(height: AppTokens.space4),
              Text(exercise.note!),
            ],
            if (exercise.progressionReason != null &&
                exercise.progressionReason!.trim().isNotEmpty) ...[
              const SizedBox(height: AppTokens.space4),
              Text(exercise.progressionReason!),
            ],
            if (exercise.instruction.trim().isNotEmpty) ...[
              const SizedBox(height: AppTokens.space8),
              Text(exercise.instruction),
            ],
          ],
        ),
      ),
    );
  }
}

class _AddExerciseSheet extends StatefulWidget {
  const _AddExerciseSheet({
    required this.trainingDay,
    required this.nextOrder,
  });

  final int trainingDay;
  final int nextOrder;

  @override
  State<_AddExerciseSheet> createState() => _AddExerciseSheetState();
}

class _AddExerciseSheetState extends State<_AddExerciseSheet> {
  late final TextEditingController name;
  late final TextEditingController muscleGroup;
  late final TextEditingController level;
  late final TextEditingController equipment;
  late final TextEditingController instruction;
  late final TextEditingController day;
  late final TextEditingController order;
  late final TextEditingController sets;
  late final TextEditingController reps;
  late final TextEditingController rest;
  late final TextEditingController targetLoad;
  late final TextEditingController targetRpe;
  late final TextEditingController minReps;
  late final TextEditingController maxReps;
  late final TextEditingController note;

  @override
  void initState() {
    super.initState();

    name = TextEditingController();
    muscleGroup = TextEditingController();
    level = TextEditingController(text: 'iniciante');
    equipment = TextEditingController();
    instruction = TextEditingController();
    day = TextEditingController(text: widget.trainingDay.toString());
    order = TextEditingController(text: widget.nextOrder.toString());
    sets = TextEditingController(text: '3');
    reps = TextEditingController(text: '10-12');
    rest = TextEditingController(text: '60');
    targetLoad = TextEditingController();
    targetRpe = TextEditingController(text: '7');
    minReps = TextEditingController(text: '10');
    maxReps = TextEditingController(text: '12');
    note = TextEditingController();
  }

  @override
  void dispose() {
    name.dispose();
    muscleGroup.dispose();
    level.dispose();
    equipment.dispose();
    instruction.dispose();
    day.dispose();
    order.dispose();
    sets.dispose();
    reps.dispose();
    rest.dispose();
    targetLoad.dispose();
    targetRpe.dispose();
    minReps.dispose();
    maxReps.dispose();
    note.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return _FormSheet(
      title: 'Adicionar exercício',
      child: Column(
        children: [
          EfTextField(label: 'Nome', controller: name),
          const SizedBox(height: AppTokens.space12),
          EfTextField(label: 'Grupo muscular', controller: muscleGroup),
          const SizedBox(height: AppTokens.space12),
          EfTextField(label: 'Nível', controller: level),
          const SizedBox(height: AppTokens.space12),
          EfTextField(label: 'Equipamento', controller: equipment),
          const SizedBox(height: AppTokens.space12),
          EfTextField(
            label: 'Instrução',
            controller: instruction,
            maxLines: 3,
          ),
          const SizedBox(height: AppTokens.space12),
          Row(
            children: [
              Expanded(
                child: _NumberField(label: 'Dia', controller: day),
              ),
              const SizedBox(width: AppTokens.space12),
              Expanded(
                child: _NumberField(label: 'Ordem', controller: order),
              ),
            ],
          ),
          const SizedBox(height: AppTokens.space12),
          Row(
            children: [
              Expanded(
                child: _NumberField(label: 'Séries', controller: sets),
              ),
              const SizedBox(width: AppTokens.space12),
              Expanded(
                child: EfTextField(
                  label: 'Repetições',
                  controller: reps,
                ),
              ),
            ],
          ),
          const SizedBox(height: AppTokens.space12),
          _NumberField(
            label: 'Descanso em segundos',
            controller: rest,
          ),
          const SizedBox(height: AppTokens.space12),
          Row(
            children: [
              Expanded(
                child: _DecimalField(
                  label: 'Carga alvo kg',
                  controller: targetLoad,
                ),
              ),
              const SizedBox(width: AppTokens.space12),
              Expanded(
                child: _NumberField(
                  label: 'RPE alvo',
                  controller: targetRpe,
                ),
              ),
            ],
          ),
          const SizedBox(height: AppTokens.space12),
          Row(
            children: [
              Expanded(
                child: _NumberField(
                  label: 'Reps mín.',
                  controller: minReps,
                ),
              ),
              const SizedBox(width: AppTokens.space12),
              Expanded(
                child: _NumberField(
                  label: 'Reps máx.',
                  controller: maxReps,
                ),
              ),
            ],
          ),
          const SizedBox(height: AppTokens.space12),
          EfTextField(
            label: 'Observação',
            controller: note,
            maxLines: 3,
          ),
          const SizedBox(height: AppTokens.space20),
          EfButton(
            label: 'Adicionar exercício',
            icon: Icons.add,
            onPressed: _submit,
          ),
        ],
      ),
    );
  }

  void _submit() {
    final trimmedName = name.text.trim();

    if (trimmedName.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Informe o nome do exercício.'),
        ),
      );
      return;
    }

    FocusScope.of(context).unfocus();

    Navigator.of(context).pop(
      WorkoutExercise(
        id: null,
        exerciseId: '',
        name: trimmedName,
        muscleGroup: muscleGroup.text.trim(),
        level: level.text.trim().isEmpty ? 'iniciante' : level.text.trim(),
        equipment: equipment.text.trim().isEmpty ? null : equipment.text.trim(),
        instruction: instruction.text.trim(),
        trainingDay: _intValue(
          day.text,
          fallback: widget.trainingDay,
        ),
        completedToday: false,
        order: _intValue(
          order.text,
          fallback: widget.nextOrder,
        ),
        note: note.text.trim().isEmpty ? null : note.text.trim(),
        sets: _intValue(sets.text, fallback: 3),
        repetitions: reps.text.trim().isEmpty ? '10-12' : reps.text.trim(),
        restSeconds: _intValue(rest.text, fallback: 60),
        targetLoadKg: _optionalDouble(targetLoad.text),
        targetRpe: _optionalInt(targetRpe.text),
        minRepetitions: _optionalInt(minReps.text),
        maxRepetitions: _optionalInt(maxReps.text),
        progressionReason: null,
      ),
    );
  }
}

class _EditWorkoutSheet extends StatefulWidget {
  const _EditWorkoutSheet({
    required this.workout,
  });

  final Workout workout;

  @override
  State<_EditWorkoutSheet> createState() => _EditWorkoutSheetState();
}

class _EditWorkoutSheetState extends State<_EditWorkoutSheet> {
  late final TextEditingController name;
  late final TextEditingController goal;
  late final TextEditingController frequency;
  late final TextEditingController weeks;
  late final TextEditingController phase;

  @override
  void initState() {
    super.initState();

    final workout = widget.workout;

    name = TextEditingController(text: workout.name);
    goal = TextEditingController(text: workout.goal);
    frequency = TextEditingController(text: workout.weeklyFrequency.toString());
    weeks = TextEditingController(text: workout.durationWeeks.toString());
    phase = TextEditingController(text: workout.phase);
  }

  @override
  void dispose() {
    name.dispose();
    goal.dispose();
    frequency.dispose();
    weeks.dispose();
    phase.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return _FormSheet(
      title: 'Editar treino',
      child: Column(
        children: [
          EfTextField(label: 'Nome', controller: name),
          const SizedBox(height: AppTokens.space12),
          EfTextField(label: 'Objetivo', controller: goal),
          const SizedBox(height: AppTokens.space12),
          Row(
            children: [
              Expanded(
                child: _NumberField(
                  label: 'Frequência',
                  controller: frequency,
                ),
              ),
              const SizedBox(width: AppTokens.space12),
              Expanded(
                child: _NumberField(
                  label: 'Semanas',
                  controller: weeks,
                ),
              ),
            ],
          ),
          const SizedBox(height: AppTokens.space12),
          EfTextField(label: 'Fase', controller: phase),
          const SizedBox(height: AppTokens.space20),
          EfButton(
            label: 'Salvar',
            icon: Icons.save_outlined,
            onPressed: _submit,
          ),
        ],
      ),
    );
  }

  void _submit() {
    FocusScope.of(context).unfocus();

    final workout = widget.workout;

    Navigator.of(context).pop(
      Workout(
        id: workout.id,
        name: name.text.trim(),
        goal: goal.text.trim(),
        weeklyFrequency: _intValue(
          frequency.text,
          fallback: workout.weeklyFrequency,
        ),
        active: workout.active,
        exercises: workout.exercises,
        message: workout.message,
        version: workout.version,
        previousWorkoutId: workout.previousWorkoutId,
        startDate: workout.startDate ?? DateTime.now(),
        endDate: workout.endDate,
        durationWeeks: _intValue(
          weeks.text,
          fallback: workout.durationWeeks,
        ),
        currentWeek: workout.currentWeek,
        phase: phase.text.trim(),
      ),
    );
  }
}

class _EditExerciseSheet extends StatefulWidget {
  const _EditExerciseSheet({
    required this.exercise,
  });

  final WorkoutExercise exercise;

  @override
  State<_EditExerciseSheet> createState() => _EditExerciseSheetState();
}

class _EditExerciseSheetState extends State<_EditExerciseSheet> {
  late final TextEditingController day;
  late final TextEditingController order;
  late final TextEditingController sets;
  late final TextEditingController reps;
  late final TextEditingController rest;
  late final TextEditingController equipment;
  late final TextEditingController note;
  late final TextEditingController targetLoad;
  late final TextEditingController targetRpe;
  late final TextEditingController minReps;
  late final TextEditingController maxReps;
  late final TextEditingController progressionReason;

  @override
  void initState() {
    super.initState();

    final exercise = widget.exercise;

    day = TextEditingController(text: exercise.trainingDay.toString());
    order = TextEditingController(text: exercise.order.toString());
    sets = TextEditingController(text: exercise.sets.toString());
    reps = TextEditingController(text: exercise.repetitions);
    rest = TextEditingController(text: exercise.restSeconds.toString());
    equipment = TextEditingController(text: exercise.equipment ?? '');
    note = TextEditingController(text: exercise.note ?? '');
    targetLoad = TextEditingController(
      text: _weightInput(exercise.targetLoadKg),
    );
    targetRpe =
        TextEditingController(text: exercise.targetRpe?.toString() ?? '');
    minReps = TextEditingController(
      text: exercise.minRepetitions?.toString() ?? '',
    );
    maxReps = TextEditingController(
      text: exercise.maxRepetitions?.toString() ?? '',
    );
    progressionReason =
        TextEditingController(text: exercise.progressionReason ?? '');
  }

  @override
  void dispose() {
    day.dispose();
    order.dispose();
    sets.dispose();
    reps.dispose();
    rest.dispose();
    equipment.dispose();
    note.dispose();
    targetLoad.dispose();
    targetRpe.dispose();
    minReps.dispose();
    maxReps.dispose();
    progressionReason.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final exercise = widget.exercise;

    return _FormSheet(
      title: exercise.name,
      child: Column(
        children: [
          Row(
            children: [
              Expanded(
                child: _NumberField(label: 'Dia', controller: day),
              ),
              const SizedBox(width: AppTokens.space12),
              Expanded(
                child: _NumberField(label: 'Ordem', controller: order),
              ),
            ],
          ),
          const SizedBox(height: AppTokens.space12),
          Row(
            children: [
              Expanded(
                child: _NumberField(label: 'Séries', controller: sets),
              ),
              const SizedBox(width: AppTokens.space12),
              Expanded(
                child: EfTextField(
                  label: 'Repetições',
                  controller: reps,
                ),
              ),
            ],
          ),
          const SizedBox(height: AppTokens.space12),
          _NumberField(
            label: 'Descanso em segundos',
            controller: rest,
          ),
          const SizedBox(height: AppTokens.space12),
          Row(
            children: [
              Expanded(
                child: _DecimalField(
                  label: 'Carga alvo kg',
                  controller: targetLoad,
                ),
              ),
              const SizedBox(width: AppTokens.space12),
              Expanded(
                child: _NumberField(
                  label: 'RPE alvo',
                  controller: targetRpe,
                ),
              ),
            ],
          ),
          const SizedBox(height: AppTokens.space12),
          Row(
            children: [
              Expanded(
                child: _NumberField(
                  label: 'Reps mín.',
                  controller: minReps,
                ),
              ),
              const SizedBox(width: AppTokens.space12),
              Expanded(
                child: _NumberField(
                  label: 'Reps máx.',
                  controller: maxReps,
                ),
              ),
            ],
          ),
          const SizedBox(height: AppTokens.space12),
          EfTextField(
            label: 'Equipamento',
            controller: equipment,
          ),
          const SizedBox(height: AppTokens.space12),
          EfTextField(
            label: 'Observação',
            controller: note,
            maxLines: 3,
          ),
          const SizedBox(height: AppTokens.space12),
          EfTextField(
            label: 'Motivo da progressão',
            controller: progressionReason,
            maxLines: 3,
          ),
          const SizedBox(height: AppTokens.space20),
          EfButton(
            label: 'Salvar exercício',
            icon: Icons.save_outlined,
            onPressed: _submit,
          ),
        ],
      ),
    );
  }

  void _submit() {
    FocusScope.of(context).unfocus();

    final exercise = widget.exercise;

    Navigator.of(context).pop(
      WorkoutExercise(
        id: exercise.id,
        exerciseId: exercise.exerciseId,
        name: exercise.name,
        muscleGroup: exercise.muscleGroup,
        level: exercise.level,
        equipment: equipment.text.trim().isEmpty ? null : equipment.text.trim(),
        instruction: exercise.instruction,
        trainingDay: _intValue(
          day.text,
          fallback: exercise.trainingDay,
        ),
        completedToday: exercise.completedToday,
        order: _intValue(
          order.text,
          fallback: exercise.order,
        ),
        note: note.text.trim().isEmpty ? null : note.text.trim(),
        sets: _intValue(
          sets.text,
          fallback: exercise.sets,
        ),
        repetitions: reps.text.trim(),
        restSeconds: _intValue(
          rest.text,
          fallback: exercise.restSeconds,
        ),
        targetLoadKg: _optionalDouble(targetLoad.text),
        targetRpe: _optionalInt(targetRpe.text),
        minRepetitions: _optionalInt(minReps.text),
        maxRepetitions: _optionalInt(maxReps.text),
        progressionReason: progressionReason.text.trim().isEmpty
            ? null
            : progressionReason.text.trim(),
      ),
    );
  }
}

class _NewWorkoutPlanSheet extends StatefulWidget {
  const _NewWorkoutPlanSheet();

  @override
  State<_NewWorkoutPlanSheet> createState() => _NewWorkoutPlanSheetState();
}

class _NewWorkoutPlanSheetState extends State<_NewWorkoutPlanSheet> {
  late final TextEditingController goal;
  late final TextEditingController level;
  late final TextEditingController days;
  late final TextEditingController duration;
  late final TextEditingController weeks;
  late final TextEditingController equipment;
  late final TextEditingController limitations;
  late final TextEditingController groups;

  @override
  void initState() {
    super.initState();

    goal = TextEditingController(text: 'Hipertrofia');
    level = TextEditingController(text: 'iniciante');
    days = TextEditingController(text: '3');
    duration = TextEditingController(text: '45');
    weeks = TextEditingController(text: '6');
    equipment = TextEditingController(text: 'halteres, elástico');
    limitations = TextEditingController();
    groups = TextEditingController();
  }

  @override
  void dispose() {
    goal.dispose();
    level.dispose();
    days.dispose();
    duration.dispose();
    weeks.dispose();
    equipment.dispose();
    limitations.dispose();
    groups.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return _FormSheet(
      title: 'Novo plano',
      child: Column(
        children: [
          EfTextField(label: 'Objetivo', controller: goal),
          const SizedBox(height: AppTokens.space12),
          EfTextField(label: 'Nível', controller: level),
          const SizedBox(height: AppTokens.space12),
          Row(
            children: [
              Expanded(
                child: _NumberField(
                  label: 'Dias/semana',
                  controller: days,
                ),
              ),
              const SizedBox(width: AppTokens.space12),
              Expanded(
                child: _NumberField(
                  label: 'Minutos',
                  controller: duration,
                ),
              ),
            ],
          ),
          const SizedBox(height: AppTokens.space12),
          _NumberField(
            label: 'Duração em semanas',
            controller: weeks,
          ),
          const SizedBox(height: AppTokens.space12),
          EfTextField(
            label: 'Equipamentos',
            controller: equipment,
          ),
          const SizedBox(height: AppTokens.space12),
          EfTextField(
            label: 'Limitações',
            controller: limitations,
          ),
          const SizedBox(height: AppTokens.space12),
          EfTextField(
            label: 'Grupos prioritários',
            controller: groups,
          ),
          const SizedBox(height: AppTokens.space20),
          EfButton(
            label: 'Gerar plano',
            icon: Icons.auto_awesome,
            onPressed: _submit,
          ),
        ],
      ),
    );
  }

  void _submit() {
    FocusScope.of(context).unfocus();

    Navigator.of(context).pop(
      WorkoutGenerationConfig(
        goal: goal.text.trim(),
        level: level.text.trim(),
        daysPerWeek: _intValue(days.text, fallback: 3),
        durationMinutes: _intValue(duration.text, fallback: 45),
        durationWeeks: _intValue(weeks.text, fallback: 6),
        equipment: _csv(equipment.text),
        limitations: _csv(limitations.text),
        priorityMuscleGroups: _csv(groups.text),
      ),
    );
  }
}

class _WorkoutExecutionSheet extends StatefulWidget {
  const _WorkoutExecutionSheet({
    required this.trainingDay,
    required this.exercises,
  });

  final int trainingDay;
  final List<WorkoutExercise> exercises;

  @override
  State<_WorkoutExecutionSheet> createState() => _WorkoutExecutionSheetState();
}

class _WorkoutExecutionSheetState extends State<_WorkoutExecutionSheet> {
  late final List<_SetControllerEntry> entries;

  @override
  void initState() {
    super.initState();

    entries = <_SetControllerEntry>[];

    for (final exercise in widget.exercises.where((item) => item.id != null)) {
      for (var set = 1; set <= exercise.sets; set++) {
        entries.add(
          _SetControllerEntry(
            exercise: exercise,
            setNumber: set,
          ),
        );
      }
    }
  }

  @override
  void dispose() {
    for (final entry in entries) {
      entry.dispose();
    }

    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return _FormSheet(
      title: 'Executar dia ${widget.trainingDay}',
      child: entries.isEmpty
          ? const Text(
              'Não existem exercícios disponíveis para este dia.',
            )
          : Column(
              children: [
                for (final entry in entries) ...[
                  Align(
                    alignment: Alignment.centerLeft,
                    child: Text(
                      '${entry.exercise.name} · série ${entry.setNumber}',
                      style: Theme.of(context).textTheme.titleSmall,
                    ),
                  ),
                  const SizedBox(height: AppTokens.space8),
                  Row(
                    children: [
                      Expanded(
                        child: _DecimalField(
                          label: 'Carga kg',
                          controller: entry.load,
                        ),
                      ),
                      const SizedBox(width: AppTokens.space8),
                      Expanded(
                        child: _NumberField(
                          label: 'Reps',
                          controller: entry.repetitions,
                        ),
                      ),
                      const SizedBox(width: AppTokens.space8),
                      Expanded(
                        child: _NumberField(
                          label: 'RPE',
                          controller: entry.rpe,
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: AppTokens.space8),
                  EfTextField(
                    label: 'Observação da série',
                    controller: entry.note,
                  ),
                  CheckboxListTile(
                    contentPadding: EdgeInsets.zero,
                    value: entry.pain,
                    title: const Text('Dor/desconforto'),
                    onChanged: (value) {
                      setState(() {
                        entry.pain = value ?? false;
                      });
                    },
                  ),
                  if (entry.pain) ...[
                    EfTextField(
                      label: 'Desconforto percebido',
                      controller: entry.painDescription,
                    ),
                    const SizedBox(height: AppTokens.space8),
                  ],
                  const SizedBox(height: AppTokens.space12),
                ],
                EfButton(
                  label: 'Finalizar treino',
                  icon: Icons.check_circle_outline,
                  onPressed: _submit,
                ),
              ],
            ),
    );
  }

  void _submit() {
    FocusScope.of(context).unfocus();

    final result = entries
        .map(
          (entry) => WorkoutSetEntry(
            exerciseId: entry.exercise.id!,
            setNumber: entry.setNumber,
            loadKg: _optionalDouble(entry.load.text),
            repetitions: _intValue(
              entry.repetitions.text,
              fallback: 0,
            ),
            rpe: _intValue(entry.rpe.text, fallback: 7),
            note:
                entry.note.text.trim().isEmpty ? null : entry.note.text.trim(),
            pain: entry.pain,
            painDescription: entry.painDescription.text.trim().isEmpty
                ? null
                : entry.painDescription.text.trim(),
          ),
        )
        .toList(growable: false);

    Navigator.of(context).pop(result);
  }
}

class _ProposalChangeCard extends StatelessWidget {
  const _ProposalChangeCard({
    required this.change,
  });

  final WorkoutEvolutionChange change;

  @override
  Widget build(BuildContext context) {
    final textTheme = Theme.of(context).textTheme;

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(AppTokens.space12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(change.exerciseName, style: textTheme.titleSmall),
            const SizedBox(height: AppTokens.space8),
            _comparisonLine(
              'Séries',
              change.currentSets.toString(),
              change.proposedSets.toString(),
            ),
            _comparisonLine(
              'Repetições',
              change.currentRepetitions,
              change.proposedRepetitions,
            ),
            _comparisonLine(
              'Carga',
              _kg(change.currentLoadKg),
              _kg(change.proposedLoadKg),
            ),
            _comparisonLine(
              'Descanso',
              '${change.currentRestSeconds}s',
              '${change.proposedRestSeconds}s',
            ),
            const SizedBox(height: AppTokens.space8),
            Text(change.reason),
          ],
        ),
      ),
    );
  }

  Widget _comparisonLine(
    String label,
    String current,
    String proposed,
  ) {
    return Padding(
      padding: const EdgeInsets.only(bottom: AppTokens.space4),
      child: Row(
        children: [
          SizedBox(width: 88, child: Text(label)),
          Expanded(child: Text('$current → $proposed')),
        ],
      ),
    );
  }
}

class _FormSheet extends StatelessWidget {
  const _FormSheet({
    required this.title,
    required this.child,
  });

  final String title;
  final Widget child;

  @override
  Widget build(BuildContext context) {
    return SafeArea(
      child: SingleChildScrollView(
        keyboardDismissBehavior: ScrollViewKeyboardDismissBehavior.onDrag,
        padding: EdgeInsets.only(
          left: AppTokens.space16,
          right: AppTokens.space16,
          top: AppTokens.space16,
          bottom: MediaQuery.of(context).viewInsets.bottom + AppTokens.space16,
        ),
        child: Center(
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 720),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              mainAxisSize: MainAxisSize.min,
              children: [
                Text(
                  title,
                  style: Theme.of(context).textTheme.titleLarge,
                ),
                const SizedBox(height: AppTokens.space16),
                child,
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _NumberField extends StatelessWidget {
  const _NumberField({
    required this.label,
    required this.controller,
  });

  final String label;
  final TextEditingController controller;

  @override
  Widget build(BuildContext context) {
    return EfTextField(
      label: label,
      controller: controller,
      keyboardType: TextInputType.number,
      inputFormatters: [
        FilteringTextInputFormatter.digitsOnly,
      ],
    );
  }
}

class _DecimalField extends StatelessWidget {
  const _DecimalField({
    required this.label,
    required this.controller,
  });

  final String label;
  final TextEditingController controller;

  @override
  Widget build(BuildContext context) {
    return EfTextField(
      label: label,
      controller: controller,
      keyboardType: const TextInputType.numberWithOptions(decimal: true),
      inputFormatters: [
        FilteringTextInputFormatter.allow(RegExp(r'[0-9,.]')),
      ],
    );
  }
}

class _SetControllerEntry {
  _SetControllerEntry({
    required this.exercise,
    required this.setNumber,
  })  : load = TextEditingController(
          text: _weightInput(exercise.targetLoadKg),
        ),
        repetitions = TextEditingController(
          text: _defaultRepetitions(exercise.repetitions),
        ),
        rpe = TextEditingController(
          text: exercise.targetRpe?.toString() ?? '7',
        ),
        note = TextEditingController(),
        painDescription = TextEditingController();

  final WorkoutExercise exercise;
  final int setNumber;

  final TextEditingController load;
  final TextEditingController repetitions;
  final TextEditingController rpe;
  final TextEditingController note;
  final TextEditingController painDescription;

  bool pain = false;

  void dispose() {
    load.dispose();
    repetitions.dispose();
    rpe.dispose();
    note.dispose();
    painDescription.dispose();
  }

  static String _defaultRepetitions(String value) {
    final matches = RegExp(r'\d+').allMatches(value).toList();

    if (matches.isEmpty) {
      return '10';
    }

    return matches.last.group(0) ?? '10';
  }
}

enum _ExerciseAction {
  edit,
  replace,
  move,
  history,
  remove,
}

int _intValue(
  String value, {
  required int fallback,
}) {
  return int.tryParse(value.trim()) ?? fallback;
}

int? _optionalInt(String value) {
  final trimmed = value.trim();

  if (trimmed.isEmpty) {
    return null;
  }

  return int.tryParse(trimmed);
}

double? _optionalDouble(String value) {
  final normalized = value.trim().replaceAll(',', '.');

  if (normalized.isEmpty) {
    return null;
  }

  return double.tryParse(normalized);
}

List<String> _csv(String value) {
  return value
      .split(',')
      .map((item) => item.trim())
      .where((item) => item.isNotEmpty)
      .toList(growable: false);
}

String _kg(double? value) {
  if (value == null) {
    return '-';
  }

  return '${value.toStringAsFixed(1)} kg';
}

String _weightInput(double? value) {
  if (value == null) {
    return '';
  }

  if (value % 1 == 0) {
    return value.toStringAsFixed(0);
  }

  return value.toStringAsFixed(1);
}

String _dateLabel(DateTime? value) {
  if (value == null) {
    return '-';
  }

  final day = value.day.toString().padLeft(2, '0');
  final month = value.month.toString().padLeft(2, '0');

  return '$day/$month/${value.year}';
}
