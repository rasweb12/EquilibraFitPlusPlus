import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../core/sync/sync_engine.dart';
import '../features/premium/data/google_play_billing.dart';
import '../design_system/theme/app_theme.dart';
import '../design_system/theme/theme_mode_controller.dart';
import 'router.dart';

class EquilibraFitApp extends ConsumerStatefulWidget {
  const EquilibraFitApp({super.key});

  @override
  ConsumerState<EquilibraFitApp> createState() => _EquilibraFitAppState();
}

class _EquilibraFitAppState extends ConsumerState<EquilibraFitApp> {
  @override
  void initState() {
    super.initState();
    Future.microtask(() => ref.read(syncEngineProvider).start());
  }

  @override
  Widget build(BuildContext context) {
    final router = ref.watch(routerProvider);
    ref.watch(googlePlayBillingProvider);
    final themeMode = ref.watch(themeModeProvider);

    return MaterialApp.router(
      title: 'EquilibraFit++',
      debugShowCheckedModeBanner: false,
      theme: AppTheme.light(),
      darkTheme: AppTheme.dark(),
      themeMode: themeMode,
      locale: const Locale('pt', 'BR'),
      supportedLocales: const [
        Locale('pt', 'BR'),
      ],
      localizationsDelegates: GlobalMaterialLocalizations.delegates,
      routerConfig: router,
    );
  }
}
