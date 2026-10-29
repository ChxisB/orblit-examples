using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;
using Orblit.Examples;

namespace Orblit.Gallery.Tests;

/// <summary>
/// The display made of Avalonia controls: what it says, how full its bar is, how many pips are lit and
/// in what colour, all from the settings.
/// </summary>
public sealed class NativeInterfaceTests
{
    private const uint MossBar = 0xFF2F7A4E;
    private const uint MossPip = 0xFF4FA871;
    private const uint Unlit = 0xFF333E4C;

    [Fact]
    public Task AnInterfaceBuiltInCSharpLaysOut() => Headless.Run(() =>
    {
        var window = Shown(new NativeInterfaceExample { Health = 0.4, Shields = 3 });

        Gallery.Text(window, "Docking bay");
        Gallery.Text(window, "POWER");
        Gallery.Text(window, "40%");
        // 0.4 of 224 is 89.6, drawn as 90.
        Assert.Equal(90, Named<Border>(window, "power_bar").Bounds.Width);
        window.Close();
    });

    [Fact]
    public void HalvesRoundUp()
    {
        // 12.5, which rounding to even would print as 12.
        Assert.Equal("13%", new NativeInterfaceExample { Health = 0.125 }.PowerLabel);
        // 10.5 pixels, which rounding to even would draw as 10.
        Assert.Equal(11, new NativeInterfaceExample { Health = 0.046875 }.BarWidth);
    }

    [Fact]
    public Task TheSettingsAreWhatTheDisplayShows() => Headless.Run(() =>
    {
        var example = new NativeInterfaceExample();
        var window = Shown(example);
        OverlayTests.Control<Setting>(example, "Power").Change(0.678);
        // Half a pip rounds up, as the slider shows it.
        OverlayTests.Control<Setting>(example, "Shields").Change(2.5);
        OverlayTests.Control<Choice>(example, "Accent").Select?.Invoke("moss");
        Overlay(window).Refresh(1);
        Headless.Render();

        Gallery.Text(window, "68%");
        var bar = Named<Border>(window, "power_bar");
        Assert.Equal(152, bar.Bounds.Width);
        Assert.Equal(MossBar, ColourOf(bar));
        Assert.Equal(MossBar, ColourOf(Named<Border>(window, "accent_dot")));
        Assert.Equal([MossPip, MossPip, MossPip, Unlit, Unlit], Enumerable.Range(0, 5).Select(i => ColourOf(Named<Border>(window, $"pip_{i}"))));
        Assert.Equal(3, OverlayTests.Control<Setting>(example, "Shields").Value);
        Assert.Equal("moss", OverlayTests.Control<Choice>(example, "Accent").Selected);
        window.Close();
    });

    [Fact]
    public Task TheStageChangesTheControlsItHasRatherThanMakingNewOnes() => Headless.Run(() =>
    {
        var example = new NativeInterfaceExample();
        var window = Shown(example);
        var bar = Named<Border>(window, "power_bar");

        example.Health = 0.3;
        Overlay(window).Refresh(2);
        Headless.Render();

        Assert.Same(bar, Named<Border>(window, "power_bar"));
        Gallery.Text(window, "30%");
        window.Close();
    });

    private static Window Shown(NativeInterfaceExample example)
    {
        var window = new Window { Width = 900, Height = 700, Content = new NativeInterfaceOverlay(example) };
        window.Show();
        Headless.Render();
        return window;
    }

    private static NativeInterfaceOverlay Overlay(Window window) => Assert.IsType<NativeInterfaceOverlay>(window.Content);

    private static T Named<T>(Window window, string name)
        where T : Control => Assert.Single(window.GetVisualDescendants().OfType<T>(), one => one.Name == name);

    private static uint ColourOf(Border border) => Assert.IsAssignableFrom<ISolidColorBrush>(border.Background).Color.ToUInt32();
}
