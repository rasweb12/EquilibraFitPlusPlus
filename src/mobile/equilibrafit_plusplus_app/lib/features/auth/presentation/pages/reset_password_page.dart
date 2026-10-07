import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/errors/app_failure.dart';
import '../../../../core/http/api_client.dart';
import '../controllers/session_controller.dart';

class ResetPasswordPage extends ConsumerStatefulWidget {
  const ResetPasswordPage({required this.tokenHash, super.key});
  final String? tokenHash;
  @override
  ConsumerState<ResetPasswordPage> createState() => _ResetPasswordPageState();
}

class _ResetPasswordPageState extends ConsumerState<ResetPasswordPage> {
  final _password = TextEditingController();
  final _confirmation = TextEditingController();
  final _form = GlobalKey<FormState>();
  bool _sending = false;
  String? _error;

  @override
  void dispose() {
    _password.dispose();
    _confirmation.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => Scaffold(
        appBar: AppBar(title: const Text('EquilibraFit++')),
        body: SafeArea(
          child: Center(
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 480),
              child: ListView(
                padding: const EdgeInsets.all(24),
                children: [
                  Text(
                    'Nova senha',
                    style: Theme.of(context).textTheme.headlineSmall,
                  ),
                  const SizedBox(height: 24),
                  if (widget.tokenHash == null || widget.tokenHash!.isEmpty)
                    const Text(
                      'Link invalido. Solicite uma nova recuperacao.',
                    ),
                  if (_error != null)
                    Text(
                      _error!,
                      style: TextStyle(
                        color: Theme.of(context).colorScheme.error,
                      ),
                    ),
                  Form(
                    key: _form,
                    child: Column(
                      children: [
                        TextFormField(
                          controller: _password,
                          obscureText: true,
                          autofillHints: const [
                            AutofillHints.newPassword,
                          ],
                          decoration: const InputDecoration(labelText: 'Senha'),
                          validator: (value) => value != null &&
                                  value.length >= 8 &&
                                  value.length <= 128
                              ? null
                              : 'Use de 8 a 128 caracteres.',
                        ),
                        const SizedBox(height: 16),
                        TextFormField(
                          controller: _confirmation,
                          obscureText: true,
                          decoration: const InputDecoration(
                            labelText: 'Confirmar senha',
                          ),
                          validator: (value) => value == _password.text
                              ? null
                              : 'As senhas nao conferem.',
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 24),
                  FilledButton.icon(
                    onPressed: _sending ||
                            widget.tokenHash == null ||
                            widget.tokenHash!.isEmpty
                        ? null
                        : _submit,
                    icon: const Icon(Icons.lock_reset),
                    label: Text(
                      _sending ? 'Atualizando...' : 'Atualizar senha',
                    ),
                  ),
                ],
              ),
            ),
          ),
        ),
      );

  Future<void> _submit() async {
    if (!(_form.currentState?.validate() ?? false)) return;
    setState(() {
      _sending = true;
      _error = null;
    });
    try {
      await ref.read(apiClientProvider).postJson(
        '/api/v1/auth/recuperar/confirmar',
        body: <String, Object?>{
          'tokenHash': widget.tokenHash,
          'senha': _password.text,
        },
      );
      await ref.read(sessionControllerProvider.notifier).signOut();
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Senha atualizada. Entre novamente.'),
          ),
        );
        context.go('/login');
      }
    } on AppFailure catch (failure) {
      if (mounted) setState(() => _error = failure.message);
    } finally {
      if (mounted) setState(() => _sending = false);
    }
  }
}
