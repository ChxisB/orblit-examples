using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Orblit.Examples;

namespace Orblit.Gallery;

/// <summary>
/// The voxel walk's hands: keys walk and jump, a drag looks, a click digs and a right click puts a
/// block back. Shown only while walking, so that otherwise a drag turns the stage as anywhere else.
/// </summary>
internal sealed class WalkOverlay : Panel, IStageLayer
{
    private const string Hint = "WASD to walk · space to jump · drag to look\nclick to dig · right-click to put one back";

    // Radians of look for each pixel dragged.
    private const double Turn = 0.005;

    // A press that moves less than this before it lets go is a click rather than a look.
    private const double Tap = 4;

    private static readonly IBrush Faint = GalleryColours.Of(0x99FFFFFF);

    private readonly VoxelExample _voxels;
    private Point? _dragging;
    private double _travelled;

    public WalkOverlay(VoxelExample voxels)
    {
        _voxels = voxels;
        // Without a background the clicks between the crosshair and the hint would fall through to the stage.
        Background = Brushes.Transparent;
        Focusable = true;
        Cursor = new Cursor(StandardCursorType.Cross);
        IsVisible = false;
        Children.Add(new Border
        {
            Width = 15,
            Height = 15,
            BorderBrush = Faint,
            BorderThickness = new Thickness(1.5, 1.5, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        });
        Children.Add(new Border
        {
            Margin = new Thickness(12),
            Padding = new Thickness(10, 7),
            CornerRadius = new CornerRadius(6),
            Background = GalleryColours.Of(0xAA0E1116),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Bottom,
            Child = new TextBlock { Text = Hint, FontSize = 11.5, LineHeight = 17.25, Foreground = Brushes.White },
        });
    }

    public void Refresh(double seconds)
    {
        var walking = _voxels.Walking;
        if (walking && !IsVisible)
        {
            IsVisible = true;
            Focus();
        }

        IsVisible = walking;
    }

    /// <summary>
    /// The W3C name of <paramref name="key"/>, which is what the example asks about. Avalonia names a
    /// letter by the letter alone, and the rest it names as W3C does.
    /// </summary>
    internal static string Code(PhysicalKey key)
    {
        var name = key.ToString();
        return name.Length == 1 ? "Key" + name : name;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        _voxels.Held.Add(Code(e.PhysicalKey));
        e.Handled = true;
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        _voxels.Held.Remove(Code(e.PhysicalKey));
        e.Handled = true;
    }

    protected override void OnLostFocus(FocusChangedEventArgs e)
    {
        base.OnLostFocus(e);
        // A key let go of somewhere else never comes back here, and would walk on for ever.
        _voxels.Held.Clear();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        Focus();
        _dragging = e.GetPosition(this);
        _travelled = 0;
        e.Pointer.Capture(this);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_dragging is { } from)
        {
            var at = e.GetPosition(this);
            Vector moved = at - from;
            _voxels.Look(-moved.X * Turn, -moved.Y * Turn);
            _travelled += moved.Length;
            _dragging = at;
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_dragging is not null && _travelled < Tap)
        {
            if (e.InitialPressMouseButton == MouseButton.Right)
            {
                _voxels.Place();
            }
            else
            {
                _voxels.Dig();
            }
        }

        _dragging = null;
        e.Handled = true;
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        _dragging = null;
    }
}
