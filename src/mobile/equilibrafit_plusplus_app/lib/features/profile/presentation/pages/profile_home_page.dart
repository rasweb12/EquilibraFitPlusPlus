import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../design_system/theme/theme_mode_controller.dart';
import '../../../../design_system/tokens/app_tokens.dart';
import '../../../../design_system/widgets/ef_button.dart';
import '../../../../design_system/widgets/ef_feedback_banner.dart';
import '../../../../design_system/widgets/ef_scaffold.dart';
import '../../../auth/presentation/controllers/session_controller.dart';

class ProfileHomePage extends ConsumerWidget {
  const ProfileHomePage({super.key});

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
                    user?.name ?? 'Usuário',
                    style: Theme.of(context).textTheme.titleLarge,
                  ),
                  const SizedBox(height: AppTokens.space4),
                  Text(user?.role ?? 'Usuário'),
                ],
              ),
            ),
          ),
          const SizedBox(height: AppTokens.space16),
          const EfFeedbackBanner(
            title: 'Privacidade',
            message:
                'Você mantém controle sobre seus dados. Solicitações sensíveis são tratadas com segurança pela API.',
          ),
          const SizedBox(height: AppTokens.space16),
          Card(
            child: Padding(
              padding: const EdgeInsets.all(AppTokens.space16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Text(
                    'Saúde e preferências',
                    style: Theme.of(context).textTheme.titleMedium,
                  ),
                  const SizedBox(height: AppTokens.space12),
                  EfButton(
                    label: 'Atualizar questionário',
                    icon: Icons.fact_check_outlined,
                    variant: EfButtonVariant.secondary,
                    onPressed: () => context.go('/onboarding?return=/profile'),
                  ),
                  const SizedBox(height: AppTokens.space12),
                  EfButton(
                    label: 'Registrar peso',
                    icon: Icons.monitor_weight_outlined,
                    variant: EfButtonVariant.secondary,
                    onPressed: () => context.go('/progress'),
                  ),
                ],
              ),
            ),
          ),
          const SizedBox(height: AppTokens.space16),
          Card(
            child: Padding(
              padding: const EdgeInsets.all(AppTokens.space16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text('Tema', style: Theme.of(context).textTheme.titleMedium),
                  const SizedBox(height: AppTokens.space12),
                  SegmentedButton<ThemeMode>(
                    selected: <ThemeMode>{themeMode},
                    onSelectionChanged: (selection) {
                      ref.read(themeModeProvider.notifier).state =
                          selection.first;
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
              label: 'Assinatura',
              icon: Icons.workspace_premium_outlined,
              variant: EfButtonVariant.secondary,
              onPressed: () => context.go('/premium'),),
          const SizedBox(height: AppTokens.space12),
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
}
