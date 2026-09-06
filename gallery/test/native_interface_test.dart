import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:orbis_gallery/src/examples/interface_native.dart';
import 'package:orbis_script_ui/orbis_script_ui.dart';

/// The Dart-built interface draws too, and by the same route.
///
/// Worth its own test because the point of it is that there is no script — so
/// nothing else would notice if it quietly stopped working.
void main() {
  testWidgets('an interface built in Dart lays out', (tester) async {
    final example = NativeInterfaceExample()
      ..health = 0.4
      ..shields = 3;

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

    expect(find.text('Docking bay'), findsOneWidget);
    // `uppercase` in the class list, and it means it.
    expect(find.text('POWER'), findsOneWidget);
    expect(find.text('40%'), findsOneWidget);
  });

  test('it uses nothing the vocabulary does not know', () {
    expect(UiBuilder().unknownIn(NativeInterfaceExample().description), isEmpty);
  });
}
