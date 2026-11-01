using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Orblit.Camera;
using Orblit.Examples;

namespace Orblit.Gallery;

/// <summary>
/// The live shot's guides over the cameras example: the soft zone with the frame outside it shaded,
/// the dead zone inside it, where the shot aims, and where the subject is.
/// </summary>
internal sealed class FramingOverlay : Panel, IStageLayer
{
    private readonly CamerasExample _cameras;
    private readonly TextBlock _label = new()
    {
        Margin = new Thickness(12, 10),
        FontSize = 12,
        FontWeight = FontWeight.SemiBold,
        Foreground = GalleryColours.Of(0xDDFFFFFF),
        HorizontalAlignment = HorizontalAlignment.Left,
        VerticalAlignment = VerticalAlignment.Top,
    };

    private readonly Guides _guides = new();

    public FramingOverlay(CamerasExample cameras)
    {
        _cameras = cameras;
        IsHitTestVisible = false;
        Children.Add(_guides);
        Children.Add(_label);
    }

    public void Refresh(double seconds)
    {
        var framing = _cameras.Framing;
        _guides.Framing = framing;
        _guides.InvalidateVisual();
        _label.IsVisible = framing is not null;
        _label.Text = framing is { Blending: true } ? $"{framing.Label}  ·  blending" : framing?.Label;
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        // A dead zone is a fraction of the frame, so where the subject sits in it depends on the frame's shape.
        if (e.NewSize is { Width: > 0, Height: > 0 } size)
        {
            _cameras.Aspect = size.Width / size.Height;
        }
    }
}

/// <summary>The guides themselves, drawn rather than laid out, since a panel draws only its background.</summary>
internal sealed class Guides : Control
{
    private static readonly IBrush Outside = GalleryColours.Of(0x22E0446A);
    private static readonly IBrush DeadFill = GalleryColours.Of(0x1A54B6F0);
    private static readonly Pen SoftEdge = new(GalleryColours.Of(0x88E0446A), 1);
    private static readonly Pen DeadEdge = new(GalleryColours.Of(0xCC54B6F0), 1.5);
    private static readonly Pen Aim = new(GalleryColours.Of(0x9954B6F0), 1);
    private static readonly Pen Subject = new(GalleryColours.Of(0xFFF5D65B), 2);

    public CameraFraming? Framing { get; set; }

    public override void Render(DrawingContext context)
    {
        if (Framing is not { } framing)
        {
            return;
        }

        var size = Bounds.Size;
        var soft = Scaled(framing.Guides.Soft, size);
        context.DrawGeometry(Outside, null, new CombinedGeometry(GeometryCombineMode.Exclude, new RectangleGeometry(new Rect(size)), new RectangleGeometry(soft)));
        context.DrawRectangle(null, SoftEdge, soft);
        context.DrawRectangle(DeadFill, DeadEdge, Scaled(framing.Guides.Dead, size));

        var aim = new Point(framing.Guides.ScreenX * size.Width, framing.Guides.ScreenY * size.Height);
        context.DrawLine(Aim, aim - new Vector(7, 0), aim + new Vector(7, 0));
        context.DrawLine(Aim, aim - new Vector(0, 7), aim + new Vector(0, 7));

        if (framing.Subject.InFront)
        {
            // The subject is projected to the frame's -1 to 1 with y up, and the guides are fractions with y down.
            var x = (framing.Subject.X + 1) / 2 * size.Width;
            var y = (1 - framing.Subject.Y) / 2 * size.Height;
            context.DrawRectangle(null, Subject, new Rect(x - 5.5, y - 5.5, 11, 11));
        }
    }

    private static Rect Scaled(ScreenRect rect, Size size) =>
        new(rect.Left * size.Width, rect.Top * size.Height, rect.Width * size.Width, rect.Height * size.Height);
}
