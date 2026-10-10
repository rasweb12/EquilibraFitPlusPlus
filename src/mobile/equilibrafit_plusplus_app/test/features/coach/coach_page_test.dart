import 'dart:async';

import 'package:dio/dio.dart';
import 'package:equilibrafit_plusplus_app/core/http/api_client.dart';
import 'package:equilibrafit_plusplus_app/core/sync/sync_service.dart';
import 'package:equilibrafit_plusplus_app/features/coach/data/coach_repository.dart';
import 'package:equilibrafit_plusplus_app/features/coach/domain/coach_reply.dart';
import 'package:equilibrafit_plusplus_app/features/coach/presentation/pages/coach_page.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  for (final width in [320.0, 360.0, 430.0]) {
    testWidgets('Coach provider selector fits a $width screen', (tester) async {
      await _pumpCoach(tester, _FakeCoachRepository(), width: width);
      final selector = tester.widget<SegmentedButton<String>>(
        find.byType(SegmentedButton<String>),
      );
      expect(selector.selected, {'openai'});
      final openai = tester.getRect(find.text('OpenAI'));
      final gemini = tester.getRect(find.text('Gemini'));
      expect(openai.overlaps(gemini), isFalse);
      expect(openai.left, greaterThanOrEqualTo(0));
      expect(gemini.right, lessThanOrEqualTo(width));
      expect(tester.takeException(), isNull);
    });
  }

  testWidgets('switches Coach provider without changing the conversation', (
    tester,
  ) async {
    final repository = _FakeCoachRepository();
    await _pumpCoach(tester, repository);
    await _send(tester);
    expect(repository.providers, ['openai']);

    await tester.ensureVisible(find.text('Gemini'));
    await tester.tap(find.text('Gemini'));
    await _send(tester);
    expect(repository.providers, ['openai', 'gemini']);
    expect(repository.sessions, [null, 'session-1']);

    await tester.ensureVisible(find.text('OpenAI'));
    await tester.tap(find.text('OpenAI'));
    await _send(tester);
    expect(repository.providers, ['openai', 'gemini', 'openai']);
    expect(repository.sessions.last, 'session-1');
    expect(tester.takeException(), isNull);
  });

  testWidgets('disables provider changes while a request is in progress', (
    tester,
  ) async {
    final repository = _FakeCoachRepository();
    repository.pending = Completer<CoachReply>();
    await _pumpCoach(tester, repository);
    await tester.enterText(find.byType(TextFormField), 'Como adaptar meu treino?');
    await tester.ensureVisible(find.text('Enviar'));
    await tester.tap(find.text('Enviar'));
    await tester.pump();
    expect(
      tester.widget<SegmentedButton<String>>(
        find.byType(SegmentedButton<String>),
      ).onSelectionChanged,
      isNull,
    );
    repository.pending!.complete(_reply);
    await tester.pumpAndSettle();
    expect(
      tester.widget<SegmentedButton<String>>(
        find.byType(SegmentedButton<String>),
      ).onSelectionChanged,
      isNotNull,
    );
  });
}

Future<void> _pumpCoach(
  WidgetTester tester,
  _FakeCoachRepository repository, {
  double width = 430,
}) async {
  tester.view.devicePixelRatio = 1;
  tester.view.physicalSize = Size(width, 900);
  addTearDown(tester.view.resetDevicePixelRatio);
  addTearDown(tester.view.resetPhysicalSize);
  await tester.pumpWidget(
    ProviderScope(
      overrides: [
        coachRepositoryProvider.overrideWithValue(repository),
        syncSummaryProvider.overrideWith(
          (ref) async => const SyncSummary(
            pending: 0,
            syncing: 0,
            synced: 0,
            failed: 0,
            conflict: 0,
          ),
        ),
      ],
      child: const MaterialApp(home: CoachPage()),
    ),
  );
  await tester.pumpAndSettle();
}

Future<void> _send(WidgetTester tester) async {
  await tester.enterText(find.byType(TextFormField), 'Como adaptar meu treino?');
  await tester.ensureVisible(find.text('Enviar'));
  await tester.tap(find.text('Enviar'));
  await tester.pumpAndSettle();
}

const _reply = CoachReply(
  sessionId: 'session-1',
  content: 'Resposta de teste.',
  healthNotice: 'Aviso de teste.',
);

class _FakeCoachRepository extends CoachRepository {
  _FakeCoachRepository() : super(ApiClient(Dio()));

  final providers = <String>[];
  final sessions = <String?>[];
  Completer<CoachReply>? pending;

  @override
  Future<CoachReply> sendMessage({
    required String message,
    String? sessionId,
    String provider = 'openai',
  }) async {
    providers.add(provider);
    sessions.add(sessionId);
    return pending == null ? _reply : await pending!.future;
  }
}
