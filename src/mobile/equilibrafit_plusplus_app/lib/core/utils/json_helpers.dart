Map<String, Object?> jsonObject(Object? value) {
  if (value is Map<String, Object?>) {
    return value;
  }

  if (value is Map) {
    return value.map((key, item) => MapEntry(key.toString(), item));
  }

  return <String, Object?>{};
}

List<Map<String, Object?>> jsonObjectList(Object? value) {
  if (value is Iterable) {
    return value.map(jsonObject).toList(growable: false);
  }

  return const [];
}

List<String> jsonStringList(Object? value) {
  if (value is Iterable) {
    return value
        .map((item) => item.toString().trim())
        .where((item) => item.isNotEmpty)
        .toList(growable: false);
  }

  return const [];
}

String jsonString(Object? value, {String fallback = ''}) {
  return value?.toString() ?? fallback;
}

int jsonInt(Object? value, {int fallback = 0}) {
  if (value is int) {
    return value;
  }

  if (value is num) {
    return value.toInt();
  }

  return int.tryParse(value?.toString() ?? '') ?? fallback;
}

double jsonDouble(Object? value, {double fallback = 0}) {
  if (value is num) {
    return value.toDouble();
  }

  return double.tryParse(value?.toString() ?? '') ?? fallback;
}

bool jsonBool(Object? value, {bool fallback = false}) {
  if (value is bool) {
    return value;
  }

  return fallback;
}

DateTime? jsonDate(Object? value) {
  return DateTime.tryParse(value?.toString() ?? '');
}
