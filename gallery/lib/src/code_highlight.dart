/// Colours the words of a snippet, so it can be drawn as code.
library;

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
    while (at < text.length && text.codeUnitAt(at) == spaceUnit) {
      at++;
    }
    return at;
  }

  CodeLine slice(int start, [int? end]) =>
      CodeLine(text.substring(start, end), tones.sublist(start, end));

  CodeLine trimRight() {
    var end = text.length;
    while (end > 0 && text.codeUnitAt(end - 1) == spaceUnit) {
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


const spaceUnit = 0x20;

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
  while (at < s.length && s.codeUnitAt(at) == spaceUnit) {
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
