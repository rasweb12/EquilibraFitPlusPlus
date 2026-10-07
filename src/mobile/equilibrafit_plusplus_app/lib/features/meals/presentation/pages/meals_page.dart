import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/utils/formatters.dart';
import '../../../../core/utils/portuguese_text.dart';
import '../../../../core/utils/portuguese_text_input_formatter.dart';
import '../../../../design_system/tokens/app_tokens.dart';
import '../../../../design_system/widgets/ef_button.dart';
import '../../../../design_system/widgets/ef_empty_state.dart';
import '../../../../design_system/widgets/ef_feedback_banner.dart';
import '../../../../design_system/widgets/ef_scaffold.dart';
import '../../../../design_system/widgets/ef_section.dart';
import '../../../dashboard/data/dashboard_repository.dart';
import '../../data/meal_repository.dart';
import '../../domain/meal_entry.dart';

class MealsPage extends ConsumerStatefulWidget {
  const MealsPage({super.key});

  @override
  ConsumerState<MealsPage> createState() => _MealsPageState();
}

class _MealsPageState extends ConsumerState<MealsPage> {
  final _searchController = TextEditingController();
  var _query = '';

  @override
  void dispose() {
    _searchController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final meals = ref.watch(mealsProvider);

    return EfScaffold(
      title: 'Refeições',
      subtitle: 'Registre, revise e ajuste quando precisar.',
      currentIndex: 1,
      onRefresh: () async {
        ref.invalidate(mealsProvider);
        ref.invalidate(dashboardSummaryProvider);
        await ref.read(mealsProvider.future);
      },
      actions: [
        IconButton(
          tooltip: 'Atualizar',
          onPressed: () => ref.invalidate(mealsProvider),
          icon: const Icon(Icons.refresh),
        ),
      ],
      body: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Wrap(
            spacing: AppTokens.space12,
            runSpacing: AppTokens.space12,
            children: [
              EfButton(
                label: 'Registrar',
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
                label: 'Texto/IA',
                icon: Icons.auto_awesome,
                variant: EfButtonVariant.secondary,
                onPressed: () => context.go('/meals/new'),
              ),
              EfButton(
                label: 'Código',
                icon: Icons.qr_code_scanner,
                variant: EfButtonVariant.secondary,
                onPressed: _showBarcodeDialog,
              ),
            ],
          ),
          const SizedBox(height: AppTokens.space16),
          TextField(
            controller: _searchController,
            textInputAction: TextInputAction.search,
            inputFormatters: const [PortugueseTextInputFormatter()],
            decoration: const InputDecoration(
              labelText: 'Pesquisar refeições',
              prefixIcon: Icon(Icons.search),
            ),
            onChanged: (value) => setState(() => _query = value.trim()),
          ),
          const SizedBox(height: AppTokens.space20),
          meals.when(
            data: (items) {
              final visibleItems = _filter(items, _query);
              if (items.isEmpty) {
                return EfEmptyState(
                  icon: Icons.restaurant_menu,
                  title: 'Sem refeições registradas hoje',
                  message:
                      'Sem pressa. Registre quando fizer sentido para sua rotina.',
                  actionLabel: 'Adicionar refeição',
                  onAction: () => context.go('/meals/new'),
                );
              }

              if (visibleItems.isEmpty) {
                return const EfEmptyState(
                  icon: Icons.search_off,
                  title: 'Nenhuma refeição encontrada',
                  message: 'Podemos ajustar a busca ou registrar uma nova.',
                );
              }

              return EfSection(
                title: 'Registros de hoje',
                child: Column(
                  children: [
                    for (final meal in visibleItems) ...[
                      _MealCard(
                        meal: meal,
                        onEdit: () => context.go('/meals/edit/${meal.id}'),
                        onDelete: () => _deleteMeal(context, meal),
                      ),
                      const SizedBox(height: AppTokens.space12),
                    ],
                  ],
                ),
              );
            },
            loading: () => const Center(
              child: Padding(
                padding: EdgeInsets.all(AppTokens.space32),
                child: CircularProgressIndicator(),
              ),
            ),
            error: (error, stackTrace) => const EfFeedbackBanner(
              tone: EfFeedbackTone.warning,
              title: 'Não conseguimos carregar agora',
              message:
                  'Sem problemas. O registro manual continua disponível para quando você quiser.',
            ),
          ),
        ],
      ),
    );
  }

  List<MealEntry> _filter(List<MealEntry> items, String query) {
    if (query.isEmpty) {
      return items;
    }

    final normalized = PortugueseText.normalizeForSearch(query);
    return items.where((meal) {
      final type = PortugueseText.normalizeForSearch(_mealType(meal.type));
      final itemNames = meal.items.map((item) => item.name).join(' ');
      return type.contains(normalized) ||
          PortugueseText.normalizeForSearch(itemNames).contains(normalized);
    }).toList(growable: false);
  }

  Future<void> _showBarcodeDialog() async {
    final controller = TextEditingController();
    final code = await showDialog<String>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Código de barras'),
        content: TextField(
          controller: controller,
          keyboardType: TextInputType.number,
          decoration: const InputDecoration(
            labelText: 'Informe o código do produto',
            helperText: 'Use o número do rótulo para buscar com a IA.',
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(),
            child: const Text('Cancelar'),
          ),
          FilledButton(
            onPressed: () => Navigator.of(context).pop(controller.text.trim()),
            child: const Text('Continuar'),
          ),
        ],
      ),
    );
    controller.dispose();

    if (!mounted || code == null || code.isEmpty) {
      return;
    }

    context.go(
      Uri(
        path: '/meals/new',
        queryParameters: <String, String>{
          'descricao': 'Código de barras $code',
        },
      ).toString(),
    );
  }

  Future<void> _deleteMeal(BuildContext context, MealEntry meal) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Excluir refeição?'),
        content: const Text(
          'Sem problemas. Esta ação remove o registro selecionado do dia.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(false),
            child: const Text('Cancelar'),
          ),
          FilledButton(
            onPressed: () => Navigator.of(context).pop(true),
            child: const Text('Excluir'),
          ),
        ],
      ),
    );

    if (confirmed != true) {
      return;
    }

    try {
      await ref.read(mealRepositoryProvider).delete(meal.id);
      ref.invalidate(mealsProvider);
      ref.invalidate(dashboardSummaryProvider);
      if (context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Refeição removida. Podemos ajustar.')),
        );
      }
    } catch (_) {
      if (context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text(
              'Não conseguimos remover agora. Podemos tentar novamente.',
            ),
          ),
        );
      }
    }
  }
}

class _MealCard extends StatelessWidget {
  const _MealCard({
    required this.meal,
    required this.onEdit,
    required this.onDelete,
  });

  final MealEntry meal;
  final VoidCallback onEdit;
  final VoidCallback onDelete;

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
                Expanded(
                  child: Text(
                    _mealType(meal.type),
                    style: Theme.of(context).textTheme.titleMedium,
                  ),
                ),
                Text(formatDate(meal.dateTime)),
                IconButton(
                  tooltip: 'Editar',
                  onPressed: onEdit,
                  icon: const Icon(Icons.edit_outlined),
                ),
                IconButton(
                  tooltip: 'Excluir',
                  onPressed: onDelete,
                  icon: const Icon(Icons.delete_outline),
                ),
              ],
            ),
            const SizedBox(height: AppTokens.space8),
            Text(
              '${formatDecimal(meal.calories)} kcal, '
              '${formatDecimal(meal.proteinG)}g proteína',
            ),
            const SizedBox(height: AppTokens.space12),
            for (final item in meal.items)
              Padding(
                padding: const EdgeInsets.only(bottom: AppTokens.space4),
                child: Text(
                  '${item.name} - ${formatDecimal(item.quantity)} ${item.unit}',
                ),
              ),
            if (meal.message.isNotEmpty) ...[
              const SizedBox(height: AppTokens.space8),
              Text(meal.message, style: Theme.of(context).textTheme.bodySmall),
            ],
          ],
        ),
      ),
    );
  }
}

String _mealType(String value) {
  return switch (value) {
    'CafeManha' => 'Café da manhã',
    'Almoco' => 'Almoço',
    'Jantar' => 'Jantar',
    'Lanche' => 'Lanche',
    'Ceia' => 'Ceia',
    _ => 'Refeição',
  };
}
