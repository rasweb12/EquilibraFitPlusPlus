import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../core/http/api_client.dart';
import '../../../core/utils/json_helpers.dart';
import '../domain/app_notification.dart';

final notificationRepositoryProvider = Provider<NotificationRepository>((ref) {
  return NotificationRepository(ref.watch(apiClientProvider));
});

final notificationsProvider =
    FutureProvider.autoDispose<List<AppNotification>>((ref) {
  return ref.watch(notificationRepositoryProvider).list();
});

class NotificationRepository {
  const NotificationRepository(this._client);

  final ApiClient _client;

  Future<List<AppNotification>> list() async {
    final json = await _client.getJson(
      '/api/v1/notificacoes',
      query: const <String, Object?>{
        'page': 1,
        'pageSize': 50,
      },
    );

    final items = jsonObjectList(json['items'] ?? json['Items']);
    return items.map(_fromJson).toList(growable: false);
  }

  Future<AppNotification> markAsRead(String id) async {
    final json = await _client.putJson('/api/v1/notificacoes/$id/lida');
    return _fromJson(json);
  }

  AppNotification _fromJson(Map<String, Object?> json) {
    return AppNotification(
      id: jsonString(json['id'] ?? json['Id']),
      userId: jsonString(json['usuarioId'] ?? json['UsuarioId']),
      title: jsonString(json['titulo'] ?? json['Titulo']),
      message: jsonString(json['mensagem'] ?? json['Mensagem']),
      status: jsonString(json['status'] ?? json['Status']),
      createdAt:
          jsonDate(json['criadoEm'] ?? json['CriadoEm']) ?? DateTime.now(),
    );
  }
}
