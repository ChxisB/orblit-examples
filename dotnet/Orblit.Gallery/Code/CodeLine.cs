namespace Orblit.Gallery;

/// <summary>One line of code, and the tone of each character in it.</summary>
public sealed class CodeLine
{
    private readonly Tone[] _tones;

    public CodeLine(string text, ReadOnlySpan<Tone> tones)
    {
        if (tones.Length != text.Length)
        {
            throw new ArgumentException($"A line of {text.Length} characters was given {tones.Length} tones.", nameof(tones));
        }

        Text = text;
        _tones = tones.ToArray();
    }

    public string Text { get; }

    public IReadOnlyList<Tone> Tones => _tones;

    public int Length => Text.Length;

    /// <summary>How many spaces it starts with.</summary>
    public int Indent => Text.Length - Text.TrimStart(' ').Length;

    /// <summary><paramref name="text"/> in one tone.</summary>
    public static CodeLine Of(string text, Tone tone)
    {
        var tones = new Tone[text.Length];
        Array.Fill(tones, tone);
        return new CodeLine(text, tones);
    }

    public CodeLine Slice(int start, int end) => new(Text[start..end], _tones.AsSpan(start, end - start));

    public CodeLine Slice(int start) => Slice(start, Length);

    public CodeLine TrimEnd() => Slice(0, Text.TrimEnd(' ').Length);

    /// <summary>This line with <paramref name="rest"/> after it.</summary>
    public CodeLine Append(CodeLine rest) => new(Text + rest.Text, [.. _tones, .. rest._tones]);
}
