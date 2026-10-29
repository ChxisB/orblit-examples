using Avalonia.Controls;
using Orblit.Examples;
using Orblit.Ui;
using Orblit.Ui.Avalonia;

namespace Orblit.Gallery.Tests;

/// <summary>
/// The interface built in C# draws too, and by the same route. Worth its own test, because the point
/// of it is that there is no script, so nothing else would notice if it quietly stopped working.
/// </summary>
public sealed class NativeInterfaceTests
{
    [Fact]
    public Task AnInterfaceBuiltInCSharpLaysOut() => Headless.Run(() =>
    {
        var example = new NativeInterfaceExample { Health = 0.4, Shields = 3 };
        var window = Shown(new UiBuilder(new UiBuilderOptions()).Build(example.Description));

        Gallery.Text(window, "Docking bay");
        // `uppercase` in the class list, and it means it.
        Gallery.Text(window, "POWER");
        Gallery.Text(window, "40%");
        window.Close();
    });

    [Fact]
    public void ItUsesNothingTheVocabularyDoesNotKnow() =>
        Assert.Empty(new UiBuilder(new UiBuilderOptions()).UnknownIn(new NativeInterfaceExample().Description));

    [Fact]
    public void TheSettingsAreWhatTheDescriptionIsBuiltFrom()
    {
        var example = new NativeInterfaceExample();
        OverlayTests.Control<Setting>(example, "Power").Change(0.678);
        // Half a pip rounds up, as the slider shows it.
        OverlayTests.Control<Setting>(example, "Shields").Change(2.5);
        OverlayTests.Control<Choice>(example, "Accent").Select?.Invoke("moss");

        var nodes = example.Description.Walk().Select(one => one.Node).ToList();
        Assert.Contains(nodes, node => node.Text == "68%");
        Assert.Contains(nodes, node => node.Css == "width: 152px" && node.Classes == "h-2 rounded-full bg-moss-500");
        Assert.Equal(3, nodes.Count(node => node.Classes == "w-6 h-1 rounded-full bg-moss-400"));
        Assert.Equal(2, nodes.Count(node => node.Classes == "w-6 h-1 rounded-full bg-slate-700"));
        Assert.Equal(3, OverlayTests.Control<Setting>(example, "Shields").Value);
        Assert.Equal("moss", OverlayTests.Control<Choice>(example, "Accent").Selected);
    }

    [Fact]
    public Task TheStageBuildsTheInterfaceAgainOnlyWhenItSaysSomethingNew() => Headless.Run(() =>
    {
        var example = new NativeInterfaceExample();
        var overlay = new NativeInterfaceOverlay(example);
        var window = Shown(overlay);
        var surface = Assert.IsType<UiSurface>(overlay.Child);
        var built = surface.Child;

        overlay.Refresh(1);
        Headless.Render();
        Assert.Same(built, surface.Child);

        example.Health = 0.3;
        overlay.Refresh(2);
        Headless.Render();
        Assert.NotSame(built, surface.Child);
        Gallery.Text(window, "30%");
        window.Close();
    });

    private static Window Shown(Control content)
    {
        var window = new Window { Width = 900, Height = 700, Content = content };
        window.Show();
        Headless.Render();
        return window;
    }
}
