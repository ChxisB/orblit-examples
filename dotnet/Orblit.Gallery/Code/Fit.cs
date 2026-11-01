using System.Text.RegularExpressions;

namespace Orblit.Gallery;

/// <summary>Breaks coloured lines to a width without losing how they line up.</summary>
public static partial class Fit
{
    /// <summary>
    /// <paramref name="lines"/> broken to fit <paramref name="columns"/>
    /// characters, where somebody writing for a page that narrow would have
    /// broken them.
    /// </summary>
    /// <remarks>
    /// A paragraph of comment that does not fit is flowed again to the width,
    /// as one paragraph rather than as each of its lines wrapped separately. A
    /// comment at the end of a line that does not fit goes on a line of its
    /// own above it. Code that still does not fit is broken at a space, after a
    /// comma where there is one. It carries on four deeper than the line it
    /// belongs to, so the indentation that says what belongs to what survives
    /// the break. Or it carries on as deep as the line after it, where that was
    /// lined up by hand to carry on from this one. Lines that fit are left
    /// exactly as written, alignment and all.
    /// </remarks>
    public static IReadOnlyList<CodeLine> To(IReadOnlyList<CodeLine> lines, int columns)
    {
        var fitted = new List<CodeLine>();
        var i = 0;
        while (i < lines.Count)
        {
            var paragraph = Paragraph(lines, i);
            if (paragraph.Count > 0)
            {
                fitted.AddRange(FitParagraph(paragraph, columns));
                i += paragraph.Count;
                continue;
            }

            var line = lines[i++];
            if (line.Length <= columns)
            {
                fitted.Add(line);
            }
            else
            {
                fitted.AddRange(Break(line, i < lines.Count ? lines[i] : null, columns));
            }
        }

        return fitted;
    }

    /// <summary>The comment lines from <paramref name="at"/> that read as one paragraph, or none if that line is not a comment.</summary>
    private static List<Remark> Paragraph(IReadOnlyList<CodeLine> lines, int at)
    {
        var paragraph = new List<Remark>();
        for (var i = at; i < lines.Count; i++)
        {
            var remark = Remark.Of(lines[i]);
            if (remark is null || (paragraph.Count > 0 && !paragraph[^1].CarriesOnTo(remark)))
            {
                break;
            }

            paragraph.Add(remark);
        }

        return paragraph;
    }

    private static IEnumerable<CodeLine> FitParagraph(List<Remark> paragraph, int columns)
    {
        if (!paragraph.Exists(remark => remark.Line.Length > columns))
        {
            return paragraph.Select(remark => remark.Line);
        }

        var first = paragraph[0];
        return Flow(first.Indent, first.Marker, string.Join(' ', paragraph.Select(remark => remark.Body)), columns);
    }

    /// <summary><paramref name="line"/>, which is code too long for <paramref name="columns"/>, as the lines it breaks into.</summary>
    private static List<CodeLine> Break(CodeLine line, CodeLine? next, int columns)
    {
        if (TrailingRemark(line) is not { } at)
        {
            return Wrap(line, columns, AlignedHang(line, next));
        }

        var code = line.Slice(0, at).TrimEnd();
        return
        [
            .. Flow(line.Indent, "//", line.Text[(at + 2)..].Trim(), columns),
            .. Wrap(code, columns, AlignedHang(code, next)),
        ];
    }

    /// <summary>Where the comment at the end of <paramref name="line"/> starts, or null if it has none.</summary>
    private static int? TrailingRemark(CodeLine line)
    {
        for (var at = line.Indent; at < line.Length; at++)
        {
            if (line.Tones[at] == Tone.Comment)
            {
                return at > line.Indent && line.Text.AsSpan(at).StartsWith("//", StringComparison.Ordinal) ? at : null;
            }
        }

        return null;
    }

    /// <summary><paramref name="words"/> as a comment <paramref name="indent"/> deep, filling each line up to <paramref name="columns"/>.</summary>
    private static IEnumerable<CodeLine> Flow(int indent, string marker, string words, int columns)
    {
        var lead = $"{new string(' ', indent)}{marker} ";
        return Rows(words, Math.Max(columns - lead.Length, 16))
            .Select(row => CodeLine.Of((lead + row).TrimEnd(), Tone.Comment));
    }

    private static List<string> Rows(string words, int width)
    {
        var rows = new List<string>();
        var row = "";
        foreach (var whole in words.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        {
            // A word longer than a whole line, such as a path, is cut.
            var word = whole;
            while (word.Length > width)
            {
                if (row.Length > 0)
                {
                    rows.Add(row);
                }

                row = "";
                rows.Add(word[..width]);
                word = word[width..];
            }

            if (row.Length > 0 && row.Length + 1 + word.Length > width)
            {
                rows.Add(row);
                row = "";
            }

            row = row.Length == 0 ? word : $"{row} {word}";
        }

        if (row.Length > 0 || rows.Count == 0)
        {
            rows.Add(row);
        }

        return rows;
    }

    /// <summary>
    /// How deep <paramref name="next"/> is, if it carries on from
    /// <paramref name="line"/> at a depth somebody chose by hand. That is
    /// deeper than the two a block steps in by, and after a line that does not
    /// open a bracket, so it is not the first line of a block.
    /// </summary>
    private static int? AlignedHang(CodeLine line, CodeLine? next)
    {
        if (next is null || string.IsNullOrWhiteSpace(next.Text) || next.Indent <= line.Indent + 2)
        {
            return null;
        }

        return line.Text.TrimEnd() is [.., '(' or '[' or '{'] ? null : next.Indent;
    }

    /// <summary>
    /// <paramref name="line"/>, which is code, broken into lines of at most
    /// <paramref name="columns"/>. Each after the first carries on
    /// <paramref name="hang"/> deep, or four deeper than the line if that is null.
    /// </summary>
    private static List<CodeLine> Wrap(CodeLine line, int columns, int? hang)
    {
        var deep = Math.Min(hang ?? line.Indent + 4, columns / 2);
        var rows = new List<CodeLine>();
        var rest = line;
        var lead = line.Indent;
        while (rest.Length > columns)
        {
            var at = BreakIn(rest, lead, columns);
            rows.Add(rest.Slice(0, at).TrimEnd());
            var from = at;
            while (from < rest.Length && rest.Text[from] == ' ')
            {
                from++;
            }

            rest = CodeLine.Of(new string(' ', deep), Tone.Plain).Append(rest.Slice(from));
            lead = deep;
        }

        rows.Add(rest);
        return rows;
    }

    /// <summary>
    /// Where to break <paramref name="line"/>, which is wider than
    /// <paramref name="columns"/> and starts with <paramref name="lead"/>
    /// spaces. At a space after a comma if there is one past halfway. Else at
    /// any other space outside a string. Else in the middle of whatever is
    /// there, when there is no space at all.
    /// </summary>
    private static int BreakIn(CodeLine line, int lead, int columns) =>
        Comma(line, lead, columns)
        ?? Latest(lead, columns, at => Bare(line, at))
        ?? Latest(lead, columns, at => Space(line, at))
        ?? columns;

    /// <summary>
    /// The space after a comma past halfway that is fewest brackets deep, so
    /// that a list is not split while what it is in could be.
    /// </summary>
    private static int? Comma(CodeLine line, int lead, int columns)
    {
        var depth = Depths(line, columns);
        int? comma = null;
        for (var at = columns; at - lead >= (columns - lead) / 2; at--)
        {
            if (Bare(line, at) && line.Text[at - 1] == ',' && (comma is null || depth[at] < depth[comma.Value]))
            {
                comma = at;
            }
        }

        return comma;
    }

    /// <summary>How many brackets are open before each column, counting only code.</summary>
    private static int[] Depths(CodeLine line, int columns)
    {
        var depth = new int[columns + 1];
        var open = 0;
        for (var at = 0; at < columns && at < line.Length; at++)
        {
            if (line.Tones[at] is not (Tone.String or Tone.Comment))
            {
                open += line.Text[at] switch
                {
                    '(' or '[' or '{' => 1,
                    ')' or ']' or '}' => -1,
                    _ => 0,
                };
            }

            depth[at + 1] = open;
        }

        return depth;
    }

    private static int? Latest(int lead, int columns, Func<int, bool> good)
    {
        for (var at = columns; at > lead; at--)
        {
            if (good(at))
            {
                return at;
            }
        }

        return null;
    }

    private static bool Space(CodeLine line, int at) => at < line.Length && line.Text[at] == ' ';

    private static bool Bare(CodeLine line, int at) => Space(line, at) && line.Tones[at] != Tone.String;

    /// <summary>A line that is nothing but a <c>//</c> comment.</summary>
    /// <param name="Marker"><c>//</c>, or <c>///</c> for documentation.</param>
    /// <param name="Body">What it says, after the marker and one space.</param>
    private sealed partial record Remark(CodeLine Line, int Indent, string Marker, string Body)
    {
        public static Remark? Of(CodeLine line)
        {
            var indent = line.Indent;
            if (indent >= line.Length || line.Tones[indent] != Tone.Comment)
            {
                return null;
            }

            var match = Shape().Match(line.Text[indent..]);
            return match.Success ? new Remark(line, indent, match.Groups[1].Value, match.Groups[2].Value) : null;
        }

        /// <summary>
        /// Whether <paramref name="next"/> is more of the same paragraph: the
        /// same marker at the same depth. Neither may be a blank line, an
        /// indented line or the start of a list item, which were laid out by
        /// hand and mean to stay so.
        /// </summary>
        public bool CarriesOnTo(Remark next) =>
            next.Indent == Indent
            && next.Marker == Marker
            && Flows(Body)
            && Flows(next.Body)
            && !ListItem().IsMatch(next.Body);

        private static bool Flows(string body) => body.Length > 0 && !body.StartsWith(' ');

        [GeneratedRegex("^(///?)(?: (.*))?$")]
        private static partial Regex Shape();

        [GeneratedRegex(@"^([-*•]|\d+[.)]) ")]
        private static partial Regex ListItem();
    }
}
