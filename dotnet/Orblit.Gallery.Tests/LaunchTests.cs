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
}
