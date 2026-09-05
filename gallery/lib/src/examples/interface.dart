import 'dart:math' as math;

import 'package:flutter/material.dart';
import 'package:orbis_filament/orbis_filament.dart';
import 'package:orbis_script_ui/orbis_script_ui.dart';
import 'package:vector_math/vector_math_64.dart' hide Colors;

import '../example.dart';
import 'surface.dart' show linearOf;

/// An interface over a running scene, described rather than built.
///
/// The description below is exactly what a TypeScript file would send: a tree
/// of elements with a class list on each. Dart builds real Flutter widgets
/// from it — laid out by Flutter, drawn by Impeller — so the borrowed
/// vocabulary is a way in rather than a second box model.
class InterfaceExample extends Example {
  InterfaceExample();

  @override
  String get name => 'An interface';

  @override
  String get blurb =>
      'A heads-up display described in one tree, styled with classes or CSS.';

  @override
  ViewPoint get viewpoint => const ViewPoint(distance: 12, pitch: 0.22);

  double health = 0.72;
  int score = 1840;
  bool showPanel = true;
  String accent = 'ember';

  /// What arrives from script. Written here as the description itself so the
  /// example has no virtual machine in it — the shape is the shape either
  /// way, which is the point of a description.
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
                classes: 'gap-3 items-center px-4 py-3 rounded-lg '
                    'bg-slate-900 border border-slate-700 shadow',
                css: 'opacity: 0.94',
                children: [
                  UiNode(
                    type: 'box',
                    classes: 'w-3 h-3 rounded-full bg-$accent-500',
                  ),
                  UiNode(
                    type: 'text',
                    classes: 'text-lg font-semibold text-slate-100',
                    text: 'Sector 12',
                  ),
                  UiNode(
                    type: 'text',
                    classes: 'text-sm text-slate-400',
                    text: '· holding',
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
                        text: 'Hull',
                      ),
                      UiNode(
                        type: 'text',
                        classes: 'text-xs text-slate-300',
                        text: '${(health * 100).round()}%',
                      ),
                    ],
                  ),
                  // A bar is two boxes: the track, and as much of it as is
                  // left. No progress widget, no second component set.
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
                  UiNode(
                    type: 'row',
                    classes: 'justify-between items-baseline pt-1',
                    children: [
                      const UiNode(
                        type: 'text',
                        classes: 'text-xs uppercase text-slate-400',
                        text: 'Score',
                      ),
                      UiNode(
                        type: 'text',
                        classes: 'text-2xl font-semibold text-slate-100',
                        text: '$score',
                      ),
                    ],
                  ),
                ],
              ),
              if (showPanel)
                UiNode(
                  type: 'row',
                  classes: 'gap-2',
                  children: [
                    UiNode(
                      type: 'button',
                      classes: 'px-3 py-2 rounded-md bg-$accent-500 '
                          'text-white text-sm font-medium',
                      text: 'Repair',
                      props: const {'onPressed': 'repair'},
                    ),
                    const UiNode(
                      type: 'button',
                      classes: 'px-3 py-2 rounded-md bg-slate-800 '
                          'border border-slate-600 text-slate-200 text-sm',
                      text: 'Take damage',
                      props: {'onPressed': 'damage'},
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
            key: 700 + i,
            transform: Matrix4.identity()
              ..setTranslation(Vector3(
                math.cos(seconds * 0.3 + i * 1.6) * 2.6,
                -0.4 + math.sin(seconds * 0.5 + i) * 0.35,
                math.sin(seconds * 0.3 + i * 1.6) * 2.6,
              ))
              ..rotateY(seconds * 0.4 + i)
              ..scaleByDouble(0.6, 0.6, 0.6, 1),
            colour: linearOf(
              i.isEven ? const Color(0xFFD9634F) : const Color(0xFF5FA8D3),
            ),
          ),
        OrbisObject(
          key: 690,
          transform: Matrix4.identity()
            ..setTranslation(Vector3(0, -1.7, 0))
            ..scaleByDouble(10.0, 0.06, 10.0, 1),
          colour: linearOf(const Color(0xFF39404A)),
          castShadows: false,
        ),
      ],
      lights: [
        OrbisLight(
          key: 710,
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

  /// The interface, over the scene.
  @override
  Widget? overlay(BuildContext context, VoidCallback changed) {
    return ScriptedSurface(
      description: description,
      onEvent: (handler, payload) {
        // What the host does with an event. In a game this goes back into the
        // script that drew the interface; here it changes the same state the
        // description is built from, which is the same loop with the virtual
        // machine taken out of it.
        switch (handler) {
          case 'repair':
            health = (health + 0.15).clamp(0.0, 1.0);
            score += 25;
          case 'damage':
            health = (health - 0.2).clamp(0.0, 1.0);
        }
        changed();
      },
    );
  }

  @override
  Widget settings(BuildContext context, VoidCallback changed) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Setting(
          label: 'Hull',
          value: health,
          min: 0,
          max: 1,
          onChanged: (value) {
            health = value;
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
        Toggle(
          label: 'Buttons',
          value: showPanel,
          onChanged: (value) {
            showPanel = value;
            changed();
          },
        ),
      ],
    );
  }

  @override
  String get code => '''
// TypeScript describes the tree. Dart builds the real widgets.
import { column, row, box, text, button, mount } from "@orbis/ui";

mount(() =>
  column({ class: "p-5 gap-3 items-start" },
    row({ class: "gap-3 items-center px-4 py-3 rounded-lg bg-slate-900 " +
                 "border border-slate-700 shadow", style: "opacity: 0.94" },
      box({ class: "w-3 h-3 rounded-full bg-ember-500" }),
      text("Sector 12", { class: "text-lg font-semibold text-slate-100" }),
    ),

    // A bar is two boxes: the track, and as much of it as is left.
    box({ class: "w-full h-2 rounded-full bg-slate-700 clip" },
      box({ class: "h-2 rounded-full bg-ember-500",
            style: `width: \${health * 224}px` }),
    ),

    button("Repair", {
      class: "px-3 py-2 rounded-md bg-ember-500 text-white text-sm",
      onPressed: () => { health = Math.min(1, health + 0.15); },
    }),
  ),
);

// Classes and CSS resolve to the same style, and CSS is laid over the class
// list rather than replacing it. A class nobody knows is ignored, not fatal.
// A callback crosses as a name: the function stays in the script, and
// pressing something sends the name back and asks for the tree again.
''';
}
