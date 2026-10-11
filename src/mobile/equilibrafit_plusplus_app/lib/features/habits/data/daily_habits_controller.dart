import 'package:flutter/foundation.dart';
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
    _initialLoad = _load();
  }

  static const _boxName = 'daily_habits';
  final HabitsRepository _repository;
  late final Future<void> _initialLoad;
  Future<void> _saveQueue = Future<void>.value();
  int _revision = 0;
  Box<dynamic>? _box;

  @visibleForTesting
  Future<void> get initialized => _initialLoad;

  Future<void> incrementWater() {
    return _update(
      (current) => current.copyWith(
        waterCups: (current.waterCups + 1).clamp(0, 20).toInt(),
      ),
    );
  }

  Future<void> decrementWater() {
    return _update(
      (current) => current.copyWith(
        waterCups: (current.waterCups - 1).clamp(0, 20).toInt(),
      ),
    );
  }

  Future<void> setSleepHours(double value) {
    return _update(
      (current) => current.copyWith(sleepHours: value.clamp(0, 14)),
    );
  }

  Future<void> setMood(int value) {
    return _update(
      (current) => current.copyWith(mood: value.clamp(1, 5).toInt()),
    );
  }

  Future<void> toggleMeditation(bool value) {
    return _update(
      (current) => current.copyWith(meditationDone: value),
    );
  }

  Future<void> toggleStretching(bool value) {
    return _update(
      (current) => current.copyWith(stretchingDone: value),
    );
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

  Future<void> _update(
    DailyHabits Function(DailyHabits current) update,
  ) async {
    await _initialLoad;

    final snapshot = update(state);
    state = snapshot;
    final revision = ++_revision;
    final operation = _saveQueue.then(
      (_) => _save(snapshot, revision),
    );
    _saveQueue = operation.catchError((Object _, StackTrace __) {});
    await operation;
  }

  Future<void> _save(DailyHabits snapshot, int revision) async {
    final box = await _openBox();
    final todayKey = _todayKey();
    await box.put(todayKey, snapshot.toJson());

    try {
      final remote = await _repository.saveDaily(
        date: todayKey,
        habits: snapshot,
      );
      if (revision == _revision) {
        state = remote;
        await box.put(todayKey, remote.toJson());
      }
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
