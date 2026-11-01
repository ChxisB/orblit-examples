using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Orblit.Filament;
using Orblit.Stage.Avalonia;

namespace Orblit.Gallery;

/// <summary>
/// The wall's screen in the gallery. It opens the screen's controls when the stage shows it, draws them
/// again when they change, and hands the screen the pointers that began on it. A drag that begins
/// anywhere else is left for the stage to orbit with.
/// </summary>
internal sealed class WorldUiOverlay : Border, IStageLayer
{
    private readonly WorldUiExample _example;
    private readonly Func<PanelHost> _host;
    private readonly Func<RenderCamera> _camera;

    private ControlPanel? _panel;

    // A panel takes one pointer, so the first finger that began on the screen owns it until it lifts.
    private IPointer? _held;

    /// <param name="example">The example this draws the screen of.</param>
    /// <param name="host">The window the screen is drawn in. Called when the overlay joins the stage, not before.</param>
    /// <param name="camera">The camera the scene is drawn from, which a pointer is aimed through.</param>
    public WorldUiOverlay(WorldUiExample example, Func<PanelHost> host, Func<RenderCamera> camera)
    {
        _example = example;
        _host = host;
        _camera = camera;
        // Clear, but still under the pointer, so a press on the screen reaches it.
        Background = Brushes.Transparent;
    }

    public void Refresh(double seconds) => _example.Sync();

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _panel = _host().Open(new WorldUiFace(_example), WorldUiExample.Screen);
        _example.Mount(_panel);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _example.Unmount();
        _panel?.Dispose();
        _panel = null;
        _held = null;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        // Touch and a pen report the left button, so the primary press is the only one that clicks.
        if (_held is not null || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed
            || _example.Wall is not { } wall || RayOf(e) is not { } ray || !wall.Hits(ray))
        {
            return;
        }

        _held = e.Pointer;
        e.Pointer.Capture(this);
        wall.Down(ray);
        // Handled, so the stage does not also start an orbit.
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (e.Pointer == _held && _example.Wall is { } wall && RayOf(e) is { } ray)
        {
            wall.Move(ray);
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (e.Pointer != _held)
        {
            return;
        }

        _held = null;
        e.Pointer.Capture(null);
        if (_example.Wall is { } wall && RayOf(e) is { } ray)
        {
            wall.Up(ray);
        }
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        if (e.Pointer == _held)
        {
            // The system took the touch back, so the press is not a click.
            _held = null;
            _example.Wall?.Cancel();
        }
    }

    private PointerRay? RayOf(PointerEventArgs e) =>
        Bounds.Width > 0 && Bounds.Height > 0 ? PointerRay.FromCamera(_camera(), e.GetPosition(this), Bounds.Size) : null;
}
