import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/utils/formatters.dart';
import '../../../../design_system/tokens/app_tokens.dart';
import '../../../../design_system/widgets/ef_button.dart';
import '../../../../design_system/widgets/ef_empty_state.dart';
import '../../../../design_system/widgets/ef_feedback_banner.dart';
import '../../../../design_system/widgets/ef_metric_tile.dart';
import '../../../../design_system/widgets/ef_scaffold.dart';
import '../../../../design_system/widgets/ef_section.dart';
import '../../../dashboard/data/dashboard_repository.dart';
import '../../../dashboard/domain/dashboard_summary.dart';
import '../../../habits/data/daily_habits_controller.dart';
import '../../data/progress_repository.dart';

class ProgressPage extends ConsumerWidget {
  const ProgressPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final dashboard = ref.watch(dashboardSummaryProvider);
    final habits = ref.watch(dailyHabitsProvider);

    return EfScaffold(
      title: 'Evolução',
      subtitle: 'Tendência importa mais que um dia isolado.',
      currentIndex: 3,
      onRefresh: () async {
        ref.invalidate(dashboardSummaryProvider);
        await ref.read(dashboardSummaryProvider.future);
      },
      actions: [
        IconButton(
          tooltip: 'Registrar peso',
          onPressed: () => _showWeightDialog(context, ref),
          icon: const Icon(Icons.add),
        ),
      ],
      body: dashboard.when(
        data: (summary) {
          final progress = summary.progress;

          if (progress.lastWeightKg == null) {
            return Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                const EfEmptyState(
                  icon: Icons.show_chart,
                  title: 'Sem registros de evolução',
                  message:
                      'Registre seu peso quando fizer sentido. A tendência aparece aqui sem julgamentos.',
                ),
                EfButton(
                  label: 'Registrar peso',
                  icon: Icons.monitor_weight_outlined,
                  onPressed: () => _showWeightDialog(context, ref),
                ),
                const SizedBox(height: AppTokens.space20),
                _EvolutionTimeline(
                  summary: summary,
                  habitsWaterMl: habits.waterMl,
                ),
              ],
            );
          }

          final variation = progress.weightVariationKg;

          return Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              const EfFeedbackBanner(
                message:
                    'Vamos observar tendência e contexto. Pequenas variações fazem parte do processo.',
              ),
              const SizedBox(height: AppTokens.space20),
              Wrap(
                spacing: AppTokens.space12,
                runSpacing: AppTokens.space12,
                children: [
                  SizedBox(
                    width: 180,
                    child: EfMetricTile(
                      label: 'Último peso',
                      value:
                          '${formatDecimal(progress.lastWeightKg!, decimals: 1)} kg',
                      supportingText: progress.lastUpdate == null
                          ? null
                          : formatDate(progress.lastUpdate!),
                      icon: Icons.monitor_weight_outlined,
                    ),
                  ),
                  SizedBox(
                    width: 180,
                    child: EfMetricTile(
                      label: 'Variação',
                      value: variation == null
                          ? '--'
                          : '${formatDecimal(variation, decimals: 1)} kg',
                      supportingText: 'Período recente',
                      icon: Icons.trending_up,
                    ),
                  ),
                ],
              ),
              const SizedBox(height: AppTokens.space20),
              _EvolutionTimeline(
                summary: summary,
                habitsWaterMl: habits.waterMl,
              ),
              const SizedBox(height: AppTokens.space20),
              EfButton(
                label: 'Registrar peso',
                icon: Icons.monitor_weight_outlined,
                variant: EfButtonVariant.secondary,
                onPressed: () => _showWeightDialog(context, ref),
              ),
              const SizedBox(height: AppTokens.space20),
              Card(
                child: SizedBox(
                  height: 180,
                  child: CustomPaint(
                    painter: _TrendPainter(
                      color: Theme.of(context).colorScheme.primary,
                    ),
                    child: const Center(child: Text('Gráfico de tendência')),
                  ),
                ),
              ),
            ],
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
          title: 'Evolução indisponível agora',
          message:
              'Sem problemas. Vamos tentar novamente quando a conexão estiver estável.',
        ),
      ),
    );
  }

  Future<void> _showWeightDialog(BuildContext context, WidgetRef ref) async {
    final weightController = TextEditingController();
    final noteController = TextEditingController();

    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Registrar peso'),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            TextField(
              controller: weightController,
              keyboardType: TextInputType.number,
              decoration: const InputDecoration(labelText: 'Peso em kg'),
            ),
            const SizedBox(height: AppTokens.space12),
            TextField(
              controller: noteController,
              decoration:
                  const InputDecoration(labelText: 'Observação opcional'),
            ),
          ],
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(false),
            child: const Text('Cancelar'),
          ),
          FilledButton(
            onPressed: () => Navigator.of(context).pop(true),
            child: const Text('Salvar'),
          ),
        ],
      ),
    );

    final weight = double.tryParse(weightController.text.replaceAll(',', '.'));
    final note = noteController.text.trim();
    weightController.dispose();
    noteController.dispose();

    if (confirmed != true) {
      return;
    }

    if (weight == null || weight < 25 || weight > 350) {
      if (context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Informe um peso válido.')),
        );
      }
      return;
    }

    try {
      await ref.read(progressRepositoryProvider).saveWeight(
            weightKg: weight,
            note: note.isEmpty ? null : note,
          );
      ref.invalidate(dashboardSummaryProvider);
      if (context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Peso registrado. Vamos acompanhar a tendência.'),
          ),
        );
      }
    } catch (_) {
      if (context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text(
              'Não conseguimos salvar agora. Podemos tentar novamente.',
            ),
          ),
        );
      }
    }
  }
}

class _EvolutionTimeline extends StatelessWidget {
  const _EvolutionTimeline({
    required this.summary,
    required this.habitsWaterMl,
  });

  final DashboardSummary summary;
  final int habitsWaterMl;

  @override
  Widget build(BuildContext context) {
    final items = <_TimelineItem>[
      _TimelineItem(
        icon: Icons.monitor_weight_outlined,
        title: 'Peso',
        description: summary.progress.lastWeightKg == null
            ? 'Aguardando primeiro registro.'
            : '${formatDecimal(summary.progress.lastWeightKg!, decimals: 1)} kg registrados.',
      ),
      _TimelineItem(
        icon: Icons.fitness_center,
        title: 'Treinos',
        description:
            '${summary.workout.activeWorkouts} treino ativo, ${summary.workout.weeklyFrequency} dias planejados.',
      ),
      _TimelineItem(
        icon: Icons.photo_library_outlined,
        title: 'Fotos',
        description: 'Fotos de evolução podem ser conectadas ao próximo ciclo.',
      ),
      _TimelineItem(
        icon: Icons.straighten,
        title: 'Medidas',
        description: 'Medidas corporais entram junto da bioimpedância.',
      ),
      _TimelineItem(
        icon: Icons.workspace_premium_outlined,
        title: 'Conquistas',
        description: summary.food.meals > 0
            ? 'Registro alimentar feito hoje.'
            : 'Primeira refeição do dia em aberto.',
      ),
      _TimelineItem(
        icon: Icons.auto_awesome,
        title: 'IA',
        description:
            'Coach considera ${summary.food.meals} refeições e ${formatLitersFromMl(habitsWaterMl)} L de água hoje.',
      ),
    ];

    return EfSection(
      title: 'Timeline de evolução',
      child: Card(
        child: Padding(
          padding: const EdgeInsets.all(AppTokens.space16),
          child: Column(
            children: [
              for (final item in items)
                ListTile(
                  leading: Icon(item.icon),
                  title: Text(item.title),
                  subtitle: Text(item.description),
                ),
            ],
          ),
        ),
      ),
    );
  }
}

class _TimelineItem {
  const _TimelineItem({
    required this.icon,
    required this.title,
    required this.description,
  });

  final IconData icon;
  final String title;
  final String description;
}

class _TrendPainter extends CustomPainter {
  const _TrendPainter({required this.color});

  final Color color;

  @override
  void paint(Canvas canvas, Size size) {
    final paint = Paint()
      ..color = color
      ..strokeWidth = 3
      ..style = PaintingStyle.stroke
      ..strokeCap = StrokeCap.round;

    final path = Path()
      ..moveTo(16, size.height * 0.72)
      ..cubicTo(
        size.width * 0.28,
        size.height * 0.56,
        size.width * 0.38,
        size.height * 0.82,
        size.width * 0.52,
        size.height * 0.52,
      )
      ..cubicTo(
        size.width * 0.66,
        size.height * 0.26,
        size.width * 0.78,
        size.height * 0.54,
        size.width - 16,
        size.height * 0.36,
      );

    canvas.drawPath(path, paint);
  }

  @override
  bool shouldRepaint(covariant _TrendPainter oldDelegate) {
    return oldDelegate.color != color;
  }
}
