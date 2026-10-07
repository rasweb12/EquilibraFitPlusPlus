import 'package:flutter/material.dart';

import '../tokens/app_tokens.dart';

class EfMacroBar extends StatelessWidget {
  const EfMacroBar({
    required this.label,
    required this.value,
    required this.goal,
    required this.color,
    super.key,
  });

  final String label;
  final double value;
  final double goal;
  final Color color;

  @override
  Widget build(BuildContext context) {
    final progress = goal <= 0 ? 0.0 : (value / goal).clamp(0.0, 1.0);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          children: [
            Expanded(
              child: Text(label, style: Theme.of(context).textTheme.labelLarge),
            ),
            Text('${value.toStringAsFixed(0)}g / ${goal.toStringAsFixed(0)}g'),
          ],
        ),
        const SizedBox(height: AppTokens.space8),
        ClipRRect(
          borderRadius: BorderRadius.circular(AppTokens.radius),
          child: LinearProgressIndicator(
            minHeight: 10,
            value: progress,
            color: color,
            backgroundColor: color.withValues(alpha: 0.14),
          ),
        ),
      ],
    );
  }
}
