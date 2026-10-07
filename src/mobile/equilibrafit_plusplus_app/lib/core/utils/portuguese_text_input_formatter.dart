import 'package:flutter/services.dart';

import 'portuguese_text.dart';

/// Keeps Portuguese text entry friendly on physical keyboards and emulators.
class PortugueseTextInputFormatter extends TextInputFormatter {
  /// Creates a formatter for Portuguese text composition.
  const PortugueseTextInputFormatter();

  @override
  TextEditingValue formatEditUpdate(
    TextEditingValue oldValue,
    TextEditingValue newValue,
  ) {
    if (newValue.composing.isValid && !newValue.composing.isCollapsed) {
      return newValue;
    }

    final composedText = PortugueseText.composeDeadKeys(newValue.text);
    if (composedText == newValue.text) {
      return newValue;
    }

    return newValue.copyWith(
      text: composedText,
      selection: _composeSelection(newValue),
      composing: TextRange.empty,
    );
  }

  TextSelection _composeSelection(TextEditingValue value) {
    return TextSelection(
      baseOffset: _composeOffset(value.text, value.selection.baseOffset),
      extentOffset: _composeOffset(value.text, value.selection.extentOffset),
      affinity: value.selection.affinity,
      isDirectional: value.selection.isDirectional,
    );
  }

  int _composeOffset(String text, int offset) {
    if (offset < 0) {
      return offset;
    }

    final safeOffset = offset.clamp(0, text.length).toInt();
    return PortugueseText.composeDeadKeys(text.substring(0, safeOffset)).length;
  }
}
