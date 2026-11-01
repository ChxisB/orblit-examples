using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Platform;
using Orblit.Examples;
using Orblit.Filament;

namespace Orblit.Gallery.Tests;

/// <summary>
/// What three examples draw over their scene, each on its own in a window.
/// </summary>
public sealed class OverlayTests
{
    private const string Rate = " a second if nothing waited for the display";

    private const double Step = 1 / 60.0;

    [Fact]
    public Task TheFramingNamesTheLiveShotAndTheFrameGivesItsShape() => Headless.Run(() =>
    {
        var cameras = new CamerasExample();
        var overlay = new FramingOverlay(cameras);
        var window = Shown(overlay, 800, 400);
        Assert.Equal(2, cameras.Aspect);

        Run(cameras, 60);
        overlay.Refresh(1);
        Assert.Equal(["Chase"], Lines(overlay));

        Pick(cameras, "", "Watchtower");
        cameras.Scene(cameras.ViewPoint.ToRenderCamera(), 61 / 60.0);
        overlay.Refresh(1);
        Assert.Equal(["Watchtower  ·  blending"], Lines(overlay));

        Pick(cameras, "", "Orbit");
        cameras.Scene(cameras.ViewPoint.ToRenderCamera(), 62 / 60.0);
        overlay.Refresh(1);
        Assert.Empty(Lines(overlay));
        window.Close();
    });

    [Fact]
    public Task TheGuidesShadeOutsideTheSoftZoneAndBoxTheSubject() => Headless.Run(() =>
    {
        var cameras = new CamerasExample();
        var overlay = new FramingOverlay(cameras);
        var window = Shown(overlay, 800, 400);
        Assert.Equal(Colors.Black, Pixel(window, 2, 2));

        Run(cameras, 60);
        overlay.Refresh(1);
        Headless.Render();

        var framing = cameras.Framing!; // the chase shot has guides
        var outside = Pixel(window, 2, 2);
        Assert.True(outside.R > 20 && outside.R > outside.G && outside.R > outside.B, $"Outside the soft zone is {outside}.");
        var aim = new Point(framing.Guides.ScreenX * 800, framing.Guides.ScreenY * 400);
        Assert.True(Pixel(window, aim.X, aim.Y).B > 60, "There is no aim cross.");
        // The subject's box is drawn on a square 11 across, centred on it.
        var subject = Pixel(window, ((framing.Subject.X + 1) / 2 * 800) - 5.5, (1 - framing.Subject.Y) / 2 * 400);
        Assert.True(subject.R > 200 && subject.G > 180 && subject.B < 140, $"The subject's box is {subject}.");
        window.Close();
    });

    [Theory]
    [InlineData(PhysicalKey.W, "KeyW")]
    [InlineData(PhysicalKey.A, "KeyA")]
    [InlineData(PhysicalKey.Space, "Space")]
    [InlineData(PhysicalKey.ShiftLeft, "ShiftLeft")]
    [InlineData(PhysicalKey.ArrowUp, "ArrowUp")]
    [InlineData(PhysicalKey.Digit1, "Digit1")]
    public void AKeyIsNamedAsTheW3CNamesIt(PhysicalKey key, string code) => Assert.Equal(code, WalkOverlay.Code(key));

    [Fact]
    public Task WhileWalkingAKeyIsHeldUntilItIsLetGoOrFocusGoesElsewhere() => Headless.Run(() =>
    {
        var voxels = new VoxelExample();
        var overlay = new WalkOverlay(voxels);
        var elsewhere = new Button { Content = "Elsewhere" };
        var window = Shown(new DockPanel { Children = { elsewhere, overlay } }, 400, 300);
        overlay.Refresh(0);
        Assert.True(overlay.IsFocused);

        window.KeyPressQwerty(PhysicalKey.W, RawInputModifiers.None);
        window.KeyPressQwerty(PhysicalKey.Space, RawInputModifiers.None);
        window.KeyReleaseQwerty(PhysicalKey.W, RawInputModifiers.None);
        Assert.Equal(["Space"], voxels.Held);

        elsewhere.Focus();
        Assert.Empty(voxels.Held);

        Control<Toggle>(voxels, "Walk").Change(false);
        overlay.Refresh(0);
        Assert.False(overlay.IsVisible);

        // Walking again takes the keys back from wherever they went.
        Control<Toggle>(voxels, "Walk").Change(true);
        overlay.Refresh(0);
        Assert.True(overlay.IsFocused);
        window.Close();
    });

    /// <summary>
    /// One walk is driven through the overlay and its twin by the calls the overlay should make. With
    /// the trees off the two worlds are the same, so any difference is the overlay's.
    /// </summary>
    [Fact]
    public Task ADragLooksAClickDigsAndARightClickPutsOneBack() => Headless.Run(() =>
    {
        var (walked, twin) = (Walker(), Walker());
        var overlay = new WalkOverlay(walked);
        var window = Shown(overlay, 400, 300);
        overlay.Refresh(0);
        var seconds = 0.0;
        for (; seconds < 3; seconds += 1 / 60.0)
        {
            Frame(walked, seconds);
            Frame(twin, seconds);
        }

        var middle = new Point(200, 150);
        var below = middle + new Vector(40, 240);
        var untouched = Blocks(walked, seconds);
        window.MouseDown(middle, MouseButton.Left);
        window.MouseMove(below);
        window.MouseUp(below, MouseButton.Left);
        twin.Look(-40 * 0.005, -240 * 0.005);
        Assert.Equal(Frame(twin, seconds).Camera, Frame(walked, seconds).Camera);
        Assert.Equal(untouched, Blocks(walked, seconds));

        window.MouseDown(middle, MouseButton.Left);
        window.MouseUp(middle, MouseButton.Left);
        twin.Dig();
        var dug = Blocks(walked, seconds);
        Assert.NotEqual(untouched, dug);
        Assert.Equal(Blocks(twin, seconds), dug);

        window.MouseDown(middle, MouseButton.Right);
        window.MouseUp(middle, MouseButton.Right);
        twin.Place();
        Assert.NotEqual(dug, Blocks(walked, seconds));
        Assert.Equal(Blocks(twin, seconds), Blocks(walked, seconds));
        window.Close();
    });

    [Theory]
    [InlineData(0, 0xFF8A93A0, "measuring…", null)]
    [InlineData(7.99, 0xFF7FB069, "7.99 ms a frame", "125")]
    [InlineData(8, 0xFFE0B252, "8.00 ms a frame", "125")]
    [InlineData(16, 0xFFE0B252, "16.00 ms a frame", "63")]
    [InlineData(16.6, 0xFFD9634F, "16.60 ms a frame", "60")]
    public Task TheReadoutSaysWhatAFrameCostsInAColourForHowMuch(double cost, uint colour, string said, string? rate) =>
        Headless.Run(() =>
        {
            var readout = new Readout(new BenchmarkExample(), () => cost);
            var window = Shown(readout, 600, 400);

            readout.Refresh(0);

            var shown = Assert.IsType<TextBlock>(Headless.Shown<TextBlock>(readout).First());
            Assert.Equal(Color.FromUInt32(colour), Assert.IsAssignableFrom<ISolidColorBrush>(shown.Foreground).Color);
            Assert.Equal(rate is null ? [said, Load] : [said, rate + Rate, Load], Lines(readout));
            window.Close();
        });

    [Fact]
    public Task TheCostIsReadFourTimesASecondAndTheLoadEveryFrame() => Headless.Run(() =>
    {
        var cost = 5.0;
        var benchmark = new BenchmarkExample();
        var readout = new Readout(benchmark, () => cost);
        var window = Shown(readout, 600, 400);

        // Times a binary fraction holds exactly, so that a quarter of a second apart is not just under it.
        readout.Refresh(0.25);
        cost = 10;
        Control<Setting>(benchmark, "Objects").Settle?.Invoke(7);
        readout.Refresh(0.4375);
        Assert.Equal(["5.00 ms a frame", "200" + Rate, "50000 members · 7 objects · sky fair"], Lines(readout));

        readout.Refresh(0.5);
        Assert.Equal("10.00 ms a frame", Lines(readout)[0]);
        window.Close();
    });

    [Fact]
    public Task TheNoteCardShowsWhatTheExampleSaysAndGoesWhenItStops() => Headless.Run(() =>
    {
        var cameras = new CamerasExample();
        var card = new NoteCard(cameras);
        var window = Shown(card, 600, 400);
        card.Refresh(0);
        Assert.False(card.IsVisible);

        cameras.Note = "There is no file at that path.";
        card.Refresh(0);
        Assert.Equal(["There is no file at that path."], Lines(card));

        cameras.Note = null;
        card.Refresh(0);
        Assert.False(card.IsVisible);
        window.Close();
    });

    /// <summary>
    /// One run is played through the overlay, and its twin by the calls each key should make. Any
    /// difference between the two is the overlay's.
    /// </summary>
    [Fact]
    public Task TheRunnerIsPlayedFromTheKeyboard() => Headless.Run(() =>
    {
        var (played, twin) = (Runner(), Runner());
        var overlay = new RunnerOverlay(played);
        var window = Shown(overlay, 400, 300);
        Assert.True(overlay.IsFocused);
        Assert.Contains("Tap or press Space to run", Lines(overlay));
        var together = Together(played, twin);

        Press(window, PhysicalKey.Space);
        twin.Confirm();
        Assert.Equal(RunnerPhase.Running, played.Phase);
        together();

        Press(window, PhysicalKey.ArrowLeft);
        twin.Steer(-1);
        together();
        Press(window, PhysicalKey.ArrowUp);
        twin.Jump();
        together();
        // Held, it repeats, and is still one lane over.
        window.KeyPressQwerty(PhysicalKey.D, RawInputModifiers.None);
        window.KeyPressQwerty(PhysicalKey.D, RawInputModifiers.None);
        window.KeyReleaseQwerty(PhysicalKey.D, RawInputModifiers.None);
        twin.Steer(1);
        together();
        Press(window, PhysicalKey.ArrowDown);
        twin.Slide();
        together();
        Press(window, PhysicalKey.A);
        twin.Steer(-1);
        together();
        Press(window, PhysicalKey.S);
        twin.Slide();
        together();
        Press(window, PhysicalKey.W);
        twin.Jump();
        together();
        Assert.Equal(RunnerPhase.Running, played.Phase);

        Press(window, PhysicalKey.Escape);
        Assert.Equal(RunnerPhase.Paused, played.Phase);
        Press(window, PhysicalKey.Enter);
        Assert.Equal(RunnerPhase.Running, played.Phase);
        Press(window, PhysicalKey.P);
        Assert.Equal(RunnerPhase.Paused, played.Phase);
        Press(window, PhysicalKey.P);
        Assert.Equal(RunnerPhase.Running, played.Phase);
        Press(window, PhysicalKey.Escape);
        Press(window, PhysicalKey.NumPadEnter);
        Assert.Equal(RunnerPhase.Running, played.Phase);
        window.Close();
    });

    [Fact]
    public Task ARunnerSwipeSteersJumpsOrSlidesAndATapMeansGo() => Headless.Run(() =>
    {
        var (played, twin) = (Runner(), Runner());
        var overlay = new RunnerOverlay(played);
        var window = Shown(overlay, 400, 300);
        var together = Together(played, twin);
        var middle = new Point(200, 150);

        window.MouseDown(middle, MouseButton.Left);
        window.MouseUp(middle, MouseButton.Left);
        twin.Confirm();
        Assert.Equal(RunnerPhase.Running, played.Phase);

        foreach (var (by, move) in new (Vector By, Action Move)[]
        {
            (new(-40, 5), () => twin.Steer(-1)),
            (new(3, -40), twin.Jump),
            (new(40, -5), () => twin.Steer(1)),
            (new(-3, 40), twin.Slide),
        })
        {
            // Once however far it goes.
            window.MouseDown(middle, MouseButton.Left);
            window.MouseMove(middle + by);
            window.MouseMove(middle + (by * 3));
            window.MouseUp(middle + (by * 3), MouseButton.Left);
            move();
            together();
        }

        window.Close();
    });

    [Fact]
    public Task TheRunnerPausesWhenFocusGoesElsewhereUnlessTheAutopilotPlays() => Headless.Run(() =>
    {
        var runner = Runner();
        var overlay = new RunnerOverlay(runner);
        var elsewhere = new Button { Content = "Elsewhere" };
        var window = Shown(new DockPanel { Children = { elsewhere, overlay } }, 400, 300);
        Press(window, PhysicalKey.Space);
        window.KeyPressQwerty(PhysicalKey.ArrowLeft, RawInputModifiers.None);

        elsewhere.Focus();
        Assert.Equal(RunnerPhase.Paused, runner.Phase);

        // The arrow let go of over there is not held here, so pressing it again counts.
        overlay.Focus();
        window.KeyPressQwerty(PhysicalKey.ArrowLeft, RawInputModifiers.None);
        Assert.Equal(RunnerPhase.Running, runner.Phase);

        runner.Autopilot = true;
        elsewhere.Focus();
        Assert.Equal(RunnerPhase.Running, runner.Phase);
        window.Close();
    });

    [Fact]
    public Task TheRunnerShowsTheScoreWhileItRunsAndACardWhileItDoesNot() => Headless.Run(() =>
    {
        var runner = Runner();
        var overlay = new RunnerOverlay(runner);
        var window = Shown(overlay, 400, 300);
        Assert.Equal(400 / 300.0, runner.Aspect);
        Assert.DoesNotContain("0", Lines(overlay));
        Assert.DoesNotContain("Best 0", Lines(overlay));

        runner.Start();
        runner.Update(Step);
        overlay.Refresh(Step);
        Assert.Equal(["0", "0", "14 m/s"], Lines(overlay));

        runner.Pause();
        overlay.Refresh(Step);
        Assert.Equal(["0", "0", "14 m/s", "Paused", "Tap, or press Esc or Space, to carry on"], Lines(overlay));

        runner.Resume();
        runner.Autopilot = true;
        // More than one coin, so the label says what they were worth together.
        while (runner.Popup() is not { Points: > 1 })
        {
            runner.Update(Step);
        }

        overlay.Refresh(Step);
        Assert.Contains("AUTO", Lines(overlay));
        var (points, at, _) = runner.Popup()!; // the loop above ends on one
        var popup = Headless.Shown<TextBlock>(overlay).Single(text => text.Text == "+" + points);
        // Centred on the point, a 120 by 40 label.
        Assert.Equal((at.X + 1) / 2 * 400, popup.Margin.Left + 60, 6);
        Assert.Equal((1 - at.Y) / 2 * 300, popup.Margin.Top + 20, 6);

        runner.Autopilot = false;
        while (runner.Phase == RunnerPhase.Running)
        {
            runner.Update(Step);
        }

        overlay.Refresh(Step);
        int Flashes() => Headless.Shown<Border>(overlay).Count(border => border.Background is ISolidColorBrush { Color: var colour } && colour == Color.FromUInt32(0xFFE0302A));
        Assert.Equal(1, Flashes());
        Assert.DoesNotContain("Crashed!", Lines(overlay));
        for (var i = 0; i < 45; i++)
        {
            runner.Update(Step);
        }

        overlay.Refresh(Step);
        Assert.Equal(0, Flashes());
        Assert.Contains("Crashed!", Lines(overlay));
        Assert.Contains("A new best", Lines(overlay));

        // A stumble, with crashing turned off so that it is not the end.
        runner.Invincible = true;
        runner.Confirm();
        while (!runner.Careful)
        {
            runner.Update(Step);
        }

        overlay.Refresh(Step);
        Assert.Contains("Careful!", Lines(overlay));
        window.Close();
    });

    private static string Load => "50000 members · 200 objects · sky fair";

    internal static T Control<T>(Example example, string label)
        where T : ExampleSetting => example.Settings().OfType<T>().Single(setting => setting.Label == label);

    private static Window Shown(Control content, double width, double height)
    {
        var window = new Window { Width = width, Height = height, Background = Brushes.Black, Content = content };
        window.Show();
        Headless.Render();
        return window;
    }

    private static List<string?> Lines(Control control) => [.. Headless.Shown<TextBlock>(control).Select(text => text.Text)];

    private static void Pick(Example example, string label, string option) => Control<Choice>(example, label).Select?.Invoke(option);

    private static void Run(Example example, int frames)
    {
        for (var frame = 0; frame < frames; frame++)
        {
            example.Scene(example.ViewPoint.ToRenderCamera(), frame / 60.0);
        }
    }

    // A renderer that takes the models and keeps nothing, so the runner's body is in the scene.
    private static RunnerExample Runner() => new((_, _) => { }, _ => null);

    private static void Press(Window window, PhysicalKey key)
    {
        window.KeyPressQwerty(key, RawInputModifiers.None);
        window.KeyReleaseQwerty(key, RawInputModifiers.None);
    }

    /// <summary>Plays both a third of a second, and they must match at every frame.</summary>
    private static Action Together(RunnerExample played, RunnerExample twin)
    {
        var seconds = 0.0;
        return () =>
        {
            for (var frame = 0; frame < 20; frame++)
            {
                seconds += Step;
                Assert.Equal(Body(twin, seconds), Body(played, seconds));
            }
        };
    }

    /// <summary>Where the runner's body is and how it stands, as its transform's sixteen numbers.</summary>
    private static List<double> Body(RunnerExample runner, double seconds)
    {
        var body = runner.Scene(runner.ViewPoint.ToRenderCamera(), seconds).Objects.First(one => one.Key == 1).Transform;
        return [.. Enumerable.Range(0, 16).Select(k => body[k])];
    }

    private static VoxelExample Walker()
    {
        var voxels = new VoxelExample();
        Control<Toggle>(voxels, "Trees").Change(false);
        Control<Toggle>(voxels, "Walk").Change(true);
        return voxels;
    }

    private static RenderScene Frame(Example example, double seconds) => example.Scene(example.ViewPoint.ToRenderCamera(), seconds);

    private static List<float> Blocks(Example example, double seconds) =>
        [.. Frame(example, seconds).Populations.SelectMany(population => population.Transforms.Concat(population.Colours))];

    private static Color Pixel(Window window, double x, double y)
    {
        using var frame = window.CaptureRenderedFrame()!; // a shown window under Skia always has one
        using var pixels = frame.Lock();
        Assert.Equal(PixelFormat.Rgba8888, pixels.Format);
        var at = ((int)y * pixels.RowBytes) + ((int)x * 4);
        byte Byte(int offset) => Marshal.ReadByte(pixels.Address, at + offset);
        return Color.FromArgb(Byte(3), Byte(0), Byte(1), Byte(2));
    }
}
