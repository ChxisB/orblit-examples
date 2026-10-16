import 'dart:math' as math;

import 'package:flutter/material.dart';
import 'package:orblit_examples/orblit_examples.dart';
import 'package:orblit_filament/orblit_filament.dart';
import 'package:orblit_physics_soft/orblit_physics_soft.dart';
import 'package:orblit_stage/orblit_stage.dart';
import 'package:vector_math/vector_math_64.dart' hide Colors;

const _columns = 20;
const _rows = 12;
const _spacing = 0.08;
const _step = 1 / 60;

/// Where the top of the flag's pole edge is.
final _corner = Vector3(0, 3.4, 0);

/// A wind that blows along the flag and rocks across it, in gusts.
///
/// A function of the time it is told, so the same seconds always blow the same
/// wind and the cloth can be stepped as often as the clock says.
final class _Gusts implements SoftAir {
  double strength = 7;
  double seconds = 0;

  @override
  Vector3 windAt(Vector3 point) {
    final gust =
        1 + 0.45 * math.sin(seconds * 1.3) + 0.25 * math.sin(seconds * 3.1 + 1);
    final rock = 0.4 * math.sin(seconds * 2.2);
    return Vector3(gust, 0, rock) * strength;
  }
}

/// A flag on a pole, moved by a position-based cloth and drawn as a skinned
/// sheet.
///
/// The renderer takes new joint poses every frame and no new vertices, so the
/// sheet has a joint for each particle. Each frame the solver moves the
/// particles, and the sheet's joints are set to where they are.
class ClothExample extends Example {
  ClothExample();

  @override
  String get name => 'Cloth';

  @override
  ExampleSection get section => ExampleSection.effects;

  @override
  String get blurb => 'A flag on a pole. The wind gusts and the cloth answers.';

  @override
  ViewPoint get viewpoint =>
      const ViewPoint(yaw: 0, distance: 9, pitch: 0.1, height: 2.6);

  final _air = _Gusts();
  final _sheet = OrblitParticleSheet(
    columns: _columns,
    rows: _rows,
    spacing: _spacing,
    colour: const [0.62, 0.06, 0.05],
  );
  late final _flag = _hung();

  /// The renderer's name for the sheet. The bytes behind a name must not
  /// change, so a change to the sheet is a change to the version here.
  static final _resource = OrblitResources.nameFor('cloth/v1/flag.glb');
  var _ready = false;
  var _requested = false;
  var _clock = 0.0;

  /// The wind in metres a second, along the flag.
  double get wind => _air.strength;
  set wind(double value) => _air.strength = value;

  SoftBody _hung() {
    final cloth = SoftBody.cloth(
      columns: _columns,
      rows: _rows,
      origin: _corner,
      across: Vector3(_spacing, 0, 0),
      down: Vector3(0, -_spacing, 0),
      air: _air,
      // A flag lifts when the pull of the wind beats its weight, which the
      // default drag does not reach at a breeze.
      spec: SoftSpec(windDrag: 3),
    );
    // The pole holds the whole first column.
    for (var row = 0; row < _rows; row++) {
      cloth.pin(row * _columns);
    }
    return cloth;
  }

  /// Hands the sheet to the renderer, once.
  void _provide() {
    if (_requested) return;
    _requested = true;
    void failed(Object error) {
      note = 'The flag could not be handed to the renderer: $error';
    }

    try {
      OrblitResources.provide(
        _resource,
        _sheet.toGlb(),
      ).then<void>((_) => _ready = true, onError: failed);
    } on Object catch (error) {
      failed(error);
    }
  }

  /// Steps the cloth to [seconds] in fixed steps. A jump in the clock, such as
  /// the example opening again, takes no steps rather than a hundred.
  void _advance(double seconds) {
    final owed = ((seconds - _clock) / _step).floor();
    if (owed < 0 || owed > 6) {
      _clock = seconds;
      return;
    }
    for (var n = 0; n < owed; n++) {
      _air.seconds = _clock;
      _flag.step(_step);
      _clock += _step;
    }
  }

  @override
  OrblitScene scene(OrblitCamera camera, double seconds) {
    _provide();
    _advance(seconds);

    return OrblitScene(
      objects: [
        OrblitObject(
          key: 1,
          // The cube is two metres wide, so a scale is a half-size.
          transform: Matrix4.translation(Vector3(-0.05, 1.9, 0))
            ..scaleByDouble(0.04, 1.9, 0.04, 1),
          colour: linearOf(const Color(0xFFB9BEC6)),
        ),
        OrblitObject(
          key: 2,
          transform: Matrix4.translation(Vector3(0, -0.03, 0))
            ..scaleByDouble(14, 0.03, 14, 1),
          colour: linearOf(const Color(0xFF39404A)),
          castShadows: false,
        ),
        if (_ready)
          OrblitObject(
            key: 3,
            mesh: _resource,
            transform: Matrix4.translation(_corner),
            colour: Vector3.all(1),
            joints: _sheet.poses((i) => _flag.positionOf(i) - _corner),
          ),
      ],
      lights: [
        OrblitLight(
          key: 10,
          kind: OrblitLightKind.directional,
          intensity: 76000,
          direction: Vector3(-0.4, -0.7, -0.6)..normalize(),
          colour: linearOf(const Color(0xFFFFF3E0)),
          sunAngularRadius: 1.2,
        ),
      ],
      sky: OrblitSky(
        colour: linearOf(const Color(0xFF6E8DB4)),
        zenith: linearOf(const Color(0xFF2F5F97)),
        horizon: linearOf(const Color(0xFFB9CBDD)),
        ambient: 22000,
        bodyDirection: Vector3(0.4, 0.7, 0.6)..normalize(),
        bodyColour: linearOf(const Color(0xFFFFF6E8)),
        bodySize: 0.011,
        quality: SkyQuality.fair,
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
          label: 'Wind',
          value: wind,
          min: 0,
          max: 14,
          decimals: 1,
          unit: ' m/s',
          onChanged: (value) {
            wind = value;
            changed();
          },
        ),
      ],
    );
  }

  @override
  String get code => '''
// The solver moves particles. The renderer takes joints, not vertices, so the
// sheet has a vertex and a joint for every particle.
final sheet = OrblitParticleSheet(columns: 20, rows: 12, spacing: 0.08);
await OrblitResources.provide(name, sheet.toGlb());

final flag = SoftBody.cloth(
  columns: 20, rows: 12, origin: corner,
  across: Vector3(0.08, 0, 0), down: Vector3(0, -0.08, 0),
  air: gusts, // a SoftAir: the wind at a point
);
for (var row = 0; row < 12; row++) {
  flag.pin(row * 20); // the pole holds the first column
}

// Every frame: step the cloth, then set each joint to its particle.
flag.step(1 / 60);
OrblitObject(
  key: 3, mesh: name, transform: Matrix4.translation(corner),
  colour: Vector3.all(1),
  joints: sheet.poses((i) => flag.positionOf(i) - corner),
);
''';
}
