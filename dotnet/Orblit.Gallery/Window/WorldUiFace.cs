using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Orblit.Gallery;

/// <summary>
/// The ticket booth's screen: a button that counts and a slider that sets the sun. Real controls, drawn
/// by <see cref="Orblit.Stage.Avalonia.PanelHost"/> and shown on the wall.
/// </summary>
internal sealed class WorldUiFace : Border
{
    private static readonly IBrush White = Brushes.White;

    private readonly WorldUiExample _example;
    private readonly TextBlock _count = new() { Name = "booth_count", FontSize = 36, Foreground = White };

    public WorldUiFace(WorldUiExample example)
    {
        _example = example;
        Background = GalleryColours.Of(0xFF16222E);
        Padding = new Thickness(32);
        Child = Layout();
        ShowCount();
    }

    private Grid Layout()
    {
        var grid = new Grid { RowDefinitions = new RowDefinitions("Auto,Auto,16,*,16,64") };
        Place(grid, new TextBlock { Text = "Ticket booth", FontSize = 56, FontWeight = FontWeight.Bold, Foreground = White }, 0);
        Place(grid, _count, 1);
        Place(grid, Press(), 3);
        Place(grid, Daylight(), 5);
        return grid;
    }

    private static void Place(Grid grid, Control content, int row)
    {
        Grid.SetRow(content, row);
        grid.Children.Add(content);
    }

    private Button Press()
    {
        var press = new Button
        {
            Name = "booth_press",
            Content = "Press",
            FontSize = 56,
            FontWeight = FontWeight.Bold,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
        };
        press.Click += (_, _) =>
        {
            _example.Presses++;
            ShowCount();
        };
        return press;
    }

    private Grid Daylight()
    {
        var slider = new Slider { Name = "booth_daylight", Minimum = 0, Maximum = 1, Value = _example.Level, VerticalAlignment = VerticalAlignment.Center };
        slider.ValueChanged += (_, e) => _example.Level = e.NewValue;
        var row = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*") };
        row.Children.Add(new TextBlock { Text = "Daylight", FontSize = 36, Foreground = White, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 24, 0) });
        Grid.SetColumn(slider, 1);
        row.Children.Add(slider);
        return row;
    }

    private void ShowCount() => _count.Text = $"Pressed {_example.Presses} times";
}
