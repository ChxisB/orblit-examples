using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Orblit.Gallery;

/// <summary>The colours more than one part of the window draws with.</summary>
internal static class GalleryColours
{
    public static readonly IBrush Window = Of(0xFF0F1319);

    /// <summary>The list and the panel on either side of the stage.</summary>
    public static readonly IBrush Side = Of(0xFF141922);

    /// <summary>The line between a side and the stage, and around the code.</summary>
    public static readonly IBrush Edge = Of(0xFF232B36);

    public static readonly IBrush Accent = Of(0xFFC25E22);

    /// <summary>A setting's label and a caption.</summary>
    public static readonly IBrush Body = Of(0xFFB6BFCC);

    public static readonly IBrush Name = Of(0xFFC5CDD8);

    /// <summary>What is said about a name, under or after it.</summary>
    public static readonly IBrush Blurb = Of(0xFF7C8798);

    public static readonly IBrush Heading = Of(0xFF8A95A6);

    public static readonly IBrush Title = Of(0xFF6E7A8C);

    /// <summary>Counts, chevrons and what cannot be acted on.</summary>
    public static readonly IBrush Quiet = Of(0xFF5C6472);

    public static IBrush Of(uint argb) => new ImmutableSolidColorBrush(Color.FromUInt32(argb));
}
