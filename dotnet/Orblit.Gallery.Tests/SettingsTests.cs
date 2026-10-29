using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Orblit.Examples;

namespace Orblit.Gallery.Tests;

/// <summary>Settings rows that the every-example test only shows, and does not use.</summary>
public sealed class SettingsTests
{
    /// <summary>The system's file picker does not open off a screen, so the choosing itself is not run here.</summary>
    [Fact]
    public Task AnEntryForAFileHasAButtonThatSaysWhetherOneIsChosen() => Headless.Run(() =>
    {
        var video = new VideoExample(_ => null, path => path == "/films/a.mp4");
        var settings = new SettingsView(video);
        var window = new Window { Width = 320, Height = 700, Content = settings };
        window.Show();
        Headless.Render();
        Assert.Equal(["Choose a film…"], Choosers(settings));

        Headless.Shown<TextBox>(settings).Single(box => AutomationProperties.GetName(box) == "Or a path").Text = "/films/a.mp4";
        Headless.Render();

        Assert.Equal(["Choose another…"], Choosers(settings));
        Assert.Null(video.Note);
        Assert.Empty(Choosers(new SettingsView(new MeshesExample(_ => null))));
        window.Close();
    });

    private static List<string?> Choosers(SettingsView settings) =>
        [.. settings.GetLogicalDescendants().OfType<Button>().Select(button => button.Content as string).Where(text => text?.StartsWith("Choose", StringComparison.Ordinal) ?? false)];
}
