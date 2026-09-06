import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:orbis_gallery/src/examples/interface.dart';
import 'package:orbis_script_ui/orbis_script_ui.dart';

/// The interface example really is driven by its TypeScript.
///
/// Worth a test rather than a look, because the failure mode is silent: a
/// script that does not run leaves an empty corner of the screen, and an
/// example that quietly stopped demonstrating the thing it exists to
/// demonstrate is worse than one that never did.
void main() {
  group('the interface example', () {
    testWidgets('draws what script/hud.tsx describes', (tester) async {
      final example = InterfaceExample()
        ..health = 0.5
        ..score = 1840
        ..showPanel = true;

      await tester.pumpWidget(
        MaterialApp(
          home: Scaffold(
            body: SizedBox(
              width: 900,
              height: 700,
              child: UiBuilder().build(example.description),
            ),
          ),
        ),
      );

      // Every one of these is written in the .tsx and nowhere else.
      expect(find.text('Sector 12'), findsOneWidget);
      expect(find.text('· holding'), findsOneWidget);
      // The class list says `uppercase`, and it means it.
      expect(find.text('HULL'), findsOneWidget);
      expect(find.text('50%'), findsOneWidget);
      expect(find.text('1840'), findsOneWidget);
      expect(find.text('Repair'), findsOneWidget);
    });

    test('the settings write into the script rather than around it', () {
      final example = InterfaceExample()..health = 0.25;
      expect(_textsIn(example.description), contains('25%'));

      example.health = 0.9;
      expect(_textsIn(example.description), contains('90%'));
    });

    test('pressing a button runs the script that was written for it', () {
      final example = InterfaceExample()..health = 0.5;

      final repair = _find(example.description, 'Repair')!;
      final handler = repair.handlerFor('onPressed')!;

      expect(_textsIn(example.description), contains('50%'));

      // Straight into QuickJS, into the arrow function written in the .tsx.
      example.host.eval('__orbis_ui.dispatch("$handler")');

      // Which raised the hull by the amount that file says, with nothing on
      // this side involved in the arithmetic or holding the result.
      expect(_textsIn(example.description), contains('62%'));
      expect(example.health, closeTo(0.62, 1e-9));
    });

    test('what it asks for is all in the vocabulary', () {
      final builder = UiBuilder();
      expect(builder.unknownIn(InterfaceExample().description), isEmpty);
    });
  });
}

List<String> _textsIn(UiNode node) => [
      ?node.text,
      for (final child in node.children) ..._textsIn(child),
    ];

UiNode? _find(UiNode node, String text) {
  if (node.text == text) return node;
  for (final child in node.children) {
    final found = _find(child, text);
    if (found != null) return found;
  }
  return null;
}
