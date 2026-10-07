import 'package:flutter/material.dart';

enum EfButtonVariant { primary, secondary, text }

class EfButton extends StatelessWidget {
  const EfButton({
    required this.label,
    required this.onPressed,
    this.icon,
    this.isLoading = false,
    this.variant = EfButtonVariant.primary,
    super.key,
  });

  final String label;
  final IconData? icon;
  final bool isLoading;
  final VoidCallback? onPressed;
  final EfButtonVariant variant;

  @override
  Widget build(BuildContext context) {
    final child = _ButtonContent(
      label: label,
      icon: icon,
      isLoading: isLoading,
    );
    final callback = isLoading ? null : onPressed;

    return switch (variant) {
      EfButtonVariant.primary => ElevatedButton(
          onPressed: callback,
          child: child,
        ),
      EfButtonVariant.secondary => OutlinedButton(
          onPressed: callback,
          child: child,
        ),
      EfButtonVariant.text => TextButton(onPressed: callback, child: child),
    };
  }
}

class _ButtonContent extends StatelessWidget {
  const _ButtonContent({
    required this.label,
    required this.isLoading,
    this.icon,
  });

  final String label;
  final IconData? icon;
  final bool isLoading;

  @override
  Widget build(BuildContext context) {
    final progress = SizedBox.square(
      dimension: 18,
      child: CircularProgressIndicator(
        strokeWidth: 2,
        color: Theme.of(context).colorScheme.onPrimary,
      ),
    );

    return AnimatedSwitcher(
      duration: const Duration(milliseconds: 160),
      child: isLoading
          ? progress
          : Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                if (icon != null) ...[
                  Icon(icon, size: 18),
                  const SizedBox(width: 8),
                ],
                Flexible(child: Text(label, overflow: TextOverflow.ellipsis)),
              ],
            ),
    );
  }
}
