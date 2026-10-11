import 'package:equilibrafit_plusplus_app/features/progress/presentation/pages/progress_page.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  testWidgets('weight dialog owns its fields through the closing transition',
      (tester) async {
    WeightEntry? result;

    await tester.pumpWidget(
      MaterialApp(
        home: Builder(
          builder: (context) => Scaffold(
            body: FilledButton(
              onPressed: () async {
                result = await showDialog<WeightEntry>(
                  context: context,
                  builder: (context) => const WeightEntryDialog(),
                );
              },
              child: const Text('Abrir'),
            ),
          ),
        ),
      ),
    );

    await tester.tap(find.text('Abrir'));
    await tester.pumpAndSettle();
    await tester.enterText(find.byType(TextFormField).at(0), '78,5');
    await tester.enterText(find.byType(TextFormField).at(1), 'Após treino');
    await tester.tap(find.text('Salvar'));
    await tester.pumpAndSettle();

    expect(result?.weightKg, 78.5);
    expect(result?.note, 'Após treino');
    expect(tester.takeException(), isNull);
  });

  testWidgets('weight dialog keeps invalid values open', (tester) async {
    await tester.pumpWidget(
      const MaterialApp(home: Scaffold(body: WeightEntryDialog())),
    );

    await tester.enterText(find.byType(TextFormField).first, '10');
    await tester.tap(find.text('Salvar'));
    await tester.pump();

    expect(find.text('Informe um peso válido.'), findsOneWidget);
    expect(find.byType(WeightEntryDialog), findsOneWidget);
  });
}
