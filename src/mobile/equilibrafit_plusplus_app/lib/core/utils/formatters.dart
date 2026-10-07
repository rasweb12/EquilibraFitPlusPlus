import 'package:intl/intl.dart';

String formatDecimal(num value, {int decimals = 0}) {
  return NumberFormat.decimalPatternDigits(
    locale: 'pt_BR',
    decimalDigits: decimals,
  ).format(value);
}

String formatDate(DateTime value) {
  return DateFormat('dd/MM/yyyy', 'pt_BR').format(value);
}

String formatLitersFromMl(num milliliters) {
  return formatDecimal(milliliters / 1000, decimals: 1);
}
