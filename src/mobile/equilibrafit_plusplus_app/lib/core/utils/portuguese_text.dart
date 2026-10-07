/// Utilities for Portuguese text normalization and keyboard composition.
final class PortugueseText {
  PortugueseText._();

  static const Map<String, String> _deadKeyCompositions = {
    '~a': 'ã',
    '~A': 'Ã',
    '~o': 'õ',
    '~O': 'Õ',
    '~n': 'ñ',
    '~N': 'Ñ',
    '^a': 'â',
    '^A': 'Â',
    '^e': 'ê',
    '^E': 'Ê',
    '^i': 'î',
    '^I': 'Î',
    '^o': 'ô',
    '^O': 'Ô',
    '^u': 'û',
    '^U': 'Û',
    '\'a': 'á',
    '\'A': 'Á',
    '\'e': 'é',
    '\'E': 'É',
    '\'i': 'í',
    '\'I': 'Í',
    '\'o': 'ó',
    '\'O': 'Ó',
    '\'u': 'ú',
    '\'U': 'Ú',
    '´a': 'á',
    '´A': 'Á',
    '´e': 'é',
    '´E': 'É',
    '´i': 'í',
    '´I': 'Í',
    '´o': 'ó',
    '´O': 'Ó',
    '´u': 'ú',
    '´U': 'Ú',
    '`a': 'à',
    '`A': 'À',
    '`e': 'è',
    '`E': 'È',
    '`i': 'ì',
    '`I': 'Ì',
    '`o': 'ò',
    '`O': 'Ò',
    '`u': 'ù',
    '`U': 'Ù',
    '"a': 'ä',
    '"A': 'Ä',
    '"e': 'ë',
    '"E': 'Ë',
    '"i': 'ï',
    '"I': 'Ï',
    '"o': 'ö',
    '"O': 'Ö',
    '"u': 'ü',
    '"U': 'Ü',
    ',c': 'ç',
    ',C': 'Ç',
  };

  static const Map<String, String> _combiningCompositions = {
    'a\u0303': 'ã',
    'A\u0303': 'Ã',
    'o\u0303': 'õ',
    'O\u0303': 'Õ',
    'a\u0302': 'â',
    'A\u0302': 'Â',
    'e\u0302': 'ê',
    'E\u0302': 'Ê',
    'i\u0302': 'î',
    'I\u0302': 'Î',
    'o\u0302': 'ô',
    'O\u0302': 'Ô',
    'u\u0302': 'û',
    'U\u0302': 'Û',
    'a\u0301': 'á',
    'A\u0301': 'Á',
    'e\u0301': 'é',
    'E\u0301': 'É',
    'i\u0301': 'í',
    'I\u0301': 'Í',
    'o\u0301': 'ó',
    'O\u0301': 'Ó',
    'u\u0301': 'ú',
    'U\u0301': 'Ú',
    'a\u0300': 'à',
    'A\u0300': 'À',
    'e\u0300': 'è',
    'E\u0300': 'È',
    'i\u0300': 'ì',
    'I\u0300': 'Ì',
    'o\u0300': 'ò',
    'O\u0300': 'Ò',
    'u\u0300': 'ù',
    'U\u0300': 'Ù',
    'u\u0308': 'ü',
    'U\u0308': 'Ü',
    'c\u0327': 'ç',
    'C\u0327': 'Ç',
  };

  static const Map<String, String> _searchReplacements = {
    'á': 'a',
    'à': 'a',
    'â': 'a',
    'ã': 'a',
    'ä': 'a',
    'å': 'a',
    'é': 'e',
    'è': 'e',
    'ê': 'e',
    'ë': 'e',
    'í': 'i',
    'ì': 'i',
    'î': 'i',
    'ï': 'i',
    'ó': 'o',
    'ò': 'o',
    'ô': 'o',
    'õ': 'o',
    'ö': 'o',
    'ú': 'u',
    'ù': 'u',
    'û': 'u',
    'ü': 'u',
    'ç': 'c',
    'ñ': 'n',
  };

  static const Set<String> _combiningMarks = {
    '\u0300',
    '\u0301',
    '\u0302',
    '\u0303',
    '\u0308',
    '\u0327',
  };

  /// Composes common Portuguese dead-key sequences such as `~a` into `ã`.
  static String composeDeadKeys(String value) {
    if (value.length < 2) {
      return value;
    }

    final buffer = StringBuffer();
    var index = 0;
    while (index < value.length) {
      if (index + 1 < value.length) {
        final pair = value.substring(index, index + 2);
        final composed =
            _deadKeyCompositions[pair] ?? _combiningCompositions[pair];
        if (composed != null) {
          buffer.write(composed);
          index += 2;
          continue;
        }
      }

      buffer.write(value[index]);
      index++;
    }

    return buffer.toString();
  }

  /// Lowercases and removes Portuguese diacritics for accent-insensitive search.
  static String normalizeForSearch(String value) {
    final buffer = StringBuffer();
    for (final char in value.toLowerCase().split('')) {
      if (_combiningMarks.contains(char)) {
        continue;
      }

      buffer.write(_searchReplacements[char] ?? char);
    }

    return buffer.toString();
  }
}
