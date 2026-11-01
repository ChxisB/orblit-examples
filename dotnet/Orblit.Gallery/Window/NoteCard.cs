using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Orblit.Examples;

namespace Orblit.Gallery;

/// <summary>
/// What the example has to say about its scene, such as a file it could not find, in the middle of
/// the stage. A scene that went wrong still draws, so without this it would only look odd.
/// </summary>
internal sealed class NoteCard : Border, IStageLayer
{
    private readonly Example _example;
    private readonly TextBlock _text = new()
    {
        FontSize = 13,
        LineHeight = 19.5,
        Foreground = Brushes.White,
        TextWrapping = TextWrapping.Wrap,
        TextAlignment = TextAlignment.Center,
    };

    public NoteCard(Example example)
    {
        _example = example;
        IsHitTestVisible = false;
        IsVisible = false;
        MaxWidth = 460;
        Padding = new Thickness(20);
        CornerRadius = new CornerRadius(10);
        Background = GalleryColours.Of(0xE6161A21);
        HorizontalAlignment = HorizontalAlignment.Center;
        VerticalAlignment = VerticalAlignment.Center;
        Child = _text;
    }

    public void Refresh(double seconds)
    {
        _text.Text = _example.Note;
        IsVisible = _example.Note is not null;
    }
}
