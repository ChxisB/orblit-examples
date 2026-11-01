using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Orblit.Examples;
using Orblit.Filament;
using Orblit.Filament.Avalonia;
using Orblit.Stage.Avalonia;

namespace Orblit.Gallery;

/// <summary>
/// The scene of the example on show, which a drag turns and a scroll moves in and out, with what
/// the example draws over it. Off the renderer, it says why there is no scene.
/// </summary>
internal sealed class StageView : Border
{
    private readonly RendererViewport? _renderer;
    private readonly Panel _layers = new();
    private readonly double _orbit;
    private Example _showing;
    private string? _heard;
    private double _facing;
    private Point? _dragging;

    // Made when an example needs it, because the window it holds is one nobody asked for otherwise.
    private PanelHost? _panels;

    /// <param name="showing">The example to show first.</param>
    /// <param name="renderer">Whether to draw with the renderer, which runs on macOS so far.</param>
    /// <param name="orbit">How fast the camera circles on its own, in radians a second.</param>
    /// <param name="view">Where the camera looks from first, which is the example's own unless the launch framed it.</param>
    public StageView(Example showing, bool renderer, double orbit, ViewPoint view)
    {
        _showing = showing;
        _orbit = orbit;
        View = view;
        _facing = View.Yaw;
        // Without a background a drag on the stage would fall through it.
        Background = Brushes.Transparent;
        Child = _layers;
        if (renderer)
        {
            _renderer = new RendererViewport();
            _layers.Children.Add(_renderer);
            Layer(showing);
        }
        else
        {
            Background = GalleryColours.Of(0xFF161B22);
            _layers.Children.Add(new TextBlock
            {
                Text = "The renderer runs on macOS so far.",
                Foreground = GalleryColours.Blurb,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            });
        }
    }

    /// <summary>Where the camera looks from now.</summary>
    public ViewPoint View { get; private set; }

    /// <summary>Shows <paramref name="example"/> from where it says to look at it.</summary>
    public void Show(Example example)
    {
        _showing = example;
        _heard = null;
        View = example.ViewPoint;
        _facing = View.Yaw;
        if (_renderer is not null)
        {
            Layer(example);
        }
    }

    /// <summary>Draws the example as it is <paramref name="seconds"/> after it was shown.</summary>
    public void Draw(double seconds)
    {
        // From the clock rather than added up each frame, so that a clip meant to be exactly one turn
        // is one turn whatever the frame rate did.
        if (_orbit != 0)
        {
            View = View with { Yaw = _facing + (_orbit * seconds) };
        }

        if (_renderer is { } viewport && viewport.Frame(_showing.Scene(View.ToRenderCamera(), seconds), seconds) && viewport.Renderer is { } drawn)
        {
            _showing.Device ??= drawn.GetProfile();
            Hear(_showing, drawn.GetNotes(), ref _heard);
        }

        foreach (var layer in _layers.Children.OfType<IStageLayer>())
        {
            layer.Refresh(seconds);
        }
    }

    /// <summary>
    /// Gives <paramref name="example"/> what the renderer said of the scene it drew. A note the example
    /// set itself stays until the example clears it. The renderer's own, kept in <paramref name="heard"/>,
    /// goes when the renderer stops saying it, as when a model it was converting is drawn.
    /// </summary>
    internal static void Hear(Example example, IReadOnlyList<Note> notes, ref string? heard)
    {
        var said = new Dictionary<string, string>();
        foreach (var note in notes)
        {
            said[note.About] = note.Saying;
        }

        var (problems, models) = AssetInfo.Split(said);
        foreach (var model in models)
        {
            example.Models[model.Path] = model;
        }

        if (example.Note is { } shown && shown == heard && !problems.Values.Contains(shown))
        {
            example.Note = null;
        }

        if (problems.Count > 0 && example.Note is null)
        {
            example.Note = heard = problems.Values.First();
        }
    }

    /// <summary>
    /// What <paramref name="example"/> draws over its scene, if anything. <paramref name="panels"/> opens the
    /// window an example draws controls in, and <paramref name="camera"/> is where the scene is seen from.
    /// </summary>
    internal static Control? OverlayFor(Example example, Func<double> gpuMilliseconds, Func<PanelHost> panels, Func<RenderCamera> camera) => example switch
    {
        CamerasExample cameras => new FramingOverlay(cameras),
        VoxelExample voxels => new WalkOverlay(voxels),
        BenchmarkExample benchmark => new Readout(benchmark, gpuMilliseconds),
        NativeInterfaceExample native => new NativeInterfaceOverlay(native),
        RunnerExample runner => new RunnerOverlay(runner),
        WorldUiExample world => new WorldUiOverlay(world, panels, camera),
        _ => null,
    };

    /// <summary>
    /// Only over a drawn scene, because each layer describes what the scene shows, and a card would
    /// sit over the reason there is none.
    /// </summary>
    private void Layer(Example example)
    {
        _layers.Children.RemoveRange(1, _layers.Children.Count - 1);
        if (OverlayFor(example, GpuMilliseconds, Panels, () => View.ToRenderCamera()) is { } overlay)
        {
            _layers.Children.Add(overlay);
        }

        _layers.Children.Add(new NoteCard(example));
    }

    private PanelHost Panels() => _panels ??= new PanelHost();

    private double GpuMilliseconds() => _renderer?.Renderer?.GetStats().GpuMilliseconds ?? 0;

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _panels?.Dispose();
        _panels = null;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        _dragging = e.GetPosition(this);
        e.Pointer.Capture(this);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_dragging is { } from)
        {
            var at = e.GetPosition(this);
            View = ViewMoves.Orbited(View, at - from);
            _dragging = at;
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _dragging = null;
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        _dragging = null;
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        // A wheel notch is a hundred of the pixels a trackpad scrolls by.
        View = ViewMoves.Zoomed(View, -e.Delta.Y * 100);
        e.Handled = true;
    }
}

/// <summary>Something drawn over the stage, brought up to date once a frame.</summary>
internal interface IStageLayer
{
    void Refresh(double seconds);
}
