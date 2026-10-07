import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../design_system/theme/theme_mode_controller.dart';
import '../../../../design_system/tokens/app_tokens.dart';
import '../../../../design_system/widgets/ef_button.dart';
import '../../../../design_system/widgets/ef_feedback_banner.dart';
import '../../../../design_system/widgets/ef_scaffold.dart';
import '../../../auth/presentation/controllers/session_controller.dart';

class ProfilePage extends ConsumerWidget {
  const ProfilePage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final session = ref.watch(sessionControllerProvider);
    final user = session.user;
    final themeMode = ref.watch(themeModeProvider);

    return EfScaffold(
      title: 'Perfil',
      subtitle: user?.email ?? 'Dados da conta',
      currentIndex: 4,
      body: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Card(
            child: Padding(
              padding: const EdgeInsets.all(AppTokens.space16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    user?.name ?? 'Usuario',
                    style: Theme.of(context).textTheme.titleLarge,
                  ),
                  const SizedBox(height: AppTokens.space4),
                  Text(user?.role ?? 'Usuario'),
                ],
              ),
            ),
          ),
          const SizedBox(height: AppTokens.space16),
          const EfFeedbackBanner(
            title: 'Privacidade',
            message:
                'Você mantém controle sobre seus dados. Solicitações sensíveis devem ser tratadas com segurança pela API.',
          ),
          const SizedBox(height: AppTokens.space16),
          Card(
            child: Padding(
              padding: const EdgeInsets.all(AppTokens.space16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    'Tema',
                    style: Theme.of(context).textTheme.titleMedium,
                  ),
                  const SizedBox(height: AppTokens.space12),
                  SegmentedButton<ThemeMode>(
                    selected: <ThemeMode>{themeMode},
                    onSelectionChanged: (selection) {
                      _setTheme(ref, selection.first);
                    },
                    segments: const [
                      ButtonSegment(
                        value: ThemeMode.system,
                        label: Text('Sistema'),
                        icon: Icon(Icons.brightness_auto_outlined),
                      ),
                      ButtonSegment(
                        value: ThemeMode.light,
                        label: Text('Claro'),
                        icon: Icon(Icons.light_mode_outlined),
                      ),
                      ButtonSegment(
                        value: ThemeMode.dark,
                        label: Text('Escuro'),
                        icon: Icon(Icons.dark_mode_outlined),
                      ),
                    ],
                  ),
                ],
              ),
            ),
          ),
          const SizedBox(height: AppTokens.space20),
          EfButton(
            label: 'Sair',
            icon: Icons.logout,
            variant: EfButtonVariant.secondary,
            onPressed: () async {
              await ref.read(sessionControllerProvider.notifier).signOut();
              if (context.mounted) {
                context.go('/welcome');
              }
            },
          ),
        ],
      ),
    );
  }

  void _setTheme(WidgetRef ref, ThemeMode mode) {
    ref.read(themeModeProvider.notifier).state = mode;
  }
}
