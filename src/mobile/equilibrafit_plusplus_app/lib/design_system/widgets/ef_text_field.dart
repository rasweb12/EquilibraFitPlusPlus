import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import '../../core/utils/portuguese_text_input_formatter.dart';

class EfTextField extends StatelessWidget {
  const EfTextField({
    required this.label,
    required this.controller,
    this.hint,
    this.keyboardType,
    this.textInputAction,
    this.obscureText = false,
    this.maxLines = 1,
    this.validator,
    this.suffixIcon,
    this.textCapitalization = TextCapitalization.none,
    this.enableSuggestions,
    this.autocorrect,
    this.inputFormatters,
    this.autofillHints,
    this.enablePortugueseInput = true,
    super.key,
  });

  final String label;
  final String? hint;
  final TextEditingController controller;
  final TextInputType? keyboardType;
  final TextInputAction? textInputAction;
  final bool obscureText;
  final int maxLines;
  final FormFieldValidator<String>? validator;
  final Widget? suffixIcon;
  final TextCapitalization textCapitalization;
  final bool? enableSuggestions;
  final bool? autocorrect;
  final List<TextInputFormatter>? inputFormatters;
  final Iterable<String>? autofillHints;
  final bool enablePortugueseInput;

  @override
  Widget build(BuildContext context) {
    return TextFormField(
      controller: controller,
      keyboardType: keyboardType,
      textInputAction: textInputAction,
      obscureText: obscureText,
      maxLines: maxLines,
      validator: validator,
      textCapitalization: textCapitalization,
      enableSuggestions: enableSuggestions ?? !obscureText,
      autocorrect: autocorrect ?? !obscureText,
      inputFormatters: _resolvedInputFormatters(),
      autofillHints: autofillHints,
      smartDashesType:
          obscureText ? SmartDashesType.disabled : SmartDashesType.enabled,
      smartQuotesType:
          obscureText ? SmartQuotesType.disabled : SmartQuotesType.enabled,
      decoration: InputDecoration(
        labelText: label,
        hintText: hint,
        suffixIcon: suffixIcon,
      ),
    );
  }

  List<TextInputFormatter>? _resolvedInputFormatters() {
    final formatters = <TextInputFormatter>[
      ...?inputFormatters,
    ];

    if (_shouldComposePortugueseInput()) {
      formatters.add(const PortugueseTextInputFormatter());
    }

    return formatters.isEmpty ? null : formatters;
  }

  bool _shouldComposePortugueseInput() {
    if (!enablePortugueseInput || obscureText) {
      return false;
    }

    return switch (keyboardType) {
      TextInputType.emailAddress ||
      TextInputType.number ||
      TextInputType.phone ||
      TextInputType.url ||
      TextInputType.visiblePassword =>
        false,
      _ => true,
    };
  }
}
