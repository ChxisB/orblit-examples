import 'dart:io' show Platform;

import 'package:flutter/foundation.dart';
import 'package:flutter/gestures.dart';
import 'package:flutter/material.dart';
import 'package:flutter/scheduler.dart';
import 'package:flutter/services.dart';
import 'package:orbis_filament/orbis_filament.dart';

import 'src/example.dart';
import 'src/examples/cameras.dart';
import 'src/examples/crowd.dart';
import 'src/examples/day_and_night.dart';
import 'src/examples/interface.dart';
import 'src/examples/lights.dart';
import 'src/examples/many.dart';
import 'src/examples/meshes.dart';
import 'src/examples/surface.dart';
import 'src/examples/weather.dart';

void main() => runApp(const GalleryApp());

/// What the engine does, one technique at a time.
///
/// Every example is its own file and stands on its own: what it needs to work
/// is what is written in it, and that is what the code panel shows. The
/// gallery itself only picks between them, drives a camera and runs a clock.
class GalleryApp extends StatelessWidget {
  const GalleryApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'Orbis Examples',
      debugShowCheckedModeBanner: false,
      theme: ThemeData(
        useMaterial3: true,
        brightness: Brightness.dark,
        colorScheme: ColorScheme.fromSeed(
          seedColor: const Color(0xFFC25E22),
          brightness: Brightness.dark,
        ),
        scaffoldBackgroundColor: const Color(0xFF0F1319),
        textTheme: const TextTheme(
          bodySmall: TextStyle(fontSize: 12, color: Color(0xFFB6BFCC)),
        ),
      ),
      home: const Gallery(),
    );
  }
}

class Gallery extends StatefulWidget {
  const Gallery({super.key});

  @override
  State<Gallery> createState() => _GalleryState();
}

class _GalleryState extends State<Gallery> with SingleTickerProviderStateMixin {
  late final List<Example> _examples = [
    SurfaceExample(),
    LightsExample(),
    DayAndNightExample(),
    WeatherExample(),
    InterfaceExample(),
    ManyExample(),
    CrowdExample(),
    CamerasExample(),
    MeshesExample(),
  ];

  /// Which one to open on.
  ///
  /// Named by an environment variable so a screenshot, a bug report or a demo
  /// can start where it means to rather than on whatever happens to be first.
  late Example _showing = _examples.firstWhere(
    (example) => example.name
        .toLowerCase()
        .startsWith(_wanted?.toLowerCase() ?? '\u0000'),
    orElse: () => _examples.first,
  );

  static final String? _wanted =
      const String.fromEnvironment('ORBIS_EXAMPLE').isEmpty
          ? Platform.environment['ORBIS_EXAMPLE']
          : const String.fromEnvironment('ORBIS_EXAMPLE');
  late GalleryCamera _camera = GalleryCamera.from(_showing.viewpoint);

  /// One clock for the lot.
  ///
  /// An example is handed how long *it* has been on screen rather than the
  /// wall time, so opening one always starts it at the beginning — a day
  /// cycle that began four minutes ago in another example is not a day cycle
  /// anybody asked to watch from the middle.
  /// Started in initState rather than declared with an initialiser.
  ///
  /// A `late final` field is built the first time it is read, and nothing
  /// here ever read this one — so the ticker was never created and never
  /// started. Everything the examples animate from Dart was frozen at zero,
  /// and it did not show because the sky and the rain run off the renderer's
  /// own clock and carried on moving regardless.
  Ticker? _clock;
  Duration _startedAt = Duration.zero;
  bool _restarting = true;
  double _seconds = 0;

  Offset? _dragging;

  @override
  void initState() {
    super.initState();
    _clock = createTicker(_tick)..start();
  }

  void _tick(Duration elapsed) {
    if (_restarting) {
      _startedAt = elapsed;
      _restarting = false;
    }
    setState(() => _seconds = (elapsed - _startedAt).inMicroseconds / 1e6);
  }

  void _show(Example example) {
    setState(() {
      _showing = example;
      _camera = GalleryCamera.from(example.viewpoint);
      _restarting = true;
      _seconds = 0;
    });
  }

  @override
  void dispose() {
    _clock?.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final available = !kIsWeb && defaultTargetPlatform == TargetPlatform.macOS;

    return Scaffold(
      body: Row(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          _ExampleList(
            examples: _examples,
            showing: _showing,
            onShow: _show,
          ),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                _Header(example: _showing),
                Expanded(
                  child: Padding(
                    padding: const EdgeInsets.fromLTRB(16, 0, 16, 16),
                    child: ClipRRect(
                      borderRadius: BorderRadius.circular(10),
                      child: Stack(
                        children: [
                          Positioned.fill(
                            child:
                                available ? _stage() : const _Unavailable(),
                          ),
                          // Whatever the example draws over its scene, which
                          // for most of them is nothing.
                          if (_showing.overlay(
                                context,
                                () => setState(() {}),
                              )
                              case final over?)
                            Positioned.fill(child: over),
                        ],
                      ),
                    ),
                  ),
                ),
              ],
            ),
          ),
          _Panel(example: _showing, onChanged: () => setState(() {})),
        ],
      ),
    );
  }

  Widget _stage() {
    return Listener(
      onPointerSignal: (event) {
        if (event is! PointerScrollEvent) return;
        setState(() => _camera.zoom(event.scrollDelta.dy));
      },
      child: GestureDetector(
        behavior: HitTestBehavior.opaque,
        onPanStart: (details) => _dragging = details.localPosition,
        onPanUpdate: (details) {
          final from = _dragging;
          if (from == null) return;
          setState(() => _camera.orbit(details.localPosition - from));
          _dragging = details.localPosition;
        },
        onPanEnd: (_) => _dragging = null,
        child: OrbisView(
          scene: _showing.scene(_camera.toRenderCamera(), _seconds),
          onSceneNotes: (notes) {
            // Only one example has anything to say about a file it could not
            // load; the rest have nothing to report and nowhere to put it.
            final example = _showing;
            if (example is MeshesExample && notes.isNotEmpty) {
              setState(() => example.note = notes.values.first);
            }
          },
        ),
      ),
    );
  }
}

/// The list on the left.
class _ExampleList extends StatelessWidget {
  const _ExampleList({
    required this.examples,
    required this.showing,
    required this.onShow,
  });

  final List<Example> examples;
  final Example showing;
  final ValueChanged<Example> onShow;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: 232,
      decoration: const BoxDecoration(
        color: Color(0xFF141922),
        border: Border(right: BorderSide(color: Color(0xFF232B36))),
      ),
      child: ListView(
        padding: const EdgeInsets.symmetric(vertical: 12),
        children: [
          const Padding(
            padding: EdgeInsets.fromLTRB(16, 6, 16, 14),
            child: Text(
              'ORBIS',
              style: TextStyle(
                fontSize: 12,
                letterSpacing: 3,
                fontWeight: FontWeight.w600,
                color: Color(0xFFC25E22),
              ),
            ),
          ),
          for (final example in examples)
            _ListRow(
              example: example,
              selected: identical(example, showing),
              onTap: () => onShow(example),
            ),
        ],
      ),
    );
  }
}

class _ListRow extends StatelessWidget {
  const _ListRow({
    required this.example,
    required this.selected,
    required this.onTap,
  });

  final Example example;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        margin: const EdgeInsets.fromLTRB(8, 2, 8, 2),
        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 9),
        decoration: BoxDecoration(
          color: selected ? const Color(0xFF1F2732) : Colors.transparent,
          borderRadius: BorderRadius.circular(6),
          border: Border(
            left: BorderSide(
              color: selected ? const Color(0xFFC25E22) : Colors.transparent,
              width: 2,
            ),
          ),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              example.name,
              style: TextStyle(
                fontSize: 13,
                fontWeight: FontWeight.w600,
                color: selected ? Colors.white : const Color(0xFFC5CDD8),
              ),
            ),
            const SizedBox(height: 2),
            Text(
              example.blurb,
              style: const TextStyle(fontSize: 11, color: Color(0xFF7C8798)),
            ),
          ],
        ),
      ),
    );
  }
}

class _Header extends StatelessWidget {
  const _Header({required this.example});

  final Example example;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(18, 18, 18, 12),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            example.name,
            style: const TextStyle(fontSize: 19, fontWeight: FontWeight.w600),
          ),
          const SizedBox(height: 3),
          Text(
            '${example.blurb}  ·  drag to orbit, scroll to zoom',
            style: const TextStyle(fontSize: 12, color: Color(0xFF7C8798)),
          ),
        ],
      ),
    );
  }
}

/// The settings and the code, on the right.
class _Panel extends StatelessWidget {
  const _Panel({required this.example, required this.onChanged});

  final Example example;
  final VoidCallback onChanged;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: 380,
      decoration: const BoxDecoration(
        color: Color(0xFF141922),
        border: Border(left: BorderSide(color: Color(0xFF232B36))),
      ),
      child: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          const _PanelTitle('Settings'),
          example.settings(context, onChanged),
          const SizedBox(height: 20),
          const _PanelTitle('How it is written'),
          _Code(example.code),
        ],
      ),
    );
  }
}

class _PanelTitle extends StatelessWidget {
  const _PanelTitle(this.text);

  final String text;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 10),
      child: Text(
        text.toUpperCase(),
        style: const TextStyle(
          fontSize: 10.5,
          letterSpacing: 1.6,
          fontWeight: FontWeight.w600,
          color: Color(0xFF6E7A8C),
        ),
      ),
    );
  }
}

class _Code extends StatelessWidget {
  const _Code(this.source);

  final String source;

  @override
  Widget build(BuildContext context) {
    return Stack(
      children: [
        Container(
          width: double.infinity,
          padding: const EdgeInsets.all(12),
          decoration: BoxDecoration(
            color: const Color(0xFF0D1117),
            borderRadius: BorderRadius.circular(6),
            border: Border.all(color: const Color(0xFF232B36)),
          ),
          child: SelectableText(
            source.trim(),
            style: const TextStyle(
              fontFamily: 'Menlo',
              fontSize: 11,
              height: 1.5,
              color: Color(0xFFC5CDD8),
            ),
          ),
        ),
        Positioned(
          top: 4,
          right: 4,
          child: IconButton(
            iconSize: 14,
            tooltip: 'Copy',
            icon: const Icon(Icons.copy_all_outlined),
            color: const Color(0xFF6E7A8C),
            onPressed: () =>
                Clipboard.setData(ClipboardData(text: source.trim())),
          ),
        ),
      ],
    );
  }
}

class _Unavailable extends StatelessWidget {
  const _Unavailable();

  @override
  Widget build(BuildContext context) {
    return const ColoredBox(
      color: Color(0xFF161B22),
      child: Center(
        child: Text(
          'The renderer runs on macOS so far.',
          style: TextStyle(color: Color(0xFF7C8798)),
        ),
      ),
    );
  }
}
