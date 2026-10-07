import 'package:flutter/material.dart';

import '../tokens/app_tokens.dart';

enum EfFeedbackTone { support, warning, success, error }

class EfFeedbackBanner extends StatelessWidget {
  const EfFeedbackBanner({
    required this.message,
    this.title,
    this.tone = EfFeedbackTone.support,
    super.key,
  });

  final String? title;
  final String message;
  final EfFeedbackTone tone;

  @override
  Widget build(BuildContext context) {
    final color = switch (tone) {
      EfFeedbackTone.support => Theme.of(context).colorScheme.secondary,
      EfFeedbackTone.warning => AppTokens.warning,
      EfFeedbackTone.success => AppTokens.success,
      EfFeedbackTone.error => Theme.of(context).colorScheme.error,
    };

    return Semantics(
      liveRegion: true,
      child: Container(
        width: double.infinity,
        padding: const EdgeInsets.all(AppTokens.space16),
        decoration: BoxDecoration(
          color: color.withValues(alpha: 0.12),
          borderRadius: BorderRadius.circular(AppTokens.radius),
          border: Border.all(color: color.withValues(alpha: 0.28)),
        ),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Icon(_iconForTone(tone), color: color),
            const SizedBox(width: AppTokens.space12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  if (title != null) ...[
                    Text(title!, style: Theme.of(context).textTheme.titleSmall),
                    const SizedBox(height: AppTokens.space4),
                  ],
                  Text(message),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  IconData _iconForTone(EfFeedbackTone tone) {
    return switch (tone) {
      EfFeedbackTone.support => Icons.favorite_border,
      EfFeedbackTone.warning => Icons.info_outline,
      EfFeedbackTone.success => Icons.check_circle_outline,
      EfFeedbackTone.error => Icons.error_outline,
    };
  }
}
