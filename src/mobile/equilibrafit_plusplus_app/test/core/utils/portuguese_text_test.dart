import 'package:equilibrafit_plusplus_app/core/utils/portuguese_text.dart';
import 'package:test/test.dart';

void main() {
  group('PortugueseText', () {
    test('composes common dead-key sequences', () {
      expect(PortugueseText.composeDeadKeys('P~ao com ovo'), 'Pão com ovo');
      expect(PortugueseText.composeDeadKeys('Refei,c~ao'), 'Refeição');
      expect(PortugueseText.composeDeadKeys('Jo~ao Nu~nez'), 'João Nuñez');
      expect(PortugueseText.composeDeadKeys('Voce^'), 'Voce^');
      expect(PortugueseText.composeDeadKeys('Voce'), 'Voce');
      expect(
        PortugueseText.composeDeadKeys('Voce esta bem?'),
        'Voce esta bem?',
      );
      expect(PortugueseText.composeDeadKeys('Voce ´e forte'), 'Voce é forte');
      expect(PortugueseText.composeDeadKeys('Acucar'), 'Acucar');
    });

    test('normalizes search without losing Portuguese matches', () {
      expect(PortugueseText.normalizeForSearch('Pão'), 'pao');
      expect(PortugueseText.normalizeForSearch('Refeição'), 'refeicao');
      expect(
        PortugueseText.normalizeForSearch('Água e Proteína'),
        'agua e proteina',
      );
    });
  });
}
