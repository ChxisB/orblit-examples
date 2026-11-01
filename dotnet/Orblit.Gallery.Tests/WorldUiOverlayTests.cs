using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media;
using Orblit.Filament;
using Orblit.Stage.Avalonia;
using Orblit.Ui.Avalonia;

namespace Orblit.Gallery.Tests;

/// <summary>
/// The screen on the wall, pressed through the stage as somebody would press it: a click on the button, a
/// drag along the slider, a press beside the screen that is left for the camera.
/// </summary>
public sealed class WorldUiOverlayTests
{
    private static readonly Size View = new(960, 540);

    // Where the screen's button is, in the screen's own pixels.
    private static readonly Point Button = new(400, 280);

    [Fact]
    public Task AClickOnTheScreenPressesTheButtonAndDrawsANewPicture() => Headless.Run(() =>
    {
        using var booth = Booth.Open();
        Assert.Equal(1, booth.Pictures.Screens);

        booth.Tap(booth.Aim(Button));
        booth.Overlay.Refresh(0);

        Assert.Equal(1, booth.Example.Presses);
        Assert.Equal(2, booth.Pictures.Screens);
        Assert.Equal(0, booth.Orbits);
    });

    [Fact]
    public Task AClickBesideTheScreenDoesNothingAndIsLeftForTheCamera() => Headless.Run(() =>
    {
        using var booth = Booth.Open();

        booth.Tap(new Point(8, 8));
        booth.Overlay.Refresh(0);

        Assert.Equal(0, booth.Example.Presses);
        Assert.Equal(1, booth.Pictures.Screens);
        Assert.Equal(1, booth.Orbits);
    });

    [Fact]
    public Task OnlyTheLeftButtonPressesTheScreen() => Headless.Run(() =>
    {
        using var booth = Booth.Open();
        var at = booth.Aim(Button);

        booth.Window.MouseDown(at, MouseButton.Right);
        booth.Window.MouseUp(at, MouseButton.Right);

        Assert.Equal(0, booth.Example.Presses);
        Assert.Equal(1, booth.Orbits);
    });

    [Fact]
    public Task APressThatBeginsOnTheScreenDoesNotOrbitAndOneBesideItDoes() => Headless.Run(() =>
    {
        using var booth = Booth.Open();

        booth.Tap(booth.Aim(Button));
        Assert.Equal(0, booth.Orbits);

        booth.Tap(new Point(8, 8));
        Assert.Equal(1, booth.Orbits);
    });

    [Fact]
    public Task ADragAlongTheSliderSetsTheDaylightAndBack() => Headless.Run(() =>
    {
        using var booth = Booth.Open();
        var track = Booth.Track();

        booth.Window.MouseDown(booth.Aim(track.Middle), MouseButton.Left);
        booth.Window.MouseMove(booth.Aim(track.Right));
        Assert.True(booth.Example.Level > 0.7, $"The daylight is {booth.Example.Level}.");

        booth.Window.MouseMove(booth.Aim(track.Left));
        booth.Window.MouseUp(booth.Aim(track.Left), MouseButton.Left);

        Assert.True(booth.Example.Level < 0.2, $"The daylight is {booth.Example.Level}.");
        Assert.Equal(0, booth.Orbits);
    });

    [Fact]
    public Task ASecondFingerDoesNotTakeTheScreenOver() => Headless.Run(() =>
    {
        using var booth = Booth.Open();
        var track = Booth.Track();
        var second = new Pointer(Pointer.GetNextFreeId(), PointerType.Touch, isPrimary: false);

        booth.Window.MouseDown(booth.Aim(track.Middle), MouseButton.Left);
        booth.Raise(second, booth.Aim(track.Right), PointerUpdateKind.LeftButtonPressed);
        booth.Raise(second, booth.Aim(track.Right), PointerUpdateKind.Other);
        Assert.True(booth.Example.Level < 0.7, $"The second finger moved the daylight to {booth.Example.Level}.");

        booth.Window.MouseMove(booth.Aim(track.Right));
        Assert.True(booth.Example.Level > 0.7, $"The first finger left the daylight at {booth.Example.Level}.");
        booth.Window.MouseUp(booth.Aim(track.Right), MouseButton.Left);
    });

    [Fact]
    public Task APressTheSystemTakesBackIsNotAClickAndLeavesTheScreenFree() => Headless.Run(() =>
    {
        using var booth = Booth.Open();
        var finger = new Pointer(Pointer.GetNextFreeId(), PointerType.Touch, isPrimary: true);
        var at = booth.Aim(Button);

        booth.Raise(finger, at, PointerUpdateKind.LeftButtonPressed);
        finger.Capture(null);
        booth.Raise(finger, at, PointerUpdateKind.LeftButtonReleased);
        Assert.Equal(0, booth.Example.Presses);

        // A press the screen never heard the end of would hold the button, and the slider would not move.
        var track = Booth.Track();
        booth.Window.MouseDown(booth.Aim(track.Middle), MouseButton.Left);
        booth.Window.MouseMove(booth.Aim(track.Right));
        booth.Window.MouseUp(booth.Aim(track.Right), MouseButton.Left);
        Assert.True(booth.Example.Level > 0.7, $"The daylight is {booth.Example.Level}.");
    });

    [Fact]
    public Task TheScreenSaysHowManyTimesItWasPressedAndTheSliderSetsTheDaylight() => Headless.Run(() =>
    {
        var example = new WorldUiExample((_, _) => { }, _ => true);
        var window = new Window { Width = 800, Height = 450, Content = new Panel { Width = 800, Height = 450, Children = { new WorldUiFace(example) } } };
        window.Show();
        Headless.Render();
        Gallery.Text(window, "Ticket booth");
        Gallery.Text(window, "Pressed 0 times");

        Headless.Click(window, "booth_press");
        Headless.Find<Slider>(window, "booth_daylight").Value = 0.9;

        Gallery.Text(window, "Pressed 1 times");
        Assert.Equal(0.9, example.Level);
        window.Close();
    });

    [Fact]
    public Task WhenTheStageLetsGoOfTheOverlayTheScreenIsTakenDown() => Headless.Run(() =>
    {
        using var booth = Booth.Open();
        booth.Overlay.Refresh(0);

        booth.Stage.Child = null;

        Assert.Null(booth.Example.Wall);
        Assert.Equal(2, booth.Pictures.Released.Count);
    });

    [Fact]
    public Task AFrameOfNamesAPressAndAPictureOfTwoHundredStayInsideLooseBounds() => Headless.Run(() =>
    {
        using var booth = Booth.Open();
        var camera = booth.Example.ViewPoint.ToRenderCamera();
        const int frames = 200;

        var scene = Stopwatch.StartNew();
        for (var i = 0; i < frames; i++)
        {
            booth.Example.Scene(camera, i / 60.0);
        }

        scene.Stop();

        var middle = booth.Aim(Button);
        var press = Stopwatch.StartNew();
        booth.Tap(middle);
        booth.Overlay.Refresh(0);
        press.Stop();

        var pictures = new RecordedPictures();
        using var plates = new Nameplates(new LabelAtlas(), new NameplatesOptions { FadeStart = 6, FadeEnd = 14, Provide = pictures.Provide, Release = pictures.Release });
        var drawing = Stopwatch.StartNew();
        plates.Sync(Enumerable.Range(0, 200).Select(i => $"Name {i}"));
        drawing.Stop();

        // Loose, so only a change of an order of magnitude fails it.
        Assert.True(scene.Elapsed.TotalMilliseconds / frames < 25, $"A frame takes {scene.Elapsed.TotalMilliseconds / frames:F1} ms.");
        Assert.True(press.ElapsedMilliseconds < 1000, $"A press takes {press.ElapsedMilliseconds} ms to reach a picture.");
        Assert.True(drawing.ElapsedMilliseconds < 2000, $"Two hundred strings take {drawing.ElapsedMilliseconds} ms to draw.");
    });

    /// <summary>The overlay in a window, over a stage that counts the presses it is left.</summary>
    private sealed class Booth : IDisposable
    {
        private readonly PanelHost _host = new();
        private readonly RenderCamera _camera;

        private Booth(RecordedPictures pictures)
        {
            Pictures = pictures;
            Example = new WorldUiExample(pictures.Provide, pictures.Release);
            _camera = Example.ViewPoint.ToRenderCamera();
            Overlay = new WorldUiOverlay(Example, () => _host, () => _camera);
            Stage = new Border { Child = Overlay, Background = Brushes.Black };
            Stage.AddHandler(InputElement.PointerPressedEvent, (object? _, PointerPressedEventArgs _) => Orbits++);
            Window = new Window { Width = View.Width, Height = View.Height, Content = Stage };
        }

        public RecordedPictures Pictures { get; }

        public WorldUiExample Example { get; }

        public WorldUiOverlay Overlay { get; }

        public Border Stage { get; }

        public Window Window { get; }

        /// <summary>How many presses the screen did not take, which the stage would orbit with.</summary>
        public int Orbits { get; private set; }

        public static Booth Open()
        {
            var booth = new Booth(new RecordedPictures());
            booth.Window.Show();
            // A pointer lands only after the host has drawn the panel a frame or two.
            Headless.Render();
            Headless.Render();
            booth.Overlay.Refresh(0);
            return booth;
        }

        /// <summary>Where the slider's track is on the screen, laid out in a window of its own.</summary>
        public static (Point Left, Point Middle, Point Right) Track()
        {
            var face = new WorldUiFace(new WorldUiExample((_, _) => { }, _ => true));
            var window = new Window { Width = 800, Height = 450, Content = new Panel { Width = 800, Height = 450, Children = { face } } };
            window.Show();
            Headless.Render();
            var slider = Headless.Find<Slider>(window, "booth_daylight");
            var top = slider.TranslatePoint(default, face)!.Value; // the face is laid out, so the slider has a place in it
            var y = top.Y + (slider.Bounds.Height / 2);
            window.Close();
            return (new Point(top.X + 4, y), new Point(top.X + (slider.Bounds.Width / 2), y), new Point(top.X + slider.Bounds.Width - 4, y));
        }

        public void Tap(Point at)
        {
            Window.MouseDown(at, MouseButton.Left);
            Window.MouseUp(at, MouseButton.Left);
        }

        /// <summary>
        /// The point on the stage where the screen's pixel <paramref name="onScreen"/> is seen. Found by looking,
        /// because the screen is on a wall seen from an angle.
        /// </summary>
        public Point Aim(Point onScreen)
        {
            var wall = Example.Wall!; // mounted when the window was shown
            var best = (Distance: double.MaxValue, At: default(Point));
            for (var x = 0; x < View.Width; x += 2)
            {
                for (var y = 0; y < View.Height; y += 2)
                {
                    var at = new Point(x, y);
                    if (wall.PixelAt(PointerRay.FromCamera(_camera, at, View)) is not { } pixel)
                    {
                        continue;
                    }

                    var gap = new Vector(pixel.X - onScreen.X, pixel.Y - onScreen.Y).Length;
                    if (gap < best.Distance)
                    {
                        best = (gap, at);
                    }
                }
            }

            Assert.True(best.Distance < 8, $"No point on the stage shows {onScreen} of the screen.");
            return best.At;
        }

        /// <summary>A pointer that is not the window's mouse, doing what <paramref name="kind"/> says at <paramref name="at"/>.</summary>
        public void Raise(IPointer pointer, Point at, PointerUpdateKind kind)
        {
            var held = kind == PointerUpdateKind.LeftButtonReleased ? RawInputModifiers.None : RawInputModifiers.LeftMouseButton;
            var properties = new PointerPointProperties(held, kind);
            PointerEventArgs args = kind switch
            {
                PointerUpdateKind.LeftButtonPressed => new PointerPressedEventArgs(Overlay, pointer, Window, at, 0, properties, KeyModifiers.None),
                PointerUpdateKind.LeftButtonReleased => new PointerReleasedEventArgs(Overlay, pointer, Window, at, 0, properties, KeyModifiers.None, MouseButton.Left),
                _ => new PointerEventArgs(InputElement.PointerMovedEvent, Overlay, pointer, Window, at, 0, properties, KeyModifiers.None),
            };
            Overlay.RaiseEvent(args);
        }

        public void Dispose()
        {
            Window.Close();
            Example.Dispose();
            _host.Dispose();
        }
    }
}
