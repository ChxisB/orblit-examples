import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:orblit_gallery/main.dart';
import 'package:orblit_gallery/src/code_view.dart';

/// The code panel: coloured by what each word is, and broken to fit the panel
/// where somebody would have broken it, rather than wherever the text engine
/// runs out of room.
void main() {
  TestWidgetsFlutterBinding.ensureInitialized();
  final examples = galleryExamples();

  test('only the two script examples are read as TSX', () {
    expect([
      for (final example in examples)
        if (CodeLanguage.of(example.code) == CodeLanguage.tsx) example.name,
    ], unorderedEquals(['An interface in TypeScript', 'Spawning from script']));
  });

  test('Dart is coloured by what each word is', () {
    final lines = highlight(
      "@override\n"
      "final light = OrblitLight(key: 200, castShadows: true)..normalize();\n"
      "  // lux\n"
      "for (final x in xs) print('a // b', 0xFF0B1224, 0.53);",
      CodeLanguage.dart,
    );
    expect(_tone(lines, '@override'), Tone.type);
    expect(_tone(lines, 'final'), Tone.keyword);
    expect(_tone(lines, 'light'), Tone.name);
    expect(_tone(lines, 'OrblitLight'), Tone.type);
    expect(_tone(lines, 'key'), Tone.name);
    expect(_tone(lines, '200'), Tone.number);
    expect(_tone(lines, 'true'), Tone.keyword);
    expect(_tone(lines, 'normalize'), Tone.call);
    expect(_tone(lines, '// lux'), Tone.comment);
    expect(_tone(lines, 'for'), Tone.control);
    expect(_tone(lines, 'print'), Tone.call);
    // A comment marker inside a string is part of the string.
    expect(_tone(lines, "'a // b'"), Tone.string);
    expect(_tone(lines, '0xFF0B1224'), Tone.number);
    expect(_tone(lines, '0.53'), Tone.number);
  });

  test('TSX tells a tag from a less-than, and text from code', () {
    final lines = highlight(
      'for (let i = 0; i < many; i++) {}\n'
      'return (\n'
      '  <button class="px-3" onPressed={() => { go(1); }}>\n'
      '    Repair\n'
      '  </button>\n'
      ');\n'
      'mount(() => <Hud />);',
      CodeLanguage.tsx,
    );
    expect(_tone(lines, '<'), Tone.plain); // i < many
    expect(_tone(lines, 'many'), Tone.name);
    expect(_tone(lines, 'button'), Tone.tag);
    expect(_tone(lines, 'class'), Tone.name); // an attribute, not `class`
    expect(_tone(lines, '"px-3"'), Tone.string);
    expect(_tone(lines, 'go'), Tone.call);
    // What sits between the tags is text, whatever case it is in...
    expect(_tone(lines, 'Repair'), Tone.plain);
    // ...and once the last tag is closed, what follows is code again.
    expect(_tone(lines, 'mount'), Tone.call);
    expect(_tone(lines, 'Hud'), Tone.type);
  });

  test('every example fits the panel, and nothing is lost in fitting it', () {
    for (final columns in [40, 54, 80]) {
      for (final example in examples) {
        final lines = highlight(example.code, CodeLanguage.of(example.code));
        final fitted = fit(lines, columns);
        for (final line in fitted) {
          expect(
            line.length,
            lessThanOrEqualTo(columns),
            reason: '${example.name} at $columns: "${line.text}"',
          );
        }
        // The same characters, in whatever lines, less the comment markers
        // that flowing a comment adds.
        expect(
          _letters(fitted),
          _letters(lines),
          reason: '${example.name} at $columns',
        );
      }
    }
  });

  test('a paragraph of comment is flowed as one', () {
    final lines = highlight(
      '  // The key is what makes saying it again cheap. Same key, same\n'
      '  // object: the renderer moves what moved.\n'
      '  //\n'
      '  // A second paragraph.',
      CodeLanguage.dart,
    );
    expect(
      [for (final line in fit(lines, 36)) line.text],
      [
        '  // The key is what makes saying it',
        '  // again cheap. Same key, same',
        '  // object: the renderer moves what',
        '  // moved.',
        '  //',
        '  // A second paragraph.',
      ],
    );
  });

  test('code that does not fit carries on deeper than it started', () {
    final lines = highlight(
      '    key: 1000 + i,          // its own, for as long as it exists\n'
      '    spawn({ id: "core", at: [0, 0.4, 0], size: [0.8, 2.4, 0.8] });',
      CodeLanguage.tsx,
    );
    expect(
      [for (final line in fit(lines, 44)) line.text],
      [
        // The comment on the end goes above the line it was on.
        '    // its own, for as long as it exists',
        '    key: 1000 + i,',
        // Broken after a comma, and four deeper.
        '    spawn({ id: "core", at: [0, 0.4, 0],',
        '        size: [0.8, 2.4, 0.8] });',
      ],
    );
  });

  test('a list is not split while what it is in could be', () {
    final lines = highlight(
      '  spawn({ id: "core", at: [0, 0.4, 0], size: [0.8, 2.4, 0.8],\n'
      '          colour: "#F2F4F7" });',
      CodeLanguage.tsx,
    );
    expect(
      [for (final line in fit(lines, 52)) line.text],
      [
        // There is room for `size: [0.8,` on the first row, but the comma
        // after `at` is one bracket shallower...
        '  spawn({ id: "core", at: [0, 0.4, 0],',
        // ...and what is broken off lines up with the line written after it.
        '          size: [0.8, 2.4, 0.8],',
        '          colour: "#F2F4F7" });',
      ],
    );
  });

  test('lines that fit are left exactly as written', () {
    final lines = highlight(
      'OrblitLight(\n'
      '  intensity: 82000,                  // lux\n'
      '  falloffRadius: 24,                 // metres\n'
      ')',
      CodeLanguage.dart,
    );
    expect(
      [for (final line in fit(lines, 60)) line.text],
      [for (final line in lines) line.text],
    );
  });

  testWidgets('the panel shows the code coloured, and opens it wider', (
    tester,
  ) async {
    tester.view.physicalSize = const Size(1600, 1000);
    tester.view.devicePixelRatio = 1;
    addTearDown(tester.view.reset);

    await tester.pumpWidget(const GalleryApp());
    await tester.pump();

    expect(find.byType(CodeView), findsOneWidget);
    expect(find.text('Dart'), findsOneWidget);

    final shown = tester.widget<SelectableText>(
      find.descendant(
        of: find.byType(CodeView),
        matching: find.byType(SelectableText),
      ),
    );
    final colours = <Color?>{};
    shown.textSpan!.visitChildren((span) {
      colours.add(span.style?.color);
      return true;
    });
    expect(colours.length, greaterThanOrEqualTo(5));

    // The gallery's clock never stops, so this waits out the dialog's
    // transition rather than for everything to settle.
    await tester.tap(find.byTooltip('Open wider'));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 400));
    expect(find.byType(CodeView), findsNWidgets(2));

    await tester.tap(find.byTooltip('Close'));
    await tester.pump();
    await tester.pump(const Duration(milliseconds: 400));
    expect(find.byType(CodeView), findsOneWidget);
  });

  testWidgets('long code opens at its first rows, and shows all when asked', (
    tester,
  ) async {
    // A short window, so there is more code than room for it.
    await _open(tester, height: 700);
    final first = _rowsShown(tester);
    final rowHeight = 11 * 1.55;
    expect(first, ((700 - 320) / rowHeight).floor());

    await tester.tap(find.textContaining('Show all'));
    await _settle(tester);
    expect(_rowsShown(tester), greaterThan(first));

    await tester.ensureVisible(find.text('Show less'));
    await tester.pump();
    await tester.tap(find.text('Show less'));
    await _settle(tester);
    expect(_rowsShown(tester), first);
  });

  testWidgets('the bar folds the code away, and it stays folded', (
    tester,
  ) async {
    await _open(tester);
    final code = find.descendant(
      of: find.byType(CodeView),
      matching: find.byType(SelectableText),
    );
    expect(code, findsOneWidget);

    await tester.tap(find.text('Dart'));
    await _settle(tester);
    expect(code, findsNothing);
    // The bar is still there, to say what is folded and to open it again.
    expect(find.textContaining(' lines'), findsOneWidget);

    // Folded for the next example too: somebody who folded it is looking at
    // scenes, not code.
    await tester.tap(find.text('Virtual cameras'));
    await _settle(tester);
    expect(code, findsNothing);

    await tester.tap(find.text('Dart'));
    await _settle(tester);
    expect(code, findsOneWidget);
  });
}

Future<void> _open(WidgetTester tester, {double height = 1000}) async {
  tester.view.physicalSize = Size(1600, height);
  tester.view.devicePixelRatio = 1;
  addTearDown(tester.view.reset);
  await tester.pumpWidget(const GalleryApp());
  await tester.pump();
}

/// The gallery's clock never stops, so this waits out an animation rather
/// than for everything to settle.
Future<void> _settle(WidgetTester tester) async {
  await tester.pump();
  await tester.pump(const Duration(milliseconds: 400));
}

/// How many rows the panel's code is showing.
int _rowsShown(WidgetTester tester) {
  final shown = tester.widget<SelectableText>(
    find.descendant(
      of: find.byType(CodeView),
      matching: find.byType(SelectableText),
    ),
  );
  return '\n'.allMatches(shown.textSpan!.toPlainText()).length + 1;
}

/// The tone of the first character of the first place [text] appears.
Tone _tone(List<CodeLine> lines, String text) {
  for (final line in lines) {
    final at = line.text.indexOf(text);
    if (at >= 0) return line.tones[at];
  }
  throw StateError('"$text" is not in the code');
}

/// Every character but space and slashes, in order of code unit.
List<int> _letters(List<CodeLine> lines) => [
  for (final line in lines)
    for (final unit in line.text.codeUnits)
      if (unit != 0x20 && unit != 0x2F) unit,
]..sort();
