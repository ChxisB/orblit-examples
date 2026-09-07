import 'dart:math' as math;

import 'package:flutter/material.dart';
import 'package:orbis_filament/orbis_filament.dart';
import 'package:orbis_script_ui/orbis_script_ui.dart';
import 'package:vector_math/vector_math_64.dart' hide Colors;

import 'package:orbis_examples/orbis_examples.dart';

/// The same kind of interface, described in Dart.
///
/// There is no script here and no engine — the description is built directly.
/// That is worth having as well as the TypeScript one, for two reasons.
///
/// A game written entirely in Dart should not have to embed a JavaScript
/// engine to draw a heads-up display; the vocabulary and the builder are the
/// useful part, and neither needs a script.
///
/// And it shows what the boundary actually is. Everything below the
/// [description] is identical in both examples, because a description is a
/// description: the shape a script sends and the shape Dart builds are the
/// same shape, which is the reason script can send one at all.
class NativeInterfaceExample extends Example {
  NativeInterfaceExample();

  @override
  String get name => 'An interface in Dart';

  @override
  String get blurb =>
      'The same description, built directly, with no script in the way.';

  @override
  ViewPoint get viewpoint => const ViewPoint(distance: 12, pitch: 0.22);

  double health = 0.72;
  int shields = 3;
  String accent = 'steel';

  /// Written out rather than sent.
  ///
  /// Every one of these is what the TypeScript example's script produces as
  /// JSON — the same element names, the same class lists.
  UiNode get description => UiNode(
    type: 'stack',
    classes: 'full',
    children: [
      UiNode(
        type: 'column',
        classes: 'p-5 gap-3 items-start top-0 left-0',
        children: [
          UiNode(
            type: 'row',
            classes: 'gap-3 items-center px-4 py-3 rounded-lg bg-slate-900 '
                'border border-slate-700 shadow',
            css: 'opacity: 0.94',
            children: [
              UiNode(
                type: 'box',
                classes: 'w-3 h-3 rounded-full bg-$accent-500',
              ),
              const UiNode(
                type: 'text',
                classes: 'text-lg font-semibold text-slate-100',
                text: 'Docking bay',
              ),
              const UiNode(
                type: 'text',
                classes: 'text-sm text-slate-400',
                text: '· clear',
              ),
            ],
          ),
          UiNode(
            type: 'column',
            classes: 'gap-2 px-4 py-3 rounded-lg bg-slate-900 '
                'border border-slate-700 w-64',
            css: 'opacity: 0.94',
            children: [
              UiNode(
                type: 'row',
                classes: 'justify-between items-center',
                children: [
                  const UiNode(
                    type: 'text',
                    classes: 'text-xs uppercase text-slate-400',
                    text: 'Power',
                  ),
                  UiNode(
                    type: 'text',
                    classes: 'text-xs text-slate-300',
                    text: '${(health * 100).round()}%',
                  ),
                ],
              ),
              // A bar is two boxes: the track, and as much of it as is left.
              // No progress widget, and no second component set.
              UiNode(
                type: 'box',
                classes: 'w-full h-2 rounded-full bg-slate-700 clip',
                children: [
                  UiNode(
                    type: 'box',
                    classes: 'h-2 rounded-full bg-$accent-500',
                    css: 'width: ${(health * 224).round()}px',
                  ),
                ],
              ),
              // A row of pips, which in Dart is a list comprehension and in
              // TypeScript is a map. The description does not know or care.
              UiNode(
                type: 'row',
                classes: 'gap-1 pt-2',
                children: [
                  for (var i = 0; i < 5; i++)
                    UiNode(
                      type: 'box',
                      classes: 'w-6 h-1 rounded-full '
                          '${i < shields ? 'bg-$accent-400' : 'bg-slate-700'}',
                    ),
                ],
              ),
            ],
          ),
        ],
      ),
    ],
  );

  @override
  OrbisScene scene(OrbisCamera camera, double seconds) {
    return OrbisScene(
      objects: [
        for (var i = 0; i < 4; i++)
          OrbisObject(
            key: 850 + i,
            transform: Matrix4.identity()
              ..setTranslation(
                Vector3(
                  math.cos(seconds * 0.3 + i * 1.6) * 2.6,
                  -0.4 + math.sin(seconds * 0.5 + i) * 0.35,
                  math.sin(seconds * 0.3 + i * 1.6) * 2.6,
                ),
              )
              ..rotateY(seconds * 0.4 + i)
              ..scaleByDouble(0.6, 0.6, 0.6, 1),
            colour: linearOf(
              i.isEven ? const Color(0xFF5FA8D3) : const Color(0xFF7FB069),
            ),
          ),
        OrbisObject(
          key: 840,
          transform: Matrix4.identity()
            ..setTranslation(Vector3(0, -1.7, 0))
            ..scaleByDouble(10.0, 0.06, 10.0, 1),
          colour: linearOf(const Color(0xFF39404A)),
          castShadows: false,
        ),
      ],
      lights: [
        OrbisLight(
          key: 860,
          kind: OrbisLightKind.directional,
          intensity: 74000,
          direction: Vector3(-0.4, -1, -0.45)..normalize(),
          colour: linearOf(const Color(0xFFFFF3E0)),
          sunAngularRadius: 1.5,
        ),
      ],
      sky: OrbisSky(colour: linearOf(const Color(0xFF161C25)), ambient: 11000),
      camera: camera,
    );
  }

  @override
  Widget? overlay(BuildContext context, VoidCallback changed) =>
      UiBuilder(onEvent: (handler, payload) => changed()).build(description);

  @override
  Widget settings(BuildContext context, VoidCallback changed) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Setting(
          label: 'Power',
          value: health,
          min: 0,
          max: 1,
          onChanged: (value) {
            health = value;
            changed();
          },
        ),
        Setting(
          label: 'Shields',
          value: shields.toDouble(),
          min: 0,
          max: 5,
          decimals: 0,
          onChanged: (value) {
            shields = value.round();
            changed();
          },
        ),
        Choice(
          label: 'Accent',
          options: const ['ember', 'steel', 'moss', 'rose'],
          selected: accent,
          onSelect: (option) {
            accent = option;
            changed();
          },
        ),
      ],
    );
  }

  @override
  String get code => '''
// The same description, written rather than sent. No engine, no script.

UiNode get description => UiNode(
  type: 'column',
  classes: 'gap-2 px-4 py-3 rounded-lg bg-slate-900 border border-slate-700',
  children: [
    UiNode(
      type: 'text',
      classes: 'text-xs uppercase text-slate-400',
      text: 'Power',
    ),
    // A bar is two boxes: the track, and as much of it as is left.
    UiNode(
      type: 'box',
      classes: 'w-full h-2 rounded-full bg-slate-700 clip',
      children: [
        UiNode(
          type: 'box',
          classes: 'h-2 rounded-full bg-\$accent-500',
          css: 'width: \${(health * 224).round()}px',
        ),
      ],
    ),
  ],
);

// And then, exactly as in the TypeScript example:
UiBuilder().build(description)
''';
}
