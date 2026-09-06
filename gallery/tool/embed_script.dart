import 'dart:convert';
import 'dart:io';

/// Puts the compiled game script inside the app that runs it.
///
/// A real game would load its script from a file and reload it while running,
/// which is most of the point of having one. An example has to run for anybody
/// who clones this repository without a TypeScript compiler on their machine,
/// so what it loads is checked in — generated from `script/hud.tsx` by
/// `npm run build`.
void main() {
  final compiled = File('script/build/hud.js');
  if (!compiled.existsSync()) {
    stderr.writeln(
      'No script/build/hud.js. Run `npm run build` from the repository root '
      'first — that is what compiles script/hud.tsx.',
    );
    exit(1);
  }

  final source = compiled.readAsStringSync();

  File('lib/src/examples/hud_script.g.dart').writeAsStringSync('''
// Generated from script/hud.tsx by `npm run build`. Do not edit.
//
// The compiled output of the TypeScript the interface example runs. Checked in
// so the examples work without a TypeScript compiler present.

/// The interface example's own script, as the engine takes it.
const String hudScript = ${jsonEncode(source).replaceAll(r'$', r'\$')};
''');

  stdout.writeln('Embedded ${source.length} characters from script/hud.tsx');
}
