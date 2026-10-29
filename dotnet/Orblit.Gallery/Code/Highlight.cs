using System.Text.RegularExpressions;

namespace Orblit.Gallery;

/// <summary>Colours the words of a snippet, so it can be drawn as code.</summary>
public static partial class Highlight
{
    private static readonly Words CSharp = new(
        Control:
        [
            "await", "break", "case", "catch", "continue", "default", "do", "else", "finally", "for", "foreach", "goto",
            "if", "in", "lock", "return", "switch", "throw", "try", "using", "when", "while", "yield",
        ],
        Keywords:
        [
            "abstract", "and", "as", "async", "base", "class", "const", "delegate", "enum", "event", "explicit", "extern",
            "false", "fixed", "get", "implicit", "init", "interface", "internal", "is", "namespace", "nameof", "new",
            "not", "null", "operator", "or", "out", "override", "params", "partial", "private", "protected", "public",
            "readonly", "record", "ref", "required", "sealed", "set", "sizeof", "static", "struct", "this", "true",
            "typeof", "var", "virtual", "void", "volatile", "with",
        ],
        Types:
        [
            "bool", "byte", "char", "decimal", "double", "dynamic", "float", "int", "long", "nint", "nuint", "object",
            "sbyte", "short", "string", "uint", "ulong", "ushort",
        ]);

    private static readonly Words Script = new(
        Control:
        [
            "await", "break", "case", "catch", "continue", "default", "do", "else", "export", "finally", "for", "from",
            "if", "import", "in", "of", "return", "switch", "throw", "try", "while", "yield",
        ],
        Keywords:
        [
            "abstract", "as", "async", "class", "const", "declare", "delete", "enum", "extends", "false", "function",
            "get", "implements", "instanceof", "interface", "keyof", "let", "new", "null", "readonly", "set", "static",
            "super", "this", "true", "type", "typeof", "undefined", "var", "void",
        ],
        Types: ["any", "bigint", "boolean", "never", "number", "object", "string", "symbol", "unknown"]);

    /// <summary>Which language <paramref name="source"/> is in.</summary>
    /// <remarks>
    /// The snippets do not say, so this looks for what only a script has: a
    /// file name ending in .ts or .tsx, or an import or export in ES form.
    /// </remarks>
    public static CodeLanguage LanguageOf(string source) => ScriptShape().IsMatch(source) ? CodeLanguage.Tsx : CodeLanguage.CSharp;

    /// <summary><paramref name="source"/> as lines, with every character given a <see cref="Tone"/>.</summary>
    /// <remarks>
    /// A scanner rather than a parser. It knows what a comment, a string, a
    /// number and a word look like, and which words are keywords. It guesses
    /// the rest from the case of the first letter and what comes next. That is
    /// as much as colour needs, and it is wrong only where an editor's
    /// highlighter without the compiler behind it would be wrong too.
    /// Leading and trailing blank lines are dropped, and so is trailing space.
    /// </remarks>
    public static IReadOnlyList<CodeLine> Lines(string source, CodeLanguage language) =>
        Split(source, new Scanner(source, language).Run());

    private static List<CodeLine> Split(string source, Tone[] tones)
    {
        var lines = new List<CodeLine>();
        var start = 0;
        for (var at = 0; at <= source.Length; at++)
        {
            if (at == source.Length || source[at] == '\n')
            {
                lines.Add(new CodeLine(source[start..at], tones.AsSpan(start, at - start)).TrimEnd());
                start = at + 1;
            }
        }

        var first = lines.FindIndex(line => line.Length > 0);
        return first < 0 ? [] : lines.GetRange(first, lines.FindLastIndex(line => line.Length > 0) - first + 1);
    }

    private static bool IsWordStart(char c) => char.IsAsciiLetter(c) || c is '_' or '$';

    private static bool IsWordPart(char c) => IsWordStart(c) || char.IsAsciiDigit(c);

    /// <summary>Whether <paramref name="word"/> is capitalised past any leading underscores: a type, by the convention both languages keep.</summary>
    private static bool IsCapital(string word) => word.TrimStart('_', '$') is [var first, ..] && char.IsAsciiLetterUpper(first);

    [GeneratedRegex(@"\.tsx?\b|^import \{|^export ", RegexOptions.Multiline)]
    private static partial Regex ScriptShape();

    // The suffix is C#'s, as in 0.5f. A script never writes one.
    [GeneratedRegex(@"\G(?:0[xX][0-9a-fA-F_]+|\d[\d_]*(?:\.\d[\d_]*)?(?:[eE][+-]?\d+)?)[fFdDmMuUlL]*")]
    private static partial Regex NumberShape();

    private sealed record Words(HashSet<string> Control, HashSet<string> Keywords, HashSet<string> Types);

    private sealed class Scanner
    {
        // An entry in _open that is an element, whose children are text until
        // the next tag or brace. Any other entry is a braced expression among
        // them, and says how deep in braces it is.
        private const int Element = 0;

        private readonly string _s;
        private readonly bool _script;
        private readonly Words _words;
        private readonly Tone[] _tones;
        private readonly List<int> _open = [];

        // The last character of code, skipping space and comments. It is what
        // tells `a < b` from `<box>`, and `x.y` from `y`.
        private char _last;
        private string _lastWord = "";
        private bool _inTag;
        private bool _closing;
        private int _tagBraces;
        private int _i;

        public Scanner(string source, CodeLanguage language)
        {
            _s = source;
            _script = language == CodeLanguage.Tsx;
            _words = _script ? Script : CSharp;
            _tones = new Tone[source.Length];
        }

        private bool InChildren => _open.Count > 0 && _open[^1] == Element;

        public Tone[] Run()
        {
            while (_i < _s.Length)
            {
                Step();
            }

            return _tones;
        }

        // In order: what the first that matches takes, nothing after it sees.
        private void Step()
        {
            if (AmongChildren() || Space() || Comment() || Quote() || TagEnd() || TagStart() || Number() || Word())
            {
                return;
            }

            Punctuation();
        }

        private bool AmongChildren()
        {
            if (!_script || _inTag || !InChildren)
            {
                return false;
            }

            if (_s[_i] == '{')
            {
                _open.Add(1);
                _last = '{';
                _i++;
                return true;
            }

            if (_s[_i] == '<')
            {
                return false;
            }

            while (_i < _s.Length && _s[_i] is not ('<' or '{'))
            {
                _i++;
            }

            return true;
        }

        private bool Space()
        {
            if (_s[_i] is not (' ' or '\n' or '\t' or '\r'))
            {
                return false;
            }

            _i++;
            return true;
        }

        private bool Comment()
        {
            int to;
            if (StartsAt(_i, "//"))
            {
                var end = _s.IndexOf('\n', _i);
                to = end < 0 ? _s.Length : end;
            }
            else if (StartsAt(_i, "/*"))
            {
                var end = _s.IndexOf("*/", _i + 2, StringComparison.Ordinal);
                to = end < 0 ? _s.Length : end + 2;
            }
            else
            {
                return false;
            }

            Paint(_i, to, Tone.Comment);
            _i = to;
            return true;
        }

        private bool Quote()
        {
            // A C# string may start with $ or @, which belong to it.
            var at = _i;
            while (!_script && at < _s.Length && _s[at] is '$' or '@')
            {
                at++;
            }

            if (at >= _s.Length || !(_s[at] is '\'' or '"' || (_script && _s[at] == '`')))
            {
                return false;
            }

            var to = !_script && StartsAt(at, "\"\"\"") ? RawEnd(at) : StringEnd(at);
            Paint(_i, to, Tone.String);
            _last = _s[at];
            _i = to;
            return true;
        }

        /// <summary>Just past the closing quote of the string at <paramref name="start"/>.</summary>
        /// <remarks>
        /// A string left open ends with its line, so one stray quote colours
        /// one line wrongly rather than everything after it.
        /// </remarks>
        private int StringEnd(int start)
        {
            var quote = _s[start];
            for (var at = start + 1; at < _s.Length; at++)
            {
                if (_s[at] == '\\')
                {
                    at++;
                }
                else if (_s[at] == quote)
                {
                    return at + 1;
                }
                else if (_s[at] == '\n' && quote != '`')
                {
                    return at;
                }
            }

            return _s.Length;
        }

        /// <summary>Just past the C# raw string at <paramref name="start"/>, which ends at as many quotes as it opened with.</summary>
        private int RawEnd(int start)
        {
            var quotes = start;
            while (quotes < _s.Length && _s[quotes] == '"')
            {
                quotes++;
            }

            var fence = _s[start..quotes];
            var end = _s.IndexOf(fence, quotes, StringComparison.Ordinal);
            return end < 0 ? _s.Length : end + fence.Length;
        }

        private bool TagEnd()
        {
            if (!_script || !_inTag || _tagBraces != 0)
            {
                return false;
            }

            if (StartsAt(_i, "/>"))
            {
                Paint(_i, _i + 2, Tone.Bracket);
                _inTag = false;
                _last = '>';
                _i += 2;
                return true;
            }

            if (_s[_i] != '>')
            {
                return false;
            }

            Paint(_i, _i + 1, Tone.Bracket);
            _inTag = false;
            if (!_closing)
            {
                _open.Add(Element);
            }
            else if (InChildren)
            {
                _open.RemoveAt(_open.Count - 1);
            }

            _last = '>';
            _i++;
            return true;
        }

        private bool TagStart()
        {
            if (!_script || _inTag || _s[_i] != '<' || !OpensTag())
            {
                return false;
            }

            var from = _i + 1;
            _closing = from < _s.Length && _s[from] == '/';
            if (_closing)
            {
                from++;
            }

            Paint(_i, from, Tone.Bracket);
            var to = TagNameEnd(from);
            Paint(from, to, IsCapital(_s[from..to]) ? Tone.Type : Tone.Tag);
            _inTag = true;
            _tagBraces = 0;
            _last = 'a';
            _i = to;
            return true;
        }

        /// <summary>Just past a tag's name, which may hold dots and dashes, as in <c>Hud.Bar</c> or <c>my-box</c>.</summary>
        private int TagNameEnd(int from)
        {
            var to = from;
            while (to < _s.Length && (IsWordPart(_s[to]) || _s[to] is '.' or '-'))
            {
                to++;
            }

            return to;
        }

        /// <summary>Whether the <c>&lt;</c> here opens a markup tag rather than comparing.</summary>
        /// <remarks>
        /// Among an element's children it always does. Elsewhere it does when
        /// a name or a slash follows it and nothing that could be compared
        /// comes before it. <c>return (&lt;box&gt;</c> and <c>=&gt; &lt;Hud /&gt;</c>
        /// open tags, and <c>ring &lt; rings</c> does not.
        /// </remarks>
        private bool OpensTag()
        {
            if (InChildren)
            {
                return true;
            }

            if (_i + 1 >= _s.Length)
            {
                return false;
            }

            var next = _s[_i + 1];
            var startsName = IsWordStart(next) || next is '/' or '>';
            var compared = IsWordPart(_last) || _last is ')' or ']';
            return startsName && !compared;
        }

        private bool Number()
        {
            if (!char.IsAsciiDigit(_s[_i]))
            {
                return false;
            }

            var to = _i + NumberShape().Match(_s, _i).Length;
            Paint(_i, to, Tone.Number);
            _last = '0';
            _i = to;
            return true;
        }

        private bool Word()
        {
            if (!IsWordStart(_s[_i]))
            {
                return false;
            }

            var to = _i;
            while (to < _s.Length && IsWordPart(_s[to]))
            {
                to++;
            }

            var word = _s[_i..to];
            Paint(_i, to, ToneOf(word, NextAt(to)));
            _last = 'a';
            _lastWord = word;
            _i = to;
            return true;
        }

        /// <summary>The tone of <paramref name="word"/>, where <paramref name="next"/> is the first character after it that is not a space.</summary>
        private Tone ToneOf(string word, int next)
        {
            if (_inTag && _tagBraces == 0)
            {
                return Tone.Name;
            }

            if (_last == '.')
            {
                return Is(next, '(') ? Tone.Call : Tone.Name;
            }

            if (_words.Control.Contains(word))
            {
                return Tone.Control;
            }

            if (_words.Keywords.Contains(word))
            {
                return Tone.Keyword;
            }

            if (_words.Types.Contains(word))
            {
                return Tone.Type;
            }

            return _script ? ScriptToneOf(word, next) : CSharpToneOf(word, next);
        }

        private Tone ScriptToneOf(string word, int next)
        {
            if (_lastWord == "function")
            {
                return Tone.Call;
            }

            if (IsCapital(word))
            {
                return Tone.Type;
            }

            return Is(next, '(') ? Tone.Call : Tone.Name;
        }

        // C# capitalises methods and properties as well as types. So a call
        // is told by its bracket, and a member being set by its equals sign.
        private Tone CSharpToneOf(string word, int next)
        {
            if (_lastWord == "new")
            {
                return Tone.Type;
            }

            if (Is(next, '('))
            {
                return Tone.Call;
            }

            if (!IsCapital(word))
            {
                return Tone.Name;
            }

            return Is(next, '=') && !Is(next + 1, '=') ? Tone.Name : Tone.Type;
        }

        private void Punctuation()
        {
            var c = _s[_i];
            if (_script && c == '{')
            {
                OpenBrace();
            }
            else if (_script && c == '}')
            {
                CloseBrace();
            }

            _last = c;
            _lastWord = "";
            _i++;
        }

        private void OpenBrace()
        {
            if (_inTag)
            {
                _tagBraces++;
            }
            else if (_open.Count > 0)
            {
                _open[^1]++;
            }
        }

        private void CloseBrace()
        {
            if (_inTag)
            {
                _tagBraces--;
            }
            else if (_open.Count > 0 && _open[^1] != Element && --_open[^1] == 0)
            {
                _open.RemoveAt(_open.Count - 1);
            }
        }

        private void Paint(int from, int to, Tone tone) => _tones.AsSpan(from, to - from).Fill(tone);

        private bool StartsAt(int at, string text) => _s.AsSpan(at).StartsWith(text, StringComparison.Ordinal);

        private int NextAt(int at)
        {
            while (at < _s.Length && _s[at] == ' ')
            {
                at++;
            }

            return at;
        }

        private bool Is(int at, char c) => at < _s.Length && _s[at] == c;
    }
}
