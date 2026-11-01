using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Orblit.Gallery;

/// <summary>
/// The C# example's display over its scene. Built once, and changed in place every frame, since a
/// setting changes what it says.
/// </summary>
internal sealed class NativeInterfaceOverlay : StackPanel, IStageLayer
{
    private static readonly IBrush Bright = GalleryColours.Of(0xFFE9EEF4);
    private static readonly IBrush Soft = GalleryColours.Of(0xFFB2BECD);
    private static readonly IBrush Quiet = GalleryColours.Of(0xFF8593A6);
    private static readonly IBrush Groove = GalleryColours.Of(0xFF333E4C);
    private static readonly IBrush Plate = GalleryColours.Of(0xFF141920);

    private readonly NativeInterfaceExample _example;
    private readonly Border _dot = new() { Name = "accent_dot", Width = 12, Height = 12, CornerRadius = new CornerRadius(6) };
    private readonly TextBlock _power = new() { FontSize = 11, Foreground = Soft, HorizontalAlignment = HorizontalAlignment.Right };
    private readonly Border _bar = new() { Name = "power_bar", Height = 8, CornerRadius = new CornerRadius(4), HorizontalAlignment = HorizontalAlignment.Left };
    private readonly Border[] _pips = [.. Enumerable.Range(0, 5).Select(i => new Border { Name = $"pip_{i}", Width = 24, Height = 4, CornerRadius = new CornerRadius(2) })];

    // No accent is fully clear, so the first refresh always makes the brushes.
    private (uint Bar, uint Pip) _colours;
    private IBrush _lit = Groove;

    public NativeInterfaceOverlay(NativeInterfaceExample example)
    {
        _example = example;
        // Something to read, with nothing to press, so the scene under it still turns.
        IsHitTestVisible = false;
        Margin = new Thickness(20);
        Spacing = 12;
        HorizontalAlignment = HorizontalAlignment.Left;
        VerticalAlignment = VerticalAlignment.Top;
        Children.Add(Card(Chip()));
        Children.Add(Card(Readout()));
        Refresh(0);
    }

    public void Refresh(double seconds)
    {
        // Brushes are made when the accent changes, not every frame.
        var colours = _example.AccentColours;
        if (colours != _colours)
        {
            _colours = colours;
            var accent = GalleryColours.Of(colours.Bar);
            _dot.Background = accent;
            _bar.Background = accent;
            _lit = GalleryColours.Of(colours.Pip);
        }

        _bar.Width = _example.BarWidth;
        _power.Text = _example.PowerLabel;
        for (var i = 0; i < _pips.Length; i++)
        {
            _pips[i].Background = i < _example.Shields ? _lit : Groove;
        }
    }

    // Slightly see-through, so the scene shows at the edges.
    private static Border Card(Control content) => new()
    {
        Child = content,
        Background = Plate,
        BorderBrush = Groove,
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(10),
        Padding = new Thickness(16, 12),
        Opacity = 0.94,
        HorizontalAlignment = HorizontalAlignment.Left,
    };

    private StackPanel Chip() => new()
    {
        Orientation = Orientation.Horizontal,
        Spacing = 12,
        Children =
        {
            _dot,
            new TextBlock { Text = "Docking bay", FontSize = 16, FontWeight = FontWeight.SemiBold, Foreground = Bright, VerticalAlignment = VerticalAlignment.Center },
            new TextBlock { Text = "· clear", FontSize = 12.5, Foreground = Quiet, VerticalAlignment = VerticalAlignment.Center },
        },
    };

    private StackPanel Readout()
    {
        var heading = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        heading.Children.Add(new TextBlock { Text = "POWER", FontSize = 11, LetterSpacing = 0.6, Foreground = Quiet });
        Grid.SetColumn(_power, 1);
        heading.Children.Add(_power);

        var pips = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, Margin = new Thickness(0, 8, 0, 0) };
        pips.Children.AddRange(_pips);
        return new StackPanel
        {
            Width = NativeInterfaceExample.Track,
            Spacing = 8,
            Children =
            {
                heading,
                new Border { Height = 8, CornerRadius = new CornerRadius(4), Background = Groove, ClipToBounds = true, Child = _bar },
                pips,
            },
        };
    }
}
