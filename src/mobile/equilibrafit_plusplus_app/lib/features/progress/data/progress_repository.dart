import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/http/api_client.dart';

final progressRepositoryProvider = Provider<ProgressRepository>((ref) {
  return ProgressRepository(ref.watch(apiClientProvider));
});

class ProgressRepository {
  const ProgressRepository(this._client);

  final ApiClient _client;

  Future<void> saveWeight({
    required double weightKg,
    String? note,
  }) async {
    await _client.postJson(
      '/api/v1/evolucao/peso',
      body: <String, Object?>{
        'data': _dateOnly(DateTime.now()),
        'pesoKg': weightKg,
        'percentualGordura': null,
        'percentualMassaMagra': null,
        'observacao': note,
      },
    );
  }

  String _dateOnly(DateTime value) {
    final year = value.year.toString().padLeft(4, '0');
    final month = value.month.toString().padLeft(2, '0');
    final day = value.day.toString().padLeft(2, '0');
    return '$year-$month-$day';
  }
}
