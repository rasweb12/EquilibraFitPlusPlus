import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/http/api_client.dart';

final planRepositoryProvider = Provider<PlanRepository>((ref) {
  return PlanRepository(ref.watch(apiClientProvider));
});

class PlanRepository {
  const PlanRepository(this._client);

  final ApiClient _client;

  Future<void> generate() async {
    await _client.postJson(
      '/api/v1/planos/alimentar/gerar',
      body: const <String, Object?>{
        'rotina': null,
        'preferencias': <String>[],
        'restricoes': <String>[],
      },
      timeout: ApiClient.aiRequestTimeout,
    );
  }
}
