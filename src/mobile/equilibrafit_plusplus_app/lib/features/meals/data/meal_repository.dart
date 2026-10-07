import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/ai/ai_pending_request_service.dart';
import '../../../core/errors/app_failure.dart';
import '../../../core/http/api_client.dart';
import '../../../core/utils/json_helpers.dart';
import '../../../core/utils/local_id_generator.dart';
import '../../auth/presentation/controllers/session_controller.dart';
import '../domain/meal_entry.dart';

final mealRepositoryProvider = Provider<MealRepository>((ref) {
  final usuarioId = ref.watch(
    sessionControllerProvider.select((state) => state.user?.id),
  );
  final tenantId = ref.watch(
    sessionControllerProvider.select((state) => state.user?.tenantId),
  );

  return MealRepository(
    ref.watch(apiClientProvider),
    pendingRequestService: ref.watch(aiPendingRequestServiceProvider),
    tenantId: tenantId,
    usuarioId: usuarioId,
  );
});

final mealsProvider = FutureProvider.autoDispose<List<MealEntry>>((ref) {
  return ref.watch(mealRepositoryProvider).listToday();
});

class MealRepository {
  const MealRepository(
    this._client, {
    AiPendingRequestStore? pendingRequestService,
    String? tenantId,
    String? usuarioId,
  })  : _pendingRequestService = pendingRequestService,
        _tenantId = tenantId ?? 'default',
        _usuarioId = usuarioId;

  final ApiClient _client;
  final AiPendingRequestStore? _pendingRequestService;
  final String _tenantId;
  final String? _usuarioId;

  Future<List<MealEntry>> listToday() async {
    final today = DateTime.now();
    final date = _dateOnly(today);
    final json = await _client.getJson(
      '/api/v1/alimentacao',
      query: <String, Object?>{
        'inicio': date,
        'fim': date,
        'page': 1,
        'pageSize': 30,
      },
    );

    final items = jsonObjectList(json['items'] ?? json['Items']);
    return items.map(_fromJson).toList(growable: false);
  }

  Future<MealEntry> getById(String id) async {
    final json = await _client.getJson('/api/v1/alimentacao/$id');
    return _fromJson(json);
  }

  Future<MealEntry> create({
    required String type,
    required List<MealItem> items,
  }) async {
    final json = await _client.postJson(
      '/api/v1/alimentacao',
      body: <String, Object?>{
        'dataHora': DateTime.now().toUtc().toIso8601String(),
        'tipoRefeicao': type,
        'itens': items.map((item) => item.toRequestJson()).toList(),
        'confirmadoPeloUsuario': true,
      },
    );

    return _fromJson(json);
  }

  Future<MealEntry> update({
    required String id,
    required String type,
    required DateTime dateTime,
    required List<MealItem> items,
  }) async {
    final json = await _client.putJson(
      '/api/v1/alimentacao/$id',
      body: <String, Object?>{
        'dataHora': dateTime.toUtc().toIso8601String(),
        'tipoRefeicao': type,
        'itens': items.map((item) => item.toRequestJson()).toList(),
        'confirmadoPeloUsuario': true,
      },
    );

    return _fromJson(json);
  }

  Future<MealEstimation> estimateText({
    required String description,
    required String type,
  }) async {
    final body = <String, Object?>{
      'descricao': description,
      'tipoRefeicao': type,
    };
    final Map<String, Object?> json;

    try {
      json = await _client.postJson(
        '/api/v1/alimentacao/estimar-texto',
        body: body,
        timeout: ApiClient.aiRequestTimeout,
      );
    } on AppFailure catch (failure) {
      if (failure.code != 'network_unavailable') {
        rethrow;
      }

      return _savePendingTextEstimation(body);
    }

    return _estimationFromJson(json);
  }

  Future<MealEstimation> _savePendingTextEstimation(
    Map<String, Object?> body,
  ) async {
    final usuarioId = _usuarioId;
    final pendingRequestService = _pendingRequestService;

    if (usuarioId == null ||
        usuarioId.trim().isEmpty ||
        pendingRequestService == null) {
      throw const AppFailure(
        'Essa análise precisa de internet. Preencha os itens manualmente e tente novamente quando estiver online.',
        code: 'ai_requires_connection',
      );
    }

    await pendingRequestService.enqueue(
      operationId: newLocalOperationId(),
      tenantId: _tenantId,
      usuarioId: usuarioId,
      type: 'meal.text_estimation',
      payload: <String, Object?>{
        'path': '/api/v1/alimentacao/estimar-texto',
        'body': body,
      },
    );

    return const MealEstimation(
      items: [],
      message:
          'Essa análise precisa de internet. Salvei a solicitação para processar quando a conexão voltar; você pode preencher os itens manualmente agora.',
      model: 'pending',
      fallbackUsed: true,
    );
  }

  Future<MealEstimation> recognizeImage({
    required String imageBase64,
    required String type,
  }) async {
    final json = await _client.postJson(
      '/api/v1/alimentacao/reconhecer-refeicao',
      body: <String, Object?>{
        'imageBase64': imageBase64,
        'tipoRefeicao': type,
        'contexto': 'Foto carregada da galeria pelo usuario.',
      },
      timeout: ApiClient.aiRequestTimeout,
    );

    return MealEstimation(
      items: jsonObjectList(
        json['itens'] ?? json['Itens'],
      ).map(_itemFromJson).toList(growable: false),
      message: jsonString(json['mensagem'] ?? json['Mensagem']),
      model: jsonString(json['modelo'] ?? json['Modelo']),
      fallbackUsed: jsonBool(json['usouFallback'] ?? json['UsouFallback']),
    );
  }

  Future<void> delete(String id) {
    return _client.delete('/api/v1/alimentacao/$id');
  }

  MealEstimation _estimationFromJson(Map<String, Object?> json) {
    return MealEstimation(
      items: jsonObjectList(
        json['itens'] ?? json['Itens'],
      ).map(_itemFromJson).toList(growable: false),
      message: jsonString(json['mensagem'] ?? json['Mensagem']),
      model: jsonString(json['modelo'] ?? json['Modelo']),
      fallbackUsed: jsonBool(json['usouFallback'] ?? json['UsouFallback']),
    );
  }

  MealEntry _fromJson(Map<String, Object?> json) {
    return MealEntry(
      id: jsonString(json['id'] ?? json['Id']),
      dateTime:
          jsonDate(json['dataHora'] ?? json['DataHora']) ?? DateTime.now(),
      type: jsonString(json['tipoRefeicao'] ?? json['TipoRefeicao']),
      origin: jsonString(json['origem'] ?? json['Origem']),
      calories: jsonDouble(json['caloriasTotal'] ?? json['CaloriasTotal']),
      proteinG: jsonDouble(json['proteinaTotalG'] ?? json['ProteinaTotalG']),
      carbsG: jsonDouble(
        json['carboidratoTotalG'] ?? json['CarboidratoTotalG'],
      ),
      fatG: jsonDouble(json['gorduraTotalG'] ?? json['GorduraTotalG']),
      confirmedByUser: jsonBool(
        json['confirmadoPeloUsuario'] ?? json['ConfirmadoPeloUsuario'],
      ),
      items: jsonObjectList(
        json['itens'] ?? json['Itens'],
      ).map(_itemFromJson).toList(growable: false),
      message: jsonString(json['mensagem'] ?? json['Mensagem']),
    );
  }

  MealItem _itemFromJson(Map<String, Object?> json) {
    return MealItem(
      id: jsonString(json['id'] ?? json['Id']),
      name: jsonString(json['nome'] ?? json['Nome']),
      quantity: jsonDouble(json['quantidade'] ?? json['Quantidade']),
      unit: jsonString(json['unidade'] ?? json['Unidade']),
      calories: jsonDouble(json['calorias'] ?? json['Calorias']),
      proteinG: jsonDouble(json['proteinaG'] ?? json['ProteinaG']),
      carbsG: jsonDouble(json['carboidratoG'] ?? json['CarboidratoG']),
      fatG: jsonDouble(json['gorduraG'] ?? json['GorduraG']),
      source: jsonString(json['fonteNutricional'] ?? json['FonteNutricional']),
    );
  }

  String _dateOnly(DateTime value) {
    final year = value.year.toString().padLeft(4, '0');
    final month = value.month.toString().padLeft(2, '0');
    final day = value.day.toString().padLeft(2, '0');
    return '$year-$month-$day';
  }
}
