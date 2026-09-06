import 'package:flutter/material.dart';
import 'package:orbis_filament/orbis_filament.dart';
import 'package:orbis_script/orbis_script.dart';
import 'package:orbis_script_scene/orbis_script_scene.dart';
import 'package:vector_math/vector_math_64.dart' hide Colors;

import '../example.dart';
import 'scripts.g.dart';
import 'surface.dart' show linearOf;

/// A world put together in TypeScript.
///
/// Every object here is spawned by `script/world.tsx`. Nothing in this file
/// decides where anything is: the engine asks the script for a frame, the
/// script's own step runs, and what comes back is a description of the whole
/// world — which Dart turns into the renderer's objects.
///
/// A description rather than a stream of commands, for the same reason the
/// interface is one: a message that says everything cannot go stale, and a
/// script reloaded halfway through cannot leave the world half-built.
class SpawningExample extends Example {
  SpawningExample();

  @override
  String get name => 'Spawning from script';

  @override
  String get blurb =>
      'Every object put there by TypeScript, and moved by it every frame.';

  @override
  ViewPoint get viewpoint =>
      const ViewPoint(distance: 26, pitch: 0.42, height: 1);

  ScriptHost? _host;

  ScriptHost get host {
    final running = _host;
    if (running != null) return running;

    final started = ScriptHost()
      // The scene runtime: spawn, destroy, and the `__orbis_scene` object the
      // host asks for a frame through.
      ..eval(SceneRuntime.source, fileName: 'orbis/scene.js');

    // The game's own file, kept under a name so its settings can be reached.
    started.eval(
      ScriptModule.around(worldScript, name: 'world'),
      fileName: 'script/world.tsx',
    );

    return _host = started;
  }

  /// The rings the script lays out, and how it moves them.
  int get rings => _number('rings').round();
  set rings(int value) {
    _write('rings', '$value');
    // A different arrangement, so the script lays the whole thing out again.
    host.eval('require("world").build();');
  }

  double get spin => _number('spin');
  set spin(double value) => _write('spin', '$value');

  bool get bob => host.eval('String(require("world").settings.bob)') == 'true';
  set bob(bool value) => _write('bob', '$value');

  int get howMany =>
      int.tryParse(host.eval('String(require("world").howMany())')) ?? 0;

  double _number(String name) =>
      double.tryParse(
        host.eval('String(require("world").settings.$name)'),
      ) ??
      0;

  void _write(String name, String value) =>
      host.eval('require("world").settings.$name = $value;');

  @override
  OrbisScene scene(OrbisCamera camera, double seconds) {
    late final List<ScriptedThing> world;
    try {
      // One call: run the script's step, and take back everything in the
      // world. Nothing on this side knows what moved.
      world = ScriptedThing.decodeAll(
        host.eval('__orbis_scene.frame($seconds)'),
      );
    } on ScriptError {
      world = const [];
    }

    return OrbisScene(
      objects: [
        for (final (index, thing) in world.indexed)
          OrbisObject(
            // The renderer keeps what it built against a number, and script
            // names things with words. One stands for the other for as long
            // as the world holds still, which between two frames it does.
            key: 800 + index,
            transform: Matrix4.identity()
              ..setTranslation(thing.position)
              ..rotateY(thing.turn * 3.14159265358979 / 180)
              ..scaleByVector3(thing.scale),
            colour: linearOf(Color(0xFF000000 | thing.colour)),
            mesh: thing.mesh,
          ),
        OrbisObject(
          key: 790,
          transform: Matrix4.identity()
            ..setTranslation(Vector3(0, -1.15, 0))
            ..scaleByDouble(26.0, 0.06, 26.0, 1),
          colour: linearOf(const Color(0xFF39404A)),
          castShadows: false,
        ),
      ],
      lights: [
        OrbisLight(
          key: 810,
          kind: OrbisLightKind.directional,
          intensity: 76000,
          direction: Vector3(-0.4, -0.92, -0.35)..normalize(),
          colour: linearOf(const Color(0xFFFFF3E0)),
          sunAngularRadius: 1.2,
        ),
      ],
      sky: OrbisSky(
        colour: linearOf(const Color(0xFF6E8DB4)),
        zenith: linearOf(const Color(0xFF2F5F97)),
        horizon: linearOf(const Color(0xFFB9CBDD)),
        ambient: 22000,
        bodyDirection: Vector3(0.4, 0.92, 0.35)..normalize(),
        bodyColour: linearOf(const Color(0xFFFFF6E8)),
        bodySize: 0.011,
        quality: SkyQuality.fair,
        clouds: OrbisClouds.cumulus(cover: 0.3, wind: Vector2(4, 1.5)),
      ),
      camera: camera,
    );
  }

  @override
  Widget settings(BuildContext context, VoidCallback changed) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Setting(
          label: 'Rings',
          value: rings.toDouble(),
          min: 1,
          max: 8,
          decimals: 0,
          onChanged: (value) {
            rings = value.round();
            changed();
          },
        ),
        Setting(
          label: 'Spin',
          value: spin,
          min: 0,
          max: 2,
          onChanged: (value) {
            spin = value;
            changed();
          },
        ),
        Toggle(
          label: 'Bob',
          value: bob,
          onChanged: (value) {
            bob = value;
            changed();
          },
        ),
        Choice(
          label: 'Drop one',
          options: const ['Drop', 'Clear'],
          selected: '',
          onSelect: (option) {
            host.eval(
              option == 'Drop'
                  ? 'require("world").drop();'
                  : 'require("world").clearDropped();',
            );
            changed();
          },
        ),
      ],
    );
  }

  @override
  String get code => '''
// script/world.tsx — every object in the scene, and everything that moves.

import { spawn, all, clear } from "orbis/scene";

export const settings = { rings: 4, spin: 0.35, bob: true };

export function build() {
  clear();
  spawn({ id: "core", at: [0, 0.4, 0], size: [0.8, 2.4, 0.8],
          colour: "#F2F4F7" });

  for (let ring = 0; ring < settings.rings; ring++) {
    const radius = 2.4 + ring * 1.9;
    const many = 6 + ring * 4;

    for (let i = 0; i < many; i++) {
      const angle = (i / many) * Math.PI * 2;
      spawn({
        id: `r\${ring}-\${i}`,
        at: [Math.cos(angle) * radius, -0.6, Math.sin(angle) * radius],
        size: [0.5, 0.5 + ring * 0.25, 0.5],
        turn: (angle * 180) / Math.PI,
        colour: colours[(ring + i) % colours.length],
      });
    }
  }
}

// The step. Everything that moves, moves here — the host only asks for a
// frame and draws whatever it is told.
export function step(seconds: number) {
  for (const thing of all()) { /* ... */ }
}
''';
}
