import 'dart:convert';
import 'dart:io';

/// Puts the compiled game scripts inside the app that runs them.
///
/// A real game loads its script from a file and reloads it while running,
/// which is most of the point of having one. An example has to run for anybody
/// who clones this repository without a TypeScript compiler on their machine,
/// so what it loads is checked in — generated from `script/*.tsx` by
/// `npm run build`.
void main() {
  final built = Directory('script/build');
  if (!built.existsSync()) {
    stderr.writeln(
      'No script/build. Run `npm run build` from the repository root first — '
      'that is what compiles the TypeScript in script/.',
    );
    exit(1);
  }

  final compiled = built
      .listSync()
      .whereType<File>()
      .where((file) => file.path.endsWith('.js'))
      .toList()
    ..sort((a, b) => a.path.compareTo(b.path));

  final buffer = StringBuffer('''
// Generated from script/*.tsx by `npm run build`. Do not edit.
//
// The compiled output of the TypeScript the examples run. Checked in so they
// work without a TypeScript compiler present.
''');

  for (final file in compiled) {
    final name = file.uri.pathSegments.last.replaceAll('.js', '');
    buffer
      ..writeln()
      ..writeln('/// `script/$name.tsx`, as the engine takes it.')
      ..writeln(
        'const String ${name}Script = '
        '${jsonEncode(file.readAsStringSync()).replaceAll(r'$', r'\$')};',
      );
  }

  File('lib/src/examples/scripts.g.dart').writeAsStringSync(buffer.toString());
  stdout.writeln(
    'Embedded ${compiled.length} scripts: '
    '${compiled.map((f) => f.uri.pathSegments.last).join(', ')}',
  );
}
