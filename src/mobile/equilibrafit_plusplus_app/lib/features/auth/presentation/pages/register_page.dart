import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/errors/app_failure.dart';
import '../../../../design_system/tokens/app_tokens.dart';
import '../../../../design_system/widgets/ef_button.dart';
import '../../../../design_system/widgets/ef_feedback_banner.dart';
import '../../../../design_system/widgets/ef_text_field.dart';
import '../../auth_providers.dart';
import '../controllers/session_controller.dart';

class RegisterPage extends ConsumerStatefulWidget {
  const RegisterPage({super.key});

  @override
  ConsumerState<RegisterPage> createState() => _RegisterPageState();
}

class _RegisterPageState extends ConsumerState<RegisterPage> {
  final _formKey = GlobalKey<FormState>();
  final _nameController = TextEditingController();
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();
  var _acceptedTerms = false;
  var _acceptedPrivacy = false;
  var _readTerms = false;
  var _readPrivacy = false;
  var _analyticsConsent = false;
  var _isLoading = false;

  @override
  void dispose() {
    _nameController.dispose();
    _emailController.dispose();
    _passwordController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        leading: IconButton(
          tooltip: 'Voltar',
          onPressed: () => context.go('/welcome'),
          icon: const Icon(Icons.arrow_back),
        ),
        actions: [
          TextButton(
            onPressed: _isLoading ? null : () => context.go('/welcome'),
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
                constraints: const BoxConstraints(maxWidth: 560),
                child: Form(
                  key: _formKey,
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      Text(
                        'Criar conta',
                        style: Theme.of(context).textTheme.headlineMedium,
                      ),
                      const SizedBox(height: AppTokens.space8),
                      const EfFeedbackBanner(
                        title: 'Privacidade desde o início',
                        message:
                            'Usamos seus dados de saúde apenas para personalizar orientações e proteger sua experiência.',
                      ),
                      const SizedBox(height: AppTokens.space24),
                      EfTextField(
                        label: 'Nome',
                        controller: _nameController,
                        keyboardType: TextInputType.name,
                        textInputAction: TextInputAction.next,
                        textCapitalization: TextCapitalization.words,
                        autofillHints: const [AutofillHints.name],
                        validator: (value) {
                          if ((value?.trim() ?? '').length < 2) {
                            return 'Informe seu nome.';
                          }

                          return null;
                        },
                      ),
                      const SizedBox(height: AppTokens.space16),
                      EfTextField(
                        label: 'E-mail',
                        controller: _emailController,
                        keyboardType: TextInputType.emailAddress,
                        textInputAction: TextInputAction.next,
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
                      EfTextField(
                        label: 'Senha',
                        hint: 'Mínimo 8 caracteres, letra e número',
                        controller: _passwordController,
                        obscureText: true,
                        textInputAction: TextInputAction.done,
                        autofillHints: const [AutofillHints.newPassword],
                        validator: (value) {
                          final password = value ?? '';
                          final hasUpper = _hasUppercaseLetter(password);
                          final hasLower = _hasLowercaseLetter(password);
                          final hasDigit = RegExp('[0-9]').hasMatch(password);
                          if (password.length < 8 ||
                              !hasUpper ||
                              !hasLower ||
                              !hasDigit) {
                            return 'Use 8 caracteres com letras maiúscula, minúscula e número.';
                          }

                          return null;
                        },
                      ),
                      const SizedBox(height: AppTokens.space16),
                      _ConsentReadingCard(
                        onReadTerms: () => _showPolicyDialog(
                          title: 'Termos de uso',
                          body: _termsText,
                          onRead: () => _readTerms = true,
                        ),
                        onReadPrivacy: () => _showPolicyDialog(
                          title: 'Política de privacidade',
                          body: _privacyText,
                          onRead: () => _readPrivacy = true,
                        ),
                      ),
                      const SizedBox(height: AppTokens.space8),
                      CheckboxListTile(
                        value: _acceptedTerms,
                        onChanged: (value) {
                          setState(() => _acceptedTerms = value ?? false);
                        },
                        title: const Text('Li e aceito os termos de uso.'),
                        subtitle: Text(
                          _readTerms
                              ? 'Leitura registrada nesta tela.'
                              : 'Abra os termos antes de continuar.',
                        ),
                        controlAffinity: ListTileControlAffinity.leading,
                      ),
                      CheckboxListTile(
                        value: _acceptedPrivacy,
                        onChanged: (value) {
                          setState(() => _acceptedPrivacy = value ?? false);
                        },
                        title: const Text(
                          'Li e aceito a política de privacidade.',
                        ),
                        subtitle: Text(
                          _readPrivacy
                              ? 'Leitura registrada nesta tela.'
                              : 'Abra a política antes de continuar.',
                        ),
                        controlAffinity: ListTileControlAffinity.leading,
                      ),
                      CheckboxListTile(
                        value: _analyticsConsent,
                        onChanged: (value) {
                          setState(() => _analyticsConsent = value ?? false);
                        },
                        title: const Text(
                          'Autorizo analytics sem dados sensiveis.',
                        ),
                        controlAffinity: ListTileControlAffinity.leading,
                      ),
                      const SizedBox(height: AppTokens.space16),
                      EfButton(
                        label: 'Continuar',
                        icon: Icons.arrow_forward,
                        isLoading: _isLoading,
                        onPressed: _submit,
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

    if (!_readTerms || !_readPrivacy) {
      _showMessage('Leia os termos e a privacidade antes de continuar.');
      return;
    }

    if (!_acceptedTerms || !_acceptedPrivacy) {
      _showMessage('Para continuar, confirme os aceites obrigatórios.');
      return;
    }

    setState(() => _isLoading = true);

    try {
      final session = await ref.read(authRepositoryProvider).register(
            name: _nameController.text,
            email: _emailController.text,
            password: _passwordController.text,
            analyticsConsent: _analyticsConsent,
          );

      ref.read(sessionControllerProvider.notifier).startSession(session);

      if (mounted) {
        context.go('/onboarding');
      }
    } on AppFailure catch (failure) {
      _showMessage(failure.message);
      if (failure.code == 'auth.email_confirmation_required' && mounted) {
        context.go('/login');
      }
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

  Future<void> _showPolicyDialog({
    required String title,
    required String body,
    required VoidCallback onRead,
  }) async {
    await showDialog<void>(
      context: context,
      builder: (context) {
        return AlertDialog(
          title: Text(title),
          content: SingleChildScrollView(child: Text(body)),
          actions: [
            TextButton(
              onPressed: () {
                Navigator.of(context).pop();
              },
              child: const Text('Fechar'),
            ),
            FilledButton(
              onPressed: () {
                onRead();
                Navigator.of(context).pop();
                setState(() {});
              },
              child: const Text('Li e entendi'),
            ),
          ],
        );
      },
    );
  }
}

class _ConsentReadingCard extends StatelessWidget {
  const _ConsentReadingCard({
    required this.onReadTerms,
    required this.onReadPrivacy,
  });

  final VoidCallback onReadTerms;
  final VoidCallback onReadPrivacy;

  @override
  Widget build(BuildContext context) {
    final colors = Theme.of(context).colorScheme;

    return DecoratedBox(
      decoration: BoxDecoration(
        border: Border.all(color: colors.outlineVariant),
        borderRadius: BorderRadius.circular(AppTokens.radius),
      ),
      child: Padding(
        padding: const EdgeInsets.all(AppTokens.space16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(
              'Transparência antes do aceite',
              style: Theme.of(context).textTheme.titleMedium,
            ),
            const SizedBox(height: AppTokens.space8),
            Text(
              'Você pode ler como seus dados, consentimentos e recursos de IA são tratados antes de criar a conta.',
              style: Theme.of(context).textTheme.bodyMedium,
            ),
            const SizedBox(height: AppTokens.space12),
            Wrap(
              spacing: AppTokens.space8,
              runSpacing: AppTokens.space8,
              children: [
                OutlinedButton.icon(
                  onPressed: onReadTerms,
                  icon: const Icon(Icons.description_outlined),
                  label: const Text('Ler termos'),
                ),
                OutlinedButton.icon(
                  onPressed: onReadPrivacy,
                  icon: const Icon(Icons.privacy_tip_outlined),
                  label: const Text('Ler privacidade'),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

bool _hasUppercaseLetter(String value) {
  return value.runes.map(String.fromCharCode).any(
        (char) => char.toUpperCase() == char && char.toLowerCase() != char,
      );
}

bool _hasLowercaseLetter(String value) {
  return value.runes.map(String.fromCharCode).any(
        (char) => char.toLowerCase() == char && char.toUpperCase() != char,
      );
}

const _termsText = '''
Ao usar o EquilibraFit++, você recebe orientações de alimentação, treino, hábitos e IA para apoiar sua rotina.

O aplicativo não substitui médicos, nutricionistas, educadores físicos ou outros profissionais de saúde.

Você continua livre para adaptar suas escolhas. O app nunca usa mensagens de culpa ou incentivo a extremos.

Para criar a conta, precisamos registrar seu nome, e-mail, senha protegida e consentimentos obrigatórios.
''';

const _privacyText = '''
Usamos seus dados de saúde apenas para personalizar orientações, proteger sua experiência e melhorar o funcionamento do produto.

Dados sensíveis, como peso, medidas, preferências, restrições e fotos de refeições, devem ter finalidade clara.

A IA pode ajudar a reconhecer refeições, gerar planos e responder dúvidas, mas suas respostas são orientativas.

Analytics opcionais não devem usar dados sensíveis. Você pode continuar sem autorizar analytics.

Você poderá solicitar exportação ou exclusão dos seus dados pelos fluxos LGPD do produto.
''';
