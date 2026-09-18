import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:orblit_examples/orblit_examples.dart';
import 'package:orblit_gallery/main.dart';

/// The list on the left, under its headings.
///
/// It opens with only the heading that holds the example on screen opened out,
/// so every heading fits in the window; the rest open and fold when clicked.
void main() {
  testWidgets('every heading is listed, and only the first is open', (
    tester,
  ) async {
    await _open(tester);

    for (final section in ExampleSection.values) {
      expect(find.text(section.label.toUpperCase()), findsOneWidget);
    }

    // The first example is on screen, so its heading is open...
    expect(find.text('Virtual cameras'), findsOneWidget);
    // ...and the others are folded.
    expect(find.text('Blocks'), findsNothing);
    expect(find.text('Spawning from script'), findsNothing);
  });

  testWidgets('a heading opens when clicked, and folds when clicked again', (
    tester,
  ) async {
    await _open(tester);

    await tester.tap(find.text('SHOWCASES'));
    await tester.pump();
    expect(find.text('Blocks'), findsOneWidget);
    // Opening one leaves the others as they were.
    expect(find.text('Virtual cameras'), findsOneWidget);

    await tester.tap(find.text('SHOWCASES'));
    await tester.pump();
    expect(find.text('Blocks'), findsNothing);
  });
}

Future<void> _open(WidgetTester tester) async {
  // A desktop window, which is what the gallery is laid out for.
  tester.view.physicalSize = const Size(1600, 1000);
  tester.view.devicePixelRatio = 1;
  addTearDown(tester.view.reset);

  await tester.pumpWidget(const GalleryApp());
  await tester.pump();
}
