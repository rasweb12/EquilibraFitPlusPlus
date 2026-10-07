import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/utils/formatters.dart';
import '../../../../design_system/tokens/app_tokens.dart';
import '../../../../design_system/widgets/ef_action_tile.dart';
import '../../../../design_system/widgets/ef_button.dart';
import '../../../../design_system/widgets/ef_empty_state.dart';
import '../../../../design_system/widgets/ef_macro_bar.dart';
import '../../../../design_system/widgets/ef_metric_tile.dart';
import '../../../../design_system/widgets/ef_scaffold.dart';
import '../../../../design_system/widgets/ef_section.dart';
import '../../../../design_system/widgets/ef_skeleton.dart';
import '../../../auth/presentation/controllers/session_controller.dart';
import '../../../habits/data/daily_habits_controller.dart';
import '../../../habits/domain/daily_habits.dart';
import '../../../notifications/data/notification_repository.dart';
import '../../data/dashboard_repository.dart';
import '../../domain/dashboard_summary.dart';

class DashboardPage extends ConsumerWidget {
  const DashboardPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final dashboard = ref.watch(dashboardSummaryProvider);
    final habits = ref.watch(dailyHabitsProvider);
    final session = ref.watch(sessionControllerProvider);
    final unreadNotifications = ref.watch(notificationsProvider).maybeWhen(
          data: (items) => items.where((item) => !item.isRead).length,
          orElse: () => 0,
        );

    return EfScaffold(
      title: 'Hoje',
      subtitle: 'Saúde sem exageros.',
      currentIndex: 0,
      onRefresh: () async {
        ref.invalidate(dashboardSummaryProvider);
        ref.invalidate(notificationsProvider);
        await ref.read(dashboardSummaryProvider.future);
      },
      actions: [
        Badge(
          isLabelVisible: unreadNotifications > 0,
          label: Text(unreadNotifications > 9 ? '9+' : '$unreadNotifications'),
          child: IconButton(
            tooltip: 'Notificações',
            onPressed: () => context.go('/notifications'),
            icon: const Icon(Icons.notifications_outlined),
          ),
        ),
        IconButton(
          tooltip: 'Atualizar',
          onPressed: () => ref.invalidate(dashboardSummaryProvider),
          icon: const Icon(Icons.refresh),
        ),
      ],
      body: AnimatedSwitcher(
        duration: const Duration(milliseconds: 240),
        child: dashboard.when(
          data: (summary) => _DashboardContent(
            key: ValueKey(summary.date.toIso8601String()),
            summary: summary,
            habits: habits,
            userName: session.user?.name,
            onWaterAdd: ref.read(dailyHabitsProvider.notifier).incrementWater,
            onWaterRemove:
                ref.read(dailyHabitsProvider.notifier).decrementWater,
            onSleepChanged:
                ref.read(dailyHabitsProvider.notifier).setSleepHours,
            onMoodChanged: ref.read(dailyHabitsProvider.notifier).setMood,
            onMeditationChanged:
                ref.read(dailyHabitsProvider.notifier).toggleMeditation,
            onStretchingChanged:
                ref.read(dailyHabitsProvider.notifier).toggleStretching,
          ),
          loading: () => const EfSkeleton(),
          error: (error, stackTrace) => EfEmptyState(
            icon: Icons.wifi_off_outlined,
            title: 'Não conseguimos atualizar agora',
            message:
                'Se estiver sem internet, seu treino salvo continua disponível neste aparelho.',
            actionLabel: 'Abrir treino salvo',
            onAction: () => context.go('/workouts'),
          ),
        ),
      ),
    );
  }
}

class _DashboardContent extends StatelessWidget {
  const _DashboardContent({
    required super.key,
    required this.summary,
    required this.habits,
    required this.userName,
    required this.onWaterAdd,
    required this.onWaterRemove,
    required this.onSleepChanged,
    required this.onMoodChanged,
    required this.onMeditationChanged,
    required this.onStretchingChanged,
  });

  final DashboardSummary summary;
  final DailyHabits habits;
  final String? userName;
  final Future<void> Function() onWaterAdd;
  final Future<void> Function() onWaterRemove;
  final Future<void> Function(double value) onSleepChanged;
  final Future<void> Function(int value) onMoodChanged;
  final Future<void> Function(bool value) onMeditationChanged;
  final Future<void> Function(bool value) onStretchingChanged;

  @override
  Widget build(BuildContext context) {
    final food = summary.food;
    final calorieGoal =
        food.calorieGoal ?? summary.currentPlan?.dailyCalories.toDouble();
    final caloriesValue = calorieGoal == null
        ? '${formatDecimal(food.calories)} kcal'
        : '${formatDecimal(food.calories)} / ${formatDecimal(calorieGoal)} kcal';
    final macroGoals = _macroGoals(calorieGoal);
    final nextMeal = _nextMealLabel(DateTime.now());
    final nextWorkout = summary.workout.nextWorkout?.trim().isNotEmpty == true
        ? summary.workout.nextWorkout!.trim()
        : summary.workout.activeWorkouts > 0
            ? 'Treino ativo'
            : 'Gerar treino';

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        _TodayHero(
          greeting: _greeting(userName),
          message: summary.supportMessage,
          goal: _goalLabel(summary.profile?.goal ?? summary.currentPlan?.goal),
          insight: _coachInsight(summary, habits),
        ),
        const SizedBox(height: AppTokens.space20),
        Wrap(
          spacing: AppTokens.space12,
          runSpacing: AppTokens.space12,
          children: [
            SizedBox(
              width: 160,
              child: EfMetricTile(
                label: 'Calorias',
                value: caloriesValue,
                supportingText: '${food.meals} refeições',
                icon: Icons.local_fire_department_outlined,
              ),
            ),
            SizedBox(
              width: 160,
              child: EfMetricTile(
                label: 'Água',
                value:
                    '${formatLitersFromMl(habits.waterMl)} / ${formatLitersFromMl(habits.waterGoalMl)} L',
                supportingText:
                    habits.waterDone ? 'Meta do dia ok' : 'Pode ajustar',
                icon: Icons.water_drop_outlined,
              ),
            ),
            SizedBox(
              width: 160,
              child: EfMetricTile(
                label: 'Peso',
                value: summary.progress.lastWeightKg == null
                    ? '--'
                    : '${formatDecimal(summary.progress.lastWeightKg!, decimals: 1)} kg',
                supportingText: summary.progress.lastUpdate == null
                    ? 'Sem registro recente'
                    : formatDate(summary.progress.lastUpdate!),
                icon: Icons.monitor_weight_outlined,
              ),
            ),
            SizedBox(
              width: 160,
              child: EfMetricTile(
                label: 'Meta diária',
                value: _goalShort(summary.currentPlan?.goal),
                supportingText: calorieGoal == null
                    ? 'Plano flexível'
                    : '${formatDecimal(calorieGoal)} kcal',
                icon: Icons.flag_outlined,
              ),
            ),
          ],
        ),
        const SizedBox(height: AppTokens.space20),
        EfSection(
          title: 'Macros',
          subtitle: 'Valores aproximados para orientar escolhas, sem rigidez.',
          child: Card(
            child: Padding(
              padding: const EdgeInsets.all(AppTokens.space16),
              child: Column(
                children: [
                  EfMacroBar(
                    label: 'Proteína',
                    value: food.proteinG,
                    goal: macroGoals.protein,
                    color: AppTokens.protein,
                  ),
                  const SizedBox(height: AppTokens.space12),
                  EfMacroBar(
                    label: 'Carboidratos',
                    value: food.carbsG,
                    goal: macroGoals.carbs,
                    color: AppTokens.carbs,
                  ),
                  const SizedBox(height: AppTokens.space12),
                  EfMacroBar(
                    label: 'Gorduras',
                    value: food.fatG,
                    goal: macroGoals.fat,
                    color: AppTokens.fat,
                  ),
                ],
              ),
            ),
          ),
        ),
        const SizedBox(height: AppTokens.space20),
        EfSection(
          title: 'Próximos passos',
          child: Column(
            children: [
              EfActionTile(
                title: nextMeal,
                subtitle:
                    'Registrar, pesquisar, fotografar ou calcular com IA.',
                icon: Icons.restaurant_menu,
                onTap: () => context.go('/meals/new'),
              ),
              const SizedBox(height: AppTokens.space12),
              EfActionTile(
                title: nextWorkout,
                subtitle: '${summary.workout.weeklyFrequency} dias planejados',
                icon: Icons.fitness_center,
                onTap: () => context.go('/workouts'),
              ),
            ],
          ),
        ),
        const SizedBox(height: AppTokens.space20),
        _HabitsCard(
          habits: habits,
          onWaterAdd: onWaterAdd,
          onWaterRemove: onWaterRemove,
          onSleepChanged: onSleepChanged,
          onMoodChanged: onMoodChanged,
          onMeditationChanged: onMeditationChanged,
          onStretchingChanged: onStretchingChanged,
        ),
        const SizedBox(height: AppTokens.space20),
        _ChecklistCard(summary: summary, habits: habits),
        const SizedBox(height: AppTokens.space20),
        Wrap(
          spacing: AppTokens.space12,
          runSpacing: AppTokens.space12,
          children: [
            EfButton(
              label: 'Refeição',
              icon: Icons.add,
              onPressed: () => context.go('/meals/new'),
            ),
            EfButton(
              label: 'Foto',
              icon: Icons.photo_camera_outlined,
              variant: EfButtonVariant.secondary,
              onPressed: () => context.go('/meals/photo'),
            ),
            EfButton(
              label: 'Coach',
              icon: Icons.chat_bubble_outline,
              variant: EfButtonVariant.secondary,
              onPressed: () => context.go('/coach'),
            ),
            EfButton(
              label: 'Plano',
              icon: Icons.fact_check_outlined,
              variant: EfButtonVariant.secondary,
              onPressed: () => context.go('/plan'),
            ),
          ],
        ),
      ],
    );
  }
}

class _TodayHero extends StatelessWidget {
  const _TodayHero({
    required this.greeting,
    required this.message,
    required this.goal,
    required this.insight,
  });

  final String greeting;
  final String message;
  final String goal;
  final String insight;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;

    return DecoratedBox(
      decoration: BoxDecoration(
        color: scheme.primaryContainer,
        borderRadius: BorderRadius.circular(AppTokens.radius),
        boxShadow: AppTokens.softShadow,
      ),
      child: Padding(
        padding: const EdgeInsets.all(AppTokens.space20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              greeting,
              style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                    color: scheme.onPrimaryContainer,
                  ),
            ),
            const SizedBox(height: AppTokens.space8),
            Text(
              goal,
              style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                    color: scheme.onPrimaryContainer,
                  ),
            ),
            const SizedBox(height: AppTokens.space16),
            Text(
              insight,
              style: Theme.of(context).textTheme.titleMedium?.copyWith(
                    color: scheme.onPrimaryContainer,
                  ),
            ),
            if (message.trim().isNotEmpty) ...[
              const SizedBox(height: AppTokens.space12),
              Text(
                message,
                style: Theme.of(context).textTheme.bodySmall?.copyWith(
                      color: scheme.onPrimaryContainer,
                    ),
              ),
            ],
          ],
        ),
      ),
    );
  }
}

class _HabitsCard extends StatelessWidget {
  const _HabitsCard({
    required this.habits,
    required this.onWaterAdd,
    required this.onWaterRemove,
    required this.onSleepChanged,
    required this.onMoodChanged,
    required this.onMeditationChanged,
    required this.onStretchingChanged,
  });

  final DailyHabits habits;
  final Future<void> Function() onWaterAdd;
  final Future<void> Function() onWaterRemove;
  final Future<void> Function(double value) onSleepChanged;
  final Future<void> Function(int value) onMoodChanged;
  final Future<void> Function(bool value) onMeditationChanged;
  final Future<void> Function(bool value) onStretchingChanged;

  @override
  Widget build(BuildContext context) {
    return EfSection(
      title: 'Hábitos',
      subtitle: 'Água, sono, humor e recuperação no mesmo lugar.',
      child: Card(
        child: Padding(
          padding: const EdgeInsets.all(AppTokens.space16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Row(
                children: [
                  const Icon(Icons.water_drop_outlined),
                  const SizedBox(width: AppTokens.space8),
                  Expanded(
                    child: Text(
                      'Água',
                      style: Theme.of(context).textTheme.titleSmall,
                    ),
                  ),
                  IconButton(
                    tooltip: 'Reduzir água',
                    onPressed: onWaterRemove,
                    icon: const Icon(Icons.remove_circle_outline),
                  ),
                  Text('${formatLitersFromMl(habits.waterMl)} L'),
                  IconButton(
                    tooltip: 'Adicionar água',
                    onPressed: onWaterAdd,
                    icon: const Icon(Icons.add_circle_outline),
                  ),
                ],
              ),
              LinearProgressIndicator(
                value: habits.waterProgress,
                color: AppTokens.water,
                minHeight: 8,
                borderRadius: BorderRadius.circular(AppTokens.radius),
              ),
              const SizedBox(height: AppTokens.space16),
              Text(
                'Sono: ${formatDecimal(habits.sleepHours, decimals: 1)}h',
                style: Theme.of(context).textTheme.titleSmall,
              ),
              Slider(
                value: habits.sleepHours.clamp(0, 14).toDouble(),
                min: 0,
                max: 14,
                divisions: 28,
                label: '${formatDecimal(habits.sleepHours, decimals: 1)}h',
                onChanged: (value) {
                  onSleepChanged(value);
                },
              ),
              const SizedBox(height: AppTokens.space8),
              Text('Humor', style: Theme.of(context).textTheme.titleSmall),
              const SizedBox(height: AppTokens.space8),
              SegmentedButton<int>(
                selected: <int>{habits.mood},
                onSelectionChanged: (selection) {
                  onMoodChanged(selection.first);
                },
                segments: const [
                  ButtonSegment(
                    value: 1,
                    icon: Icon(Icons.sentiment_very_dissatisfied),
                  ),
                  ButtonSegment(
                    value: 2,
                    icon: Icon(Icons.sentiment_dissatisfied),
                  ),
                  ButtonSegment(value: 3, icon: Icon(Icons.sentiment_neutral)),
                  ButtonSegment(
                    value: 4,
                    icon: Icon(Icons.sentiment_satisfied),
                  ),
                  ButtonSegment(
                    value: 5,
                    icon: Icon(Icons.sentiment_very_satisfied),
                  ),
                ],
              ),
              const SizedBox(height: AppTokens.space12),
              CheckboxListTile(
                value: habits.meditationDone,
                onChanged: (value) {
                  onMeditationChanged(value ?? false);
                },
                title: const Text('Meditação'),
                controlAffinity: ListTileControlAffinity.leading,
                contentPadding: EdgeInsets.zero,
              ),
              CheckboxListTile(
                value: habits.stretchingDone,
                onChanged: (value) {
                  onStretchingChanged(value ?? false);
                },
                title: const Text('Alongamento'),
                controlAffinity: ListTileControlAffinity.leading,
                contentPadding: EdgeInsets.zero,
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _ChecklistCard extends StatelessWidget {
  const _ChecklistCard({required this.summary, required this.habits});

  final DashboardSummary summary;
  final DailyHabits habits;

  @override
  Widget build(BuildContext context) {
    final items = <_ChecklistItem>[
      _ChecklistItem(
        title: 'Registrar ao menos uma refeição',
        done: summary.food.meals > 0,
      ),
      _ChecklistItem(title: 'Beber água', done: habits.waterDone),
      _ChecklistItem(
        title: 'Plano de treino ativo',
        done: summary.workout.activeWorkouts > 0,
      ),
      _ChecklistItem(
        title: 'Peso atualizado',
        done: summary.progress.lastWeightKg != null,
      ),
      _ChecklistItem(title: 'Sono registrado', done: habits.sleepHours > 0),
    ];

    return EfSection(
      title: 'Checklist do dia',
      child: Card(
        child: Padding(
          padding: const EdgeInsets.all(AppTokens.space8),
          child: Column(
            children: [
              for (final item in items)
                ListTile(
                  leading: Icon(
                    item.done
                        ? Icons.check_circle
                        : Icons.radio_button_unchecked,
                    color: item.done
                        ? AppTokens.success
                        : Theme.of(context).colorScheme.outline,
                  ),
                  title: Text(item.title),
                  dense: true,
                ),
            ],
          ),
        ),
      ),
    );
  }
}

class _ChecklistItem {
  const _ChecklistItem({required this.title, required this.done});

  final String title;
  final bool done;
}

class _MacroGoals {
  const _MacroGoals({
    required this.protein,
    required this.carbs,
    required this.fat,
  });

  final double protein;
  final double carbs;
  final double fat;
}

_MacroGoals _macroGoals(double? calories) {
  final base = calories ?? 2000;
  return _MacroGoals(
    protein: (base * 0.24 / 4).clamp(55, 180).toDouble(),
    carbs: (base * 0.46 / 4).clamp(120, 360).toDouble(),
    fat: (base * 0.30 / 9).clamp(35, 120).toDouble(),
  );
}

String _greeting(String? name) {
  final nameParts = (name ?? '').trim().split(RegExp(r'\s+'));
  final firstName = nameParts.isEmpty ? '' : nameParts.first;
  final hour = DateTime.now().hour;
  final period = hour < 12
      ? 'Bom dia'
      : hour < 18
          ? 'Boa tarde'
          : 'Boa noite';

  return firstName.isEmpty ? '$period!' : '$period, $firstName!';
}

String _coachInsight(DashboardSummary summary, DailyHabits habits) {
  final food = summary.food;
  final calorieGoal =
      food.calorieGoal ?? summary.currentPlan?.dailyCalories.toDouble();

  if (food.meals == 0 && habits.waterCups < 3) {
    return 'Você ainda não registrou refeições e está com ${formatLitersFromMl(habits.waterMl)} L de água. Comece pelo próximo registro simples.';
  }

  if (calorieGoal != null && food.calories > calorieGoal * 1.1) {
    return 'Você já registrou ${formatDecimal(food.calories)} kcal. A próxima escolha pode priorizar proteína, fibras e água.';
  }

  if (food.proteinG < _macroGoals(calorieGoal).protein * 0.45 &&
      food.meals > 0) {
    return 'Você tem ${formatDecimal(food.proteinG)}g de proteína hoje. Uma opção com ovos, iogurte, frango ou leguminosas pode equilibrar o dia.';
  }

  if (habits.sleepHours > 0 && habits.sleepHours < 6) {
    return 'Seu sono registrado foi de ${formatDecimal(habits.sleepHours, decimals: 1)}h. Hoje vale reduzir intensidade e cuidar da recuperação.';
  }

  if (summary.workout.activeWorkouts == 0) {
    return 'Ainda não há treino ativo. Podemos gerar um plano leve e progressivo para esta semana.';
  }

  return 'Você registrou ${food.meals} refeições, ${formatLitersFromMl(habits.waterMl)} L de água e ${summary.workout.activeWorkouts} treino ativo. O dia está com boa estrutura.';
}

String _nextMealLabel(DateTime now) {
  if (now.hour < 10) {
    return 'Próxima refeição: café da manhã';
  }
  if (now.hour < 13) {
    return 'Próxima refeição: almoço';
  }
  if (now.hour < 17) {
    return 'Próxima refeição: lanche';
  }
  if (now.hour < 21) {
    return 'Próxima refeição: jantar';
  }
  return 'Próxima refeição: ceia';
}

String _goalLabel(String? value) {
  final label = _goalShort(value);
  return label == 'Saúde' ? 'Meta diária flexível' : 'Meta diária: $label';
}

String _goalShort(String? value) {
  return switch (value) {
    'EmagrecimentoSustentavel' => 'Equilíbrio',
    'GanhoMassa' => 'Massa',
    'Manutencao' => 'Manutenção',
    'Condicionamento' => 'Condicionamento',
    _ => 'Saúde',
  };
}
