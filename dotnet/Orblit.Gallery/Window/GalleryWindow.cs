using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Orblit.Examples;

namespace Orblit.Gallery;

/// <summary>
/// The gallery: the examples down the side, the one on show in the middle under its name, and its
/// settings and code on the right. Bare, it is the stage alone, for recording one.
/// </summary>
internal sealed class GalleryWindow : Window
{
    private readonly Launch _launch;
    private readonly StageView _stage;
    private readonly TextBlock? _name;
    private readonly TextBlock? _blurb;
    private readonly SettingsView? _settings;
    private readonly CodeView? _code;
    private TimeSpan? _started;
    private int _loop;
    private bool _requested;

    /// <param name="examples">What the side lists, in order.</param>
    /// <param name="launch">Which example opens, and how.</param>
    /// <param name="renderer">Whether to draw with the renderer, which runs on macOS so far.</param>
    public GalleryWindow(IReadOnlyList<Example> examples, Launch launch, bool renderer)
    {
        Title = "Orblit Examples";
        Width = 1600;
        Height = 1000;
        _launch = launch;
        var showing = launch.Opening(examples);
        _stage = new StageView(showing, renderer, launch.Orbit, launch.Framed(showing.ViewPoint));
        if (launch.Bare)
        {
            Background = Brushes.Black;
            Content = _stage;
            return;
        }

        Background = GalleryColours.Window;
        _name = new TextBlock { FontSize = 19, FontWeight = FontWeight.SemiBold, Foreground = Brushes.White };
        _blurb = new TextBlock { FontSize = 12, Foreground = GalleryColours.Blurb, Margin = new Thickness(0, 3, 0, 0), TextWrapping = TextWrapping.Wrap };
        _settings = new SettingsView(showing);
        _code = new CodeView();
        var middle = new DockPanel();
        var header = new StackPanel { Margin = new Thickness(18, 18, 18, 12), Children = { _name, _blurb } };
        DockPanel.SetDock(header, Dock.Top);
        middle.Children.Add(header);
        middle.Children.Add(new Border
        {
            Margin = new Thickness(16, 0, 16, 16),
            CornerRadius = new CornerRadius(10),
            ClipToBounds = true,
            Child = _stage,
        });
        var panel = new Border
        {
            Width = 420,
            Background = GalleryColours.Side,
            BorderBrush = GalleryColours.Edge,
            BorderThickness = new Thickness(1, 0, 0, 0),
            Child = new ScrollViewer
            {
                Content = new StackPanel
                {
                    Margin = new Thickness(16),
                    Children = { Heading("Settings"), _settings, Heading("How it is written", 20), _code },
                },
            },
        };
        var row = new DockPanel();
        var list = new ExampleList(examples, showing, Show);
        DockPanel.SetDock(list, Dock.Left);
        DockPanel.SetDock(panel, Dock.Right);
        row.Children.Add(list);
        row.Children.Add(panel);
        row.Children.Add(middle);
        Content = row;
        Describe(showing);
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

    private void Show(Example example)
    {
        // Each example starts from its own beginning, seen from where it says to look.
        _started = null;
        _stage.Show(example);
        _settings?.Show(example);
        Describe(example);
    }

    private void Describe(Example example)
    {
        _name?.Text = example.Name;
        _blurb?.Text = $"{example.Blurb}  ·  drag to orbit, scroll to zoom";
        _code?.Show(example.Code, example.Name);
    }

    private void RequestFrame()
    {
        if (_requested)
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
        _stage.Draw(_launch.Clock((now - _started.Value).TotalSeconds));
        // An example's settings can change as it runs, such as a count that rises, so they are read again each frame.
        _settings?.Refresh();
        RequestFrame();
    }

    private static TextBlock Heading(string text, double above = 0) => new()
    {
        Text = text.ToUpperInvariant(),
        Margin = new Thickness(0, above, 0, 10),
        FontSize = 10.5,
        LetterSpacing = 1.6,
        FontWeight = FontWeight.SemiBold,
        Foreground = GalleryColours.Title,
    };
}
