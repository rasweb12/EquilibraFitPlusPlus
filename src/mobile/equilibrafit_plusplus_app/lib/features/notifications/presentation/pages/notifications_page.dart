import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/errors/app_failure.dart';
import '../../../../core/utils/formatters.dart';
import '../../../../design_system/tokens/app_tokens.dart';
import '../../../../design_system/widgets/ef_empty_state.dart';
import '../../../../design_system/widgets/ef_feedback_banner.dart';
import '../../../../design_system/widgets/ef_scaffold.dart';
import '../../data/notification_repository.dart';
import '../../domain/app_notification.dart';

class NotificationsPage extends ConsumerWidget {
  const NotificationsPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final notifications = ref.watch(notificationsProvider);

    return EfScaffold(
      title: 'Notificações',
      subtitle: 'Mensagens importantes para sua rotina.',
      actions: [
        TextButton(
          onPressed: () => context.go('/dashboard'),
          child: const Text('Voltar'),
        ),
        IconButton(
          tooltip: 'Atualizar',
          onPressed: () => ref.invalidate(notificationsProvider),
          icon: const Icon(Icons.refresh),
        ),
      ],
      body: notifications.when(
        data: (items) {
          if (items.isEmpty) {
            return EfEmptyState(
              icon: Icons.notifications_none_outlined,
              title: 'Nenhuma notificação por enquanto',
              message: 'Quando houver avisos importantes, eles aparecem aqui.',
              actionLabel: 'Voltar ao início',
              onAction: () => context.go('/dashboard'),
            );
          }

          return Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              EfFeedbackBanner(
                title: 'Caixa de entrada',
                message:
                    '${items.where((item) => !item.isRead).length} notificações pendentes.',
              ),
              const SizedBox(height: AppTokens.space16),
              for (final item in items) ...[
                _NotificationCard(
                  notification: item,
                  onMarkAsRead: item.isRead
                      ? null
                      : () => _markAsRead(context, ref, item),
                ),
                const SizedBox(height: AppTokens.space12),
              ],
            ],
          );
        },
        loading: () => const Center(
          child: Padding(
            padding: EdgeInsets.all(AppTokens.space32),
            child: CircularProgressIndicator(),
          ),
        ),
        error: (error, stackTrace) => EfEmptyState(
          icon: Icons.wifi_off_outlined,
          title: 'Não conseguimos carregar agora',
          message: 'Sem problemas. Podemos tentar novamente em instantes.',
          actionLabel: 'Tentar novamente',
          onAction: () => ref.invalidate(notificationsProvider),
        ),
      ),
    );
  }

  Future<void> _markAsRead(
    BuildContext context,
    WidgetRef ref,
    AppNotification notification,
  ) async {
    try {
      await ref
          .read(notificationRepositoryProvider)
          .markAsRead(notification.id);
      ref.invalidate(notificationsProvider);
    } on AppFailure catch (failure) {
      if (context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(failure.message)),
        );
      }
    }
  }
}

class _NotificationCard extends StatelessWidget {
  const _NotificationCard({
    required this.notification,
    required this.onMarkAsRead,
  });

  final AppNotification notification;
  final VoidCallback? onMarkAsRead;

  @override
  Widget build(BuildContext context) {
    final colors = Theme.of(context).colorScheme;

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(AppTokens.space16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Icon(
                  notification.isRead
                      ? Icons.mark_email_read_outlined
                      : Icons.notifications_active_outlined,
                  color: notification.isRead ? colors.outline : colors.primary,
                ),
                const SizedBox(width: AppTokens.space12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        notification.title,
                        style: Theme.of(context).textTheme.titleMedium,
                      ),
                      const SizedBox(height: AppTokens.space4),
                      Text(
                        _formatDateTime(notification.createdAt),
                        style: Theme.of(context).textTheme.bodySmall,
                      ),
                    ],
                  ),
                ),
                Chip(
                  label: Text(notification.isRead ? 'Lida' : 'Pendente'),
                ),
              ],
            ),
            const SizedBox(height: AppTokens.space12),
            Text(notification.message),
            if (onMarkAsRead != null) ...[
              const SizedBox(height: AppTokens.space12),
              Align(
                alignment: Alignment.centerRight,
                child: TextButton.icon(
                  onPressed: onMarkAsRead,
                  icon: const Icon(Icons.done),
                  label: const Text('Marcar como lida'),
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }

  String _formatDateTime(DateTime value) {
    final hour = value.hour.toString().padLeft(2, '0');
    final minute = value.minute.toString().padLeft(2, '0');
    return '${formatDate(value.toLocal())} $hour:$minute';
  }
}
