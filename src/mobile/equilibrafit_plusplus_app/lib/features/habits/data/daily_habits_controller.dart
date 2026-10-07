import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:hive_flutter/hive_flutter.dart';

import 'habits_repository.dart';
import '../domain/daily_habits.dart';

final dailyHabitsProvider =
    StateNotifierProvider<DailyHabitsController, DailyHabits>((ref) {
  return DailyHabitsController(ref.watch(habitsRepositoryProvider));
});

class DailyHabitsController extends StateNotifier<DailyHabits> {
  DailyHabitsController(this._repository) : super(const DailyHabits()) {
    _load();
  }

  static const _boxName = 'daily_habits';
  final HabitsRepository _repository;
  Box<dynamic>? _box;

  Future<void> incrementWater() async {
    state = state.copyWith(
      waterCups: (state.waterCups + 1).clamp(0, 20).toInt(),
    );
    await _save();
  }

  Future<void> decrementWater() async {
    state = state.copyWith(
      waterCups: (state.waterCups - 1).clamp(0, 20).toInt(),
    );
    await _save();
  }

  Future<void> setSleepHours(double value) async {
    state = state.copyWith(sleepHours: value.clamp(0, 14));
    await _save();
  }

  Future<void> setMood(int value) async {
    state = state.copyWith(mood: value.clamp(1, 5).toInt());
    await _save();
  }

  Future<void> toggleMeditation(bool value) async {
    state = state.copyWith(meditationDone: value);
    await _save();
  }

  Future<void> toggleStretching(bool value) async {
    state = state.copyWith(stretchingDone: value);
    await _save();
  }

  Future<void> _load() async {
    final box = await _openBox();
    final todayKey = _todayKey();
    final raw = box.get(todayKey);
    if (raw is Map<dynamic, dynamic>) {
      state = DailyHabits.fromJson(raw);
    }

    try {
      final remote = await _repository.fetchDaily(date: todayKey);
      state = remote;
      await box.put(todayKey, remote.toJson());
    } catch (_) {
      // Offline-first: local habits remain available when the API is unreachable.
    }
  }

  Future<void> _save() async {
    final box = await _openBox();
    final todayKey = _todayKey();
    await box.put(todayKey, state.toJson());

    try {
      final remote = await _repository.saveDaily(date: todayKey, habits: state);
      state = remote;
      await box.put(todayKey, remote.toJson());
    } catch (_) {
      // The local value is the source of truth until the next successful sync.
    }
  }

  Future<Box<dynamic>> _openBox() async {
    final existing = _box;
    if (existing != null && existing.isOpen) {
      return existing;
    }

    _box = await Hive.openBox<dynamic>(_boxName);
    return _box!;
  }

  String _todayKey() {
    final now = DateTime.now();
    final year = now.year.toString().padLeft(4, '0');
    final month = now.month.toString().padLeft(2, '0');
    final day = now.day.toString().padLeft(2, '0');
    return '$year-$month-$day';
  }
}
