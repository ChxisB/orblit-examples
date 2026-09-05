import 'dart:math' as math;

import 'package:flutter/material.dart';
import 'package:orbis_filament/orbis_filament.dart';
import 'package:vector_math/vector_math_64.dart' hide Colors;

import '../example.dart';
import 'surface.dart' show linearOf;

/// Air with something in it.
///
/// Three separate things that read as one: an even haze for distance, banks
/// of cloud for shape, and a curtain of drops for what is coming down. The
/// first cannot look like anything in particular, the second cannot tell you
/// how far away something is, and the third is nothing without either.
class WeatherExample extends Example {
  WeatherExample();

  @override
  String get name => 'Weather';

  @override
  String get blurb =>
      'Haze, banks of cloud and falling weather, carried by one wind.';

  @override
  ViewPoint get viewpoint => const ViewPoint(distance: 16, pitch: 0.12);

  String condition = 'Misty';

  double density = 0.055;
  double structure = 0.75;
  double cloudSize = 22;
  double height = -1.5;
  double windSpeed = 2.5;
  double windBearing = 135;
  double rain = 0;
  double snow = 0;

  static const _presets = {
    'Clear': [0.004, 0.0, 40.0, 0.0, 1.5, 0.0, 0.0],
    'Misty': [0.055, 0.75, 22.0, -1.5, 2.5, 0.0, 0.0],
    'Rain': [0.035, 0.35, 70.0, 0.0, 5.0, 0.65, 0.0],
    'Snow': [0.04, 0.4, 60.0, 0.0, 2.2, 0.0, 0.75],
  };

  void _apply(String name) {
    final preset = _presets[name]!;
    condition = name;
    density = preset[0];
    structure = preset[1];
    cloudSize = preset[2];
    height = preset[3];
    windSpeed = preset[4];
    rain = preset[5];
    snow = preset[6];
  }

  @override
  OrbisScene scene(OrbisCamera camera, double seconds) {
    final bearing = windBearing * math.pi / 180;
    final wind = Vector2(math.sin(bearing), math.cos(bearing)) * windSpeed;

    final falling = rain + snow;
    final asSnow = falling <= 0 ? 0.0 : (snow / falling).clamp(0.0, 1.0);
    double between(double wet, double white) => wet + (white - wet) * asSnow;

    return OrbisScene(
      objects: [
        for (var i = 0; i < 7; i++)
          OrbisObject(
            key: 400 + i,
            transform: Matrix4.identity()
              ..setTranslation(Vector3(
                math.cos(i * 1.9) * (3 + i * 0.7),
                -0.9 + (i % 3) * 0.4,
                math.sin(i * 1.9) * (3 + i * 0.7),
              ))
              ..rotateY(i * 0.6)
              ..scaleByDouble(0.8, 1.4 + (i % 3) * 0.7, 0.8, 1),
            colour: linearOf(
              i.isEven ? const Color(0xFF6E6A63) : const Color(0xFF8A8279),
            ),
          ),
        OrbisObject(
          key: 390,
          transform: Matrix4.identity()
            ..setTranslation(Vector3(0, -1.9, 0))
            ..scaleByDouble(40.0, 0.06, 40.0, 1),
          colour: linearOf(const Color(0xFF4A4E52)),
          castShadows: false,
        ),
      ],
      lights: [
        OrbisLight(
          key: 410,
          kind: OrbisLightKind.directional,
          // Dimmer than a clear day, because there is cloud in the way of it.
          intensity: 30000,
          direction: Vector3(-0.35, -0.8, -0.5)..normalize(),
          colour: linearOf(const Color(0xFFEFEFEA)),
          sunAngularRadius: 6,
        ),
      ],
      sky: OrbisSky(colour: linearOf(const Color(0xFF6E757D)), ambient: 22000),
      fog: OrbisFog(
        colour: linearOf(const Color(0xFFD3D8DD)),
        density: density,
        height: height,
        heightFalloff: 0.4,
        // Above zero, the same air is also drawn as banks of cloud.
        structure: structure,
        wind: wind,
        // Turns of the noise per metre: the reciprocal of how big a cloud is.
        featureSize: 1 / math.max(cloudSize, 0.5),
        thickness: 6,
      ),
      precipitation: falling <= 0
          ? OrbisPrecipitation.none
          : OrbisPrecipitation(
              colour: linearOf(
                Color.lerp(const Color(0xFFB8C6D6), const Color(0xFFF2F5F8),
                    asSnow)!,
              ),
              amount: falling.clamp(0.0, 1.0),
              // Nine metres a second for rain, under one for snow.
              fall: between(9, 0.8),
              // Snow is taken by the wind far more: it weighs nothing and it
              // has all day.
              wind: wind * between(0.6, 1.6),
              dropsPerMetre: between(8, 3.5),
              // How far a drop travels while the shutter is open. A streak,
              // or a flake.
              stretch: between(30, 5),
              threshold: between(0.7, 0.55),
            ),
      camera: camera,
    );
  }

  @override
  Widget settings(BuildContext context, VoidCallback changed) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Choice(
          label: 'Condition',
          options: _presets.keys.toList(),
          selected: condition,
          onSelect: (option) {
            _apply(option);
            changed();
          },
        ),
        Setting(
          label: 'Haze',
          value: density,
          min: 0,
          max: 0.2,
          decimals: 3,
          onChanged: (value) {
            density = value;
            changed();
          },
        ),
        Setting(
          label: 'Cloud',
          value: structure,
          min: 0,
          max: 1,
          onChanged: (value) {
            structure = value;
            changed();
          },
        ),
        if (structure > 0)
          Setting(
            label: 'Cloud size',
            value: cloudSize,
            min: 4,
            max: 120,
            unit: ' m',
            decimals: 0,
            onChanged: (value) {
              cloudSize = value;
              changed();
            },
          ),
        Setting(
          label: 'Height',
          value: height,
          min: -8,
          max: 8,
          unit: ' m',
          decimals: 1,
          onChanged: (value) {
            height = value;
            changed();
          },
        ),
        Setting(
          label: 'Rain',
          value: rain,
          min: 0,
          max: 1,
          onChanged: (value) {
            rain = value;
            changed();
          },
        ),
        Setting(
          label: 'Snow',
          value: snow,
          min: 0,
          max: 1,
          onChanged: (value) {
            snow = value;
            changed();
          },
        ),
        Setting(
          label: 'Wind',
          value: windSpeed,
          min: 0,
          max: 20,
          unit: ' m/s',
          decimals: 1,
          onChanged: (value) {
            windSpeed = value;
            changed();
          },
        ),
        Setting(
          label: 'Bearing',
          value: windBearing,
          min: 0,
          max: 360,
          unit: '°',
          decimals: 0,
          onChanged: (value) {
            windBearing = value;
            changed();
          },
        ),
      ],
    );
  }

  @override
  String get code => '''
// Haze for distance, banks for shape. One setting, because weather with no
// haze behind it reads as cut-outs hanging in clear air.
fog: OrbisFog(
  colour: Vector3(0.65, 0.69, 0.73),
  density: 0.055,           // per metre
  height: -1.5,             // where the layer lies
  heightFalloff: 0.4,       // how fast it thins going up
  structure: 0.75,          // above zero, banks of cloud are drawn as well
  wind: Vector2(1.8, -1.8), // metres a second, across the ground
  featureSize: 1 / 22,      // one over how big a cloud is, in metres
  thickness: 6,             // how deep the bank is
),

// Rain and snow are one curtain at different settings: what separates them is
// how far a drop travels while the shutter is open.
precipitation: OrbisPrecipitation(
  amount: 0.65,
  fall: 9,                  // metres a second; snow is under one
  wind: wind,
  dropsPerMetre: 6,
  stretch: 30,              // a streak. 1.2 is a flake.
),

// The renderer moves both on its own clock, so a still scene keeps raining
// without the host sending another frame.
''';
}
