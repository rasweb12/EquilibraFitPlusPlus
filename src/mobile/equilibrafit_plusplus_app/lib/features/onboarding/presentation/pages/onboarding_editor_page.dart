import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/errors/app_failure.dart';
import '../../../../design_system/tokens/app_tokens.dart';
import '../../../../design_system/widgets/ef_button.dart';
import '../../../../design_system/widgets/ef_feedback_banner.dart';
import '../../../../design_system/widgets/ef_scaffold.dart';
import '../../../../design_system/widgets/ef_text_field.dart';
import '../../../auth/presentation/controllers/session_controller.dart';
import '../../../dashboard/data/dashboard_repository.dart';
import '../../data/onboarding_repository.dart';
import '../../domain/onboarding_answer.dart';

class OnboardingEditorPage extends ConsumerStatefulWidget {
  const OnboardingEditorPage({super.key, this.returnPath});

  final String? returnPath;

  @override
  ConsumerState<OnboardingEditorPage> createState() =>
      _OnboardingEditorPageState();
}

class _OnboardingEditorPageState extends ConsumerState<OnboardingEditorPage> {
  final _formKey = GlobalKey<FormState>();
  final _birthDateController = TextEditingController();
  final _heightController = TextEditingController();
  final _weightController = TextEditingController();
  final _daysController = TextEditingController(text: '3');
  final _preferencesController = TextEditingController();
  final _restrictionsController = TextEditingController();
  final _notesController = TextEditingController();
  var _step = 0;
  var _isLoading = false;
  var _isLoadingExisting = false;
  var _sex = 'NaoInformado';
  var _goal = 'EmagrecimentoSustentavel';
  var _activityLevel = 'Moderado';

  @override
  void initState() {
    super.initState();
    _loadCurrent();
  }

  @override
  void dispose() {
    _birthDateController.dispose();
    _heightController.dispose();
    _weightController.dispose();
    _daysController.dispose();
    _preferencesController.dispose();
    _restrictionsController.dispose();
    _notesController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return EfScaffold(
      title: 'Questionário',
      subtitle: 'Etapa ${_step + 1} de 3',
      actions: [
        TextButton(
          onPressed: _isLoading ? null : _cancelOnboarding,
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
                  LinearProgressIndicator(value: (_step + 1) / 3),
                  const SizedBox(height: AppTokens.space20),
                  AnimatedSwitcher(
                    duration: const Duration(milliseconds: 180),
                    child: _buildStep(context),
                  ),
                  const SizedBox(height: AppTokens.space24),
                  Row(
                    children: [
                      Expanded(
                        child: EfButton(
                          label: 'Voltar',
                          icon: Icons.arrow_back,
                          variant: EfButtonVariant.secondary,
                          onPressed: _step == 0
                              ? null
                              : () {
                                  setState(() => _step -= 1);
                                },
                        ),
                      ),
                      const SizedBox(width: AppTokens.space12),
                      Expanded(
                        child: EfButton(
                          label: _step == 2 ? 'Salvar' : 'Avançar',
                          icon: _step == 2 ? Icons.check : Icons.arrow_forward,
                          isLoading: _isLoading,
                          onPressed: _step == 2 ? _submit : _next,
                        ),
                      ),
                    ],
                  ),
                ],
              ),
            ),
    );
  }

  Widget _buildStep(BuildContext context) {
    return switch (_step) {
      0 => _BasicDataStep(
          birthDateController: _birthDateController,
          heightController: _heightController,
          weightController: _weightController,
          sex: _sex,
          onSexChanged: (value) => setState(() => _sex = value),
        ),
      1 => _RoutineStep(
          goal: _goal,
          activityLevel: _activityLevel,
          daysController: _daysController,
          onGoalChanged: (value) => setState(() => _goal = value),
          onActivityChanged: (value) => setState(() => _activityLevel = value),
        ),
      _ => _PreferencesStep(
          preferencesController: _preferencesController,
          restrictionsController: _restrictionsController,
          notesController: _notesController,
        ),
    };
  }

  Future<void> _loadCurrent() async {
    setState(() => _isLoadingExisting = true);
    try {
      final answer = await ref.read(onboardingRepositoryProvider).getCurrent();
      if (answer != null && mounted) {
        _applyAnswer(answer);
      }
    } on AppFailure {
      // Empty questionnaire is expected for new users.
    } finally {
      if (mounted) {
        setState(() => _isLoadingExisting = false);
      }
    }
  }

  void _applyAnswer(OnboardingAnswer answer) {
    _birthDateController.text = _dateOnly(answer.birthDate);
    _heightController.text = _formatNumber(answer.heightCm);
    _weightController.text = _formatNumber(answer.currentWeightKg);
    _daysController.text = answer.trainingDaysPerWeek.toString();
    _preferencesController.text = answer.preferences.join(', ');
    _restrictionsController.text = answer.restrictions.join(', ');
    _notesController.text = answer.notes.join(', ');
    _sex = answer.biologicalSex.isEmpty ? _sex : answer.biologicalSex;
    _goal = answer.goal.isEmpty ? _goal : answer.goal;
    _activityLevel =
        answer.activityLevel.isEmpty ? _activityLevel : answer.activityLevel;
  }

  void _next() {
    if (!(_formKey.currentState?.validate() ?? false)) {
      return;
    }

    setState(() => _step += 1);
  }

  Future<void> _submit() async {
    if (!(_formKey.currentState?.validate() ?? false)) {
      return;
    }

    final answer = _buildAnswer();
    if (answer == null) {
      _showMessage('Revise os dados informados. Podemos ajustar com calma.');
      return;
    }

    setState(() => _isLoading = true);

    try {
      await ref.read(onboardingRepositoryProvider).save(answer);
      await ref.read(sessionControllerProvider.notifier).completeOnboarding();
      ref.invalidate(dashboardSummaryProvider);

      if (mounted) {
        context.go(
          widget.returnPath?.isNotEmpty == true
              ? widget.returnPath!
              : '/dashboard',
        );
      }
    } on AppFailure catch (failure) {
      _showMessage(failure.message);
    } finally {
      if (mounted) {
        setState(() => _isLoading = false);
      }
    }
  }

  OnboardingAnswer? _buildAnswer() {
    final birthDate = DateTime.tryParse(_birthDateController.text.trim());
    final height = double.tryParse(_heightController.text.replaceAll(',', '.'));
    final weight = double.tryParse(_weightController.text.replaceAll(',', '.'));
    final days = int.tryParse(_daysController.text.trim());

    if (birthDate == null || height == null || weight == null || days == null) {
      return null;
    }

    return OnboardingAnswer(
      birthDate: birthDate,
      biologicalSex: _sex,
      heightCm: height,
      currentWeightKg: weight,
      goal: _goal,
      activityLevel: _activityLevel,
      trainingDaysPerWeek: days,
      preferences: _splitList(_preferencesController.text),
      restrictions: _splitList(_restrictionsController.text),
      notes: _splitList(_notesController.text),
    );
  }

  List<String> _splitList(String value) {
    return value
        .split(',')
        .map((item) => item.trim())
        .where((item) => item.isNotEmpty)
        .toList(growable: false);
  }

  void _showMessage(String message) {
    ScaffoldMessenger.of(context)
        .showSnackBar(SnackBar(content: Text(message)));
  }

  Future<void> _cancelOnboarding() async {
    final returnPath = widget.returnPath;
    if (returnPath != null && returnPath.isNotEmpty) {
      context.go(returnPath);
      return;
    }

    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Cancelar questionário?'),
        content: const Text(
          'Sem problemas. Você pode voltar depois e continuar seu plano com calma.',
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.of(context).pop(false),
            child: const Text('Continuar preenchendo'),
          ),
          FilledButton(
            onPressed: () => Navigator.of(context).pop(true),
            child: const Text('Sair por enquanto'),
          ),
        ],
      ),
    );

    if (confirmed != true) {
      return;
    }

    await ref.read(sessionControllerProvider.notifier).signOut();
    if (mounted) {
      context.go('/welcome');
    }
  }

  String _dateOnly(DateTime value) {
    final year = value.year.toString().padLeft(4, '0');
    final month = value.month.toString().padLeft(2, '0');
    final day = value.day.toString().padLeft(2, '0');
    return '$year-$month-$day';
  }

  String _formatNumber(double value) {
    if (value == value.roundToDouble()) {
      return value.toStringAsFixed(0);
    }

    return value.toStringAsFixed(1);
  }
}

class _BasicDataStep extends StatelessWidget {
  const _BasicDataStep({
    required this.birthDateController,
    required this.heightController,
    required this.weightController,
    required this.sex,
    required this.onSexChanged,
  });

  final TextEditingController birthDateController;
  final TextEditingController heightController;
  final TextEditingController weightController;
  final String sex;
  final ValueChanged<String> onSexChanged;

  @override
  Widget build(BuildContext context) {
    return Column(
      key: const ValueKey('basic-data'),
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Text(
          'Dados para cálculos seguros',
          style: Theme.of(context).textTheme.titleLarge,
        ),
        const SizedBox(height: AppTokens.space8),
        const EfFeedbackBanner(
          message:
              'Pedimos somente o necessário para estimar metas iniciais com responsabilidade.',
        ),
        const SizedBox(height: AppTokens.space16),
        EfTextField(
          label: 'Data de nascimento',
          hint: 'AAAA-MM-DD',
          controller: birthDateController,
          keyboardType: TextInputType.datetime,
          validator: (value) {
            final date = DateTime.tryParse(value?.trim() ?? '');
            return date == null
                ? 'Informe a data no formato AAAA-MM-DD.'
                : null;
          },
        ),
        const SizedBox(height: AppTokens.space16),
        DropdownButtonFormField<String>(
          initialValue: sex,
          decoration: const InputDecoration(labelText: 'Sexo biológico'),
          items: const [
            DropdownMenuItem(value: 'Feminino', child: Text('Feminino')),
            DropdownMenuItem(value: 'Masculino', child: Text('Masculino')),
            DropdownMenuItem(
              value: 'NaoInformado',
              child: Text('Não informar'),
            ),
          ],
          onChanged: (value) {
            if (value != null) {
              onSexChanged(value);
            }
          },
        ),
        const SizedBox(height: AppTokens.space16),
        EfTextField(
          label: 'Altura',
          hint: 'cm',
          controller: heightController,
          keyboardType: TextInputType.number,
          validator: (value) => _range(value, 80, 250, 'altura'),
        ),
        const SizedBox(height: AppTokens.space16),
        EfTextField(
          label: 'Peso atual',
          hint: 'kg',
          controller: weightController,
          keyboardType: TextInputType.number,
          validator: (value) => _range(value, 25, 350, 'peso'),
        ),
      ],
    );
  }

  String? _range(String? value, num min, num max, String field) {
    final number = double.tryParse((value ?? '').replaceAll(',', '.'));
    if (number == null || number < min || number > max) {
      return 'Informe um $field válido.';
    }

    return null;
  }
}

class _RoutineStep extends StatelessWidget {
  const _RoutineStep({
    required this.goal,
    required this.activityLevel,
    required this.daysController,
    required this.onGoalChanged,
    required this.onActivityChanged,
  });

  final String goal;
  final String activityLevel;
  final TextEditingController daysController;
  final ValueChanged<String> onGoalChanged;
  final ValueChanged<String> onActivityChanged;

  @override
  Widget build(BuildContext context) {
    return Column(
      key: const ValueKey('routine'),
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Text(
          'Objetivo e rotina',
          style: Theme.of(context).textTheme.titleLarge,
        ),
        const SizedBox(height: AppTokens.space16),
        DropdownButtonFormField<String>(
          initialValue: goal,
          decoration: const InputDecoration(labelText: 'Objetivo principal'),
          items: const [
            DropdownMenuItem(
              value: 'EmagrecimentoSustentavel',
              child: Text('Emagrecer com equilíbrio'),
            ),
            DropdownMenuItem(value: 'GanhoMassa', child: Text('Ganhar massa')),
            DropdownMenuItem(value: 'Manutencao', child: Text('Manter saúde')),
            DropdownMenuItem(
              value: 'Condicionamento',
              child: Text('Melhorar condicionamento'),
            ),
          ],
          onChanged: (value) {
            if (value != null) {
              onGoalChanged(value);
            }
          },
        ),
        const SizedBox(height: AppTokens.space16),
        DropdownButtonFormField<String>(
          initialValue: activityLevel,
          decoration: const InputDecoration(labelText: 'Nível de atividade'),
          items: const [
            DropdownMenuItem(value: 'Sedentario', child: Text('Sedentário')),
            DropdownMenuItem(value: 'Leve', child: Text('Leve')),
            DropdownMenuItem(value: 'Moderado', child: Text('Moderado')),
            DropdownMenuItem(value: 'Intenso', child: Text('Intenso')),
            DropdownMenuItem(
              value: 'MuitoIntenso',
              child: Text('Muito intenso'),
            ),
          ],
          onChanged: (value) {
            if (value != null) {
              onActivityChanged(value);
            }
          },
        ),
        const SizedBox(height: AppTokens.space16),
        EfTextField(
          label: 'Dias de treino por semana',
          controller: daysController,
          keyboardType: TextInputType.number,
          validator: (value) {
            final days = int.tryParse(value?.trim() ?? '');
            return days == null || days < 0 || days > 7
                ? 'Informe um valor entre 0 e 7.'
                : null;
          },
        ),
      ],
    );
  }
}

class _PreferencesStep extends StatelessWidget {
  const _PreferencesStep({
    required this.preferencesController,
    required this.restrictionsController,
    required this.notesController,
  });

  final TextEditingController preferencesController;
  final TextEditingController restrictionsController;
  final TextEditingController notesController;

  @override
  Widget build(BuildContext context) {
    return Column(
      key: const ValueKey('preferences'),
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Text(
          'Preferências e cuidados',
          style: Theme.of(context).textTheme.titleLarge,
        ),
        const SizedBox(height: AppTokens.space8),
        const EfFeedbackBanner(
          message:
              'Nada aqui é tratado como proibição. Usamos as respostas para adaptar melhor o plano.',
        ),
        const SizedBox(height: AppTokens.space16),
        EfTextField(
          label: 'Preferências alimentares',
          hint: 'Separe por vírgula',
          controller: preferencesController,
          maxLines: 2,
          textCapitalization: TextCapitalization.sentences,
        ),
        const SizedBox(height: AppTokens.space16),
        EfTextField(
          label: 'Restrições ou alergias',
          hint: 'Separe por vírgula',
          controller: restrictionsController,
          maxLines: 2,
          textCapitalization: TextCapitalization.sentences,
        ),
        const SizedBox(height: AppTokens.space16),
        EfTextField(
          label: 'Observações importantes',
          hint: 'Rotina, horários, limitações ou contexto',
          controller: notesController,
          maxLines: 3,
          textCapitalization: TextCapitalization.sentences,
        ),
      ],
    );
  }
}
