using Avalonia.Controls;

namespace Orblit.Viewport.Tests;

public sealed class ViewportWindowTests
{
    private const string Label = "Filament → IOSurface → compositor";

    [Fact]
    public Task TheShellDrawsAroundTheViewport() => Headless.Run(() =>
    {
        var window = Opened();

        var texts = Headless.Shown<TextBlock>(window).Select(text => text.Text).ToList();
        Assert.Single(texts, text => text == "Orblit");
        Assert.Single(texts, text => text == "M1 · macOS viewport");
        window.Close();
    });

    [Fact]
    public Task TheOverlayBadgeAnswersItsSwitch() => Headless.Run(() =>
    {
        var window = Opened();
        Assert.Contains(Label, Headless.Shown<TextBlock>(window).Select(text => text.Text));

        Headless.Click(window, Headless.Shown<ToggleSwitch>(window).Single());

        Assert.DoesNotContain(Label, Headless.Shown<TextBlock>(window).Select(text => text.Text));
        window.Close();
    });

    [Fact]
    public Task TheSliderResizesTheStageRatherThanScalingIt() => Headless.Run(() =>
    {
        var window = Opened();
        var stage = (Control)Headless.Shown<TextBlock>(window).Single(text => text.Text == "The renderer runs on macOS so far.").Parent!;
        var wide = stage.Bounds.Width;

        Headless.Shown<Slider>(window).Single().Value = 0.25;
        Headless.Render();

        Assert.InRange(stage.Bounds.Width, 1, wide / 2);
        window.Close();
    });

    private static ViewportWindow Opened()
    {
        var window = new ViewportWindow(renderer: false);
        window.Show();
        Headless.Render();
        return window;
    }
}
