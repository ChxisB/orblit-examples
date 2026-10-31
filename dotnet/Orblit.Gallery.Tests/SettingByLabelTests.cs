using Orblit.Examples;
using Orblit.Filament;

namespace Orblit.Gallery.Tests;

/// <summary>Setting an example's settings from the text that <c>ORBLIT_SET</c> holds.</summary>
public sealed class SettingByLabelTests
{
    [Fact]
    public void ASliderIsSetAndSettledAsWhenItsHandleIsLetGo()
    {
        var probe = new Probe();

        var problems = Set(probe, "slide=3");

        Assert.Empty(problems);
        Assert.Equal([3], probe.Changed);
        Assert.Equal([3], probe.Settled);
    }

    [Fact]
    public void AChoiceAToggleAndAnEntryAreSetByTheirLabelsInAnyCase()
    {
        var probe = new Probe();

        var problems = Set(probe, "MODE=wide", "Lamp=on", "Name=a b=c");

        Assert.Empty(problems);
        Assert.Equal("Wide", probe.Mode);
        Assert.True(probe.Lamp);
        Assert.Equal("a b=c", probe.Typed);
    }

    [Theory]
    [InlineData("Lamp=0", false)]
    [InlineData("Lamp=No", false)]
    [InlineData("Lamp=true", true)]
    [InlineData("Lamp=1", true)]
    public void AToggleTakesTheWordsAPersonWouldUse(string item, bool expected)
    {
        var probe = new Probe { Lamp = !expected };

        Assert.Empty(Set(probe, item));
        Assert.Equal(expected, probe.Lamp);
    }

    [Fact]
    public void ASettingThatIsNotThereNamesTheOnesThatAre()
    {
        var problems = Set(new Probe(), "Slid=3");

        Assert.Equal(["Probe has no setting called \"Slid\". It has \"Slide\", \"Mode\", \"Lamp\", \"Name\"."], problems);
    }

    [Theory]
    [InlineData("Slide", "\"Slide\" is not Label=value.")]
    [InlineData("=3", "\"=3\" is not Label=value.")]
    [InlineData("Slide=lots", "Slide: \"lots\" is not a number.")]
    [InlineData("Slide=NaN", "Slide: \"NaN\" is not a number.")]
    [InlineData("Slide=5", "Slide: it takes 0 to 4.")]
    [InlineData("Mode=huge", "Mode: it is one of \"Narrow\", \"Wide\".")]
    [InlineData("Lamp=dim", "Lamp: \"dim\" is not on or off.")]
    public void AnItemThatCannotBeSetIsSaidAndChangesNothing(string item, string said)
    {
        var probe = new Probe();

        Assert.Equal([said], Set(probe, item));
        Assert.Empty(probe.Changed);
        Assert.Equal("Narrow", probe.Mode);
        Assert.False(probe.Lamp);
    }

    [Fact]
    public void ARowWithNothingToActOnYetIsSaidRatherThanSkipped()
    {
        var probe = new Probe { Locked = true };

        Assert.Equal(["Mode: it has nothing to act on yet."], Set(probe, "Mode=Wide"));
        Assert.Equal(["Lamp: it has nothing to act on yet."], Set(probe, "Lamp=on"));
    }

    [Fact]
    public void ACaptionAndAButtonAreNotValuesToSet()
    {
        var problems = Set(new Probe(), "Note=hi", "Go=Wide");

        Assert.Equal(2, problems.Count);
        Assert.All(problems, one => Assert.Contains("has no setting called", one));
    }

    [Fact]
    public void AnItemTakenAfterAChangeSeesTheSettingsThatChangeMadeNew()
    {
        var probe = new Probe();

        // Wide adds a row, which the next item sets.
        Assert.Empty(Set(probe, "Mode=Wide", "Width=7"));

        Assert.Equal(7, probe.Width);
    }

    private static List<string> Set(Example example, params string[] items)
    {
        var problems = new List<string>();
        foreach (var item in items)
        {
            SettingByLabel.Set(example, item, problems.Add);
        }

        return problems;
    }

    private sealed class Probe : Example
    {
        public List<double> Changed { get; } = [];

        public List<double> Settled { get; } = [];

        public string Mode { get; private set; } = "Narrow";

        public bool Lamp { get; set; }

        public string Typed { get; private set; } = "";

        public double Width { get; private set; }

        public bool Locked { get; init; }

        public override string Name => "Probe";

        public override string Blurb => "";

        public override ExampleSection Section => ExampleSection.Basics;

        public override string Code => "";

        public override RenderScene Scene(RenderCamera camera, double seconds) => new() { Objects = [], Camera = camera };

        public override IReadOnlyList<ExampleSetting> Settings()
        {
            List<ExampleSetting> rows =
            [
                new Setting("Slide", 0, 0, 4, Changed.Add) { Settle = Settled.Add },
                new Choice("Mode", ["Narrow", "Wide"], Mode, Locked ? null : mode => Mode = mode),
                new Toggle("Lamp", Lamp, on => Lamp = on) { Enabled = !Locked },
                new TextEntry("Name", Typed, text => Typed = text),
                new Caption("Note"),
                new Buttons("Go", ["Wide"], _ => { }),
            ];
            if (Mode == "Wide")
            {
                rows.Add(new Setting("Width", Width, 0, 10, width => Width = width));
            }

            return rows;
        }
    }
}
