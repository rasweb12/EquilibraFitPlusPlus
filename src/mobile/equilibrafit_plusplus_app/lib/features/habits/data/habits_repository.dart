import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/http/api_client.dart';
import '../domain/daily_habits.dart';

final habitsRepositoryProvider = Provider<HabitsRepository>((ref) {
  return HabitsRepository(ref.watch(apiClientProvider));
});

class HabitsRepository {
  const HabitsRepository(this._client);

  final ApiClient _client;

  Future<DailyHabits> fetchDaily({required String date}) async {
    final json = await _client.getJson(
      '/api/v1/habitos/diario',
      query: <String, Object?>{'data': date},
    );

    return DailyHabits.fromApiJson(json);
  }

  Future<DailyHabits> saveDaily({
    required String date,
    required DailyHabits habits,
  }) async {
    final json = await _client.putJson(
      '/api/v1/habitos/diario',
      body: habits.toApiJson(date: date),
    );

    return DailyHabits.fromApiJson(json);
  }
}
