import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/errors/app_failure.dart';
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
import '../../data/plan_repository.dart';

class PlanPage extends ConsumerStatefulWidget {
  const PlanPage({super.key});

  @override
  ConsumerState<PlanPage> createState() => _PlanPageState();
}

class _PlanPageState extends ConsumerState<PlanPage> {
  bool _isGenerating = false;
  String? _replacingMealId;

  @override
  Widget build(BuildContext context) {
    final dashboard = ref.watch(dashboardSummaryProvider);

    return EfScaffold(
      title: 'Plano',
      subtitle: 'Metas seguras e explicadas.',
      currentIndex: 2,
      body: dashboard.when(
        data: (summary) {
          final plan = summary.currentPlan;
          if (plan == null) {
            return Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                EfEmptyState(
                  icon: Icons.fact_check_outlined,
                  title: 'Nenhum plano ativo ainda',
                  message: summary.profile == null
                      ? 'Vamos preencher o questionário inicial para gerar metas com mais segurança.'
                      : 'Seu questionário já está pronto. Podemos gerar um plano flexível agora.',
                  actionLabel:
                      summary.profile == null ? 'Ir ao questionário' : null,
                  onAction: summary.profile == null
                      ? () => context.go('/onboarding')
                      : null,
                ),
                if (summary.profile != null)
                  EfButton(
                    label: 'Gerar plano',
                    icon: Icons.auto_awesome,
                    isLoading: _isGenerating,
                    onPressed: _generatePlan,
                  ),
              ],
            );
          }

          return Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              EfFeedbackBanner(
                title: 'Explicacao do plano',
                message: plan.explanation,
              ),
              const SizedBox(height: AppTokens.space20),
              Wrap(
                spacing: AppTokens.space12,
                runSpacing: AppTokens.space12,
                children: [
                  SizedBox(
                    width: 180,
                    child: EfMetricTile(
                      label: 'Calorias',
                      value: '${formatDecimal(plan.dailyCalories)} kcal',
                      supportingText: 'Meta diária',
                      icon: Icons.local_fire_department_outlined,
                    ),
                  ),
                  SizedBox(
                    width: 180,
                    child: EfMetricTile(
                      label: 'Objetivo',
                      value: _goalLabel(plan.goal),
                      icon: Icons.flag_outlined,
                    ),
                  ),
                ],
              ),
              const SizedBox(height: AppTokens.space20),
              Card(
                child: Padding(
                  padding: const EdgeInsets.all(AppTokens.space16),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        'Diretrizes',
                        style: Theme.of(context).textTheme.titleMedium,
                      ),
                      const SizedBox(height: AppTokens.space8),
                      const Text(
                        'Use o plano como referência flexível. Preferências, rotina e progresso podem ser ajustados com segurança.',
                      ),
                    ],
                  ),
                ),
              ),
              if (plan.mealSuggestions.isNotEmpty) ...[
                const SizedBox(height: AppTokens.space20),
                EfSection(
                  title: 'Sugestões de refeições',
                  subtitle:
                      'Organizadas a partir das suas preferências registradas.',
                  child: Column(
                    children: [
                      for (final suggestion in plan.mealSuggestions) ...[
                        _MealSuggestionCard(
                          suggestion: suggestion,
                          isReplacing: _replacingMealId == suggestion.id,
                          onShowPhoto: () => _showSuggestedPhoto(suggestion),
                          onShowRecipe: () => _showQuickRecipe(suggestion),
                          onShowSubstitutions: () =>
                              _showSubstitutions(plan, suggestion),
                        ),
                        const SizedBox(height: AppTokens.space12),
                      ],
                    ],
                  ),
                ),
                const SizedBox(height: AppTokens.space8),
                _PlanSupportSection(plan: plan),
              ],
            ],
          );
        },
        loading: () => const Center(
          child: Padding(
            padding: EdgeInsets.all(AppTokens.space32),
            child: CircularProgressIndicator(),
          ),
        ),
        error: (error, stackTrace) => EfEmptyState(
          icon: Icons.cloud_off_outlined,
          title: 'Plano indisponível agora',
          message:
              'Sem problemas. Tente novamente quando a conexão estiver estável.',
          actionLabel: 'Tentar novamente',
          onAction: () => ref.invalidate(dashboardSummaryProvider),
        ),
      ),
    );
  }

  Future<void> _generatePlan() async {
    setState(() => _isGenerating = true);

    try {
      await ref.read(planRepositoryProvider).generate();
      ref.invalidate(dashboardSummaryProvider);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Plano gerado. Podemos ajustar quando precisar.'),
          ),
        );
      }
    } on AppFailure catch (failure) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(failure.message)),
        );
      }
    } finally {
      if (mounted) {
        setState(() => _isGenerating = false);
      }
    }
  }

  String _goalLabel(String value) {
    return switch (value) {
      'EmagrecimentoSustentavel' => 'Equilíbrio',
      'GanhoMassa' => 'Massa',
      'Manutencao' => 'Manutenção',
      'Condicionamento' => 'Condição',
      _ => 'Saúde',
    };
  }

  Future<void> _showSuggestedPhoto(DashboardMealSuggestion suggestion) async {
    final uri = Uri.tryParse(suggestion.imageUrl ?? '');
    final canShowImage = uri != null && uri.hasScheme && uri.scheme == 'https';

    await showDialog<void>(
      context: context,
      builder: (context) => AlertDialog(
        title: Text(suggestion.name),
        content: SizedBox(
          width: 360,
          child: canShowImage
              ? ClipRRect(
                  borderRadius: BorderRadius.circular(AppTokens.radius),
                  child: Image.network(
                    uri.toString(),
                    fit: BoxFit.cover,
                    loadingBuilder: (context, child, progress) {
                      if (progress == null) {
                        return child;
                      }
                      return const AspectRatio(
                        aspectRatio: 1,
                        child: Center(child: CircularProgressIndicator()),
                      );
                    },
                    errorBuilder: (context, error, stackTrace) =>
                        const _MealContentEmptyState(
                      icon: Icons.broken_image_outlined,
                      message:
                          'Não foi possível carregar a imagem desta sugestão.',
                    ),
                  ),
                )
              : const _MealContentEmptyState(
                  icon: Icons.image_not_supported_outlined,
                  message:
                      'Esta sugestão ainda não possui uma imagem autorizada.',
                ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(),
            child: const Text('Fechar'),
          ),
        ],
      ),
    );
  }

  Future<void> _showQuickRecipe(DashboardMealSuggestion suggestion) async {
    final recipe = suggestion.quickRecipe;
    await showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      builder: (context) => SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(AppTokens.space20),
          child: recipe == null
              ? const _MealContentEmptyState(
                  icon: Icons.menu_book_outlined,
                  message:
                      'Ainda não há receita estruturada para esta sugestão. Gere um novo plano quando a IA estiver disponível.',
                )
              : SingleChildScrollView(
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        recipe.name,
                        style: Theme.of(context).textTheme.titleLarge,
                      ),
                      const SizedBox(height: AppTokens.space8),
                      Text('Tempo estimado: ${recipe.preparationMinutes} min'),
                      const SizedBox(height: AppTokens.space16),
                      Text(
                        'Ingredientes',
                        style: Theme.of(context).textTheme.titleMedium,
                      ),
                      const SizedBox(height: AppTokens.space8),
                      for (final ingredient in recipe.ingredients)
                        Padding(
                          padding:
                              const EdgeInsets.only(bottom: AppTokens.space8),
                          child: Text(
                            '${ingredient.quantity} de ${ingredient.name}',
                          ),
                        ),
                      const SizedBox(height: AppTokens.space8),
                      Text(
                        'Modo de preparo',
                        style: Theme.of(context).textTheme.titleMedium,
                      ),
                      const SizedBox(height: AppTokens.space8),
                      for (final step in recipe.preparation.indexed)
                        Padding(
                          padding:
                              const EdgeInsets.only(bottom: AppTokens.space8),
                          child: Text('${step.$1 + 1}. ${step.$2}'),
                        ),
                      const SizedBox(height: AppTokens.space12),
                      const Text(
                        'Conteúdo educativo. Ajustes clínicos ou nutricionais exigem acompanhamento profissional.',
                      ),
                    ],
                  ),
                ),
        ),
      ),
    );
  }

  Future<void> _showSubstitutions(
    DashboardPlan plan,
    DashboardMealSuggestion suggestion,
  ) async {
    if (suggestion.id == null || suggestion.substitutions.isEmpty) {
      await showModalBottomSheet<void>(
        context: context,
        builder: (context) => const SafeArea(
          child: Padding(
            padding: EdgeInsets.all(AppTokens.space20),
            child: _MealContentEmptyState(
              icon: Icons.swap_horiz_outlined,
              message:
                  'Não há substituições estruturadas para esta refeição. Gere um novo plano para receber opções compatíveis com seu perfil.',
            ),
          ),
        ),
      );
      return;
    }

    final alternative = await showModalBottomSheet<DashboardMealSubstitution>(
      context: context,
      isScrollControlled: true,
      builder: (context) => SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(AppTokens.space20),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                'Substituições',
                style: Theme.of(context).textTheme.titleLarge,
              ),
              const SizedBox(height: AppTokens.space8),
              const Text(
                'Escolha uma opção. A alteração só será aplicada após sua confirmação.',
              ),
              const SizedBox(height: AppTokens.space12),
              Flexible(
                child: ListView.separated(
                  shrinkWrap: true,
                  itemCount: suggestion.substitutions.length,
                  separatorBuilder: (context, index) => const Divider(),
                  itemBuilder: (context, index) {
                    final item = suggestion.substitutions[index];
                    final ingredients = item.ingredients
                        .map(
                          (ingredient) =>
                              '${ingredient.quantity} ${ingredient.name}',
                        )
                        .join(', ');
                    return ListTile(
                      title: Text(item.name),
                      subtitle: Text(
                        '${item.description}${ingredients.isEmpty ? '' : '\n$ingredients'}'
                        '${item.calories == null ? '' : '\nEstimativa: ${item.calories} kcal'}',
                      ),
                      isThreeLine:
                          ingredients.isNotEmpty || item.calories != null,
                      trailing: const Icon(Icons.chevron_right),
                      onTap: () => Navigator.of(context).pop(item),
                    );
                  },
                ),
              ),
            ],
          ),
        ),
      ),
    );

    if (alternative == null || !mounted) {
      return;
    }

    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Aplicar substituição?'),
        content: Text(
          'Trocar "${suggestion.name}" por "${alternative.name}" neste plano?',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(false),
            child: const Text('Cancelar'),
          ),
          FilledButton(
            onPressed: () => Navigator.of(context).pop(true),
            child: const Text('Aplicar'),
          ),
        ],
      ),
    );

    if (confirmed != true || !mounted) {
      return;
    }

    setState(() => _replacingMealId = suggestion.id);
    try {
      await ref.read(planRepositoryProvider).replaceMeal(
            planId: plan.id,
            mealId: suggestion.id!,
            alternativeIndex: alternative.index,
          );
      ref.invalidate(dashboardSummaryProvider);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Substituição aplicada ao seu plano.')),
        );
      }
    } on AppFailure catch (failure) {
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text(failure.message)));
      }
    } finally {
      if (mounted) {
        setState(() => _replacingMealId = null);
      }
    }
  }
}

class _MealSuggestionCard extends StatelessWidget {
  const _MealSuggestionCard({
    required this.suggestion,
    required this.isReplacing,
    required this.onShowPhoto,
    required this.onShowSubstitutions,
    required this.onShowRecipe,
  });

  final DashboardMealSuggestion suggestion;
  final bool isReplacing;
  final VoidCallback onShowPhoto;
  final VoidCallback onShowSubstitutions;
  final VoidCallback onShowRecipe;

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
                DecoratedBox(
                  decoration: BoxDecoration(
                    color: Theme.of(context).colorScheme.primaryContainer,
                    borderRadius: BorderRadius.circular(AppTokens.radius),
                  ),
                  child: const Padding(
                    padding: EdgeInsets.all(AppTokens.space8),
                    child: Icon(Icons.image_outlined),
                  ),
                ),
                const SizedBox(width: AppTokens.space12),
                Expanded(
                  child: Text(
                    suggestion.name,
                    style: Theme.of(context).textTheme.titleMedium,
                  ),
                ),
                Text('${suggestion.calories} kcal'),
              ],
            ),
            const SizedBox(height: AppTokens.space8),
            Text(suggestion.description),
            const SizedBox(height: AppTokens.space12),
            Wrap(
              spacing: AppTokens.space8,
              runSpacing: AppTokens.space8,
              children: [
                ActionChip(
                  avatar: const Icon(Icons.photo_outlined, size: 18),
                  label: const Text('Foto sugerida'),
                  onPressed: onShowPhoto,
                ),
                ActionChip(
                  avatar: isReplacing
                      ? const SizedBox.square(
                          dimension: 18,
                          child: CircularProgressIndicator(strokeWidth: 2),
                        )
                      : const Icon(Icons.swap_horiz_outlined, size: 18),
                  label: const Text('Substituições'),
                  onPressed: isReplacing ? null : onShowSubstitutions,
                ),
                ActionChip(
                  avatar: const Icon(Icons.menu_book_outlined, size: 18),
                  label: const Text('Receita rápida'),
                  onPressed: onShowRecipe,
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

class _MealContentEmptyState extends StatelessWidget {
  const _MealContentEmptyState({required this.icon, required this.message});

  final IconData icon;
  final String message;

  @override
  Widget build(BuildContext context) {
    return Column(
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(icon, size: 36),
        const SizedBox(height: AppTokens.space12),
        Text(message, textAlign: TextAlign.center),
      ],
    );
  }
}

class _PlanSupportSection extends StatelessWidget {
  const _PlanSupportSection({required this.plan});

  final DashboardPlan plan;

  @override
  Widget build(BuildContext context) {
    final shoppingItems = _shoppingList(plan);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        EfSection(
          title: 'Substituições equilibradas',
          child: Card(
            child: Padding(
              padding: const EdgeInsets.all(AppTokens.space16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: const [
                  Text('Proteína: ovos, frango, peixe, tofu ou leguminosas.'),
                  SizedBox(height: AppTokens.space8),
                  Text(
                    'Carboidrato: arroz, batata, mandioca, aveia ou frutas.',
                  ),
                  SizedBox(height: AppTokens.space8),
                  Text('Gorduras: azeite, castanhas, abacate ou sementes.'),
                ],
              ),
            ),
          ),
        ),
        const SizedBox(height: AppTokens.space20),
        EfSection(
          title: 'Receitas simples',
          child: Card(
            child: Padding(
              padding: const EdgeInsets.all(AppTokens.space16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: plan.mealSuggestions
                    .take(3)
                    .map(
                      (item) => Padding(
                        padding: const EdgeInsets.only(
                          bottom: AppTokens.space8,
                        ),
                        child: Text(
                          '${item.name}: monte uma porção com proteína, fibra e carboidrato conforme sua rotina.',
                        ),
                      ),
                    )
                    .toList(growable: false),
              ),
            ),
          ),
        ),
        const SizedBox(height: AppTokens.space20),
        EfSection(
          title: 'Lista de compras',
          subtitle: 'Base semanal sugerida pelo plano atual.',
          child: Card(
            child: Padding(
              padding: const EdgeInsets.all(AppTokens.space16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  for (final item in shoppingItems)
                    Padding(
                      padding: const EdgeInsets.only(bottom: AppTokens.space8),
                      child: Row(
                        children: [
                          const Icon(Icons.check_circle_outline, size: 18),
                          const SizedBox(width: AppTokens.space8),
                          Expanded(child: Text(item)),
                        ],
                      ),
                    ),
                ],
              ),
            ),
          ),
        ),
      ],
    );
  }

  List<String> _shoppingList(DashboardPlan plan) {
    final names = plan.mealSuggestions.map((item) => item.name).toList();
    return <String>{
      ...names,
      'Verduras e legumes variados',
      'Frutas para lanches',
      'Fonte de proteína da semana',
      'Água e itens de preparo simples',
    }.toList(growable: false);
  }
}
