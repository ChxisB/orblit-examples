using Orblit.Examples;

namespace Orblit.Gallery.Tests;

/// <summary>
/// The code panel's text: coloured by what each word is, and broken to fit
/// the panel where somebody would have broken it, rather than wherever the
/// text engine runs out of room.
/// </summary>
public sealed class CodeViewTests
{
    [Fact]
    public void CSharpIsColouredByWhatEachWordIs()
    {
        var lines = Highlight.Lines(
            """"
            [Fact]
            var light = new RenderLight(200) { Key = 1, CastShadows = true }.Normalize();
              // lux
            foreach (var x in xs) Print($"a // {x}", 0xFF0B1224, 0.53f);
            var style = """
                panel { padding: 8 }
                """;
            """",
            CodeLanguage.CSharp);

        Assert.Equal(Tone.Type, ToneOf(lines, "Fact"));
        Assert.Equal(Tone.Keyword, ToneOf(lines, "var"));
        Assert.Equal(Tone.Name, ToneOf(lines, "light"));
        // Capitalised and called, but made.
        Assert.Equal(Tone.Type, ToneOf(lines, "RenderLight"));
        // Capitalised like a type, but being set.
        Assert.Equal(Tone.Name, ToneOf(lines, "Key"));
        Assert.Equal(Tone.Number, ToneOf(lines, "200"));
        Assert.Equal(Tone.Keyword, ToneOf(lines, "true"));
        Assert.Equal(Tone.Call, ToneOf(lines, "Normalize"));
        Assert.Equal(Tone.Comment, ToneOf(lines, "// lux"));
        Assert.Equal(Tone.Control, ToneOf(lines, "foreach"));
        // Capitalised like a type, but called.
        Assert.Equal(Tone.Call, ToneOf(lines, "Print"));
        // A comment marker inside a string is part of the string, and so is
        // the $ before it.
        Assert.Equal(Tone.String, ToneOf(lines, "$\"a // {x}\""));
        Assert.Equal(Tone.Number, ToneOf(lines, "0xFF0B1224"));
        Assert.Equal(Tone.Number, ToneOf(lines, "0.53f"));
        // A raw string runs to its closing quotes, past the end of its line.
        Assert.Equal(Tone.String, ToneOf(lines, "panel { padding: 8 }"));
    }

    [Fact]
    public void TsxTellsATagFromALessThanAndTextFromCode()
    {
        var lines = Highlight.Lines(
            """
            for (let i = 0; i < many; i++) {}
            return (
              <button class="px-3" onPressed={() => { go(1); }}>
                Repair
              </button>
            );
            mount(() => <Hud />);
            """,
            CodeLanguage.Tsx);

        // i < many
        Assert.Equal(Tone.Plain, ToneOf(lines, "<"));
        Assert.Equal(Tone.Name, ToneOf(lines, "many"));
        Assert.Equal(Tone.Tag, ToneOf(lines, "button"));
        // An attribute, not `class`.
        Assert.Equal(Tone.Name, ToneOf(lines, "class"));
        Assert.Equal(Tone.String, ToneOf(lines, "\"px-3\""));
        Assert.Equal(Tone.Call, ToneOf(lines, "go"));
        // What sits between the tags is text, whatever case it is in...
        Assert.Equal(Tone.Plain, ToneOf(lines, "Repair"));
        // ...and once the last tag is closed, what follows is code again.
        Assert.Equal(Tone.Call, ToneOf(lines, "mount"));
        Assert.Equal(Tone.Type, ToneOf(lines, "Hud"));
    }

    [Fact]
    public void EveryExampleFitsThePanelAndNothingIsLostInFittingIt()
    {
        foreach (var columns in (int[])[40, 54, 80])
        {
            foreach (var example in GalleryExamples.All(new TempPictureFolder(), _ => null))
            {
                var lines = Highlight.Lines(example.Code, Highlight.LanguageOf(example.Code));
                var fitted = Fit.To(lines, columns);

                Assert.All(fitted, line => Assert.True(line.Length <= columns, $"{example.Name} at {columns}: \"{line.Text}\""));
                // The same characters, in whatever lines, less the comment
                // markers that flowing a comment adds.
                Assert.True(Letters(fitted).SequenceEqual(Letters(lines)), $"{example.Name} at {columns} lost or gained a character.");
            }
        }
    }

    [Fact]
    public void AParagraphOfCommentIsFlowedAsOne()
    {
        var lines = Highlight.Lines(
            """
              // The key is what makes saying it again cheap. Same key, same
              // object: the renderer moves what moved.
              //
              // A second paragraph.
            """,
            CodeLanguage.CSharp);

        // Forty columns, where flowing each line on its own would break them
        // somewhere else.
        Assert.Equal(
            [
                "  // The key is what makes saying it",
                "  // again cheap. Same key, same object:",
                "  // the renderer moves what moved.",
                "  //",
                "  // A second paragraph.",
            ],
            Texts(Fit.To(lines, 40)));
    }

    [Fact]
    public void AWordLongerThanALineIsCut()
    {
        var lines = Highlight.Lines("// from Orblit.Gallery/Code/Highlight.cs", CodeLanguage.CSharp);

        Assert.Equal(["// from", "// Orblit.Gallery/Co", "// de/Highlight.cs"], Texts(Fit.To(lines, 20)));
    }

    [Fact]
    public void CodeThatDoesNotFitCarriesOnDeeperThanItStarted()
    {
        var lines = Highlight.Lines(
            """
                key: 1000 + i,          // its own, for as long as it exists
                spawn({ id: "core", at: [0, 0.4, 0], size: [0.8, 2.4, 0.8] });
            """,
            CodeLanguage.Tsx);

        Assert.Equal(
            [
                // The comment on the end goes above the line it was on.
                "    // its own, for as long as it exists",
                "    key: 1000 + i,",
                // Broken after a comma, and four deeper.
                "    spawn({ id: \"core\", at: [0, 0.4, 0],",
                "        size: [0.8, 2.4, 0.8] });",
            ],
            Texts(Fit.To(lines, 44)));
    }

    [Fact]
    public void AListIsNotSplitWhileWhatItIsInCouldBe()
    {
        var lines = Highlight.Lines(
            """
              spawn({ id: "core", at: [0, 0.4, 0], size: [0.8, 2.4, 0.8],
                      colour: "#F2F4F7" });
            """,
            CodeLanguage.Tsx);

        Assert.Equal(
            [
                // There is room for `size: [0.8,` on the first row, but the
                // comma after `at` is one bracket shallower...
                "  spawn({ id: \"core\", at: [0, 0.4, 0],",
                // ...and what is broken off lines up with the line written
                // after it.
                "          size: [0.8, 2.4, 0.8],",
                "          colour: \"#F2F4F7\" });",
            ],
            Texts(Fit.To(lines, 52)));
    }

    [Fact]
    public void LinesThatFitAreLeftExactlyAsWritten()
    {
        var lines = Highlight.Lines(
            """
            var sun = new RenderLight
            {
                Intensity = 82000,                  // lux
                FalloffRadius = 24,                 // metres
            };
            """,
            CodeLanguage.CSharp);

        Assert.Equal(Texts(lines), Texts(Fit.To(lines, 60)));
    }

    /// <summary>The one tone of the first place <paramref name="text"/> appears.</summary>
    private static Tone ToneOf(IReadOnlyList<CodeLine> lines, string text)
    {
        foreach (var line in lines)
        {
            var at = line.Text.IndexOf(text, StringComparison.Ordinal);
            if (at >= 0)
            {
                return Assert.Single(line.Tones.Skip(at).Take(text.Length).Distinct());
            }
        }

        throw new InvalidOperationException($"\"{text}\" is not in the code.");
    }

    private static string[] Texts(IEnumerable<CodeLine> lines) => [.. lines.Select(line => line.Text)];

    /// <summary>Every character but space and slashes, in order of code unit.</summary>
    private static char[] Letters(IEnumerable<CodeLine> lines) =>
        [.. lines.SelectMany(line => line.Text).Where(c => c is not (' ' or '/')).Order()];
}
