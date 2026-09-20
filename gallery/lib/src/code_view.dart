/// The panel that shows an example's source beside the scene it draws.
library;

import 'dart:async';
import 'dart:math' as math;

import 'package:flutter/material.dart';
import 'package:flutter/services.dart';

import 'code_fit.dart';
import 'code_highlight.dart';

// Whoever shows the panel wants the whole of it: what colours a
// snippet, what breaks it to a width, and the widget that draws the
// result.
export 'code_fit.dart';
export 'code_highlight.dart';

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
