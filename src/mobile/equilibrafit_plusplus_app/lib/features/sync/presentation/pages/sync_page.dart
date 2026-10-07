import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../../../core/errors/app_failure.dart';
import '../../../../core/sync/sync_engine.dart';
import '../../../../core/sync/sync_service.dart';
import '../../../../core/sync/sync_status.dart';
import '../../../../design_system/tokens/app_tokens.dart';
import '../../../../design_system/widgets/ef_button.dart';
import '../../../../design_system/widgets/ef_empty_state.dart';
import '../../../../design_system/widgets/ef_feedback_banner.dart';
import '../../../../design_system/widgets/ef_scaffold.dart';

class SyncPage extends ConsumerWidget {
  const SyncPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final summary = ref.watch(syncSummaryProvider);
    final operations = ref.watch(syncOperationsProvider);

    return EfScaffold(
      title: 'Sincronização',
      subtitle: 'Dados salvos neste aparelho e envio para a API.',
      actions: [
        IconButton(
          tooltip: 'Atualizar',
          onPressed: () => _refresh(ref),
          icon: const Icon(Icons.refresh),
        ),
        TextButton(
          onPressed: () => context.go('/dashboard'),
          child: const Text('Voltar'),
        ),
      ],
      body: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          summary.when(
            data: (value) => _SummaryCard(
              summary: value,
              onSyncNow: () => _syncNow(context, ref),
            ),
            loading: () => const LinearProgressIndicator(),
            error: (error, stackTrace) => const EfFeedbackBanner(
              tone: EfFeedbackTone.warning,
              title: 'Sincronização indisponível',
              message: 'Não foi possível ler a fila local agora.',
            ),
          ),
          const SizedBox(height: AppTokens.space16),
          operations.when(
            data: (items) {
              if (items.isEmpty) {
                return const EfEmptyState(
                  icon: Icons.cloud_done_outlined,
                  title: 'Nada pendente',
                  message:
                      'As operações locais já foram enviadas ou não há alterações offline.',
                );
              }

              return Column(
                children: [
                  for (final item in items) ...[
                    _SyncOperationTile(
                      item: item,
                      onRetry: () => _retry(
                        context,
                        ref,
                        item.id,
                      ),
                    ),
                    const SizedBox(height: AppTokens.space8),
                  ],
                ],
              );
            },
            loading: () => const Center(
              child: CircularProgressIndicator(),
            ),
            error: (error, stackTrace) => const EfEmptyState(
              icon: Icons.error_outline,
              title: 'Fila indisponível',
              message: 'Não foi possível carregar as operações locais.',
            ),
          ),
        ],
      ),
    );
  }

  Future<void> _syncNow(
    BuildContext context,
    WidgetRef ref,
  ) async {
    try {
      await ref.read(syncEngineProvider).syncPendingOnce();

      if (!context.mounted) {
        return;
      }

      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text(
            'Sincronização concluída.',
          ),
        ),
      );
    } on AppFailure catch (failure) {
      if (!context.mounted) {
        return;
      }

      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(failure.message),
        ),
      );
    } catch (error) {
      if (!context.mounted) {
        return;
      }

      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            'Não foi possível sincronizar agora. '
            'Detalhe: $error',
          ),
        ),
      );
    } finally {
      _refresh(ref);
    }
  }

  Future<void> _retry(
    BuildContext context,
    WidgetRef ref,
    String id,
  ) async {
    try {
      await ref.read(syncServiceProvider).retry(id);
      await ref.read(syncEngineProvider).syncPendingOnce();

      if (!context.mounted) {
        return;
      }

      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text(
            'Nova tentativa de sincronização concluída.',
          ),
        ),
      );
    } on AppFailure catch (failure) {
      if (!context.mounted) {
        return;
      }

      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(failure.message),
        ),
      );
    } catch (error) {
      if (!context.mounted) {
        return;
      }

      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(
            'Não foi possível repetir a sincronização. '
            'Detalhe: $error',
          ),
        ),
      );
    } finally {
      _refresh(ref);
    }
  }

  void _refresh(WidgetRef ref) {
    ref.invalidate(syncSummaryProvider);
    ref.invalidate(syncOperationsProvider);
  }
}

class _SummaryCard extends StatelessWidget {
  const _SummaryCard({
    required this.summary,
    required this.onSyncNow,
  });

  final SyncSummary summary;
  final Future<void> Function() onSyncNow;

  @override
  Widget build(BuildContext context) {
    final tone = summary.issueCount > 0
        ? EfFeedbackTone.warning
        : summary.hasWork
            ? EfFeedbackTone.support
            : EfFeedbackTone.success;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        EfFeedbackBanner(
          tone: tone,
          title: _title(summary),
          message: _message(summary),
        ),
        const SizedBox(height: AppTokens.space12),
        Wrap(
          spacing: AppTokens.space8,
          runSpacing: AppTokens.space8,
          children: [
            _CountChip(
              label: 'Pendentes',
              value: summary.pending,
            ),
            _CountChip(
              label: 'Enviando',
              value: summary.syncing,
            ),
            _CountChip(
              label: 'Falhas',
              value: summary.failed,
            ),
            _CountChip(
              label: 'Conflitos',
              value: summary.conflict,
            ),
          ],
        ),
        const SizedBox(height: AppTokens.space16),
        EfButton(
          label:
              summary.hasWork ? 'Sincronizar agora' : 'Verificar sincronização',
          icon: Icons.sync,
          onPressed: onSyncNow,
        ),
      ],
    );
  }

  String _title(SyncSummary summary) {
    if (summary.conflict > 0) {
      return 'Conflito precisa de revisão';
    }

    if (summary.failed > 0) {
      return 'Há itens com falha';
    }

    if (summary.pending > 0 || summary.syncing > 0) {
      return 'Aguardando sincronização';
    }

    return 'Tudo sincronizado';
  }

  String _message(SyncSummary summary) {
    if (summary.conflict > 0) {
      return 'Alguns dados foram alterados tanto no servidor '
          'quanto neste aparelho. Eles permanecem preservados '
          'até que o conflito seja revisado.';
    }

    if (summary.failed > 0) {
      return 'Algumas operações não puderam ser enviadas. '
          'Você pode tentar novamente quando a conexão estiver estável.';
    }

    if (summary.pending > 0 || summary.syncing > 0) {
      return 'As alterações feitas offline continuam salvas '
          'neste aparelho e serão enviadas para a API.';
    }

    return 'Não há operações offline aguardando envio.';
  }
}

class _CountChip extends StatelessWidget {
  const _CountChip({
    required this.label,
    required this.value,
  });

  final String label;
  final int value;

  @override
  Widget build(BuildContext context) {
    return Chip(
      avatar: CircleAvatar(
        child: Text('$value'),
      ),
      label: Text(label),
    );
  }
}

class _SyncOperationTile extends StatelessWidget {
  const _SyncOperationTile({
    required this.item,
    required this.onRetry,
  });

  final SyncOutboxItem item;
  final Future<void> Function() onRetry;

  @override
  Widget build(BuildContext context) {
    final status = _status(item.status);

    // Conflito não deve ser reenviado automaticamente.
    // Apenas falhas comuns podem receber retry manual.
    final canRetry = status == SyncStatus.failed;

    return Card(
      child: ListTile(
        leading: Icon(
          _icon(status),
          color: _color(context, status),
        ),
        title: Text(
          _operationLabel(item.operation),
        ),
        subtitle: Text(
          _subtitle(item),
        ),
        trailing: canRetry
            ? IconButton(
                tooltip: 'Tentar novamente',
                onPressed: onRetry,
                icon: const Icon(Icons.refresh),
              )
            : Text(
                _statusLabel(status),
                textAlign: TextAlign.end,
              ),
      ),
    );
  }

  String _subtitle(SyncOutboxItem item) {
    final entity =
        item.entityType.trim().isEmpty ? 'Item' : _entityLabel(item.entityType);

    final error = item.lastError?.trim();

    if (error != null && error.isNotEmpty) {
      return '$entity · $error';
    }

    return '$entity · operação ${item.operationId}';
  }

  String _entityLabel(String entityType) {
    return switch (entityType) {
      'WorkoutExercise' => 'Exercício do treino',
      'WorkoutSession' => 'Sessão de treino',
      'WorkoutSet' => 'Série do treino',
      'Workout' => 'Treino',
      'Meal' => 'Refeição',
      'Weight' => 'Peso',
      'Habit' => 'Hábito',
      _ => entityType,
    };
  }

  SyncStatus _status(String value) {
    return SyncStatus.values.firstWhere(
      (item) => item.value == value,
      orElse: () => SyncStatus.pending,
    );
  }

  IconData _icon(SyncStatus status) {
    return switch (status) {
      SyncStatus.synced => Icons.check_circle_outline,
      SyncStatus.syncing => Icons.sync,
      SyncStatus.failed => Icons.error_outline,
      SyncStatus.conflict => Icons.warning_amber_outlined,
      SyncStatus.pending => Icons.schedule,
    };
  }

  Color _color(
    BuildContext context,
    SyncStatus status,
  ) {
    return switch (status) {
      SyncStatus.synced => AppTokens.success,
      SyncStatus.syncing => Theme.of(context).colorScheme.primary,
      SyncStatus.failed => Theme.of(context).colorScheme.error,
      SyncStatus.conflict => AppTokens.warning,
      SyncStatus.pending => Theme.of(context).colorScheme.outline,
    };
  }

  String _statusLabel(SyncStatus status) {
    return switch (status) {
      SyncStatus.synced => 'Sincronizado',
      SyncStatus.syncing => 'Enviando',
      SyncStatus.failed => 'Falhou',
      SyncStatus.conflict => 'Conflito',
      SyncStatus.pending => 'Pendente',
    };
  }

  String _operationLabel(String operation) {
    return switch (operation) {
      'workout_session.register' => 'Registrar sessão de treino',
      'workout_exercise.complete' => 'Conclusão de exercício',
      'workout_exercise.add' => 'Adicionar exercício',
      'workout_exercise.update' => 'Editar exercício',
      'workout_exercise.remove' => 'Remover exercício',
      'weight.register' => 'Registrar peso',
      'meal.register' => 'Registrar refeição',
      'meal.update' => 'Editar refeição',
      'meal.remove' => 'Remover refeição',
      'habit.register' => 'Registrar hábito',
      _ => _humanizeOperation(operation),
    };
  }

  String _humanizeOperation(String value) {
    if (value.trim().isEmpty) {
      return 'Operação';
    }

    return value.replaceAll('_', ' ').replaceAll('.', ' · ');
  }
}
