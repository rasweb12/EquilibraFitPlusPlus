import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/sync/sync_service.dart';
import '../tokens/app_tokens.dart';

class EfScaffold extends ConsumerWidget {
  const EfScaffold({
    required this.title,
    required this.body,
    this.subtitle,
    this.actions = const [],
    this.currentIndex,
    this.onRefresh,
    super.key,
  });

  final String title;
  final String? subtitle;
  final Widget body;
  final List<Widget> actions;
  final int? currentIndex;
  final Future<void> Function()? onRefresh;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final content = SafeArea(
      child: Align(
        alignment: Alignment.topCenter,
        child: ListView(
          physics: const AlwaysScrollableScrollPhysics(),
          padding: const EdgeInsets.all(AppTokens.space16),
          children: [
            ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 720),
              child: body,
            ),
          ],
        ),
      ),
    );

    return Scaffold(
      appBar: AppBar(
        title: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(title),
            if (subtitle != null)
              Text(subtitle!, style: Theme.of(context).textTheme.bodySmall),
          ],
        ),
        actions: [
          const _SyncStatusAction(),
          ...actions,
        ],
      ),
      body: onRefresh == null
          ? content
          : RefreshIndicator(onRefresh: onRefresh!, child: content),
      bottomNavigationBar: currentIndex == null
          ? null
          : EfBottomNav(currentIndex: currentIndex!),
    );
  }
}

class _SyncStatusAction extends ConsumerWidget {
  const _SyncStatusAction();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final summary = ref.watch(syncSummaryProvider);
    final count = summary.maybeWhen(
      data: (value) => value.attentionCount,
      orElse: () => 0,
    );
    final hasIssues = summary.maybeWhen(
      data: (value) => value.issueCount > 0,
      orElse: () => false,
    );

    return Badge(
      isLabelVisible: count > 0,
      label: Text(count > 9 ? '9+' : '$count'),
      child: IconButton(
        tooltip: 'Sincronização',
        onPressed: () => context.go('/sync'),
        icon: Icon(
          hasIssues ? Icons.sync_problem : Icons.cloud_sync_outlined,
        ),
      ),
    );
  }
}

class EfBottomNav extends StatelessWidget {
  const EfBottomNav({required this.currentIndex, super.key});

  final int currentIndex;

  @override
  Widget build(BuildContext context) {
    return NavigationBar(
      selectedIndex: currentIndex,
      onDestinationSelected: (index) {
        context.go(_routes[index]);
      },
      destinations: const [
        NavigationDestination(
          icon: Icon(Icons.dashboard_outlined),
          selectedIcon: Icon(Icons.dashboard),
          label: 'Hoje',
        ),
        NavigationDestination(
          icon: Icon(Icons.restaurant_outlined),
          selectedIcon: Icon(Icons.restaurant),
          label: 'Refeições',
        ),
        NavigationDestination(
          icon: Icon(Icons.fact_check_outlined),
          selectedIcon: Icon(Icons.fact_check),
          label: 'Plano',
        ),
        NavigationDestination(
          icon: Icon(Icons.show_chart_outlined),
          selectedIcon: Icon(Icons.show_chart),
          label: 'Evolução',
        ),
        NavigationDestination(
          icon: Icon(Icons.person_outline),
          selectedIcon: Icon(Icons.person),
          label: 'Perfil',
        ),
      ],
    );
  }
}

const _routes = <String>[
  '/dashboard',
  '/meals',
  '/plan',
  '/progress',
  '/profile',
];
