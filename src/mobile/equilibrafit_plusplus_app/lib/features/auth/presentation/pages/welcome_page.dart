import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../../../design_system/tokens/app_tokens.dart';
import '../../../../design_system/widgets/ef_button.dart';
import '../../../../design_system/widgets/ef_feedback_banner.dart';

class WelcomePage extends StatelessWidget {
  const WelcomePage({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: SafeArea(
        child: LayoutBuilder(
          builder: (context, constraints) {
            final maxContentWidth =
                (constraints.maxWidth - AppTokens.space24 * 2)
                    .clamp(0.0, 520.0)
                    .toDouble();

            return SingleChildScrollView(
              padding: const EdgeInsets.all(AppTokens.space24),
              child: Center(
                child: ConstrainedBox(
                  constraints: BoxConstraints(maxWidth: maxContentWidth),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const SizedBox(height: AppTokens.space32),
                      Text(
                        'EquilibraFit++',
                        style:
                            Theme.of(context).textTheme.displaySmall?.copyWith(
                                  fontWeight: FontWeight.w700,
                                ),
                      ),
                      const SizedBox(height: AppTokens.space8),
                      Text(
                        'Sua saúde. Seu ritmo. Seu equilíbrio.',
                        style: Theme.of(context).textTheme.titleMedium,
                      ),
                      const SizedBox(height: AppTokens.space24),
                      const EfFeedbackBanner(
                        title: 'Orientação sem culpa',
                        message:
                            'Planos de alimentação, treino e hábitos se adaptam à sua rotina com segurança.',
                      ),
                      const SizedBox(height: AppTokens.space24),
                      EfButton(
                        label: 'Criar conta',
                        icon: Icons.person_add_alt_1,
                        onPressed: () => context.go('/register'),
                      ),
                      const SizedBox(height: AppTokens.space12),
                      EfButton(
                        label: 'Entrar',
                        icon: Icons.login,
                        variant: EfButtonVariant.secondary,
                        onPressed: () => context.go('/login'),
                      ),
                      const SizedBox(height: AppTokens.space24),
                      Text(
                        'A IA apoia suas decisões e nunca substitui médicos, nutricionistas ou educadores físicos.',
                        style: Theme.of(context).textTheme.bodySmall,
                      ),
                    ],
                  ),
                ),
              ),
            );
          },
        ),
      ),
    );
  }
}
