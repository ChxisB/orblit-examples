import 'dart:convert';
import 'dart:math' as math;

import 'package:flutter/material.dart';
import 'package:orbis_filament/orbis_filament.dart';
import 'package:orbis_script/orbis_script.dart';
import 'package:orbis_script_ui/orbis_script_ui.dart';
import 'package:vector_math/vector_math_64.dart' hide Colors;

import 'package:orbis_examples/orbis_examples.dart';
import 'scripts.g.dart';

/// An interface over a running scene, written in TypeScript.
///
/// `script/hud.tsx` is the whole of it: components, props, JSX. It is compiled
/// by `npm run build`, loaded into the engine's own script host, and asked to
/// describe the interface; Dart builds real Flutter widgets from what comes
/// back — laid out by Flutter, drawn by Impeller. The borrowed class-name
/// vocabulary is a way in rather than a second box model pretending to be the
/// web's.
class InterfaceExample extends Example {
  InterfaceExample();

  @override
  String get name => 'An interface in TypeScript';

  @override
  String get blurb =>
      'Two interfaces written in .tsx, running in the engine, drawn by Flutter.';

  @override
  ViewPoint get viewpoint => const ViewPoint(distance: 12, pitch: 0.22);

  // The script owns these, not this class.
  //
  // That is the whole point rather than a detail of the wiring: a HUD written
  // in TypeScript keeps its own state, and a button in it changes that state
  // without asking Dart. Holding a copy here and pushing it in before every
  // render would overwrite whatever the script had just done — which it did,
  // and the button appeared to do nothing.
  bool get isHud => showing == 'HUD';

  double get health => _number('hull');
  set health(double value) => _write('hull', '$value');

  int get score => _number('score').round();
  set score(int value) => _write('score', '$value');

  bool get showPanel => _read('panel') == 'true';
  set showPanel(bool value) => _write('panel', '$value');

  String get accent => _read('accent');
  set accent(String value) => _write('accent', jsonEncode(value));

  String _read(String name) =>
      host.eval('String(require("hud").state.$name)');

  double _number(String name) => double.tryParse(_read(name)) ?? 0;

  void _write(String name, String value) =>
      host.eval('require("hud").state.$name = $value;');

  /// The engine running the example's own TypeScript.
  ///
  /// Not a description written in Dart that stands in for one: this is
  /// [hudScript] — the compiled output of `script/hud.tsx` — in QuickJS, asked
  /// to describe the interface after every change.
  /// Which of the example's interfaces is loaded.
  ///
  /// Two, because one proves nothing about whether the first was a special
  /// case. They share no code: each is a whole .tsx of its own.
  String showing = 'HUD';

  static const Map<String, String> _written = {
    'HUD': hudScript,
    'Menu': menuScript,
  };

  ScriptHost? _host;
  String? _loaded;

  ScriptHost get host {
    final running = _host;
    if (running != null && _loaded == showing) return running;

    // A different interface is a different script, and a script that has been
    // replaced should not leave its predecessor's state behind — so it gets a
    // fresh engine rather than a second mount into the old one.
    running?.dispose();
    _loaded = showing;

    final started = ScriptHost()
      // The interface library first: the elements, both JSX factories, and the
      // small module registry that lets a file compiled straight from
      // TypeScript run without being bundled.
      ..eval(UiRuntime.source, fileName: 'orbis/ui.js');

    // Then the game's own file, kept under a name so that what it exports can
    // be reached: its state lives in a module, and the settings write into it.
    started.eval(
      ScriptModule.around(_written[showing]!, name: 'hud'),
      fileName: 'script/${showing.toLowerCase()}.tsx',
    );

    return _host = started;
  }

  /// What the interface looks like now.
  ///
  /// Asked of the script, every time. Nothing on this side builds an element,
  /// and nothing on this side holds the state it is built from.
  UiNode get description {
    try {
      return UiNode.decode(host.eval('__orbis_ui.render()'));
    } on ScriptError catch (error) {
      // A script that stops should say so where somebody can read it, rather
      // than leaving a blank corner of the screen and no reason for it.
      return UiNode(
        type: 'column',
        classes: 'p-4 gap-2 m-5 rounded-lg bg-rose-900 border border-rose-600',
        children: [
          const UiNode(
            type: 'text',
            classes: 'text-sm font-semibold text-rose-100',
            text: 'The interface script stopped',
          ),
          UiNode(
            type: 'text',
            classes: 'text-xs text-rose-200',
            text: error.message,
          ),
        ],
      );
    }
  }

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
    return UiSurface(
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
        Choice(
          label: 'Written',
          options: _written.keys.toList(),
          selected: showing,
          onSelect: (option) {
            showing = option;
            changed();
          },
        ),
        if (!isHud)
          const Padding(
            padding: EdgeInsets.symmetric(vertical: 8),
            child: Text(
              'The menu keeps its own state — press the choices in it.',
              style: TextStyle(fontSize: 12, color: Color(0xFF8A93A0)),
            ),
          ),
        if (isHud) ...[
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
      ],
    );
  }

  @override
  String get code => '''
// script/hud.tsx — the whole of the interface. Compiled by `npm run build`
// and loaded into the engine's script host; nothing on the Dart side builds
// an element.

import { mount } from "orbis";

const state = { hull: 0.72, score: 1840, accent: "ember" };

/// A bar is two boxes: the track, and as much of it as is left. No progress
/// widget, and no second component set.
function Bar({ part }: { part: number }) {
  return (
    <box class="w-full h-2 rounded-full bg-slate-700 clip">
      <box class={`h-2 rounded-full bg-\${state.accent}-500`}
           style={`width: \${Math.round(part * 224)}px`} />
    </box>
  );
}

function Hud() {
  return (
    <column class="p-5 gap-3 items-start">
      <text class="text-lg font-semibold text-slate-100">Sector 12</text>
      <Bar part={state.hull} />
      <button class="px-3 py-2 rounded-md bg-ember-500 text-white"
              key="repair"
              onPressed={() => { state.hull = Math.min(1, state.hull + 0.12); }}>
        Repair
      </button>
    </column>
  );
}

mount(() => <Hud />);
''';
}
