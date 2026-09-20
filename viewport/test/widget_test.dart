import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:orblit_viewport/main.dart';

void main() {
  testWidgets('the shell draws around the viewport', (tester) async {
    await tester.pumpWidget(const ViewportApp());

    // The renderer needs a platform channel there is none of under the test
    // binding, so what is checked here is the chrome the M1 demo is about:
    // that a Filament view takes part in layout rather than sitting in its
    // own window on top of everything.
    expect(find.text('Orblit'), findsOneWidget);
    expect(find.text('M1 · macOS viewport'), findsOneWidget);
  });

  testWidgets('the overlay badge answers its switch', (tester) async {
    await tester.pumpWidget(const ViewportApp());

    const label = 'Filament → CVPixelBuffer → Texture';
    expect(find.text(label), findsOneWidget);

    await tester.tap(find.byType(Switch));
    await tester.pump();

    expect(find.text(label), findsNothing);
  });
}
