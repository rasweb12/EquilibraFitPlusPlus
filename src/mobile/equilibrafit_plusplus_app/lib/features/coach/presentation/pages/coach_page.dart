import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/errors/app_failure.dart';
import '../../../../design_system/tokens/app_tokens.dart';
import '../../../../design_system/widgets/ef_button.dart';
import '../../../../design_system/widgets/ef_feedback_banner.dart';
import '../../../../design_system/widgets/ef_scaffold.dart';
import '../../../../design_system/widgets/ef_text_field.dart';
import '../../data/coach_repository.dart';

class CoachPage extends ConsumerStatefulWidget {
  const CoachPage({super.key});

  @override
  ConsumerState<CoachPage> createState() => _CoachPageState();
}

class _CoachPageState extends ConsumerState<CoachPage> {
  final _formKey = GlobalKey<FormState>();
  final _messageController = TextEditingController();
  String? _sessionId;
  String? _reply;
  String? _notice;
  var _provider = 'openai';
  var _fallbackUsed = false;
  var _isLoading = false;

  @override
  void dispose() {
    _messageController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return EfScaffold(
      title: 'Coach IA',
      subtitle: 'Orientação de apoio, não substitui profissionais.',
      actions: [
        TextButton(
          onPressed: _isLoading ? null : () => context.go('/dashboard'),
          child: const Text('Voltar'),
        ),
      ],
      body: Form(
        key: _formKey,
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            const EfFeedbackBanner(
              title: 'Uso responsável',
              message:
                  'A IA pode ajudar a adaptar sua rotina, mas decisões clínicas devem ser acompanhadas por profissionais.',
            ),
            if (_reply != null) ...[
              const SizedBox(height: AppTokens.space16),
              Card(
                child: Padding(
                  padding: const EdgeInsets.all(AppTokens.space16),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        _fallbackUsed ? 'Resposta de apoio local' : 'Resposta',
                        style: Theme.of(context).textTheme.titleMedium,
                      ),
                      const SizedBox(height: AppTokens.space8),
                      Text(_reply!),
                      if (_notice != null && _notice!.isNotEmpty) ...[
                        const SizedBox(height: AppTokens.space12),
                        Text(
                          _notice!,
                          style: Theme.of(context).textTheme.bodySmall,
                        ),
                      ],
                    ],
                  ),
                ),
              ),
            ],
            const SizedBox(height: AppTokens.space16),
            SegmentedButton<String>(
              segments: const [
                ButtonSegment(value: 'openai', label: Text('OpenAI')),
                ButtonSegment(value: 'gemini', label: Text('Gemini')),
              ],
              selected: {_provider},
              onSelectionChanged: _isLoading
                  ? null
                  : (selection) {
                      setState(() => _provider = selection.single);
                    },
            ),
            const SizedBox(height: AppTokens.space16),
            EfTextField(
              label: 'Mensagem',
              hint: 'Ex.: Como adapto meu jantar hoje?',
              controller: _messageController,
              maxLines: 4,
              textCapitalization: TextCapitalization.sentences,
              validator: (value) {
                final text = value?.trim() ?? '';
                if (text.length < 3) {
                  return 'Escreva uma pergunta ou contexto curto.';
                }

                return null;
              },
            ),
            const SizedBox(height: AppTokens.space16),
            EfButton(
              label: 'Enviar',
              icon: Icons.send_outlined,
              isLoading: _isLoading,
              onPressed: _submit,
            ),
          ],
        ),
      ),
    );
  }

  Future<void> _submit() async {
    if (!(_formKey.currentState?.validate() ?? false)) {
      return;
    }

    setState(() => _isLoading = true);

    try {
      final response = await ref.read(coachRepositoryProvider).sendMessage(
            message: _messageController.text.trim(),
            sessionId: _sessionId,
            provider: _provider,
          );

      if (!mounted) return;
      setState(() {
        _sessionId = response.sessionId;
        _reply = response.content;
        _notice = response.healthNotice;
        _fallbackUsed = response.fallbackUsed;
        _messageController.clear();
      });
    } on AppFailure catch (failure) {
      if (mounted) _showMessage(failure.message);
    } finally {
      if (mounted) {
        setState(() => _isLoading = false);
      }
    }
  }

  void _showMessage(String message) {
    ScaffoldMessenger.of(
      context,
    ).showSnackBar(SnackBar(content: Text(message)));
  }
}
