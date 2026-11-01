using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Orblit.Examples;

namespace Orblit.Gallery.Tests;

/// <summary>
/// The code panel's text: coloured by what each word is, and broken to fit
/// the panel where somebody would have broken it, rather than wherever the
/// text engine runs out of room. Then the panel itself, which folds, opens
/// long code at its first rows and opens wider.
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
            var dir = @"C:\temp\"; Run(dir, @"say ""hi""
              and bye");
            var style = """
                panel { padding: 8 }
                """;
            """");

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
        // In a verbatim string a backslash does not hide the quote after it,
        // two quotes are one, and a line break is part of the string.
        Assert.Equal(Tone.String, ToneOf(lines, "@\"C:\\temp\\\""));
        Assert.Equal(Tone.Call, ToneOf(lines, "Run"));
        Assert.Equal(Tone.String, ToneOf(lines, "@\"say \"\"hi\"\""));
        Assert.Equal(Tone.String, ToneOf(lines, "and bye\""));
        // A raw string runs to its closing quotes, past the end of its line.
        Assert.Equal(Tone.String, ToneOf(lines, "panel { padding: 8 }"));
    }

    [Fact]
    public void EveryExampleFitsThePanelAndNothingIsLostInFittingIt()
    {
        foreach (var columns in (int[])[40, 54, 80])
        {
            foreach (var example in GalleryExamples.All(new TempPictureFolder(), _ => null))
            {
                var lines = Highlight.Lines(example.Code);
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
            """);

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
        var lines = Highlight.Lines("// from Orblit.Gallery/Code/Highlight.cs");

        Assert.Equal(["// from", "// Orblit.Gallery/Co", "// de/Highlight.cs"], Texts(Fit.To(lines, 20)));
    }

    [Fact]
    public void CodeThatDoesNotFitCarriesOnDeeperThanItStarted()
    {
        var lines = Highlight.Lines(
            """
                key: 1000 + i,          // its own, for as long as it exists
                spawn({ id: "core", at: [0, 0.4, 0], size: [0.8, 2.4, 0.8] });
            """);

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
            """);

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
            """);

        Assert.Equal(Texts(lines), Texts(Fit.To(lines, 60)));
    }

    [Fact]
    public Task ThePanelShowsTheCodeColouredAndOpensItWider() => Headless.Run(() =>
    {
        var window = Gallery.Open();

        Assert.Single(Headless.Shown<CodeView>(window));
        Gallery.Text(window, "C#");
        var colours = Code(window).Inlines!.OfType<Run>().Select(run => run.Foreground).Distinct();
        Assert.True(colours.Count() >= 5);

        // The next example's code takes its place, in the same room.
        var first = Gallery.Examples()[0];
        var shown = Shown(window);
        Headless.Click(window, Gallery.Text(window, Gallery.Examples().Where(one => one.Section == first.Section).ElementAt(1).Name));
        Assert.NotEqual(shown, Shown(window));

        Headless.Click(window, Button(window, "Open wider"));
        Assert.Equal(2, Headless.Shown<CodeView>(window).Count());

        Headless.Click(window, Button(window, "Close"));
        Assert.Single(Headless.Shown<CodeView>(window));
        window.Close();
    });

    [Fact]
    public Task LongCodeOpensAtItsFirstRowsAndShowsAllWhenAsked() => Headless.Run(() =>
    {
        // A short window, so there is more code than room for it.
        var window = Gallery.Open(height: 700);
        var first = RowsShown(window);
        Assert.Equal((int)Math.Floor((700 - 320) / (11 * 1.55)), first);

        // Taller, but not by a row, so the text stays, and so would what is selected in it.
        var inlines = Code(window).Inlines;
        window.Height = 705;
        Headless.Render();
        Assert.Same(inlines, Code(window).Inlines);

        Headless.Click(window, Assert.Single(Headless.Shown<TextBlock>(window), one => one.Text?.StartsWith("Show all", StringComparison.Ordinal) == true));
        Assert.True(RowsShown(window) > first);

        var less = Gallery.Text(window, "Show less");
        less.BringIntoView();
        Headless.Render();
        Headless.Click(window, less);
        Assert.Equal(first, RowsShown(window));
        window.Close();
    });

    [Fact]
    public Task TheBarFoldsTheCodeAwayAndItStaysFolded() => Headless.Run(() =>
    {
        var window = Gallery.Open();
        Code(window);

        Headless.Click(window, Gallery.Text(window, "C#"));
        Assert.Empty(Headless.Shown<SelectableTextBlock>(window));
        // The bar is still there, to say what is folded and to open it again.
        Assert.Single(Headless.Shown<TextBlock>(window), one => one.Text?.EndsWith(" lines", StringComparison.Ordinal) == true);

        // Folded for the next example too: somebody who folded it is looking
        // at scenes, not code.
        Headless.Click(window, Gallery.Text(window, "Virtual cameras"));
        Assert.Empty(Headless.Shown<SelectableTextBlock>(window));

        Headless.Click(window, Gallery.Text(window, "C#"));
        Code(window);
        window.Close();
    });

    private static SelectableTextBlock Code(Window window) => Assert.Single(Headless.Shown<SelectableTextBlock>(window));

    private static Button Button(Window window, string name) =>
        Assert.Single(Headless.Shown<Button>(window), one => AutomationProperties.GetName(one) == name);

    /// <summary>The text of the panel's code, as it is showing.</summary>
    private static string Shown(Window window) => string.Concat(Code(window).Inlines!.OfType<Run>().Select(run => run.Text));

    /// <summary>How many rows the panel's code is showing.</summary>
    private static int RowsShown(Window window) => Code(window).Inlines!.OfType<LineBreak>().Count() + 1;

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
