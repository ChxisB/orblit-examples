using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Orblit.Filament.Avalonia;

namespace Orblit.Viewport;

/// <summary>
/// A lit scene laid out as a control among controls. The chrome around it is the point. A native
/// window would sit on top of everything. This picture takes part in layout, so the badge can
/// overlap it, the slider can resize it, and the whole thing clips to a rounded rectangle.
/// </summary>
internal sealed class ViewportWindow : Window
{
    private const double Split = 0.62;

    private readonly ColumnDefinition _stage = new(Split, GridUnitType.Star);
    private readonly ColumnDefinition _panel = new(1 - Split, GridUnitType.Star);
    private readonly RendererViewport? _viewport;
    private TimeSpan? _started;
    private int _loop;
    private bool _requested;

    /// <param name="renderer">Whether to draw with the renderer, which runs on macOS so far.</param>
    public ViewportWindow(bool renderer)
    {
        Title = "Orblit Viewport";
        Width = 1100;
        Height = 700;
        Background = Of(0xFF111419);

        var badge = Badge("Filament → IOSurface → compositor");
        badge.HorizontalAlignment = HorizontalAlignment.Left;
        badge.VerticalAlignment = VerticalAlignment.Bottom;
        badge.Margin = new Thickness(14);
        var overlay = new ToggleSwitch { Content = "Overlay label", IsChecked = true, FontSize = 12 };
        overlay.IsCheckedChanged += (_, _) => badge.IsVisible = overlay.IsChecked == true;
        var width = new Slider { Minimum = 0.25, Maximum = 0.85, Value = Split };
        width.ValueChanged += (_, e) => Resize(e.NewValue);

        _viewport = renderer ? new RendererViewport() : null;
        var stage = new Panel { Children = { (Control?)_viewport ?? Unavailable(), badge } };
        var row = new Grid { ColumnDefinitions = { _stage, _panel } };
        row.Children.Add(new Border { Margin = new Thickness(16, 8, 8, 16), CornerRadius = new CornerRadius(10), ClipToBounds = true, Child = stage });
        var inspector = Inspector(width, overlay);
        Grid.SetColumn(inspector, 1);
        row.Children.Add(inspector);

        var page = new DockPanel();
        var title = TitleBar();
        DockPanel.SetDock(title, Dock.Top);
        page.Children.Add(title);
        page.Children.Add(row);
        Content = page;
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        RequestFrame();
    }

    protected override void OnClosed(EventArgs e)
    {
        // A frame already asked for still comes after the window closes. The new loop leaves it unanswered.
        _loop++;
        _requested = false;
        base.OnClosed(e);
    }

    /// <summary>The columns are stars, so the render target is resized with its box rather than scaled.</summary>
    private void Resize(double split)
    {
        _stage.Width = new GridLength(split, GridUnitType.Star);
        _panel.Width = new GridLength(1 - split, GridUnitType.Star);
    }

    private void RequestFrame()
    {
        if (_viewport is null || _requested)
        {
            return;
        }

        _requested = true;
        var loop = _loop;
        RequestAnimationFrame(now =>
        {
            if (loop == _loop)
            {
                OnFrame(now);
            }
        });
    }

    private void OnFrame(TimeSpan now)
    {
        _requested = false;
        _started ??= now;
        var seconds = (now - _started.Value).TotalSeconds;
        _viewport?.Frame(TurningCube.At(seconds), seconds);
        RequestFrame();
    }

    private static Border Unavailable() => new()
    {
        Background = Of(0xFF161B22),
        Child = new TextBlock
        {
            Text = "The renderer runs on macOS so far.",
            Foreground = Of(0xFF7C8798),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        },
    };

    private static StackPanel TitleBar() => new()
    {
        Orientation = Orientation.Horizontal,
        Spacing = 10,
        Margin = new Thickness(18, 18, 18, 10),
        Children =
        {
            new TextBlock { Text = "Orblit", FontSize = 16, FontWeight = FontWeight.Bold, LetterSpacing = -0.3, VerticalAlignment = VerticalAlignment.Center },
            Badge("M1 · macOS viewport"),
        },
    };

    private static Border Inspector(Slider width, ToggleSwitch overlay)
    {
        var footer = new TextBlock
        {
            Text = "The host hands the renderer a turning cube each frame. Avalonia's compositor shows the finished IOSurface rather than pixels pushed through the CPU.",
            FontSize = 11.5,
            LineHeight = 17,
            Foreground = Of(0xFF6E7A8C),
            TextWrapping = TextWrapping.Wrap,
        };
        DockPanel.SetDock(footer, Dock.Bottom);
        return new Border
        {
            Margin = new Thickness(8, 8, 16, 16),
            Padding = new Thickness(18),
            Background = Of(0xFF181C23),
            BorderBrush = Of(0xFF2C333E),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Child = new DockPanel
            {
                Children =
                {
                    footer,
                    new StackPanel
                    {
                        Children =
                        {
                            new TextBlock { Text = "Inspector", FontSize = 15, FontWeight = FontWeight.SemiBold },
                            new TextBlock
                            {
                                Text = "Ordinary Avalonia controls, laid out beside a Filament surface in the same tree. Moving the split resizes the render target, not just its box.",
                                Margin = new Thickness(0, 6, 0, 22),
                                FontSize = 12.5,
                                LineHeight = 19,
                                Foreground = Of(0xFF8A94A3),
                                TextWrapping = TextWrapping.Wrap,
                            },
                            new TextBlock { Text = "Viewport width", FontSize = 12 },
                            width,
                            overlay,
                        },
                    },
                },
            },
        };
    }

    private static Border Badge(string text) => new()
    {
        Padding = new Thickness(8, 3),
        Background = Of(0xCC1F242D),
        BorderBrush = Of(0xFF2C333E),
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(4),
        VerticalAlignment = VerticalAlignment.Center,
        Child = new TextBlock { Text = text, FontSize = 10.5, LetterSpacing = 0.4, Foreground = Of(0xFFA6B0BF) },
    };

    private static ImmutableSolidColorBrush Of(uint argb) => new(Color.FromUInt32(argb));
}
