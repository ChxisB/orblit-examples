using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.VisualTree;
using Orblit.Examples;

namespace Orblit.Gallery.Tests;

/// <summary>
/// The list on the left, under its headings. It opens with only the heading that holds the example
/// on show opened out, so every heading fits in the window. The rest open and fold when clicked.
/// </summary>
public sealed class SidebarTests
{
    [Fact]
    public Task EveryHeadingIsListedAndOnlyTheFirstIsOpen() => Headless.Run(() =>
    {
        var window = Gallery.Open();

        foreach (var (section, _) in Example.BySection(Gallery.Examples()))
        {
            Gallery.Text(window, section.Label().ToUpperInvariant());
        }

        // The first example is on show, so its heading is open...
        Gallery.Text(window, "Virtual cameras");
        // ...and the others are folded.
        Assert.Empty(Gallery.Texts(window, "Blocks"));
        Assert.Empty(Gallery.Texts(window, "An interface in C#"));
        window.Close();
    });

    [Fact]
    public Task AHeadingOpensWhenClickedAndFoldsWhenClickedAgain() => Headless.Run(() =>
    {
        var window = Gallery.Open();
        // The heading's first row, since only that one is sure to be on screen. A new showcase can
        // push the others below the window.
        var first = Gallery.Examples().First(one => one.Section == ExampleSection.Showcases).Name;

        var heading = Gallery.Text(window, "SHOWCASES");
        var told = new List<object?>();
        ControlAutomationPeer.CreatePeerForElement(Assert.IsType<FoldButton>(heading.FindAncestorOfType<FoldButton>())).PropertyChanged += (_, e) => told.Add(e.NewValue);

        Headless.Click(window, heading);
        Gallery.Text(window, first);
        // Opening one leaves the others as they were.
        Gallery.Text(window, "Virtual cameras");

        Headless.Click(window, heading);
        Assert.Empty(Gallery.Texts(window, first));
        // A screen reader hears each change as it happens.
        Assert.Equal([ExpandCollapseState.Expanded, ExpandCollapseState.Collapsed], told);
        window.Close();
    });
}
