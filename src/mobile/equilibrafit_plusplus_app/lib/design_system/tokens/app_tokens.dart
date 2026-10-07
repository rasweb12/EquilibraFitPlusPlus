import 'package:flutter/material.dart';

class AppTokens {
  const AppTokens._();

  static const Color primary = Color(0xFF2D8C7C);
  static const Color primaryDark = Color(0xFF17665A);
  static const Color secondary = Color(0xFF4F6FD8);
  static const Color accent = Color(0xFFE87D5D);
  static const Color success = Color(0xFF2E9D64);
  static const Color warning = Color(0xFFE4A11B);
  static const Color danger = Color(0xFFC94A4A);
  static const Color background = Color(0xFFF6F8F7);
  static const Color text = Color(0xFF1F2933);
  static const Color muted = Color(0xFF6B7280);
  static const Color water = Color(0xFF2F80ED);
  static const Color protein = Color(0xFF8E5CF6);
  static const Color carbs = Color(0xFFE0A11A);
  static const Color fat = Color(0xFFE87355);

  static const double radius = 8;
  static const double radiusSmall = 6;
  static const double radiusLarge = 12;
  static const double space4 = 4;
  static const double space8 = 8;
  static const double space12 = 12;
  static const double space16 = 16;
  static const double space20 = 20;
  static const double space24 = 24;
  static const double space32 = 32;

  static const List<BoxShadow> softShadow = [
    BoxShadow(
      color: Color(0x14000000),
      offset: Offset(0, 6),
      blurRadius: 18,
    ),
  ];
}
