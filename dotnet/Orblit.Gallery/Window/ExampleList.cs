using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Orblit.Examples;

namespace Orblit.Gallery;

/// <summary>
/// Every example, under its heading. It opens with only the heading that holds the example on show
/// opened out, so that every heading fits in the window. The rest open and fold when clicked.
/// </summary>
internal sealed class ExampleList : Border
{
    private readonly List<Section> _sections = [];
    private Example _showing;

    /// <param name="examples">What to list.</param>
    /// <param name="showing">The example on show.</param>
    /// <param name="show">Called when a row is clicked, with its example.</param>
    public ExampleList(IReadOnlyList<Example> examples, Example showing, Action<Example> show)
    {
        _showing = showing;
        Width = 232;
        Background = GalleryColours.Side;
        BorderBrush = GalleryColours.Edge;
        BorderThickness = new Thickness(0, 0, 1, 0);
        var column = new StackPanel { Margin = new Thickness(0, 12) };
        column.Children.Add(new TextBlock
        {
            Text = "ORBLIT",
            Margin = new Thickness(16, 6, 16, 14),
            FontSize = 12,
            LetterSpacing = 3,
            FontWeight = FontWeight.SemiBold,
            Foreground = GalleryColours.Accent,
        });
        foreach (var (section, listed) in Example.BySection(examples))
        {
            var one = new Section(section, listed, showing, example =>
            {
                _showing = example;
                Update();
                show(example);
            });
            _sections.Add(one);
            column.Children.Add(one.Heading);
            column.Children.Add(one.Rows);
        }

        Child = new ScrollViewer { Content = column };
        Update();
    }

    private void Update()
    {
        foreach (var section in _sections)
        {
            section.Update(_showing);
        }
    }

    /// <summary>A heading and the rows under it, which are only there while it is open.</summary>
    private sealed class Section
    {
        private readonly ExampleSection _section;
        private readonly TextBlock _label;
        private readonly Glyph _chevron = new(Glyph.ChevronDown, 16, GalleryColours.Quiet);
        private readonly List<(Example Example, Button Row, TextBlock Name)> _rows = [];
        private Example _showing;

        public Section(ExampleSection section, IReadOnlyList<Example> examples, Example showing, Action<Example> show)
        {
            _section = section;
            _showing = showing;
            var open = showing.Section == section;
            _label = new TextBlock
            {
                Text = section.Label().ToUpperInvariant(),
                FontSize = 10.5,
                LetterSpacing = 1.4,
                FontWeight = FontWeight.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
            };
            var count = new TextBlock
            {
                Text = examples.Count.ToString(System.Globalization.CultureInfo.InvariantCulture),
                FontSize = 11,
                Foreground = GalleryColours.Quiet,
                Margin = new Thickness(0, 0, 2, 0),
                VerticalAlignment = VerticalAlignment.Center,
            };
            var line = new DockPanel { LastChildFill = true };
            DockPanel.SetDock(_chevron, Dock.Right);
            DockPanel.SetDock(count, Dock.Right);
            line.Children.Add(_chevron);
            line.Children.Add(count);
            line.Children.Add(_label);
            Heading = new FoldButton(Toggle)
            {
                Content = line,
                Padding = new Thickness(18, 16, 14, 6),
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                IsOpen = open,
            };
            foreach (var example in examples)
            {
                _rows.Add(RowFor(example, show));
            }

            Rows = new StackPanel { IsVisible = open };
            Rows.Children.AddRange(_rows.Select(row => row.Row));
        }

        public FoldButton Heading { get; }

        public StackPanel Rows { get; }

        public void Update(Example showing)
        {
            _showing = showing;
            var open = Heading.IsOpen;
            Rows.IsVisible = open;
            _chevron.Data = open ? Glyph.ChevronDown : Glyph.ChevronRight;
            // A folded heading holding the example on show says so, since its row cannot.
            _label.Foreground = !open && showing.Section == _section ? GalleryColours.Accent : GalleryColours.Heading;
            foreach (var (example, row, name) in _rows)
            {
                var selected = example == showing;
                row.Background = selected ? GalleryColours.Of(0xFF1F2732) : Brushes.Transparent;
                row.BorderBrush = selected ? GalleryColours.Accent : Brushes.Transparent;
                name.Foreground = selected ? Brushes.White : GalleryColours.Name;
            }
        }

        private void Toggle()
        {
            Heading.IsOpen = !Heading.IsOpen;
            Update(_showing);
        }

        private static (Example, Button, TextBlock) RowFor(Example example, Action<Example> show)
        {
            var name = new TextBlock { Text = example.Name, FontSize = 13, FontWeight = FontWeight.SemiBold };
            var blurb = new TextBlock
            {
                Text = example.Blurb,
                FontSize = 11,
                Foreground = GalleryColours.Blurb,
                Margin = new Thickness(0, 2, 0, 0),
                TextWrapping = TextWrapping.Wrap,
            };
            var row = new Button
            {
                Content = new StackPanel { Children = { name, blurb } },
                Margin = new Thickness(8, 2),
                Padding = new Thickness(10, 9),
                CornerRadius = new CornerRadius(6),
                BorderThickness = new Thickness(2, 0, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left,
            };
            row.Click += (_, _) => show(example);
            return (example, row, name);
        }
    }
}
