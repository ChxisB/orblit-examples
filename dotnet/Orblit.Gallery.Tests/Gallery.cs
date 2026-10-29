using Avalonia.Controls;
using Orblit.Examples;

namespace Orblit.Gallery.Tests;

/// <summary>The gallery as it opens, in a desktop window, which is what it is laid out for.</summary>
internal static class Gallery
{
    public static GalleryWindow Open(double height = 1000, Launch? launch = null)
    {
        var window = new GalleryWindow(Examples(), launch ?? new Launch(null, false, 0), renderer: false) { Width = 1600, Height = height };
        window.Show();
        Headless.Render();
        return window;
    }

    public static IReadOnlyList<Example> Examples() => GalleryExamples.All(new TempPictureFolder(), _ => null);

    /// <summary>The text on screen that reads <paramref name="text"/>, of which there is one.</summary>
    public static TextBlock Text(Window window, string text) => Assert.Single(Texts(window, text));

    public static IEnumerable<TextBlock> Texts(Window window, string text) =>
        Headless.Shown<TextBlock>(window).Where(one => one.Text == text);
}
