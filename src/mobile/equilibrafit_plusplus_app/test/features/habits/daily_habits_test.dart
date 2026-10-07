import 'package:equilibrafit_plusplus_app/features/habits/domain/daily_habits.dart';
import 'package:test/test.dart';

void main() {
  group('DailyHabits', () {
    test('serializes daily habits using backend contract names', () {
      const habits = DailyHabits(
        waterCups: 7,
        waterGoalMl: 2700,
        sleepHours: 7.5,
        sleepGoalHours: 8,
        mood: 4,
        meditationDone: true,
        stretchingDone: false,
      );

      expect(
        habits.toApiJson(date: '2026-08-08'),
        <String, Object?>{
          'data': '2026-08-08',
          'aguaMl': 1750,
          'metaAguaMl': 2700,
          'sonoHoras': 7.5,
          'metaSonoHoras': 8.0,
          'humor': 4,
          'meditacaoRealizada': true,
          'alongamentoRealizado': false,
        },
      );
    });

    test('maps backend daily habits response to local model', () {
      final habits = DailyHabits.fromApiJson(<String, Object?>{
        'aguaMl': 1800,
        'metaAguaMl': 2700,
        'sonoHoras': 6.5,
        'metaSonoHoras': 8,
        'humor': 5,
        'meditacaoRealizada': false,
        'alongamentoRealizado': true,
      });

      expect(habits.waterCups, 7);
      expect(habits.waterMl, 1800);
      expect(habits.waterGoalMl, 2700);
      expect(habits.sleepHours, 6.5);
      expect(habits.sleepGoalHours, 8);
      expect(habits.mood, 5);
      expect(habits.stretchingDone, true);
    });
  });
}
