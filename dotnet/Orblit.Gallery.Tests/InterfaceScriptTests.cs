using Avalonia.Controls;
using Orblit.Examples;
using Orblit.Json;
using Orblit.Ui;
using Orblit.Ui.Avalonia;

namespace Orblit.Gallery.Tests;

/// <summary>
/// The interface example really is driven by its TypeScript. Worth a test rather than a look, because
/// the failure is silent: a script that does not run leaves an empty corner of the screen.
/// </summary>
public sealed class InterfaceScriptTests
{
    [Fact]
    public Task DrawsWhatTheHudScriptDescribes() => Headless.Run(() =>
    {
        // The score is the script's own 1840.
        using var example = new InterfaceExample { Health = 0.5 };
        var window = Shown(example.Describe());

        // Every one of these is written in the .tsx and nowhere else.
        Gallery.Text(window, "Sector 12");
        Gallery.Text(window, "· holding");
        // The class list says uppercase, and it means it.
        Gallery.Text(window, "HULL");
        Gallery.Text(window, "50%");
        Gallery.Text(window, "1840");
        Gallery.Text(window, "Repair");
        window.Close();
    });

    [Fact]
    public void TheSettingsWriteIntoTheScriptRatherThanAroundIt()
    {
        using var example = new InterfaceExample();
        OverlayTests.Control<Setting>(example, "Hull").Change(0.25);
        Assert.Contains("25%", Texts(example.Describe()));

        OverlayTests.Control<Setting>(example, "Hull").Change(0.9);
        Assert.Contains("90%", Texts(example.Describe()));
        Assert.Equal(0.9, OverlayTests.Control<Setting>(example, "Hull").Value);
    }

    [Fact]
    public void TheAccentAndTheButtonsAreTheScriptsToo()
    {
        using var example = new InterfaceExample();
        OverlayTests.Control<Choice>(example, "Accent").Select?.Invoke("steel");
        Assert.Equal("steel", OverlayTests.Control<Choice>(example, "Accent").Selected);
        Assert.Contains(example.Describe().Walk(), one => one.Node.Classes?.Contains("bg-steel-500", StringComparison.Ordinal) == true);

        OverlayTests.Control<Toggle>(example, "Buttons").Change(false);
        Assert.False(OverlayTests.Control<Toggle>(example, "Buttons").Value);
        Assert.DoesNotContain("Repair", Texts(example.Describe()));
    }

    [Fact]
    public void PressingAButtonRunsTheScriptThatWasWrittenForIt()
    {
        using var example = new InterfaceExample { Health = 0.5 };
        var handler = example.Describe().Walk().Single(one => one.Node.Text == "Repair").Node.HandlerFor("onPressed");
        Assert.Contains("50%", Texts(example.Describe()));

        // Straight into QuickJS, into the arrow function written in the .tsx.
        example.Host.Eval($"__orblit_ui.dispatch({JsonText.Encode(handler)})");

        // Raised by the amount that file says, with nothing on this side doing the sum or holding it.
        Assert.Contains("62%", Texts(example.Describe()));
        Assert.Equal(0.62, example.Health, 1e-9);
    }

    [Fact]
    public void AScriptThatStopsSaysWhyWhereItCanBeRead()
    {
        using var example = new InterfaceExample();
        example.Host.Eval("__orblit_ui.render = () => { throw new Error('no render'); }; __orblit_ui.dispatch = () => { throw new Error('no press'); };");

        var texts = Texts(example.Describe());
        Assert.Equal("The interface script stopped", texts[0]);
        Assert.Contains("no render", texts[1], StringComparison.Ordinal);

        example.Press("repair:onPressed");
        Assert.StartsWith("The interface script stopped: ", example.Note, StringComparison.Ordinal);
        Assert.Contains("no press", example.Note, StringComparison.Ordinal);
    }

    [Fact]
    public void WhatItAsksForIsAllInTheVocabulary()
    {
        using var example = new InterfaceExample();
        Assert.Empty(new UiBuilder(new UiBuilderOptions()).UnknownIn(example.Describe()));
    }

    [Fact]
    public Task TheMenuIsASecondScriptNotTheFirstOneAgain() => Headless.Run(() =>
    {
        using var example = new InterfaceExample { Showing = "Menu" };
        var window = Shown(example.Describe());

        // Written in menu.tsx, and nowhere in hud.tsx.
        Gallery.Text(window, "Paused");
        Gallery.Text(window, "Resume");
        Gallery.Text(window, "Quit to menu");
        // A list written with map, which the runtime flattens.
        Gallery.Text(window, "Punishing");
        window.Close();
    });

    [Fact]
    public void SwitchingBackGivesAFreshEngineNotTheMenuWithAHatOn()
    {
        using var example = new InterfaceExample();
        OverlayTests.Control<Choice>(example, "Written").Select?.Invoke("Menu");
        Assert.Contains("Paused", Texts(example.Describe()));
        Assert.IsType<Caption>(example.Settings()[1]);

        OverlayTests.Control<Choice>(example, "Written").Select?.Invoke("HUD");
        var texts = Texts(example.Describe());
        Assert.Contains("Sector 12", texts);
        Assert.DoesNotContain("Paused", texts);
    }

    [Fact]
    public Task APressOnTheStageGoesBackIntoTheScript() => Headless.Run(() =>
    {
        using var example = new InterfaceExample();
        var window = Shown(new ScriptOverlay(example));

        Headless.Click(window, Gallery.Text(window, "Repair"));
        Headless.Render();

        // The script's own 0.72, and the 0.12 its repair adds.
        Gallery.Text(window, "84%");
        Assert.Equal(0.84, example.Health, 1e-9);
        window.Close();
    });

    [Fact]
    public Task TheStageBuildsTheInterfaceAgainOnlyWhenTheScriptDescribesSomethingNew() => Headless.Run(() =>
    {
        using var example = new InterfaceExample();
        var overlay = new ScriptOverlay(example);
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

    private static Window Shown(UiNode description) => Shown(new UiBuilder(new UiBuilderOptions()).Build(description));

    private static Window Shown(Control content)
    {
        var window = new Window { Width = 900, Height = 700, Content = content };
        window.Show();
        Headless.Render();
        return window;
    }

    private static List<string?> Texts(UiNode node) => [.. node.Walk().Select(one => one.Node.Text).OfType<string>()];
}
