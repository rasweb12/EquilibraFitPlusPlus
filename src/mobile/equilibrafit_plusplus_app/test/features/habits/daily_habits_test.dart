import 'dart:io';

import 'package:equilibrafit_plusplus_app/features/habits/data/daily_habits_controller.dart';
import 'package:equilibrafit_plusplus_app/features/habits/data/habits_repository.dart';
import 'package:equilibrafit_plusplus_app/features/habits/domain/daily_habits.dart';
import 'package:hive_flutter/hive_flutter.dart';
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

    test('serializes rapid remote saves and keeps the newest water value',
        () async {
      final directory = await Directory.systemTemp.createTemp('habits_test_');
      Hive.init(directory.path);
      addTearDown(() async {
        await Hive.close();
        await directory.delete(recursive: true);
      });
      final repository = _DelayedHabitsRepository();
      final controller = DailyHabitsController(repository);
      addTearDown(controller.dispose);
      await controller.initialized;

      final first = controller.incrementWater();
      final second = controller.incrementWater();
      await Future.wait(<Future<void>>[first, second]);

      expect(repository.maxConcurrentSaves, 1);
      expect(repository.savedWaterMl, <int>[250, 500]);
      expect(controller.state.waterMl, 500);
    });
  });
}

class _DelayedHabitsRepository implements HabitsRepository {
  int _activeSaves = 0;
  int maxConcurrentSaves = 0;
  final List<int> savedWaterMl = <int>[];

  @override
  Future<DailyHabits> fetchDaily({required String date}) async {
    return const DailyHabits();
  }

  @override
  Future<DailyHabits> saveDaily({
    required String date,
    required DailyHabits habits,
  }) async {
    _activeSaves++;
    if (_activeSaves > maxConcurrentSaves) {
      maxConcurrentSaves = _activeSaves;
    }
    savedWaterMl.add(habits.waterMl);
    await Future<void>.delayed(const Duration(milliseconds: 20));
    _activeSaves--;
    return habits;
  }
}
