using Orblit.Examples;

namespace Orblit.Gallery.Tests;

/// <summary>What the environment asks of the gallery as it opens, for recording one example.</summary>
public sealed class LaunchTests
{
    [Fact]
    public void AnExampleIsNamedByTheStartOfItsName()
    {
        var examples = Gallery.Examples();

        Assert.Equal("Virtual cameras", Launch.From(Only("ORBLIT_EXAMPLE", "virtual")).Opening(examples).Name);
        // A name that is no example opens the first, rather than nothing.
        Assert.Equal(examples[0], Launch.From(Only("ORBLIT_EXAMPLE", "Nothing like it")).Opening(examples));
        Assert.Equal(examples[0], Launch.From(_ => null).Opening(examples));
    }

    [Fact]
    public void TheOrbitIsANumberAndAnythingElseIsNone()
    {
        Assert.Equal(0.25, Launch.From(Only("ORBLIT_ORBIT", "0.25")).Orbit);
        Assert.Equal(0, Launch.From(Only("ORBLIT_ORBIT", "fast")).Orbit);
    }

    [Theory]
    [InlineData("NaN")]
    [InlineData("Infinity")]
    [InlineData("-Infinity")]
    public void ANumberThatIsNotFiniteIsNone(string value)
    {
        var launch = Launch.From(Named(("ORBLIT_ORBIT", value), ("ORBLIT_YAW", value), ("ORBLIT_SECONDS", value)));

        Assert.Equal(0, launch.Orbit);
        Assert.Null(launch.Yaw);
        Assert.Null(launch.Seconds);
    }

    [Fact]
    public void TheViewIsTheExamplesOwnUntilALaunchNamesOneOfItsNumbers()
    {
        var view = new Orblit.Examples.ViewPoint { Yaw = 1, Pitch = 0.2, Distance = 9 };

        Assert.Equal(view, Launch.From(_ => null).Framed(view));
        var framed = Launch.From(Only("ORBLIT_YAW", "2.5")).Framed(view);
        Assert.Equal(view with { Yaw = 2.5 }, framed);
        var all = Launch.From(Named(("ORBLIT_YAW", "0"), ("ORBLIT_PITCH", "0.9"), ("ORBLIT_DISTANCE", "30"), ("ORBLIT_SECONDS", "nope"))).Framed(view);
        Assert.Equal(view with { Yaw = 0, Pitch = 0.9, Distance = 30 }, all);
    }

    [Fact]
    public void AHeldClockIsTheSameWhateverHasPassed()
    {
        Assert.Equal(2, Launch.From(Only("ORBLIT_SECONDS", "2")).Clock(7.5));
        Assert.Equal(0, Launch.From(Only("ORBLIT_SECONDS", "0")).Clock(7.5));
        Assert.Equal(7.5, Launch.From(_ => null).Clock(7.5));
    }

    [Fact]
    public void SettingsAreNamedByLabelApartBySemicolonsWithWalkAsOneOfThem()
    {
        Assert.Empty(Launch.From(_ => null).Settings);
        Assert.Equal(["Relief=9", "Trees=0"], Launch.From(Only("ORBLIT_SET", " Relief=9 ;; Trees=0; ")).Settings);
        Assert.Equal(["Walk=0", "Relief=9"], Launch.From(Named(("ORBLIT_WALK", "0"), ("ORBLIT_SET", "Relief=9"))).Settings);
    }

    [Fact]
    public void ALaunchSetsTheOpeningExamplesSettingsAndSaysWhatItCouldNotSet()
    {
        var examples = Gallery.Examples();
        var blocks = examples.OfType<VoxelExample>().Single();
        Assert.True(blocks.Walking);
        var problems = new List<string>();

        Launch.From(Named(("ORBLIT_EXAMPLE", "Blocks"), ("ORBLIT_WALK", "0"), ("ORBLIT_SET", "Reliefs=9"))).Preset(examples, problems.Add);

        Assert.False(blocks.Walking);
        Assert.Single(problems);
        Assert.StartsWith("Blocks has no setting called \"Reliefs\".", problems[0], StringComparison.Ordinal);
    }

    [Fact]
    public Task ABareWindowIsTheStageAlone() => Headless.Run(() =>
    {
        var window = Gallery.Open(launch: Launch.From(Only("ORBLIT_BARE", "1")));

        Assert.Empty(Headless.Shown<ExampleList>(window));
        Assert.Empty(Headless.Shown<CodeView>(window));
        Assert.Single(Headless.Shown<StageView>(window));
        window.Close();
    });

    [Fact]
    public void ADragTurnsTheViewAndAScrollMovesItButNeitherPastWhereItStops()
    {
        var view = new Orblit.Examples.ViewPoint();

        var turned = ViewMoves.Orbited(view, new Avalonia.Vector(100, 0));
        Assert.Equal(view.Yaw - 0.8, turned.Yaw, 9);
        // Over the top would turn the world upside down.
        Assert.Equal(1.5, ViewMoves.Orbited(view, new Avalonia.Vector(0, 10_000)).Pitch);
        Assert.Equal(1.5, ViewMoves.Zoomed(view with { Distance = 2 }, -10_000).Distance);
        Assert.Equal(400, ViewMoves.Zoomed(view with { Distance = 300 }, 10_000).Distance);
    }

    private static Func<string, string?> Only(string name, string value) => asked => asked == name ? value : null;

    private static Func<string, string?> Named(params (string Name, string Value)[] all) =>
        asked => all.FirstOrDefault(one => one.Name == asked).Value;
}
