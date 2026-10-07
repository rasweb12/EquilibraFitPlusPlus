import 'package:flutter/material.dart';

import '../tokens/app_tokens.dart';

class EfSkeleton extends StatelessWidget {
  const EfSkeleton({super.key});

  @override
  Widget build(BuildContext context) {
    final color = Theme.of(context).colorScheme.surfaceContainerHighest;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        _SkeletonBox(height: 132, color: color),
        const SizedBox(height: AppTokens.space16),
        Wrap(
          spacing: AppTokens.space12,
          runSpacing: AppTokens.space12,
          children: [
            for (var index = 0; index < 4; index++)
              SizedBox(
                width: 160,
                child: _SkeletonBox(height: 112, color: color),
              ),
          ],
        ),
        const SizedBox(height: AppTokens.space16),
        _SkeletonBox(height: 168, color: color),
      ],
    );
  }
}

class _SkeletonBox extends StatelessWidget {
  const _SkeletonBox({required this.height, required this.color});

  final double height;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return TweenAnimationBuilder<double>(
      tween: Tween(begin: 0.45, end: 0.9),
      duration: const Duration(milliseconds: 900),
      curve: Curves.easeInOut,
      builder: (context, opacity, child) {
        return Opacity(opacity: opacity, child: child);
      },
      onEnd: () {},
      child: DecoratedBox(
        decoration: BoxDecoration(
          color: color,
          borderRadius: BorderRadius.circular(AppTokens.radius),
        ),
        child: SizedBox(height: height),
      ),
    );
  }
}
