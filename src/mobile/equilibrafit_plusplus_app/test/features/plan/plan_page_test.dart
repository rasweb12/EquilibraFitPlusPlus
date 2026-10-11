import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:equilibrafit_plusplus_app/core/http/api_client.dart';
import 'package:equilibrafit_plusplus_app/features/dashboard/data/dashboard_repository.dart';
import 'package:equilibrafit_plusplus_app/features/dashboard/domain/dashboard_summary.dart';
import 'package:equilibrafit_plusplus_app/features/plan/data/plan_repository.dart';
import 'package:equilibrafit_plusplus_app/features/plan/presentation/pages/plan_page.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  testWidgets('meal actions open their corresponding content', (tester) async {
    final adapter = _RecordingAdapter();
    await _pumpPlan(tester, adapter: adapter);

    await _scrollToMealActions(tester);
    await tester.tap(find.text('Foto sugerida'));
    await tester.pumpAndSettle();
    expect(
      find.textContaining('não possui uma imagem autorizada'),
      findsOneWidget,
    );
    await tester.tap(find.text('Fechar'));
    await tester.pumpAndSettle();

    await tester.tap(find.text('Receita rápida'));
    await tester.pumpAndSettle();
    expect(find.text('Omelete simples'), findsOneWidget);
    expect(find.text('Modo de preparo'), findsOneWidget);
    await tester.tapAt(const Offset(5, 5));
    await tester.pumpAndSettle();

    await tester.tap(find.text('Substituições'));
    await tester.pumpAndSettle();
    expect(find.text('Iogurte com aveia'), findsOneWidget);
    expect(adapter.lastRequest, isNull);
  });

  testWidgets('replacement only persists after confirmation', (tester) async {
    final adapter = _RecordingAdapter();
    await _pumpPlan(tester, adapter: adapter);

    await _scrollToMealActions(tester);
    await tester.tap(find.text('Substituições'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Iogurte com aveia'));
    await tester.pumpAndSettle();
    expect(find.text('Aplicar substituição?'), findsOneWidget);
    expect(adapter.lastRequest, isNull);

    await tester.tap(find.text('Aplicar'));
    await tester.pumpAndSettle();

    expect(
      adapter.lastRequest?.path,
      '/api/v1/planos/alimentar/plan-1/refeicoes/meal-1/substituir',
    );
    expect(
      adapter.lastRequest?.data,
      <String, Object?>{'indiceAlternativa': 0},
    );
    expect(find.text('Substituição aplicada ao seu plano.'), findsOneWidget);
  });

  testWidgets('replacement failure keeps the interface available',
      (tester) async {
    final adapter = _RecordingAdapter(statusCode: 503);
    await _pumpPlan(tester, adapter: adapter);

    await _scrollToMealActions(tester);
    await tester.tap(find.text('Substituições'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Iogurte com aveia'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Aplicar'));
    await tester.pumpAndSettle();

    expect(
      find.text(
        'O serviço está temporariamente indisponível. Tente novamente em instantes.',
      ),
      findsOneWidget,
    );
    expect(find.text('Substituições'), findsOneWidget);
  });
}

Future<void> _scrollToMealActions(WidgetTester tester) async {
  await tester.scrollUntilVisible(
    find.text('Foto sugerida'),
    240,
    scrollable: find.byType(Scrollable).first,
  );
  await tester.pumpAndSettle();
}

Future<void> _pumpPlan(
  WidgetTester tester, {
  required _RecordingAdapter adapter,
}) async {
  final client = ApiClient(Dio()..httpClientAdapter = adapter);
  await tester.pumpWidget(
    ProviderScope(
      overrides: [
        dashboardSummaryProvider.overrideWith((ref) async => _summary),
        planRepositoryProvider.overrideWithValue(PlanRepository(client)),
      ],
      child: const MaterialApp(home: PlanPage()),
    ),
  );
  await tester.pumpAndSettle();
}

final _summary = DashboardSummary(
  date: DateTime(2026, 10, 11),
  supportMessage: 'Continue com calma.',
  food: DashboardFood(
    calories: 0,
    proteinG: 0,
    carbsG: 0,
    fatG: 0,
    meals: 0,
  ),
  workout: DashboardWorkout(activeWorkouts: 0, weeklyFrequency: 0),
  progress: DashboardProgress(),
  currentPlan: DashboardPlan(
    id: 'plan-1',
    dailyCalories: 1800,
    goal: 'Manutencao',
    explanation: 'Plano de teste.',
    mealSuggestions: [
      DashboardMealSuggestion(
        id: 'meal-1',
        name: 'Café da manhã',
        description: 'Uma opção simples.',
        calories: 320,
        substitutions: [
          DashboardMealSubstitution(
            index: 0,
            name: 'Iogurte com aveia',
            description: 'Alternativa de teste.',
            calories: 300,
            ingredients: [
              DashboardRecipeIngredient(name: 'Iogurte', quantity: '170 g'),
            ],
          ),
        ],
        quickRecipe: DashboardQuickRecipe(
          name: 'Omelete simples',
          preparationMinutes: 10,
          ingredients: [
            DashboardRecipeIngredient(name: 'Ovo', quantity: '2 unidades'),
          ],
          preparation: ['Misture os ingredientes.', 'Cozinhe em fogo baixo.'],
        ),
      ),
    ],
  ),
);

class _RecordingAdapter implements HttpClientAdapter {
  _RecordingAdapter({this.statusCode = 200});

  final int statusCode;
  RequestOptions? lastRequest;

  @override
  void close({bool force = false}) {}

  @override
  Future<ResponseBody> fetch(
    RequestOptions options,
    Stream<Uint8List>? requestStream,
    Future<void>? cancelFuture,
  ) async {
    lastRequest = options;
    return ResponseBody.fromString(
      '{}',
      statusCode,
      headers: {
        Headers.contentTypeHeader: [Headers.jsonContentType],
      },
    );
  }
}
