/// Breaks coloured lines to a width without losing how they line up.
library;

import 'dart:math' as math;

import 'code_highlight.dart';

/// [lines] broken to fit [columns] characters, where somebody writing for a
/// page that narrow would have broken them.
///
/// A paragraph of comment that does not fit is flowed again to the width, as
/// one paragraph rather than as each of its lines wrapped separately. A
/// comment at the end of a line that does not fit goes on a line of its own
/// above it. Code that still does not fit is broken at a space, after a comma
/// where there is one, and carries on four deeper than the line it belongs to,
/// so the indentation that says what belongs to what survives the break. Or
/// as deep as the line after it, where that was lined up by hand to carry on
/// from this one, so what was broken lines up with what was written.
///
/// Lines that fit are left exactly as written, alignment and all.
List<CodeLine> fit(List<CodeLine> lines, int columns) {
  final out = <CodeLine>[];
  var i = 0;
  while (i < lines.length) {
    final line = lines[i];

    final remark = _Remark.of(line);
    if (remark != null) {
      final paragraph = [remark];
      var j = i + 1;
      while (j < lines.length) {
        final next = _Remark.of(lines[j]);
        if (next == null || !paragraph.last.carriesOnTo(next)) break;
        paragraph.add(next);
        j++;
      }
      if (paragraph.any((one) => one.line.length > columns)) {
        final words = [for (final one in paragraph) one.body].join(' ');
        out.addAll(_flow(remark.indent, remark.marker, words, columns));
      } else {
        out.addAll(lines.sublist(i, j));
      }
      i = j;
      continue;
    }

    i++;
    if (line.length <= columns) {
      out.add(line);
      continue;
    }
    final at = _trailingRemark(line);
    final code = at == null ? line : line.slice(0, at).trimRight();
    final hang = _alignedHang(code, i < lines.length ? lines[i] : null);
    if (at != null) {
      final words = line.text.substring(at + 2).trim();
      out.addAll(_flow(line.indent, '//', words, columns));
    }
    out.addAll(_wrap(code, columns, hang));
  }
  return out;
}


/// A line that is nothing but a `//` comment.
class _Remark {
  _Remark(this.line, this.indent, this.marker, this.body);

  final CodeLine line;
  final int indent;

  /// `//`, or `///` for documentation.
  final String marker;

  /// What it says, after the marker and one space.
  final String body;

  static final _shape = RegExp(r'^(///?)(?: (.*))?$');

  static _Remark? of(CodeLine line) {
    final indent = line.indent;
    if (indent >= line.length || line.tones[indent] != Tone.comment) {
      return null;
    }
    final match = _shape.firstMatch(line.text.substring(indent));
    if (match == null) return null;
    return _Remark(line, indent, match[1]!, match[2] ?? '');
  }

  static final _listItem = RegExp(r'^([-*•]|\d+[.)]) ');

  /// Whether [next] is more of the same paragraph: the same marker at the
  /// same depth, and neither of them a blank line, an indented line or the
  /// start of a list item, which were laid out by hand and mean to stay so.
  bool carriesOnTo(_Remark next) =>
      next.indent == indent &&
      next.marker == marker &&
      body.isNotEmpty &&
      !body.startsWith(' ') &&
      next.body.isNotEmpty &&
      !next.body.startsWith(' ') &&
      !_listItem.hasMatch(next.body);
}

/// Where the comment at the end of [line] starts, or null if it has none.
int? _trailingRemark(CodeLine line) {
  for (var at = line.indent; at < line.length; at++) {
    if (line.tones[at] == Tone.comment) {
      return at > line.indent && line.text.startsWith('//', at) ? at : null;
    }
  }
  return null;
}

/// [words] as a comment [indent] deep, filling each line up to [columns].
List<CodeLine> _flow(int indent, String marker, String words, int columns) {
  final lead = '${' ' * indent}$marker ';
  final width = math.max(columns - lead.length, 16);
  final rows = <String>[];
  var row = '';
  for (var word in words.split(RegExp(r'\s+'))) {
    if (word.isEmpty) continue;
    // A word longer than a whole line, such as a path, is cut.
    while (word.length > width) {
      if (row.isNotEmpty) rows.add(row);
      row = '';
      rows.add(word.substring(0, width));
      word = word.substring(width);
    }
    if (row.isEmpty) {
      row = word;
    } else if (row.length + 1 + word.length <= width) {
      row = '$row $word';
    } else {
      rows.add(row);
      row = word;
    }
  }
  if (row.isNotEmpty || rows.isEmpty) rows.add(row);
  return [
    for (final one in rows) CodeLine.of('$lead$one'.trimRight(), Tone.comment),
  ];
}

/// How deep [next] is, if it carries on from [line] at a depth somebody
/// chose by hand: deeper than the two a block steps in by, and after a line
/// that does not open a bracket, so it is not the first line of a block.
int? _alignedHang(CodeLine line, CodeLine? next) {
  if (next == null || next.text.trim().isEmpty) return null;
  if (next.indent <= line.indent + 2) return null;
  final end = line.text.trimRight();
  if (end.endsWith('(') || end.endsWith('[') || end.endsWith('{')) return null;
  return next.indent;
}

/// [line], which is code, broken into lines of at most [columns], each after
/// the first carrying on [hang] deep, or four deeper than [line] if not.
List<CodeLine> _wrap(CodeLine line, int columns, [int? hang]) {
  final deep = math.min(hang ?? line.indent + 4, columns ~/ 2);
  final rows = <CodeLine>[];
  var rest = line;
  var lead = line.indent;
  while (rest.length > columns) {
    final at = _breakIn(rest, lead, columns);
    rows.add(rest.slice(0, at).trimRight());
    var from = at;
    while (from < rest.length && rest.text.codeUnitAt(from) == spaceUnit) {
      from++;
    }
    rest = CodeLine.of(' ' * deep, Tone.plain) + rest.slice(from);
    lead = deep;
  }
  rows.add(rest);
  return rows;
}

/// Where to break [line], which is wider than [columns] and starts with
/// [lead] spaces: at a space after a comma if there is one past halfway, the
/// one fewest brackets deep so that a list is not split while what it is in
/// could be, at any other space outside a string if not, and in the middle
/// of whatever is there if there is no space at all.
int _breakIn(CodeLine line, int lead, int columns) {
  int? latest(bool Function(int at) good) {
    for (var at = columns; at > lead; at--) {
      if (good(at)) return at;
    }
    return null;
  }

  bool space(int at) =>
      at < line.length && line.text.codeUnitAt(at) == spaceUnit;
  bool bare(int at) => space(at) && line.tones[at] != Tone.string;

  // How many brackets are open before each column, counting only code.
  final depth = List<int>.filled(columns + 1, 0);
  var open = 0;
  for (var at = 0; at < columns && at < line.length; at++) {
    final tone = line.tones[at];
    if (tone != Tone.string && tone != Tone.comment) {
      final c = line.text[at];
      if (c == '(' || c == '[' || c == '{') open++;
      if (c == ')' || c == ']' || c == '}') open--;
    }
    depth[at + 1] = open;
  }

  int? comma;
  for (var at = columns; at - lead >= (columns - lead) ~/ 2; at--) {
    if (bare(at) && line.text[at - 1] == ',') {
      if (comma == null || depth[at] < depth[comma]) comma = at;
    }
  }
  if (comma != null) return comma;
  return latest(bare) ?? latest(space) ?? columns;
}
