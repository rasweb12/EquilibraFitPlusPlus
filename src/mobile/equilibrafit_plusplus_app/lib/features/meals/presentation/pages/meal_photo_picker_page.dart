import 'dart:convert';
import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:image_picker/image_picker.dart';

import '../../../../core/errors/app_failure.dart';
import '../../../../core/utils/formatters.dart';
import '../../../../design_system/tokens/app_tokens.dart';
import '../../../../design_system/widgets/ef_button.dart';
import '../../../../design_system/widgets/ef_feedback_banner.dart';
import '../../../../design_system/widgets/ef_scaffold.dart';
import '../../../dashboard/data/dashboard_repository.dart';
import '../../data/meal_repository.dart';
import '../../domain/meal_entry.dart';

class MealPhotoPickerPage extends ConsumerStatefulWidget {
  const MealPhotoPickerPage({super.key});

  @override
  ConsumerState<MealPhotoPickerPage> createState() =>
      _MealPhotoPickerPageState();
}

class _MealPhotoPickerPageState extends ConsumerState<MealPhotoPickerPage> {
  final _picker = ImagePicker();
  var _mealType = 'Almoco';
  MealEstimation? _estimation;
  Uint8List? _imageBytes;
  var _isRecognizing = false;
  var _isSaving = false;

  @override
  Widget build(BuildContext context) {
    return EfScaffold(
      title: 'Foto da refeição',
      subtitle: 'A estimativa sempre deve ser revisada.',
      actions: [
        TextButton(
          onPressed:
              _isRecognizing || _isSaving ? null : () => context.go('/meals'),
          child: const Text('Cancelar'),
        ),
      ],
      body: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          const EfFeedbackBanner(
            tone: EfFeedbackTone.warning,
            title: 'Confirmação obrigatória',
            message:
                'Nada será salvo automaticamente. Você revisa os itens antes de confirmar.',
          ),
          const SizedBox(height: AppTokens.space16),
          DropdownButtonFormField<String>(
            initialValue: _mealType,
            decoration: const InputDecoration(labelText: 'Tipo de refeição'),
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
          if (_imageBytes != null)
            ClipRRect(
              borderRadius: BorderRadius.circular(AppTokens.radius),
              child: Image.memory(
                _imageBytes!,
                height: 220,
                fit: BoxFit.cover,
              ),
            )
          else
            Card(
              child: Padding(
                padding: const EdgeInsets.all(AppTokens.space24),
                child: Column(
                  children: [
                    Icon(
                      Icons.photo_library_outlined,
                      size: 56,
                      color: Theme.of(context).colorScheme.primary,
                    ),
                    const SizedBox(height: AppTokens.space12),
                    Text(
                      'Escolha uma foto do prato',
                      style: Theme.of(context).textTheme.titleMedium,
                    ),
                  ],
                ),
              ),
            ),
          const SizedBox(height: AppTokens.space16),
          EfButton(
            label: 'Carregar da galeria',
            icon: Icons.photo_library_outlined,
            variant: EfButtonVariant.secondary,
            isLoading: _isRecognizing,
            onPressed: _pickFromGallery,
          ),
          if (_estimation != null && _estimation!.items.isEmpty) ...[
            const SizedBox(height: AppTokens.space20),
            EfFeedbackBanner(
              tone: EfFeedbackTone.warning,
              title: 'Foto não interpretada',
              message: _estimation!.message,
            ),
            const SizedBox(height: AppTokens.space12),
            EfButton(
              label: 'Registrar manualmente',
              icon: Icons.edit_outlined,
              variant: EfButtonVariant.secondary,
              onPressed: () => context.go('/meals/new'),
            ),
          ] else if (_estimation != null) ...[
            const SizedBox(height: AppTokens.space20),
            Text(
              'Itens estimados',
              style: Theme.of(context).textTheme.titleMedium,
            ),
            const SizedBox(height: AppTokens.space12),
            for (final item in _estimation!.items) ...[
              Card(
                child: Padding(
                  padding: const EdgeInsets.all(AppTokens.space16),
                  child: Row(
                    children: [
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              item.name,
                              style: Theme.of(context).textTheme.titleMedium,
                            ),
                            const SizedBox(height: AppTokens.space4),
                            Text(
                              '${formatDecimal(item.quantity)} ${item.unit} - ${formatDecimal(item.calories)} kcal',
                            ),
                          ],
                        ),
                      ),
                      Text('${formatDecimal(item.proteinG)}g prot.'),
                    ],
                  ),
                ),
              ),
              const SizedBox(height: AppTokens.space8),
            ],
            const SizedBox(height: AppTokens.space12),
            EfButton(
              label: 'Salvar refeição',
              icon: Icons.check,
              isLoading: _isSaving,
              onPressed: _saveRecognizedMeal,
            ),
            const SizedBox(height: AppTokens.space8),
            EfButton(
              label: 'Editar manualmente',
              icon: Icons.edit_outlined,
              variant: EfButtonVariant.secondary,
              onPressed: () => context.go('/meals/new'),
            ),
          ],
        ],
      ),
    );
  }

  Future<void> _pickFromGallery() async {
    setState(() => _isRecognizing = true);

    try {
      final file = await _picker.pickImage(
        source: ImageSource.gallery,
        imageQuality: 85,
        maxWidth: 1600,
      );

      if (file == null) {
        return;
      }

      final bytes = await file.readAsBytes();
      final estimation = await ref.read(mealRepositoryProvider).recognizeImage(
            imageBase64: base64Encode(bytes),
            type: _mealType,
          );

      if (mounted) {
        setState(() {
          _imageBytes = bytes;
          _estimation = estimation;
        });
        _showMessage(estimation.message);
      }
    } on AppFailure catch (failure) {
      _showMessage(failure.message);
    } finally {
      if (mounted) {
        setState(() => _isRecognizing = false);
      }
    }
  }

  Future<void> _saveRecognizedMeal() async {
    final estimation = _estimation;
    if (estimation == null || estimation.items.isEmpty) {
      _showMessage('Carregue uma foto para revisar os itens antes de salvar.');
      return;
    }

    setState(() => _isSaving = true);

    try {
      await ref.read(mealRepositoryProvider).create(
            type: _mealType,
            items: estimation.items,
          );
      ref.invalidate(mealsProvider);
      ref.invalidate(dashboardSummaryProvider);
      if (mounted) {
        context.go('/meals');
      }
    } on AppFailure catch (failure) {
      _showMessage(failure.message);
    } finally {
      if (mounted) {
        setState(() => _isSaving = false);
      }
    }
  }

  void _showMessage(String message) {
    ScaffoldMessenger.of(context)
        .showSnackBar(SnackBar(content: Text(message)));
  }
}
