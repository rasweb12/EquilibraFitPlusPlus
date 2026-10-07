import 'package:equilibrafit_plusplus_app/features/onboarding/domain/onboarding_answer.dart';
import 'package:test/test.dart';

void main() {
  group('OnboardingAnswer', () {
    test('serializes questionnaire using backend contract names', () {
      final answer = OnboardingAnswer(
        birthDate: DateTime.utc(1994, 7, 22),
        biologicalSex: 'Feminino',
        heightCm: 168,
        currentWeightKg: 72.5,
        goal: 'EmagrecimentoSustentavel',
        activityLevel: 'Moderado',
        trainingDaysPerWeek: 4,
        preferences: const ['arroz', 'feijao'],
        restrictions: const ['lactose'],
        notes: const ['rotina com almoco fora'],
      );

      expect(
        answer.toJson(),
        <String, Object?>{
          'dataNascimento': '1994-07-22',
          'sexoBiologico': 'Feminino',
          'alturaCm': 168,
          'pesoAtualKg': 72.5,
          'objetivo': 'EmagrecimentoSustentavel',
          'nivelAtividade': 'Moderado',
          'diasTreinoSemana': 4,
          'preferencias': const ['arroz', 'feijao'],
          'restricoes': const ['lactose'],
          'observacoes': const ['rotina com almoco fora'],
        },
      );
    });
  });
}
