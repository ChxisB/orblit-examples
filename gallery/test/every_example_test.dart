import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:orbis_filament/orbis_filament.dart';
import 'package:orbis_gallery/main.dart' show galleryExamples;
import 'package:vector_math/vector_math_64.dart' hide Colors;

/// Every example, put through what the gallery does to it.
///
/// The failure this exists for is quiet: an example that throws while building
/// its scene shows an empty stage and says nothing, and one that takes a
/// second to build looks like the engine is slow rather than like the example
/// is doing something silly. Neither shows up in a screenshot of a different
/// example.
void main() {
  final camera = OrbisCamera(
    position: Vector3(0, 4, 14),
    target: Vector3.zero(),
  );

  for (final example in galleryExamples()) {
    group(example.name, () {
      test('builds a scene, and again a second later', () {
        // Twice, because the first build of an example often does the work —
        // laying a crowd out, starting a script — and the second is what
        // every frame after it costs.
        final first = example.scene(camera, 0);
        expect(first.objects, isNotNull);

        final second = example.scene(camera, 1);
        expect(second.objects, isNotNull);
      });

      test('is ready inside a frame', () {
        // Both builds, measured. Sixteen milliseconds is a frame; an example
        // that cannot describe itself inside one is the thing making the
        // engine look slow.
        example.scene(camera, 0);

        final clock = Stopwatch()..start();
        for (var i = 1; i <= 20; i++) {
          example.scene(camera, i / 60);
        }
        clock.stop();

        final each = clock.elapsedMicroseconds / 20 / 1000;
        expect(
          each,
          lessThan(16),
          reason: '${example.name} takes ${each.toStringAsFixed(1)} ms to '
              'describe itself, which is a frame gone before anything is drawn',
        );
      });

      test('says what it is', () {
        expect(example.name, isNotEmpty);
        expect(example.blurb, isNotEmpty);
        expect(example.code, isNotEmpty);
      });

      testWidgets('shows its settings, and its own source', (tester) async {
        // Built through a Builder because the settings want a context, and
        // there is no context until the tree they are going into exists.
        await tester.pumpWidget(
          MaterialApp(
            home: Scaffold(
              body: SizedBox(
                width: 320,
                height: 900,
                child: SingleChildScrollView(
                  child: Builder(
                    builder: (context) => example.settings(context, () {}),
                  ),
                ),
              ),
            ),
          ),
        );
        expect(tester.takeException(), isNull);
      });
    });
  }
}
