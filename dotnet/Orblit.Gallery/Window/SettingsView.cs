using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Orblit.Examples;

namespace Orblit.Gallery;

/// <summary>
/// An example's settings, drawn from what <see cref="Example.Settings"/> says they are now. It is
/// asked again after every change and every frame, since a setting can change without being touched.
/// </summary>
internal sealed class SettingsView : StackPanel
{
    private static readonly FontFamily Mono = new("Menlo, Consolas, DejaVu Sans Mono");
    private static readonly IBrush DeadLabel = GalleryColours.Of(0xFF6A7385);
    private static readonly IBrush LiveNote = GalleryColours.Of(0x80FFFFFF);
    private static readonly IBrush DeadNote = GalleryColours.Of(0x47FFFFFF);

    private readonly List<Row> _rows = [];
    private Example _example;

    // While true, what a control reports is the view setting it, not somebody using it.
    private bool _refreshing;

    public SettingsView(Example example)
    {
        _example = example;
        Refresh();
    }

    public void Show(Example example)
    {
        _example = example;
        _rows.Clear();
        Children.Clear();
        Refresh();
    }

    /// <summary>
    /// Draws the settings as they are now. A row whose setting is still there takes the newer copy in
    /// place, so that a slider being dragged or a line being typed into is not replaced under the pointer.
    /// </summary>
    public void Refresh()
    {
        if (_refreshing)
        {
            return;
        }

        _refreshing = true;
        try
        {
            var settings = _example.Settings();
            if (settings.Count == _rows.Count && settings.Select((setting, i) => _rows[i].Fits(setting)).All(fits => fits))
            {
                for (var i = 0; i < settings.Count; i++)
                {
                    _rows[i].Take(settings[i]);
                }
            }
            else
            {
                _rows.Clear();
                Children.Clear();
                _rows.AddRange(settings.Select(RowFor));
                Children.AddRange(_rows.Select(row => row.View));
            }
        }
        finally
        {
            _refreshing = false;
        }
    }

    private Row RowFor(ExampleSetting setting) => setting switch
    {
        Setting slider => SliderRow(slider),
        TextEntry entry => EntryRow(entry),
        Caption caption => CaptionRow(caption),
        _ => ChipsRow(setting),
    };

    private void Changed(Action change)
    {
        if (!_refreshing)
        {
            change();
            Refresh();
        }
    }

    private Row SliderRow(Setting first)
    {
        var setting = first;
        var slider = new Slider { VerticalAlignment = VerticalAlignment.Center };
        // Fluent's slider is sized for a touch screen. These are the panel's.
        slider.Resources["SliderHorizontalThumbWidth"] = 12.0;
        slider.Resources["SliderHorizontalThumbHeight"] = 12.0;
        slider.Resources["SliderThumbCornerRadius"] = new CornerRadius(6);
        slider.Resources["SliderTrackThemeHeight"] = 2.0;
        slider.Resources["SliderHorizontalHeight"] = 24.0;
        AutomationProperties.SetName(slider, first.Label);
        var shown = new TextBlock
        {
            Width = 64,
            TextAlignment = TextAlignment.Right,
            FontFamily = Mono,
            FontSize = 11,
            Foreground = GalleryColours.Body,
            VerticalAlignment = VerticalAlignment.Center,
        };
        void Take(ExampleSetting newer)
        {
            setting = (Setting)newer;
            slider.Minimum = setting.Min;
            slider.Maximum = setting.Max;
            slider.Value = Math.Clamp(setting.Value, setting.Min, setting.Max);
            shown.Text = setting.Value.ToString($"F{setting.Decimals}", CultureInfo.InvariantCulture) + setting.Unit;
        }

        slider.ValueChanged += (_, e) => Changed(() => setting.Change(e.NewValue));
        // Let go of, by the pointer or by a key that moves it.
        slider.AddHandler(PointerReleasedEvent, (_, _) => setting.Settle?.Invoke(slider.Value), RoutingStrategies.Tunnel, handledEventsToo: true);
        slider.KeyUp += (_, e) =>
        {
            if (e.Key is Key.Left or Key.Right or Key.Up or Key.Down or Key.Home or Key.End or Key.PageUp or Key.PageDown)
            {
                setting.Settle?.Invoke(slider.Value);
            }
        };
        Take(first);
        return new Row(Line(Label(first.Label, GalleryColours.Body), slider, shown, 2), first, Take);
    }

    private Row ChipsRow(ExampleSetting first)
    {
        var chips = Chips.Of(first);
        var label = Label(first.Label, GalleryColours.Body);
        var wrap = new WrapPanel { ItemSpacing = 4, LineSpacing = 4, VerticalAlignment = VerticalAlignment.Center };
        var note = new TextBlock { FontSize = 11, Margin = new Thickness(96, 0, 0, 2), TextWrapping = TextWrapping.Wrap };
        void Take(ExampleSetting newer)
        {
            chips = Chips.Of(newer);
            if (!wrap.Children.OfType<Button>().Select(chip => (string?)chip.Content).SequenceEqual(chips.Options))
            {
                wrap.Children.Clear();
                foreach (var option in chips.Options)
                {
                    var chip = new Button { Content = option, Padding = new Thickness(9, 4), CornerRadius = new CornerRadius(4), FontSize = 11.5, MinHeight = 0, BorderThickness = default };
                    chip.Click += (_, _) => Changed(() => chips.Select?.Invoke(option));
                    wrap.Children.Add(chip);
                }
            }

            label.Foreground = chips.Live ? GalleryColours.Body : DeadLabel;
            foreach (var chip in wrap.Children.OfType<Button>())
            {
                chips.Paint(chip);
            }

            note.IsVisible = chips.Note is not null;
            note.Text = chips.Note;
            note.Foreground = chips.Live ? LiveNote : DeadNote;
        }

        Take(first);
        var line = Line(label, wrap, null, 4);
        return new Row(new StackPanel { Children = { line, note } }, first, Take);
    }

    private Row EntryRow(TextEntry first)
    {
        var entry = first;
        // As typed, and never set back from the example, so that what it makes of the text does not
        // move the caret.
        var box = new TextBox { Text = first.Value, PlaceholderText = first.Hint, FontFamily = Mono, FontSize = 12 };
        AutomationProperties.SetName(box, first.Label);
        box.TextChanged += (_, _) => Changed(() => entry.Change(box.Text ?? ""));
        var view = new StackPanel
        {
            Margin = new Thickness(0, 4),
            Spacing = 4,
            Children = { Label(first.Label, GalleryColours.Body), box },
        };
        if (first.Pick is null)
        {
            return new Row(view, first, newer => entry = (TextEntry)newer);
        }

        var pick = new Button { Content = first.Pick.Button, Padding = new Thickness(9, 4), CornerRadius = new CornerRadius(4), FontSize = 11.5, MinHeight = 0 };
        pick.Click += async (_, _) =>
        {
            // Into the box, so a chosen file is taken exactly as a typed one is.
            if (entry.Pick is { } wanted && await PickFile(wanted) is { } path)
            {
                box.Text = path;
            }
        };
        view.Children.Insert(1, pick);
        return new Row(view, first, newer =>
        {
            entry = (TextEntry)newer;
            pick.Content = entry.Pick?.Button;
        });
    }

    /// <summary>The local path of the one file chosen, or null for none, or for one with no path on this disk.</summary>
    private async Task<string?> PickFile(FilePick pick)
    {
        if (TopLevel.GetTopLevel(this) is not { } top)
        {
            return null;
        }

        var chosen = await top.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType(string.Join(", ", pick.Extensions)) { Patterns = [.. pick.Extensions.Select(extension => "*." + extension)] }],
        });
        return chosen.Count > 0 ? chosen[0].TryGetLocalPath() : null;
    }

    private static Row CaptionRow(Caption first)
    {
        var text = new TextBlock
        {
            FontSize = 12,
            Foreground = GalleryColours.Body,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 2, 0, 6),
            Text = first.Label,
        };
        return new Row(text, first, newer => text.Text = newer.Label);
    }

    private static TextBlock Label(string text, IBrush ink) =>
        new() { Text = text, FontSize = 12, Foreground = ink, TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center };

    /// <summary>A label 96 wide, then <paramref name="control"/>, then what it says on the right if anything.</summary>
    private static DockPanel Line(TextBlock label, Control control, Control? end, double gap)
    {
        label.Width = 96;
        var line = new DockPanel { Margin = new Thickness(0, gap) };
        DockPanel.SetDock(label, Dock.Left);
        line.Children.Add(label);
        if (end is not null)
        {
            DockPanel.SetDock(end, Dock.Right);
            line.Children.Add(end);
        }

        line.Children.Add(control);
        return line;
    }

    /// <summary>A setting as drawn, and how it takes a newer copy of the same setting.</summary>
    private sealed class Row(Control view, ExampleSetting first, Action<ExampleSetting> take)
    {
        public Control View => view;

        public void Take(ExampleSetting setting) => take(setting);

        // A caption is matched by kind alone, because its label is its text.
        public bool Fits(ExampleSetting setting) =>
            setting.GetType() == first.GetType() && (setting is Caption || setting.Label == first.Label);
    }

    /// <summary>
    /// A row of options to press. A choice, a switch as Off and On, and a row of buttons, which is a
    /// choice where nothing stays chosen.
    /// </summary>
    private sealed record Chips(IReadOnlyList<string> Options, string? Selected, Action<string>? Select, string? Note)
    {
        private static readonly IBrush ChosenDead = GalleryColours.Of(0xFF4A3226);
        private static readonly IBrush ChosenDeadInk = GalleryColours.Of(0xFF9A8B82);
        private static readonly IBrush Unchosen = GalleryColours.Of(0xFF232833);
        private static readonly IBrush UnchosenInk = GalleryColours.Of(0xFF98A2B3);

        public bool Live => Select is not null;

        public static Chips Of(ExampleSetting setting) => setting switch
        {
            Choice choice => new(choice.Options, choice.Selected, choice.Select, null),
            Toggle toggle => new(
                ["Off", "On"],
                toggle.Value ? "On" : "Off",
                toggle.Enabled ? option => toggle.Change(option == "On") : null,
                toggle.Note),
            Buttons buttons => new(buttons.Options, null, buttons.Press, null),
            _ => throw new ArgumentException($"No row draws a {setting.GetType().Name}.", nameof(setting)),
        };

        public void Paint(Button chip)
        {
            var selected = (string?)chip.Content == Selected;
            (chip.Background, chip.Foreground) = (selected, Live) switch
            {
                (true, true) => (GalleryColours.Accent, Brushes.White),
                (true, false) => (ChosenDead, ChosenDeadInk),
                (false, true) => (Unchosen, UnchosenInk),
                (false, false) => (Unchosen, GalleryColours.Quiet),
            };
            // Drawn greyed rather than disabled, which would paint over which one is chosen.
            chip.IsHitTestVisible = Live;
            chip.Focusable = Live;
        }
    }
}
