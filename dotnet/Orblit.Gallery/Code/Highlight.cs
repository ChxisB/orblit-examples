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

    /// <summary><paramref name="source"/> as lines, with every character given a <see cref="Tone"/>.</summary>
    /// <remarks>
    /// A scanner rather than a parser. It knows what a comment, a string, a
    /// number and a word look like, and which words are keywords. It guesses
    /// the rest from the case of the first letter and what comes next. That is
    /// as much as colour needs, and it is wrong only where an editor's
    /// highlighter without the compiler behind it would be wrong too.
    /// Leading and trailing blank lines are dropped, and so is trailing space.
    /// </remarks>
    public static IReadOnlyList<CodeLine> Lines(string source) =>
        Split(source, new Scanner(source).Run());

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

    private static bool IsWordStart(char c) => char.IsAsciiLetter(c) || c == '_';

    private static bool IsWordPart(char c) => IsWordStart(c) || char.IsAsciiDigit(c);

    /// <summary>Whether <paramref name="word"/> is capitalised past any leading underscores: a type, by convention.</summary>
    private static bool IsCapital(string word) => word.TrimStart('_') is [var first, ..] && char.IsAsciiLetterUpper(first);

    // A suffix, as in 0.5f, is part of the number.
    [GeneratedRegex(@"\G(?:0[xX][0-9a-fA-F_]+|\d[\d_]*(?:\.\d[\d_]*)?(?:[eE][+-]?\d+)?)[fFdDmMuUlL]*")]
    private static partial Regex NumberShape();

    private sealed record Words(HashSet<string> Control, HashSet<string> Keywords, HashSet<string> Types);

    private sealed class Scanner
    {
        private readonly string _s;
        private readonly Tone[] _tones;

        // The last character of code, skipping space and comments. It is what
        // tells `x.y` from `y`.
        private char _last;
        private string _lastWord = "";
        private int _i;

        public Scanner(string source)
        {
            _s = source;
            _tones = new Tone[source.Length];
        }

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
            if (Space() || Comment() || Quote() || Number() || Word())
            {
                return;
            }

            Punctuation();
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
            while (at < _s.Length && _s[at] is '$' or '@')
            {
                at++;
            }

            if (at >= _s.Length || _s[at] is not ('\'' or '"'))
            {
                return false;
            }

            var to = StartsAt(at, "\"\"\"") ? RawEnd(at) : StringEnd(at);
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
                else if (_s[at] == '\n')
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
            if (_last == '.')
            {
                return Is(next, '(') ? Tone.Call : Tone.Name;
            }

            if (CSharp.Control.Contains(word))
            {
                return Tone.Control;
            }

            if (CSharp.Keywords.Contains(word))
            {
                return Tone.Keyword;
            }

            if (CSharp.Types.Contains(word))
            {
                return Tone.Type;
            }

            return CSharpToneOf(word, next);
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
            _last = _s[_i];
            _lastWord = "";
            _i++;
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
