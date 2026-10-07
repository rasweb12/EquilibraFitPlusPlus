import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/errors/app_failure.dart';
import '../../../../design_system/tokens/app_tokens.dart';
import '../../../../design_system/widgets/ef_button.dart';
import '../../../../design_system/widgets/ef_feedback_banner.dart';
import '../../../../design_system/widgets/ef_scaffold.dart';
import '../../../../design_system/widgets/ef_text_field.dart';
import '../../../dashboard/data/dashboard_repository.dart';
import '../../data/meal_repository.dart';
import '../../domain/meal_entry.dart';

class MealEditorPage extends ConsumerStatefulWidget {
  const MealEditorPage({super.key, this.mealId, this.initialDescription});

  final String? mealId;
  final String? initialDescription;

  @override
  ConsumerState<MealEditorPage> createState() => _MealEditorPageState();
}

class _MealEditorPageState extends ConsumerState<MealEditorPage> {
  final _formKey = GlobalKey<FormState>();
  final _estimateController = TextEditingController();
  final _items = <_FoodItemDraft>[_FoodItemDraft()];
  var _mealType = 'Almoco';
  DateTime? _mealDateTime;
  var _isLoading = false;
  var _isEstimating = false;
  var _isLoadingExisting = false;

  bool get _isEditing => widget.mealId != null;

  @override
  void initState() {
    super.initState();
    final initialDescription = widget.initialDescription?.trim();
    if (initialDescription != null && initialDescription.isNotEmpty) {
      _estimateController.text = initialDescription;
    }
    if (_isEditing) {
      _loadExistingMeal();
    }
  }

  @override
  void dispose() {
    _estimateController.dispose();
    for (final item in _items) {
      item.dispose();
    }
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return EfScaffold(
      title: _isEditing ? 'Editar refeição' : 'Nova refeição',
      subtitle: 'Você pode ajustar tudo antes de salvar.',
      actions: [
        TextButton(
          onPressed: _isLoading ? null : () => context.go('/meals'),
          child: const Text('Cancelar'),
        ),
      ],
      body: _isLoadingExisting
          ? const Center(
              child: Padding(
                padding: EdgeInsets.all(AppTokens.space32),
                child: CircularProgressIndicator(),
              ),
            )
          : Form(
              key: _formKey,
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  const EfFeedbackBanner(
                    message:
                        'Registre de forma aproximada quando necessário. O importante é criar consistência.',
                  ),
                  const SizedBox(height: AppTokens.space16),
                  DropdownButtonFormField<String>(
                    initialValue: _mealType,
                    decoration: const InputDecoration(
                      labelText: 'Tipo de refeição',
                    ),
                    items: const [
                      DropdownMenuItem(
                        value: 'CafeManha',
                        child: Text('Café da manhã'),
                      ),
                      DropdownMenuItem(value: 'Almoco', child: Text('Almoço')),
                      DropdownMenuItem(value: 'Jantar', child: Text('Jantar')),
                      DropdownMenuItem(value: 'Lanche', child: Text('Lanche')),
                      DropdownMenuItem(value: 'Ceia', child: Text('Ceia')),
                      DropdownMenuItem(value: 'Outro', child: Text('Outro')),
                    ],
                    onChanged: (value) {
                      if (value != null) {
                        setState(() => _mealType = value);
                      }
                    },
                  ),
                  const SizedBox(height: AppTokens.space20),
                  _AiEstimateCard(
                    controller: _estimateController,
                    isLoading: _isEstimating,
                    onEstimate: _estimateFromText,
                  ),
                  const SizedBox(height: AppTokens.space20),
                  for (var index = 0; index < _items.length; index++) ...[
                    _FoodItemForm(
                      index: index,
                      draft: _items[index],
                      canRemove: _items.length > 1,
                      onRemove: () => _removeItem(index),
                    ),
                    const SizedBox(height: AppTokens.space12),
                  ],
                  EfButton(
                    label: 'Adicionar item',
                    icon: Icons.add,
                    variant: EfButtonVariant.secondary,
                    onPressed: _addItem,
                  ),
                  const SizedBox(height: AppTokens.space20),
                  EfButton(
                    label: _isEditing ? 'Salvar alterações' : 'Salvar refeição',
                    icon: Icons.check,
                    isLoading: _isLoading,
                    onPressed: _submit,
                  ),
                ],
              ),
            ),
    );
  }

  Future<void> _loadExistingMeal() async {
    setState(() => _isLoadingExisting = true);

    try {
      final meal =
          await ref.read(mealRepositoryProvider).getById(widget.mealId!);
      _replaceItems(meal.items.map(_FoodItemDraft.fromMealItem).toList());
      if (mounted) {
        setState(() {
          _mealType = meal.type;
          _mealDateTime = meal.dateTime;
          _isLoadingExisting = false;
        });
      }
    } on AppFailure catch (failure) {
      if (mounted) {
        setState(() => _isLoadingExisting = false);
        _showMessage(failure.message);
      }
    }
  }

  void _addItem() {
    setState(() => _items.add(_FoodItemDraft()));
  }

  void _removeItem(int index) {
    final item = _items.removeAt(index);
    item.dispose();
    setState(() {});
  }

  Future<void> _estimateFromText() async {
    final description = _estimateController.text.trim();
    if (description.length < 3) {
      _showMessage('Descreva a refeição com algumas palavras.');
      return;
    }

    setState(() => _isEstimating = true);

    try {
      final estimation = await ref.read(mealRepositoryProvider).estimateText(
            description: description,
            type: _mealType,
          );

      if (estimation.items.isEmpty) {
        _showMessage(
          estimation.message.isEmpty
              ? 'Não encontramos itens com segurança. Podemos preencher manualmente.'
              : estimation.message,
        );
        return;
      }

      _replaceItems(estimation.items.map(_FoodItemDraft.fromMealItem).toList());
      if (mounted) {
        _showMessage(estimation.message);
      }
    } on AppFailure catch (failure) {
      _showMessage(failure.message);
    } finally {
      if (mounted) {
        setState(() => _isEstimating = false);
      }
    }
  }

  void _replaceItems(List<_FoodItemDraft> drafts) {
    for (final item in _items) {
      item.dispose();
    }

    _items
      ..clear()
      ..addAll(drafts.isEmpty ? [_FoodItemDraft()] : drafts);

    if (mounted) {
      setState(() {});
    }
  }

  Future<void> _submit() async {
    if (!(_formKey.currentState?.validate() ?? false)) {
      return;
    }

    setState(() => _isLoading = true);

    try {
      final repository = ref.read(mealRepositoryProvider);
      final items = _items.map((item) => item.toMealItem()).toList();
      if (_isEditing) {
        await repository.update(
          id: widget.mealId!,
          type: _mealType,
          dateTime: _mealDateTime ?? DateTime.now(),
          items: items,
        );
      } else {
        await repository.create(type: _mealType, items: items);
      }

      ref.invalidate(mealsProvider);
      ref.invalidate(dashboardSummaryProvider);

      if (mounted) {
        context.go('/meals');
      }
    } on AppFailure catch (failure) {
      _showMessage(failure.message);
    } finally {
      if (mounted) {
        setState(() => _isLoading = false);
      }
    }
  }

  void _showMessage(String message) {
    ScaffoldMessenger.of(context)
        .showSnackBar(SnackBar(content: Text(message)));
  }
}

class _AiEstimateCard extends StatelessWidget {
  const _AiEstimateCard({
    required this.controller,
    required this.isLoading,
    required this.onEstimate,
  });

  final TextEditingController controller;
  final bool isLoading;
  final VoidCallback onEstimate;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(AppTokens.space16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(
              'Calcular com IA',
              style: Theme.of(context).textTheme.titleMedium,
            ),
            const SizedBox(height: AppTokens.space12),
            EfTextField(
              label: 'Descreva a refeição',
              hint: 'Ex.: pão com ovo, arroz, feijão e frango',
              controller: controller,
              maxLines: 2,
              textCapitalization: TextCapitalization.sentences,
            ),
            const SizedBox(height: AppTokens.space12),
            EfButton(
              label: 'Calcular itens',
              icon: Icons.auto_awesome,
              variant: EfButtonVariant.secondary,
              isLoading: isLoading,
              onPressed: onEstimate,
            ),
          ],
        ),
      ),
    );
  }
}

class _FoodItemForm extends StatelessWidget {
  const _FoodItemForm({
    required this.index,
    required this.draft,
    required this.canRemove,
    required this.onRemove,
  });

  final int index;
  final _FoodItemDraft draft;
  final bool canRemove;
  final VoidCallback onRemove;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(AppTokens.space16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Row(
              children: [
                Expanded(
                  child: Text(
                    'Item ${index + 1}',
                    style: Theme.of(context).textTheme.titleMedium,
                  ),
                ),
                if (canRemove)
                  IconButton(
                    tooltip: 'Remover item',
                    onPressed: onRemove,
                    icon: const Icon(Icons.delete_outline),
                  ),
              ],
            ),
            const SizedBox(height: AppTokens.space12),
            EfTextField(
              label: 'Alimento',
              controller: draft.name,
              keyboardType: TextInputType.text,
              textCapitalization: TextCapitalization.sentences,
              validator: (value) =>
                  (value?.trim() ?? '').isEmpty ? 'Informe o alimento.' : null,
            ),
            const SizedBox(height: AppTokens.space12),
            Row(
              children: [
                Expanded(
                  child: EfTextField(
                    label: 'Quantidade',
                    controller: draft.quantity,
                    keyboardType: TextInputType.number,
                    validator: _positiveNumber,
                  ),
                ),
                const SizedBox(width: AppTokens.space12),
                Expanded(
                  child: EfTextField(
                    label: 'Unidade',
                    controller: draft.unit,
                    textCapitalization: TextCapitalization.none,
                    validator: (value) => (value?.trim() ?? '').isEmpty
                        ? 'Informe a unidade.'
                        : null,
                  ),
                ),
              ],
            ),
            const SizedBox(height: AppTokens.space12),
            Row(
              children: [
                Expanded(
                  child: EfTextField(
                    label: 'Kcal',
                    controller: draft.calories,
                    keyboardType: TextInputType.number,
                    validator: _zeroOrPositiveNumber,
                  ),
                ),
                const SizedBox(width: AppTokens.space12),
                Expanded(
                  child: EfTextField(
                    label: 'Proteína',
                    controller: draft.protein,
                    keyboardType: TextInputType.number,
                    validator: _zeroOrPositiveNumber,
                  ),
                ),
              ],
            ),
            const SizedBox(height: AppTokens.space12),
            Row(
              children: [
                Expanded(
                  child: EfTextField(
                    label: 'Carboidrato',
                    controller: draft.carbs,
                    keyboardType: TextInputType.number,
                    validator: _zeroOrPositiveNumber,
                  ),
                ),
                const SizedBox(width: AppTokens.space12),
                Expanded(
                  child: EfTextField(
                    label: 'Gordura',
                    controller: draft.fat,
                    keyboardType: TextInputType.number,
                    validator: _zeroOrPositiveNumber,
                  ),
                ),
              ],
            ),
            const SizedBox(height: AppTokens.space12),
            EfTextField(
              label: 'Fonte nutricional',
              hint: 'Opcional',
              controller: draft.source,
              textCapitalization: TextCapitalization.sentences,
            ),
          ],
        ),
      ),
    );
  }

  String? _positiveNumber(String? value) {
    final number = double.tryParse((value ?? '').replaceAll(',', '.'));
    if (number == null || number <= 0) {
      return 'Informe um valor maior que zero.';
    }

    return null;
  }

  String? _zeroOrPositiveNumber(String? value) {
    final number = double.tryParse((value ?? '').replaceAll(',', '.'));
    if (number == null || number < 0) {
      return 'Informe um valor válido.';
    }

    return null;
  }
}

class _FoodItemDraft {
  _FoodItemDraft()
      : name = TextEditingController(),
        quantity = TextEditingController(),
        unit = TextEditingController(text: 'g'),
        calories = TextEditingController(),
        protein = TextEditingController(),
        carbs = TextEditingController(),
        fat = TextEditingController(),
        source = TextEditingController();

  _FoodItemDraft.fromMealItem(MealItem item)
      : name = TextEditingController(text: item.name),
        quantity = TextEditingController(text: _formatNumber(item.quantity)),
        unit = TextEditingController(text: item.unit),
        calories = TextEditingController(text: _formatNumber(item.calories)),
        protein = TextEditingController(text: _formatNumber(item.proteinG)),
        carbs = TextEditingController(text: _formatNumber(item.carbsG)),
        fat = TextEditingController(text: _formatNumber(item.fatG)),
        source = TextEditingController(text: item.source ?? '');

  final TextEditingController name;
  final TextEditingController quantity;
  final TextEditingController unit;
  final TextEditingController calories;
  final TextEditingController protein;
  final TextEditingController carbs;
  final TextEditingController fat;
  final TextEditingController source;

  MealItem toMealItem() {
    return MealItem(
      name: name.text.trim(),
      quantity: _number(quantity.text),
      unit: unit.text.trim(),
      calories: _number(calories.text),
      proteinG: _number(protein.text),
      carbsG: _number(carbs.text),
      fatG: _number(fat.text),
      source: source.text.trim().isEmpty ? null : source.text.trim(),
    );
  }

  void dispose() {
    name.dispose();
    quantity.dispose();
    unit.dispose();
    calories.dispose();
    protein.dispose();
    carbs.dispose();
    fat.dispose();
    source.dispose();
  }

  double _number(String value) {
    return double.tryParse(value.replaceAll(',', '.')) ?? 0;
  }

  static String _formatNumber(double value) {
    if (value == value.roundToDouble()) {
      return value.toStringAsFixed(0);
    }

    return value.toStringAsFixed(1);
  }
}
