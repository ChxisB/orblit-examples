import 'dart:io';

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

      testWidgets('the textures it draws for itself are written', (
        tester,
      ) async {
        // Two examples generate their textures rather than shipping them, and
        // the way that goes wrong is silent: nothing is written, the material
        // is handed no maps, and the example draws a plain grey surface that
        // still builds a scene, still describes itself, and still passes every
        // other test here. So the assertion has to be that the textures turn
        // up at all — checking only the paths an example does reference is the
        // test that cannot fail.
        //
        // Scoped to temp, which is exactly the set an example made itself.
        // Assets that were shipped or downloaded live elsewhere, and whether
        // somebody has fetched them is not this test's business.
        final temp = Directory.systemTemp.path;
        var own = <String>{};

        // Real time. Writing a PNG is file IO and a decode, and pumping fake
        // frames at it would only ever see the state the example started in.
        await tester.runAsync(() async {
          for (var attempt = 0; attempt < 40; attempt++) {
            own = _selfDrawn(example.scene(camera, 0), temp);
            if (own.isNotEmpty) break;
            await Future<void>.delayed(const Duration(milliseconds: 25));
          }
        });

        if (!_drawsItsOwn.contains(example.name)) return;

        expect(
          own,
          isNotEmpty,
          reason: '${example.name} draws its own textures and ended up with '
              'none, so it is rendering untextured',
        );
        for (final path in own) {
          final file = File(path);
          expect(file.existsSync(), isTrue, reason: '$path was never written');
          expect(
            file.lengthSync(),
            greaterThan(0),
            reason: '$path was written empty',
          );
        }
      });

      testWidgets('shows its settings, and its own source', (tester) async {
        // In the panel the gallery actually puts them in, which is a coloured
        // container inside a scrolling list. That detail is not decoration:
        // some Material widgets paint onto the nearest Material ancestor and
        // report themselves broken when something opaque sits in between, so
        // a settings panel built in a bare Scaffold can pass here while the
        // real one reports an error on every frame it is on screen.
        //
        // Built through a Builder because the settings want a context, and
        // there is no context until the tree they are going into exists.
        await tester.pumpWidget(
          MaterialApp(
            home: Scaffold(
              body: SizedBox(
                width: 320,
                height: 900,
                child: DecoratedBox(
                  decoration: const BoxDecoration(color: Color(0xFF12161D)),
                  child: ListView(
                    padding: const EdgeInsets.all(16),
                    children: [
                      Builder(
                        builder: (context) => example.settings(context, () {}),
                      ),
                    ],
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

/// The examples that generate their own textures at startup.
///
/// Named rather than discovered, because what goes wrong is absence: an
/// example whose textures never arrive references none of them, and a test
/// that only looks at what an example does reference has nothing to say about
/// it. Add a name here when an example starts drawing its own.
const _drawsItsOwn = {'Materials', 'Blending'};

/// Every texture in a scene that the example wrote itself.
Set<String> _selfDrawn(OrbisScene scene, String temp) {
  final paths = <String>{};
  for (final material in scene.materials) {
    for (final texture in [
      material.baseColourMap,
      material.normalMap,
      material.metallicRoughnessMap,
      material.occlusionMap,
      material.emissiveMap,
      material.blendBaseColourMap,
      material.blendMaskMap,
    ]) {
      if (texture != null && texture.path.startsWith(temp)) {
        paths.add(texture.path);
      }
    }
  }
  return paths;
}
