using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace Orblit.Gallery;

/// <summary>
/// An example's code, coloured and broken to fit, under a bar that names its language and copies it.
/// In the panel the bar folds it away, long code opens at its first rows, and a button opens it wider
/// over the window.
/// </summary>
internal sealed class CodeView : Border
{
    private static readonly FontFamily Mono = new("Menlo, Consolas, DejaVu Sans Mono");
    private static readonly IBrush Paper = GalleryColours.Of(0xFF0D1117);
    private static readonly IBrush Rule = GalleryColours.Of(0xFF1B222C);

    // Visual Studio Code's dark theme, which is what most people reading C# already read it in, on
    // GitHub's dark background. Comments are quieter than its green: here they are most of what is
    // written, and they are the explanation rather than the thing explained.
    private static readonly Dictionary<Tone, IBrush> Inks = new()
    {
        [Tone.Plain] = GalleryColours.Of(0xFFD4D4D4),
        [Tone.Comment] = GalleryColours.Of(0xFF7A8699),
        [Tone.Keyword] = GalleryColours.Of(0xFF569CD6),
        [Tone.Control] = GalleryColours.Of(0xFFC586C0),
        [Tone.Name] = GalleryColours.Of(0xFF9CDCFE),
        [Tone.Type] = GalleryColours.Of(0xFF4EC9B0),
        [Tone.Call] = GalleryColours.Of(0xFFDCDCAA),
        [Tone.String] = GalleryColours.Of(0xFFCE9178),
        [Tone.Number] = GalleryColours.Of(0xFFB5CEA8),
    };

    private readonly Action? _close;
    private readonly double _size;
    private readonly Thickness _pad;
    private readonly Typeface _typeface = new(Mono);
    private readonly SelectableTextBlock _text;
    private readonly Border _fade;
    private readonly Border _showAll;
    private readonly TextBlock _showAllLabel = new() { FontSize = 11.5, VerticalAlignment = VerticalAlignment.Center };
    private readonly Glyph _showAllGlyph = new(Glyph.ChevronDown, 16, GalleryColours.Heading);
    private readonly Control _body;
    private readonly Border _bar;
    private readonly FoldButton? _fold;
    private readonly Glyph _chevron = new(Glyph.ChevronDown, 16, GalleryColours.Quiet);
    private readonly TextBlock _title = new() { FontSize = 13, FontWeight = FontWeight.SemiBold, Foreground = GalleryColours.Name, Margin = new Thickness(0, 0, 10, 0) };
    private readonly TextBlock _language = new() { Text = "C#", FontSize = 10.5, LetterSpacing = 0.6, FontWeight = FontWeight.SemiBold, Foreground = GalleryColours.Title };
    private readonly TextBlock _count = new() { FontSize = 10.5, Foreground = GalleryColours.Quiet, Margin = new Thickness(8, 0, 0, 0) };
    private readonly TextBlock _copied = new() { Text = "Copied", FontSize = 11, Foreground = GalleryColours.Of(0xFF7FB88A), Margin = new Thickness(0, 0, 2, 0), IsVisible = false, VerticalAlignment = VerticalAlignment.Center };
    private readonly Glyph _copyGlyph = new(Glyph.Copy, 14, GalleryColours.Blurb);
    private readonly Button _wider;

    private string _source = "";
    private IReadOnlyList<CodeLine> _lines = [];
    private IReadOnlyList<CodeLine> _rows = [];
    // What the code was last laid out for. Room is 0 until it has been.
    private (double Room, int FirstRows, bool Whole) _laid;
    private bool _folded;
    private bool _whole;
    private IDisposable? _copiedFor;
    private TopLevel? _window;

    /// <summary>The code as the panel shows it.</summary>
    public CodeView()
        : this(null)
    {
    }

    /// <param name="close">
    /// Null for the view in the panel. Otherwise this is the wide view over the window, and the
    /// action takes it away.
    /// </param>
    private CodeView(Action? close)
    {
        _close = close;
        var wide = close is not null;
        _size = wide ? 12.5 : 11;
        _pad = wide ? new Thickness(20, 14, 20, 20) : new Thickness(12, 10, 12, 12);
        Background = Paper;
        CornerRadius = new CornerRadius(wide ? 8 : 6);
        BorderBrush = GalleryColours.Edge;
        BorderThickness = new Thickness(1);
        ClipToBounds = true;

        _text = new SelectableTextBlock
        {
            FontFamily = Mono,
            FontSize = _size,
            LineHeight = _size * 1.55,
            Foreground = Inks[Tone.Plain],
            Margin = _pad,
            TextWrapping = TextWrapping.NoWrap,
        };
        _fade = Fade();
        _showAll = ShowAllRow();
        var body = new StackPanel { Children = { new Panel { Children = { _text, _fade } }, _showAll } };
        _body = wide ? new ScrollViewer { Content = body } : body;

        _wider = BarButton(new Glyph(wide ? Glyph.Close : Glyph.Wider, 14, GalleryColours.Blurb), wide ? "Close" : "Open wider", close ?? OpenWide);
        var lead = Lead(wide);
        _fold = wide ? null : new FoldButton(Fold) { Content = lead, Padding = new Thickness(8, 0, 0, 0), IsOpen = true };
        _bar = Bar(wide, _fold ?? (Control)lead);

        var column = new DockPanel();
        DockPanel.SetDock(_bar, Dock.Top);
        column.Children.Add(_bar);
        column.Children.Add(_body);
        Child = column;
    }

    /// <summary>Shows <paramref name="source"/>, from the example named <paramref name="title"/>. Folded stays folded.</summary>
    public void Show(string source, string title)
    {
        _title.Text = title;
        if (source == _source)
        {
            return;
        }

        _source = source;
        _lines = Highlight.Lines(source);
        _count.Text = $"{_lines.Count} lines";
        // Asked of one example, so it goes back to the first rows for the next.
        _whole = false;
        _laid = default;
        Lay();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _window = TopLevel.GetTopLevel(this);
        _window?.SizeChanged += WindowSized;
        SizeChanged += Sized;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _window?.SizeChanged -= WindowSized;
        _window = null;
        SizeChanged -= Sized;
        base.OnDetachedFromVisualTree(e);
    }

    private void Sized(object? sender, SizeChangedEventArgs e)
    {
        if (e.WidthChanged)
        {
            Lay();
        }
    }

    private void WindowSized(object? sender, SizeChangedEventArgs e) => Lay();

    private void Lay()
    {
        // Less the border, the padding and the room selectable text keeps for its caret.
        var room = Bounds.Width - 2 - _pad.Left - _pad.Right - 4;
        if (room <= 0)
        {
            return;
        }

        // A resize that changes none of these leaves the text alone, and what is selected in it.
        var laid = (Room: room, FirstRows: FirstRows(), Whole: _whole);
        if (laid == _laid)
        {
            return;
        }

        if (room != _laid.Room)
        {
            _rows = FitTo(room);
        }

        _laid = laid;
        Cut(_close is null && _rows.Count > laid.FirstRows + 6);
    }

    /// <summary>
    /// How many rows long code opens at: as many as the window has room for below the settings, so that
    /// it is only cut short where it would have scrolled.
    /// </summary>
    private int FirstRows() => Math.Max(12, (int)Math.Floor(((_window?.ClientSize.Height ?? 0) - 320) / (_size * 1.55)));

    /// <param name="cut">Whether the code is long enough to open at its first rows. A few rows over is not worth a button.</param>
    private void Cut(bool cut)
    {
        var cutShort = cut && !_whole;
        _text.Inlines = InlinesOf(cutShort ? _rows.Take(FirstRows()) : _rows);
        _fade.IsVisible = cutShort;
        _showAll.IsVisible = cut;
        _showAll.BorderThickness = _whole ? new Thickness(0, 1, 0, 0) : default;
        _showAllLabel.Text = _whole ? "Show less" : $"Show all {_lines.Count} lines";
        _showAllGlyph.Data = _whole ? Glyph.ChevronUp : Glyph.ChevronDown;
    }

    /// <summary>The code broken to fit <paramref name="room"/>, measured in the font it is drawn in.</summary>
    private IReadOnlyList<CodeLine> FitTo(double room)
    {
        var columns = Math.Max(24, (int)Math.Floor(room / (WidthOf(new string('0', 40)) / 40)));
        var rows = Fit.To(_lines, columns);
        // Counting columns is exact only while every character is as wide as a zero, and a character
        // the font lacks is drawn from another. So the count is checked against a real layout, with
        // fewer columns until no row is too wide.
        while (columns > 24 && rows.Any(row => WidthOf(row.Text) > room))
        {
            columns--;
            rows = Fit.To(_lines, columns);
        }

        return rows;
    }

    private double WidthOf(string text) =>
        new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, _typeface, _size, null).Width;

    private static InlineCollection InlinesOf(IEnumerable<CodeLine> rows)
    {
        var inlines = new InlineCollection();
        var first = true;
        foreach (var line in rows)
        {
            if (!first)
            {
                inlines.Add(new LineBreak());
            }

            first = false;
            var start = 0;
            for (var at = 1; at <= line.Length; at++)
            {
                if (at == line.Length || line.Tones[at] != line.Tones[start])
                {
                    var tone = line.Tones[start];
                    inlines.Add(new Run(line.Text[start..at])
                    {
                        Foreground = Inks[tone],
                        FontStyle = tone == Tone.Comment ? FontStyle.Italic : FontStyle.Normal,
                    });
                    start = at;
                }
            }
        }

        return inlines;
    }

    private void Fold()
    {
        _folded = !_folded;
        _fold?.IsOpen = !_folded;
        _body.IsVisible = !_folded;
        _bar.BorderThickness = _folded ? default : new Thickness(0, 0, 0, 1);
        _chevron.Data = _folded ? Glyph.ChevronRight : Glyph.ChevronDown;
    }

    /// <summary>What the bar says: the language and how long the code is, after the title or the fold's chevron.</summary>
    private StackPanel Lead(bool wide)
    {
        var lead = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        if (wide)
        {
            lead.Margin = new Thickness(20, 0, 0, 0);
            lead.Children.Add(_title);
        }
        else
        {
            _chevron.Margin = new Thickness(0, 0, 4, 0);
            lead.Children.Add(_chevron);
        }

        lead.Children.Add(_language);
        lead.Children.Add(_count);
        return lead;
    }

    // The rows that are not showing are under the fade, rather than cut off as if the code ended there.
    private static Border Fade() => new()
    {
        Height = 48,
        VerticalAlignment = VerticalAlignment.Bottom,
        IsHitTestVisible = false,
        Background = new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
            GradientStops = { new GradientStop(Color.FromUInt32(0x000D1117), 0), new GradientStop(Color.FromUInt32(0xFF0D1117), 1) },
        },
    };

    private Border Bar(bool wide, Control lead)
    {
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
            Children = { _copied, BarButton(_copyGlyph, "Copy", () => _ = CopyAsync()), _wider },
        };
        var line = new DockPanel();
        DockPanel.SetDock(buttons, Dock.Right);
        line.Children.Add(buttons);
        line.Children.Add(lead);
        return new Border
        {
            Height = wide ? 40 : 30,
            Padding = new Thickness(0, 0, 4, 0),
            BorderBrush = Rule,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Child = line,
        };
    }

    private Border ShowAllRow()
    {
        var button = new Button
        {
            Background = Brushes.Transparent,
            BorderThickness = default,
            CornerRadius = default,
            Padding = default,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            Foreground = GalleryColours.Heading,
            Content = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, Children = { _showAllLabel, _showAllGlyph } },
        };
        button.Click += (_, _) =>
        {
            _whole = !_whole;
            Lay();
        };
        return new Border { Height = 30, BorderBrush = Rule, Child = button };
    }

    private static Button BarButton(Glyph glyph, string tip, Action press)
    {
        var button = new Button
        {
            Width = 28,
            Height = 28,
            Padding = default,
            Background = Brushes.Transparent,
            BorderThickness = default,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            Content = glyph,
        };
        ToolTip.SetTip(button, tip);
        AutomationProperties.SetName(button, tip);
        button.Click += (_, _) => press();
        return button;
    }

    private async Task CopyAsync()
    {
        if (TopLevel.GetTopLevel(this)?.Clipboard is not { } clipboard)
        {
            return;
        }

        // As written, not as broken to fit the panel.
        await clipboard.SetTextAsync(_source);
        _copiedFor?.Dispose();
        _copied.IsVisible = true;
        _copyGlyph.Data = Glyph.Check;
        _copiedFor = DispatcherTimer.RunOnce(
            () =>
            {
                _copied.IsVisible = false;
                _copyGlyph.Data = Glyph.Copy;
            },
            TimeSpan.FromMilliseconds(1600));
    }

    /// <summary>The code over the whole window, dimmed behind it, until it is closed.</summary>
    private void OpenWide()
    {
        if (OverlayLayer.GetOverlayLayer(this) is not { } layer)
        {
            return;
        }

        var barrier = new Border
        {
            Background = GalleryColours.Of(0x99000000),
            Width = layer.Bounds.Width,
            Height = layer.Bounds.Height,
        };
        void Fill(object? sender, SizeChangedEventArgs e) => (barrier.Width, barrier.Height) = (layer.Bounds.Width, layer.Bounds.Height);
        void Close()
        {
            layer.SizeChanged -= Fill;
            layer.Children.Remove(barrier);
            _wider.Focus();
        }

        var wide = new CodeView(Close)
        {
            MaxWidth = 760,
            Margin = new Thickness(40),
            VerticalAlignment = VerticalAlignment.Center,
        };
        wide.Show(_source, _title.Text ?? "");
        barrier.Child = wide;
        // A press on the dimmed window, or Escape, takes it away as Close does.
        barrier.PointerPressed += (_, e) =>
        {
            if (e.Source == barrier)
            {
                Close();
            }
        };
        barrier.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        };
        layer.SizeChanged += Fill;
        layer.Children.Add(barrier);
        wide._wider.Focus();
    }
}
