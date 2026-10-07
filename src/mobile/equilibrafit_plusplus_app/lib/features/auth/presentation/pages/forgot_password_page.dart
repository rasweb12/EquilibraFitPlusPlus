import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../../core/http/api_client.dart';
import '../../../../core/errors/app_failure.dart';

import '../../../../design_system/tokens/app_tokens.dart';
import '../../../../design_system/widgets/ef_button.dart';
import '../../../../design_system/widgets/ef_feedback_banner.dart';
import '../../../../design_system/widgets/ef_text_field.dart';

class ForgotPasswordPage extends ConsumerStatefulWidget {
  const ForgotPasswordPage({super.key});

  @override
  ConsumerState<ForgotPasswordPage> createState() => _ForgotPasswordPageState();
}

class _ForgotPasswordPageState extends ConsumerState<ForgotPasswordPage> {
  final _formKey = GlobalKey<FormState>();
  final _emailController = TextEditingController();
  var _submitted = false;
  var _sending = false;

  @override
  void dispose() {
    _emailController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        leading: IconButton(
          tooltip: 'Voltar',
          onPressed: () => context.go('/login'),
          icon: const Icon(Icons.arrow_back),
        ),
        actions: [
          TextButton(
            onPressed: () => context.go('/login'),
            child: const Text('Cancelar'),
          ),
        ],
      ),
      body: SafeArea(
        child: Align(
          alignment: Alignment.topCenter,
          child: ListView(
            padding: const EdgeInsets.all(AppTokens.space24),
            children: [
              ConstrainedBox(
                constraints: const BoxConstraints(maxWidth: 520),
                child: Form(
                  key: _formKey,
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      Text(
                        'Recuperar senha',
                        style: Theme.of(context).textTheme.headlineMedium,
                      ),
                      const SizedBox(height: AppTokens.space16),
                      if (_submitted)
                        const EfFeedbackBanner(
                          tone: EfFeedbackTone.success,
                          title: 'Solicitação recebida',
                          message:
                              'Se houver uma conta para este e-mail, enviaremos as instruções com segurança.',
                        )
                      else ...[
                        EfTextField(
                          label: 'E-mail',
                          controller: _emailController,
                          keyboardType: TextInputType.emailAddress,
                          autofillHints: const [AutofillHints.email],
                          validator: (value) {
                            final email = value?.trim() ?? '';
                            if (email.isEmpty || !email.contains('@')) {
                              return 'Informe um e-mail válido.';
                            }

                            return null;
                          },
                        ),
                        const SizedBox(height: AppTokens.space16),
                        EfButton(
                          label: 'Enviar instruções',
                          icon: Icons.mark_email_read_outlined,
                          onPressed: _sending ? null : _submit,
                        ),
                      ],
                      const SizedBox(height: AppTokens.space12),
                      EfButton(
                        label: 'Voltar para entrar',
                        variant: EfButtonVariant.text,
                        onPressed: () => context.go('/login'),
                      ),
                    ],
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  Future<void> _submit() async {
    if (!(_formKey.currentState?.validate() ?? false)) {
      return;
    }

    setState(() => _sending = true);
    try {
      await ref.read(apiClientProvider).postJson('/api/v1/auth/recuperar',
          body: <String, Object?>{'email': _emailController.text.trim()},);
      if (mounted) setState(() => _submitted = true);
    } on AppFailure catch (failure) {
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text(failure.message)));
      }
    } finally {
      if (mounted) setState(() => _sending = false);
    }
  }
}
