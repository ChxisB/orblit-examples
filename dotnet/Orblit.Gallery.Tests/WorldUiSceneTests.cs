using System.Text.Json;
using Avalonia;
using Orblit.Examples;
using Orblit.Filament;
using Orblit.Stage.Avalonia;

namespace Orblit.Gallery.Tests;

/// <summary>
/// The scene of the interface in the world, with a screen that is a stand-in and so needs no window.
/// Before the first picture it is the recorded scene in <c>fixtures/world_ui.json</c>, frame for frame.
/// After, it is what this example sends, pinned here.
/// </summary>
public sealed class WorldUiSceneTests
{
    private const int Frames = 300;
    private const int Room = 3;
    private const int Crowd = 200;

    private static readonly JsonElement Golden = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "fixtures", "world_ui.json"))).RootElement;

    private static readonly RenderCamera Camera = new WorldUiExample((_, _) => { }, _ => true).ViewPoint.ToRenderCamera();

    [Fact]
    public void TheFixtureIsFromCommittedSource() =>
        Assert.DoesNotContain("+dirty", Golden.GetProperty("sourceCommit").GetString());

    [Fact]
    public void BeforeTheFirstPictureEveryFrameIsTheSceneTheFixtureHolds()
    {
        using var example = Mounted(new RecordedPictures());

        var frames = Enumerable.Range(0, Frames).Select(i => SceneDigest.Message(example, i / 60.0)).ToList();

        var wanted = Golden.GetProperty("values").EnumerateArray().Select(value => value.GetString());
        var first = wanted.Zip(frames.Select(SceneDigest.Of)).TakeWhile(pair => pair.First == pair.Second).Count();
        var keys = Golden.GetProperty("keyValues").EnumerateObject().ToDictionary(key => key.Name, key => key.Value.GetString());
        var differing = keys.Where(key => SceneDigest.OfKey(key.Key, frames) != key.Value).Select(key => key.Key);
        Assert.Equal(Frames, first);
        Assert.Empty(differing);
        Assert.Equal(keys.Keys.Order(StringComparer.Ordinal), frames.SelectMany(frame => frame.Keys).Distinct().Order(StringComparer.Ordinal));
    }

    [Fact]
    public void TheControlsAreTheFixturesOwn() =>
        Assert.Equal(
            Golden.GetProperty("controls").EnumerateArray().Select(control => control.GetProperty("label").GetString()),
            new WorldUiExample((_, _) => { }, _ => true).Settings().Select(setting => setting.Label));

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void MovingAControlBeforeThePictureLeavesTheSceneTheFixtureShows(int control)
    {
        using var example = Mounted(new RecordedPictures());

        // As the fixture was recorded: a slider to the far end it is not at, a switch to the other side.
        switch (example.Settings()[control])
        {
            case Setting slider:
                slider.Change(slider.Value == slider.Max ? slider.Min : slider.Max);
                break;
            case Toggle toggle:
                toggle.Change(!toggle.Value);
                break;
        }

        var wanted = Golden.GetProperty("controls")[control].GetProperty("values")[0].GetString();
        Assert.Equal(wanted, SceneDigest.Of(SceneDigest.Message(example, 1)));
    }

    [Fact]
    public Task OnceThePicturesLandTheSceneHasTheScreenAndTwoHundredNames() => Headless.Run(() =>
    {
        var pictures = new RecordedPictures();
        using var example = Mounted(pictures);
        var before = example.Scene(Camera, 0);

        example.Sync();
        var after = example.Scene(Camera, 0);

        // The floor, the wall and the screen, and the crowd.
        Assert.Equal(Room + Crowd, before.Objects.Count);
        Assert.Equal(Room + Crowd + Crowd, after.Objects.Count);
        Assert.Equal((1, 1), (pictures.Screens, pictures.Names));
        // The screen's material, and one for each name.
        Assert.Equal(1 + Crowd, after.Materials.Count);
    });

    [Fact]
    public Task WithNoNamesThereAreOnlyTheRoomAndTheCrowd() => Headless.Run(() =>
    {
        using var example = Mounted(new RecordedPictures());
        ((Setting)example.Settings()[0]).Change(0);

        example.Sync();

        Assert.Equal(Room + Crowd, example.Scene(Camera, 0).Objects.Count);
    });

    [Fact]
    public Task DistinctTextDrawsAnotherPictureAndLetsGoOfTheOldOne() => Headless.Run(() =>
    {
        var pictures = new RecordedPictures();
        using var example = Mounted(pictures);
        example.Sync();
        var shared = pictures.Provided.Single(name => name.Contains("nameplates", StringComparison.Ordinal));

        ((Toggle)example.Settings()[1]).Change(true);
        example.Sync();

        Assert.Equal(2, pictures.Names);
        Assert.Equal([shared], pictures.Released);
    });

    [Fact]
    public void HalfANameRoundsUp()
    {
        using var example = Mounted(new RecordedPictures());

        ((Setting)example.Settings()[0]).Change(100.5);

        Assert.Equal(101, example.Labels);
    }

    [Fact]
    public void TheSliderOnTheWallSetsHowBrightTheSunIs()
    {
        using var example = Mounted(new RecordedPictures());
        Assert.Equal(70000, example.Scene(Camera, 0).Lights.Single().Intensity);

        example.Level = 1;

        Assert.Equal(120000, example.Scene(Camera, 0).Lights.Single().Intensity);
    }

    [Fact]
    public Task LettingGoReleasesWhatWasHandedOverAndTakesTheScreenOutOfTheScene() => Headless.Run(() =>
    {
        var pictures = new RecordedPictures();
        var example = Mounted(pictures);
        example.Sync();

        example.Dispose();

        Assert.Equal(2, pictures.Released.Count);
        Assert.Null(example.Wall);
        Assert.Equal(Room - 1 + Crowd, example.Scene(Camera, 0).Objects.Count);
    });

    private static WorldUiExample Mounted(RecordedPictures pictures)
    {
        var example = new WorldUiExample(pictures.Provide, pictures.Release);
        example.Mount(new Stand());
        return example;
    }

    // A screen that is only a size and a picture, so that the scene can be read without a window to draw in.
    private sealed class Stand : IPanel
    {
        private bool _dirty = true;

        public Size Size => WorldUiExample.Screen;

        public bool IsDirty => _dirty;

        public byte[] Paint()
        {
            _dirty = false;
            return [0];
        }

        public void Send(PanelPointer input)
        {
        }
    }
}
