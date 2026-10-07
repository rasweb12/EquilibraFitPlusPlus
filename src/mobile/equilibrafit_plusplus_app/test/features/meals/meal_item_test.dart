import 'package:equilibrafit_plusplus_app/features/meals/domain/meal_entry.dart';
import 'package:test/test.dart';

void main() {
  group('MealItem', () {
    test('serializes item using food log request names', () {
      const item = MealItem(
        name: 'Arroz',
        quantity: 120,
        unit: 'g',
        calories: 156,
        proteinG: 3,
        carbsG: 34,
        fatG: 0.4,
        source: 'Tabela nutricional',
      );

      expect(
        item.toRequestJson(),
        <String, Object?>{
          'nome': 'Arroz',
          'quantidade': 120,
          'unidade': 'g',
          'calorias': 156,
          'proteinaG': 3,
          'carboidratoG': 34,
          'gorduraG': 0.4,
          'fonteNutricional': 'Tabela nutricional',
        },
      );
    });
  });
}
