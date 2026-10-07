import 'package:flutter/material.dart';

import '../tokens/app_tokens.dart';

class EfMetricTile extends StatelessWidget {
  const EfMetricTile({
    required this.label,
    required this.value,
    this.icon,
    this.supportingText,
    super.key,
  });

  final String label;
  final String value;
  final String? supportingText;
  final IconData? icon;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(AppTokens.space16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          mainAxisSize: MainAxisSize.min,
          children: [
            Row(
              children: [
                if (icon != null) ...[
                  Icon(icon, size: 18),
                  const SizedBox(width: AppTokens.space8),
                ],
                Expanded(
                  child: Text(
                    label,
                    style: Theme.of(context).textTheme.labelLarge,
                  ),
                ),
              ],
            ),
            const SizedBox(height: AppTokens.space12),
            FittedBox(
              fit: BoxFit.scaleDown,
              alignment: Alignment.centerLeft,
              child: Text(
                value,
                style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                      fontWeight: FontWeight.w700,
                    ),
              ),
            ),
            if (supportingText != null) ...[
              const SizedBox(height: AppTokens.space4),
              Text(
                supportingText!,
                style: Theme.of(context).textTheme.bodySmall,
              ),
            ],
          ],
        ),
      ),
    );
  }
}
