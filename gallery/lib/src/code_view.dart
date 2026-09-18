import 'dart:async';
import 'dart:math' as math;

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

/// What a snippet is written in.
enum CodeLanguage {
  dart('Dart'),
  tsx('TSX');

  const CodeLanguage(this.label);

  /// What the code block says it is.
  final String label;

  /// Which language [source] is in.
  ///
  /// The snippets do not say, so this looks for what only a script has: a
  /// file name ending in .ts or .tsx, or an import or export in ES form.
  static CodeLanguage of(String source) {
    final script = RegExp(r'\.tsx?\b|^import \{|^export ', multiLine: true);
    return script.hasMatch(source) ? tsx : dart;
  }
}

/// What a character of code is, for the colour it is drawn in.
enum Tone {
  plain,
  comment,

  /// Declarations and literals: `final`, `class`, `true`, `null`.
  keyword,

  /// What decides where execution goes next: `if`, `for`, `return`, `import`.
  control,

  /// Variables, fields, named arguments and markup attributes.
  name,
  type,
  call,
  string,
  number,

  /// A lower-case markup element, such as `<box>`.
  tag,

  /// The angle brackets around one.
  bracket,
}

/// One line of code, and the tone of each character in it.
class CodeLine {
  CodeLine(this.text, this.tones) : assert(text.length == tones.length);

  CodeLine.of(String text, Tone tone)
    : this(text, List.filled(text.length, tone));

  final String text;
  final List<Tone> tones;

  int get length => text.length;

  /// How many spaces it starts with.
  int get indent {
    var at = 0;
    while (at < text.length && text.codeUnitAt(at) == _space) {
      at++;
    }
    return at;
  }

  CodeLine slice(int start, [int? end]) =>
      CodeLine(text.substring(start, end), tones.sublist(start, end));

  CodeLine trimRight() {
    var end = text.length;
    while (end > 0 && text.codeUnitAt(end - 1) == _space) {
      end--;
    }
    return slice(0, end);
  }

  CodeLine operator +(CodeLine other) =>
      CodeLine(text + other.text, [...tones, ...other.tones]);
}

/// [source] as lines, with every character given a [Tone].
///
/// A scanner rather than a parser: it knows what a comment, a string, a
/// number and a word look like, and which words are keywords, and it guesses
/// the rest from the case of the first letter and what comes next. That is as
/// much as colour needs, and it is wrong only where an editor's highlighter
/// without the analyser behind it would be wrong too.
///
/// Leading and trailing blank lines are dropped, and so is trailing space.
List<CodeLine> highlight(String source, CodeLanguage language) {
  final script = language == CodeLanguage.tsx;
  final keywords = script ? _scriptKeywords : _dartKeywords;
  final control = script ? _scriptControl : _dartControl;
  final types = script ? _scriptTypes : _dartTypes;

  final s = source;
  final n = s.length;
  final tones = List<Tone>.filled(n, Tone.plain);
  void paint(int from, int to, Tone tone) => tones.fillRange(from, to, tone);

  // The last character of code, skipping space and comments. It is what
  // tells `a < b` from `<box>`, and `x.y` from `y`.
  var last = '';
  var lastWord = '';

  // Markup, in a script. Each entry is an open element, whose children are
  // text until the next tag or brace, or a braced expression among them,
  // with how deep in braces it is.
  const element = 0;
  final open = <int>[];
  var inTag = false;
  var closing = false;
  var tagBraces = 0;

  var i = 0;
  while (i < n) {
    final c = s[i];

    if (script && !inTag && open.isNotEmpty && open.last == element) {
      if (c == '{') {
        open.add(1);
        last = c;
        i++;
        continue;
      }
      if (c != '<') {
        while (i < n && s[i] != '<' && s[i] != '{') {
          i++;
        }
        continue;
      }
    }

    if (c == ' ' || c == '\n' || c == '\t' || c == '\r') {
      i++;
      continue;
    }

    if (s.startsWith('//', i)) {
      final end = s.indexOf('\n', i);
      final to = end < 0 ? n : end;
      paint(i, to, Tone.comment);
      i = to;
      continue;
    }
    if (s.startsWith('/*', i)) {
      final end = s.indexOf('*/', i + 2);
      final to = end < 0 ? n : end + 2;
      paint(i, to, Tone.comment);
      i = to;
      continue;
    }

    if (c == "'" || c == '"' || (script && c == '`')) {
      final to = _stringEnd(s, i, script);
      paint(i, to, Tone.string);
      last = c;
      i = to;
      continue;
    }

    if (script && inTag && tagBraces == 0) {
      if (s.startsWith('/>', i)) {
        paint(i, i + 2, Tone.bracket);
        inTag = false;
        last = '>';
        i += 2;
        continue;
      }
      if (c == '>') {
        paint(i, i + 1, Tone.bracket);
        inTag = false;
        if (!closing) {
          open.add(element);
        } else if (open.isNotEmpty && open.last == element) {
          open.removeLast();
        }
        last = c;
        i++;
        continue;
      }
    }

    if (script && !inTag && c == '<' && _opensTag(s, i, last, open)) {
      var from = i + 1;
      closing = from < n && s[from] == '/';
      if (closing) from++;
      paint(i, from, Tone.bracket);
      var to = from;
      while (to < n &&
          (_isWordPart(s.codeUnitAt(to)) || s[to] == '.' || s[to] == '-')) {
        to++;
      }
      paint(from, to, _isCapital(s.substring(from, to)) ? Tone.type : Tone.tag);
      inTag = true;
      tagBraces = 0;
      last = 'a';
      i = to;
      continue;
    }

    if (_isDigit(s.codeUnitAt(i))) {
      final to = _number.matchAsPrefix(s, i)!.end;
      paint(i, to, Tone.number);
      last = '0';
      i = to;
      continue;
    }

    final annotation =
        !script && c == '@' && i + 1 < n && _isWordStart(s.codeUnitAt(i + 1));
    if (annotation || _isWordStart(s.codeUnitAt(i))) {
      final from = annotation ? i + 1 : i;
      var to = from;
      while (to < n && _isWordPart(s.codeUnitAt(to))) {
        to++;
      }
      final word = s.substring(from, to);
      final Tone tone;
      if (annotation) {
        tone = Tone.type;
      } else if (inTag && tagBraces == 0) {
        tone = Tone.name;
      } else if (last == '.') {
        tone = _next(s, to) == '(' ? Tone.call : Tone.name;
      } else if (control.contains(word)) {
        tone = Tone.control;
      } else if (keywords.contains(word)) {
        tone = Tone.keyword;
      } else if (types.contains(word)) {
        tone = Tone.type;
      } else if (lastWord == 'function') {
        tone = Tone.call;
      } else if (_isCapital(word)) {
        tone = Tone.type;
      } else if (_next(s, to) == '(') {
        tone = Tone.call;
      } else {
        tone = Tone.name;
      }
      paint(i, to, tone);
      last = 'a';
      lastWord = word;
      i = to;
      continue;
    }

    if (script && c == '{') {
      if (inTag) {
        tagBraces++;
      } else if (open.isNotEmpty) {
        open[open.length - 1]++;
      }
    } else if (script && c == '}') {
      if (inTag) {
        tagBraces--;
      } else if (open.isNotEmpty && open.last != element) {
        if (--open[open.length - 1] == 0) open.removeLast();
      }
    }
    last = c;
    lastWord = '';
    i++;
  }

  final lines = <CodeLine>[];
  var start = 0;
  for (var at = 0; at <= n; at++) {
    if (at == n || s.codeUnitAt(at) == 0x0A) {
      lines.add(CodeLine(s.substring(start, at), tones.sublist(start, at)));
      start = at + 1;
    }
  }
  final trimmed = [for (final line in lines) line.trimRight()];
  while (trimmed.isNotEmpty && trimmed.first.length == 0) {
    trimmed.removeAt(0);
  }
  while (trimmed.isNotEmpty && trimmed.last.length == 0) {
    trimmed.removeLast();
  }
  return trimmed;
}

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

/// The code an example is written with, coloured, and laid out to fit.
///
/// The snippets are written for eighty columns and the panel is about fifty
/// wide. Left to the text engine, a line that does not fit carries on at the
/// left edge, under code indented eight deep, and the shape that says what
/// belongs to what is gone. So the lines are broken here instead — see
/// [fit] — and the button in the corner shows it at its own width.
///
/// In the panel it folds away to its bar when the bar is clicked, and stays
/// folded from one example to the next, for somebody who has come for the
/// scene rather than the code. Code longer than the panel is tall opens at
/// its first rows, with the rest a click away.
class CodeView extends StatefulWidget {
  const CodeView(this.source, {super.key, this.title, this.wide = false});

  final String source;

  /// What the wide view is headed with: the example's name.
  final String? title;

  /// Whether this is the wide view, in a dialog of its own.
  final bool wide;

  @override
  State<CodeView> createState() => _CodeViewState();
}

class _CodeViewState extends State<CodeView> {
  late CodeLanguage _language;
  late List<CodeLine> _lines;

  // The panel is built again every frame, for the clock; laying the code out
  // and colouring it is only redone when the width or the code changes.
  ((double, TextScaler, int), _LaidOut)? _laidOut;
  (TextScaler, double)? _advance;

  Timer? _copied;

  /// Whether only the bar is showing.
  bool _folded = false;

  /// Whether long code is showing all of it, rather than its first rows.
  /// Asked of one example, so it goes back to the first rows for the next.
  bool _whole = false;

  @override
  void initState() {
    super.initState();
    _read();
  }

  @override
  void didUpdateWidget(CodeView old) {
    super.didUpdateWidget(old);
    if (old.source != widget.source) _read();
  }

  @override
  void dispose() {
    _copied?.cancel();
    super.dispose();
  }

  void _read() {
    _language = CodeLanguage.of(widget.source);
    _lines = highlight(widget.source, _language);
    _laidOut = null;
    _whole = false;
  }

  /// Everything spelled out, down to the spacing: Material's body text has a
  /// quarter of a pixel between letters, which is nothing in a sentence and
  /// two columns across a line of code, and a style that left it to be
  /// inherited would be measured one way and drawn another.
  TextStyle get _style => TextStyle(
    fontFamily: 'Menlo',
    fontFamilyFallback: const ['Consolas', 'DejaVu Sans Mono', 'monospace'],
    fontSize: widget.wide ? 12.5 : 11,
    fontWeight: FontWeight.w400,
    height: 1.55,
    letterSpacing: 0,
    wordSpacing: 0,
    color: _colours[Tone.plain],
  );

  /// How wide one character is. Every character, since the font is
  /// monospaced, which is what lets [fit] count columns rather than measure.
  double _advanceOf(TextStyle style, TextScaler scaler) {
    final known = _advance;
    if (known != null && known.$1 == scaler) return known.$2;
    final probe = TextPainter(
      text: TextSpan(text: '0' * 40, style: style),
      textDirection: TextDirection.ltr,
      textScaler: scaler,
    )..layout();
    final advance = probe.width / 40;
    probe.dispose();
    _advance = (scaler, advance);
    return advance;
  }

  _LaidOut _laidOutFor(
    double width,
    TextStyle style,
    TextScaler scaler, {
    required int firstRows,
  }) {
    // Less the room selectable text keeps for its caret.
    final room = width - 4;
    final key = (room, scaler, firstRows);
    final known = _laidOut;
    if (known != null && known.$1 == key) return known.$2;
    var columns = math.max(24, (room / _advanceOf(style, scaler)).floor());

    // Counting columns is exact only while every character is as wide as a
    // zero, and a dash or a dot the font lacks is drawn from another font. So
    // the count is checked against a real layout, and if the text engine
    // would break any row, there are fewer columns until it would not.
    var rows = fit(_lines, columns);
    var laidOut = _spanOf(rows, style);
    while (columns > 24) {
      final painter = TextPainter(
        text: laidOut,
        textDirection: TextDirection.ltr,
        textScaler: scaler,
      )..layout(maxWidth: room);
      final broken =
          painter.computeLineMetrics().length > math.max(1, rows.length);
      painter.dispose();
      if (!broken) break;
      columns--;
      rows = fit(_lines, columns);
      laidOut = _spanOf(rows, style);
    }
    final shown = (
      whole: laidOut,
      // A few rows over is not worth a button to see them.
      firstRows: rows.length > firstRows + 6 && !widget.wide
          ? _spanOf(rows.sublist(0, firstRows), style)
          : null,
    );
    _laidOut = (key, shown);
    return shown;
  }

  TextSpan _spanOf(List<CodeLine> rows, TextStyle style) {
    final spans = <InlineSpan>[];
    for (final line in rows) {
      if (spans.isNotEmpty) spans.add(const TextSpan(text: '\n'));
      var start = 0;
      for (var at = 1; at <= line.length; at++) {
        if (at == line.length || line.tones[at] != line.tones[start]) {
          spans.add(
            TextSpan(
              text: line.text.substring(start, at),
              style: _toneStyles[line.tones[start]],
            ),
          );
          start = at;
        }
      }
    }
    return TextSpan(style: style, children: spans);
  }

  void _copy() {
    // As written, not as broken to fit the panel.
    Clipboard.setData(
      ClipboardData(text: [for (final line in _lines) line.text].join('\n')),
    );
    _copied?.cancel();
    setState(() {
      _copied = Timer(const Duration(milliseconds: 1600), () {
        if (mounted) setState(() => _copied = null);
      });
    });
  }

  void _openWide() {
    showDialog<void>(
      context: context,
      barrierColor: Colors.black.withValues(alpha: 0.6),
      builder: (context) => Dialog(
        backgroundColor: Colors.transparent,
        elevation: 0,
        insetPadding: const EdgeInsets.all(40),
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: 760),
          child: CodeView(widget.source, title: widget.title, wide: true),
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final scaler = MediaQuery.textScalerOf(context);
    // What it is drawn with is what it is measured with: the style the text
    // inherits, with everything that matters to a column overridden.
    final style = DefaultTextStyle.of(context).style.merge(_style);
    final pad = widget.wide
        ? const EdgeInsets.fromLTRB(20, 14, 20, 20)
        : const EdgeInsets.fromLTRB(12, 10, 12, 12);

    // Long code opens at as many rows as the window has room for below the
    // settings, so that it is only cut short where it would have scrolled.
    final rowHeight = scaler.scale(style.fontSize!) * style.height!;
    final window = MediaQuery.sizeOf(context).height;
    final firstRows = math.max(12, ((window - 320) / rowHeight).floor());

    final Widget body = LayoutBuilder(
      builder: (context, space) {
        final laidOut = _laidOutFor(
          space.maxWidth - pad.horizontal,
          style,
          scaler,
          firstRows: firstRows,
        );
        final opening = laidOut.firstRows;
        final code = Padding(
          padding: pad,
          child: SelectableText.rich(
            opening != null && !_whole ? opening : laidOut.whole,
          ),
        );
        if (opening == null) return code;
        return Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            if (_whole)
              code
            else
              Stack(
                children: [
                  code,
                  // The rows that are not showing are under the fade, rather
                  // than cut off as if the code ended there.
                  const Positioned(
                    left: 0,
                    right: 0,
                    bottom: 0,
                    height: 48,
                    child: IgnorePointer(
                      child: DecoratedBox(
                        decoration: BoxDecoration(
                          gradient: LinearGradient(
                            begin: Alignment.topCenter,
                            end: Alignment.bottomCenter,
                            colors: [Color(0x000D1117), _background],
                          ),
                        ),
                      ),
                    ),
                  ),
                ],
              ),
            _showAll(),
          ],
        );
      },
    );

    return Container(
      clipBehavior: Clip.antiAlias,
      decoration: BoxDecoration(
        color: _background,
        borderRadius: BorderRadius.circular(widget.wide ? 8 : 6),
        border: Border.all(color: const Color(0xFF232B36)),
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          _bar(),
          if (widget.wide)
            Flexible(child: SingleChildScrollView(child: body))
          else
            AnimatedSize(
              duration: const Duration(milliseconds: 180),
              curve: Curves.easeOutCubic,
              alignment: Alignment.topCenter,
              child: _folded ? const SizedBox(width: double.infinity) : body,
            ),
        ],
      ),
    );
  }

  Widget _showAll() {
    return Container(
      height: 30,
      decoration: _whole
          ? const BoxDecoration(
              border: Border(top: BorderSide(color: Color(0xFF1B222C))),
            )
          : null,
      child: TextButton.icon(
        onPressed: () => setState(() => _whole = !_whole),
        icon: Icon(_whole ? Icons.expand_less : Icons.expand_more, size: 16),
        iconAlignment: IconAlignment.end,
        label: Text(_whole ? 'Show less' : 'Show all ${_lines.length} lines'),
        style: TextButton.styleFrom(
          foregroundColor: const Color(0xFF8A95A6),
          textStyle: const TextStyle(fontSize: 11.5),
          shape: const RoundedRectangleBorder(),
          padding: EdgeInsets.zero,
        ),
      ),
    );
  }

  Widget _bar() {
    final title = widget.title;
    final foldable = !widget.wide;
    const quiet = Color(0xFF5C6472);

    final bar = Container(
      height: widget.wide ? 40 : 30,
      padding: EdgeInsets.only(left: widget.wide ? 20 : 8, right: 4),
      decoration: _folded
          ? null
          : const BoxDecoration(
              border: Border(bottom: BorderSide(color: Color(0xFF1B222C))),
            ),
      child: Row(
        children: [
          if (foldable) ...[
            Icon(
              _folded ? Icons.chevron_right : Icons.expand_more,
              size: 16,
              color: quiet,
            ),
            const SizedBox(width: 4),
          ],
          if (widget.wide && title != null) ...[
            Text(
              title,
              style: const TextStyle(
                fontSize: 13,
                fontWeight: FontWeight.w600,
                color: Color(0xFFC5CDD8),
              ),
            ),
            const SizedBox(width: 10),
          ],
          Text(
            _language.label,
            style: const TextStyle(
              fontSize: 10.5,
              letterSpacing: 0.6,
              fontWeight: FontWeight.w600,
              color: Color(0xFF6E7A8C),
            ),
          ),
          const SizedBox(width: 8),
          Text(
            '${_lines.length} lines',
            style: const TextStyle(fontSize: 10.5, color: quiet),
          ),
          const Spacer(),
          if (_copied != null)
            const Padding(
              padding: EdgeInsets.only(right: 2),
              child: Text(
                'Copied',
                style: TextStyle(fontSize: 11, color: Color(0xFF7FB88A)),
              ),
            ),
          _BarButton(
            icon: _copied == null ? Icons.copy_all_outlined : Icons.check,
            tooltip: 'Copy',
            onPressed: _copy,
          ),
          if (widget.wide)
            _BarButton(
              icon: Icons.close,
              tooltip: 'Close',
              onPressed: () => Navigator.of(context).pop(),
            )
          else
            _BarButton(
              icon: Icons.open_in_full,
              tooltip: 'Open wider',
              onPressed: _openWide,
            ),
        ],
      ),
    );
    if (!foldable) return bar;

    // The whole bar folds and unfolds it, less the buttons on it, which take
    // their own clicks.
    return Semantics(
      expanded: !_folded,
      child: Material(
        type: MaterialType.transparency,
        child: InkWell(
          onTap: () => setState(() => _folded = !_folded),
          hoverColor: Colors.white.withValues(alpha: 0.03),
          splashColor: Colors.transparent,
          highlightColor: Colors.white.withValues(alpha: 0.04),
          child: bar,
        ),
      ),
    );
  }
}

/// The code laid out to fit, and its first rows alone where there are too
/// many to show at once.
typedef _LaidOut = ({TextSpan whole, TextSpan? firstRows});

class _BarButton extends StatelessWidget {
  const _BarButton({
    required this.icon,
    required this.tooltip,
    required this.onPressed,
  });

  final IconData icon;
  final String tooltip;
  final VoidCallback onPressed;

  @override
  Widget build(BuildContext context) {
    return IconButton(
      icon: Icon(icon),
      iconSize: 14,
      tooltip: tooltip,
      padding: EdgeInsets.zero,
      constraints: const BoxConstraints.tightFor(width: 28, height: 28),
      color: const Color(0xFF7C8798),
      onPressed: onPressed,
    );
  }
}

// The colours are Visual Studio Code's dark theme, which is what most people
// reading Dart already read it in, on GitHub's dark background. Comments are
// quieter than its green: here they are most of what is written, and they
// are the explanation rather than the thing explained.
const _background = Color(0xFF0D1117);

const _colours = {
  Tone.plain: Color(0xFFD4D4D4),
  Tone.comment: Color(0xFF7A8699),
  Tone.keyword: Color(0xFF569CD6),
  Tone.control: Color(0xFFC586C0),
  Tone.name: Color(0xFF9CDCFE),
  Tone.type: Color(0xFF4EC9B0),
  Tone.call: Color(0xFFDCDCAA),
  Tone.string: Color(0xFFCE9178),
  Tone.number: Color(0xFFB5CEA8),
  Tone.tag: Color(0xFF569CD6),
  Tone.bracket: Color(0xFF808080),
};

final _toneStyles = {
  for (final MapEntry(key: tone, value: colour) in _colours.entries)
    tone: TextStyle(
      color: colour,
      fontStyle: tone == Tone.comment ? FontStyle.italic : null,
    ),
};

const _space = 0x20;

final _number = RegExp(
  r'0[xX][0-9a-fA-F_]+|\d[\d_]*(?:\.\d[\d_]*)?(?:[eE][+-]?\d+)?',
);

const _dartControl = {
  'assert', 'async', 'await', 'break', 'case', 'catch', 'continue', //
  'default', 'do', 'else', 'export', 'finally', 'for', 'if', 'import', 'in',
  'rethrow', 'return', 'switch', 'sync', 'throw', 'try', 'while', 'yield',
};

const _dartKeywords = {
  'abstract', 'as', 'base', 'class', 'const', 'covariant', 'deferred', //
  'dynamic', 'enum', 'extends', 'extension', 'external', 'factory', 'false',
  'final', 'get', 'implements', 'interface', 'is', 'late', 'mixin', 'new',
  'null', 'operator', 'required', 'sealed', 'set', 'static', 'super', 'this',
  'true', 'typedef', 'var', 'void', 'when', 'with',
};

const _dartTypes = {'int', 'double', 'num', 'bool'};

const _scriptControl = {
  'await', 'break', 'case', 'catch', 'continue', 'default', 'do', 'else', //
  'export', 'finally', 'for', 'from', 'if', 'import', 'in', 'of', 'return',
  'switch', 'throw', 'try', 'while', 'yield',
};

const _scriptKeywords = {
  'abstract', 'as', 'async', 'class', 'const', 'declare', 'delete', 'enum', //
  'extends', 'false', 'function', 'get', 'implements', 'instanceof',
  'interface', 'keyof', 'let', 'new', 'null', 'readonly', 'set', 'static',
  'super', 'this', 'true', 'type', 'typeof', 'undefined', 'var', 'void',
};

const _scriptTypes = {
  'any', 'bigint', 'boolean', 'never', 'number', 'object', 'string', //
  'symbol', 'unknown',
};

bool _isDigit(int unit) => unit >= 0x30 && unit <= 0x39;

bool _isWordStart(int unit) =>
    (unit >= 0x41 && unit <= 0x5A) ||
    (unit >= 0x61 && unit <= 0x7A) ||
    unit == 0x5F || // _
    unit == 0x24; // $

bool _isWordPart(int unit) => _isWordStart(unit) || _isDigit(unit);

/// Whether [word] is capitalised, past any leading underscores: a type, by
/// the convention both languages keep.
bool _isCapital(String word) {
  for (final unit in word.codeUnits) {
    if (unit == 0x5F || unit == 0x24) continue;
    return unit >= 0x41 && unit <= 0x5A;
  }
  return false;
}

/// The next character at or after [at] that is not a space.
String _next(String s, int at) {
  while (at < s.length && s.codeUnitAt(at) == _space) {
    at++;
  }
  return at < s.length ? s[at] : '';
}

/// Where the string starting at [start] ends, just past its closing quote.
///
/// A string left open ends with its line, so one stray quote colours one line
/// wrongly rather than everything after it.
int _stringEnd(String s, int start, bool script) {
  final quote = s[start];
  final triple = !script && s.startsWith(quote * 3, start);
  var at = start + (triple ? 3 : 1);
  while (at < s.length) {
    final c = s[at];
    if (c == r'\') {
      at += 2;
    } else if (triple) {
      if (s.startsWith(quote * 3, at)) return at + 3;
      at++;
    } else if (c == quote) {
      return at + 1;
    } else if (c == '\n' && quote != '`') {
      return at;
    } else {
      at++;
    }
  }
  return s.length;
}

/// Whether the `<` at [at] opens a markup tag rather than comparing.
///
/// Among an element's children it always does. Elsewhere it does when a name
/// or a slash follows it and nothing that could be compared comes before it:
/// `return (<box>` and `=> <Hud />` open tags, `ring < rings` does not.
bool _opensTag(String s, int at, String last, List<int> open) {
  if (open.isNotEmpty && open.last == 0) return true;
  if (at + 1 >= s.length) return false;
  final next = s.codeUnitAt(at + 1);
  final startsName = _isWordStart(next) || next == 0x2F || next == 0x3E;
  final after = last.isEmpty ? 0 : last.codeUnitAt(0);
  final compared = _isWordPart(after) || last == ')' || last == ']';
  return startsName && !compared;
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
    while (from < rest.length && rest.text.codeUnitAt(from) == _space) {
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

  bool space(int at) => at < line.length && line.text.codeUnitAt(at) == _space;
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
