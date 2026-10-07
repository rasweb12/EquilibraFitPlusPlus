import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/http/api_client.dart';
import '../../../core/utils/json_helpers.dart';
import '../domain/onboarding_answer.dart';

final onboardingRepositoryProvider = Provider<OnboardingRepository>((ref) {
  return OnboardingRepository(ref.watch(apiClientProvider));
});

class OnboardingRepository {
  const OnboardingRepository(this._client);

  final ApiClient _client;

  Future<void> save(OnboardingAnswer answer) async {
    await _client.postJson('/api/v1/onboarding', body: answer.toJson());
  }

  Future<OnboardingAnswer?> getCurrent() async {
    final json = await _client.getJson('/api/v1/onboarding/me');
    if (json.isEmpty) {
      return null;
    }

    final birthDate = jsonDate(
      json['dataNascimento'] ?? json['DataNascimento'],
    );
    if (birthDate == null) {
      return null;
    }

    return OnboardingAnswer(
      birthDate: birthDate,
      biologicalSex: jsonString(
        json['sexoBiologico'] ?? json['SexoBiologico'],
      ),
      heightCm: jsonDouble(json['alturaCm'] ?? json['AlturaCm']),
      currentWeightKg: jsonDouble(
        json['pesoAtualKg'] ?? json['PesoAtualKg'],
      ),
      goal: jsonString(json['objetivo'] ?? json['Objetivo']),
      activityLevel: jsonString(
        json['nivelAtividade'] ?? json['NivelAtividade'],
      ),
      trainingDaysPerWeek: jsonInt(
        json['diasTreinoSemana'] ?? json['DiasTreinoSemana'],
      ),
      preferences: jsonStringList(
        json['preferencias'] ?? json['Preferencias'],
      ),
      restrictions: jsonStringList(
        json['restricoes'] ?? json['Restricoes'],
      ),
      notes: jsonStringList(json['observacoes'] ?? json['Observacoes']),
    );
  }
}
