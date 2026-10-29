using Avalonia.Controls;
using Avalonia.Media;
using GlyphPath = Avalonia.Controls.Shapes.Path;

namespace Orblit.Gallery;

/// <summary>
/// A small outline picture drawn from path data on a 24 unit square, as the editor draws its icons.
/// There is no icon font.
/// </summary>
internal sealed class Glyph : Viewbox
{
    public const string ChevronRight = "M10 7l5 5-5 5";
    public const string ChevronDown = "M7 10l5 5 5-5";
    public const string ChevronUp = "M7 14l5-5 5 5";
    public const string Close = "M6 6l12 12M18 6L6 18";
    public const string Copy = "M9 9h11v11H9zM5 15H4V4h11v1";
    public const string Check = "M5 12.5l4.5 4.5L19 7.5";
    public const string Wider = "M14 4h6v6M20 4l-7 7M10 20H4v-6M4 20l7-7";

    private readonly GlyphPath _path;

    public Glyph(string data, double size, IBrush ink)
    {
        _path = new GlyphPath
        {
            Data = Geometry.Parse(data),
            Stroke = ink,
            StrokeThickness = 1.7,
            StrokeLineCap = PenLineCap.Round,
            StrokeJoin = PenLineJoin.Round,
        };
        Width = size;
        Height = size;
        // A press on the picture is a press on whatever it sits in.
        IsHitTestVisible = false;
        Child = new Canvas { Width = 24, Height = 24, Children = { _path } };
    }

    public string Data
    {
        set => _path.Data = Geometry.Parse(value);
    }
}
