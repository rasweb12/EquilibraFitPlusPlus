import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/http/api_client.dart';
import '../../../core/utils/json_helpers.dart';
import '../domain/dashboard_summary.dart';

final dashboardRepositoryProvider = Provider<DashboardRepository>((ref) {
  return DashboardRepository(ref.watch(apiClientProvider));
});

final dashboardSummaryProvider = FutureProvider.autoDispose<DashboardSummary>(
  (ref) => ref.watch(dashboardRepositoryProvider).getToday(),
);

class DashboardRepository {
  const DashboardRepository(this._client);

  final ApiClient _client;

  Future<DashboardSummary> getToday() async {
    final json = await _client.getJson('/api/v1/dashboard/me');
    return _fromJson(json);
  }

  DashboardSummary _fromJson(Map<String, Object?> json) {
    final food = jsonObject(json['alimentacao'] ?? json['Alimentacao']);
    final workout = jsonObject(json['treino'] ?? json['Treino']);
    final progress = jsonObject(json['evolucao'] ?? json['Evolucao']);
    final profile = jsonObject(json['perfil'] ?? json['Perfil']);
    final plan = jsonObject(json['planoAtual'] ?? json['PlanoAtual']);
    final date = jsonDate(json['data'] ?? json['Data']) ?? DateTime.now();

    return DashboardSummary(
      date: date,
      supportMessage: jsonString(
        json['mensagemApoio'] ?? json['MensagemApoio'],
        fallback: 'O importante e continuar. Vamos adaptar o dia com calma.',
      ),
      profile: profile.isEmpty
          ? null
          : DashboardProfile(
              currentWeightKg: jsonDouble(
                profile['pesoAtualKg'] ?? profile['PesoAtualKg'],
              ),
              heightCm: jsonDouble(profile['alturaCm'] ?? profile['AlturaCm']),
              bmi: jsonDouble(profile['imc'] ?? profile['Imc']),
              goal: jsonString(profile['objetivo'] ?? profile['Objetivo']),
            ),
      food: DashboardFood(
        calories: jsonDouble(
          food['caloriasConsumidas'] ?? food['CaloriasConsumidas'],
        ),
        proteinG: jsonDouble(food['proteinaG'] ?? food['ProteinaG']),
        carbsG: jsonDouble(food['carboidratoG'] ?? food['CarboidratoG']),
        fatG: jsonDouble(food['gorduraG'] ?? food['GorduraG']),
        meals: jsonInt(
          food['refeicoesRegistradas'] ?? food['RefeicoesRegistradas'],
        ),
        calorieGoal: _nullableDouble(
          food['metaCalorias'] ?? food['MetaCalorias'],
        ),
        calorieBalance: _nullableDouble(
          food['saldoCalorico'] ?? food['SaldoCalorico'],
        ),
      ),
      workout: DashboardWorkout(
        activeWorkouts: jsonInt(
          workout['treinosAtivos'] ?? workout['TreinosAtivos'],
        ),
        weeklyFrequency: jsonInt(
          workout['frequenciaSemanalPlanejada'] ??
              workout['FrequenciaSemanalPlanejada'],
        ),
        nextWorkout: _nullableString(
          workout['proximoTreino'] ?? workout['ProximoTreino'],
        ),
      ),
      progress: DashboardProgress(
        lastWeightKg: _nullableDouble(
          progress['ultimoPesoKg'] ?? progress['UltimoPesoKg'],
        ),
        lastUpdate: jsonDate(
          progress['ultimaAtualizacao'] ?? progress['UltimaAtualizacao'],
        ),
        weightVariationKg: _nullableDouble(
          progress['variacaoPesoKg'] ?? progress['VariacaoPesoKg'],
        ),
      ),
      currentPlan: plan.isEmpty
          ? null
          : DashboardPlan(
              id: jsonString(plan['id'] ?? plan['Id']),
              dailyCalories: jsonInt(
                plan['caloriasDia'] ?? plan['CaloriasDia'],
              ),
              goal: jsonString(plan['objetivo'] ?? plan['Objetivo']),
              explanation: jsonString(plan['explicacao'] ?? plan['Explicacao']),
              mealSuggestions: jsonObjectList(
                plan['sugestoesRefeicao'] ?? plan['SugestoesRefeicao'],
              ).map(_mealSuggestionFromJson).toList(growable: false),
            ),
    );
  }

  DashboardMealSuggestion _mealSuggestionFromJson(Map<String, Object?> json) {
    final substitutions = jsonObjectList(
      json['substituicoes'] ?? json['Substituicoes'],
    ).map(_substitutionFromJson).toList(growable: false);
    final recipe = jsonObject(json['receitaRapida'] ?? json['ReceitaRapida']);

    return DashboardMealSuggestion(
      name: jsonString(json['nome'] ?? json['Nome']),
      description: jsonString(json['descricao'] ?? json['Descricao']),
      calories: jsonInt(json['calorias'] ?? json['Calorias']),
      id: _nullableString(json['id'] ?? json['Id']),
      imageUrl: _nullableString(json['imagemUrl'] ?? json['ImagemUrl']),
      substitutions: substitutions,
      quickRecipe: recipe.isEmpty ? null : _recipeFromJson(recipe),
    );
  }

  DashboardMealSubstitution _substitutionFromJson(Map<String, Object?> json) {
    return DashboardMealSubstitution(
      index: jsonInt(json['indice'] ?? json['Indice']),
      name: jsonString(json['nome'] ?? json['Nome']),
      description: jsonString(json['descricao'] ?? json['Descricao']),
      calories: _nullableInt(json['calorias'] ?? json['Calorias']),
      ingredients: jsonObjectList(
        json['ingredientes'] ?? json['Ingredientes'],
      ).map(_ingredientFromJson).toList(growable: false),
    );
  }

  DashboardQuickRecipe _recipeFromJson(Map<String, Object?> json) {
    final preparation = json['preparo'] ?? json['Preparo'];
    return DashboardQuickRecipe(
      name: jsonString(json['nome'] ?? json['Nome']),
      ingredients: jsonObjectList(
        json['ingredientes'] ?? json['Ingredientes'],
      ).map(_ingredientFromJson).toList(growable: false),
      preparation: preparation is List
          ? preparation
              .whereType<Object?>()
              .map((item) => item?.toString().trim() ?? '')
              .where((item) => item.isNotEmpty)
              .toList(growable: false)
          : const <String>[],
      preparationMinutes: jsonInt(
        json['tempoPreparoMinutos'] ?? json['TempoPreparoMinutos'],
      ),
    );
  }

  DashboardRecipeIngredient _ingredientFromJson(Map<String, Object?> json) {
    return DashboardRecipeIngredient(
      name: jsonString(json['nome'] ?? json['Nome']),
      quantity: jsonString(json['quantidade'] ?? json['Quantidade']),
    );
  }

  double? _nullableDouble(Object? value) {
    if (value == null) {
      return null;
    }

    return jsonDouble(value);
  }

  String? _nullableString(Object? value) {
    final text = value?.toString().trim();
    return text == null || text.isEmpty ? null : text;
  }

  int? _nullableInt(Object? value) {
    if (value == null) {
      return null;
    }

    return jsonInt(value);
  }
}
