using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Orblit.Examples;
using Orblit.Filament;
using Orblit.VectorMath;

namespace Orblit.Gallery.Tests;

/// <summary>
/// Every example, put through what the gallery does to it. The failure this
/// exists for is quiet. An example that throws while it builds its scene shows
/// an empty stage and says nothing. One that takes a second to build makes the
/// engine look slow. Neither shows in a picture of another example.
/// </summary>
public sealed class EveryExampleTests
{
    private static readonly RenderCamera Camera = new() { Position = new Vector3(0, 4, 14), Target = Vector3.Zero() };

    /// <summary>
    /// The examples that draw their own textures when they start. Named rather
    /// than found, because what goes wrong is absence. An example whose
    /// textures never arrive names none of them.
    /// </summary>
    private static readonly HashSet<string> DrawsItsOwn = ["Materials", "Blending"];

    public static TheoryData<string> Names() => [.. All().Select(example => example.Name)];

    [Theory]
    [MemberData(nameof(Names))]
    public void BuildsASceneAndAgainASecondLater(string name)
    {
        // Twice, because the first build often does the work, such as laying
        // a crowd out. The second is what every frame after it costs.
        var example = Example(name);

        Assert.NotNull(example.Scene(Camera, 0).Objects);
        Assert.NotNull(example.Scene(Camera, 1).Objects);
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void IsReadyInsideAFrame(string name)
    {
        var example = Example(name);
        example.Scene(Camera, 0);

        var clock = Stopwatch.StartNew();
        for (var i = 1; i <= 20; i++)
        {
            example.Scene(Camera, i / 60.0);
        }

        // Sixteen milliseconds is a frame. An example that cannot describe
        // itself inside one is what makes the engine look slow.
        var each = clock.Elapsed.TotalMilliseconds / 20;
        Assert.True(each < 16, $"{name} takes {each:F1} ms to describe itself, which is a frame gone before anything is drawn.");
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void SaysWhatItIs(string name)
    {
        var example = Example(name);

        Assert.NotEmpty(example.Name);
        Assert.NotEmpty(example.Blurb);
        Assert.NotEmpty(example.Code);
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void TheTexturesItDrawsForItselfAreWritten(string name)
    {
        // The temporary folder holds exactly what an example made itself.
        // Shipped and downloaded files live elsewhere.
        var own = SelfDrawn(Example(name).Scene(Camera, 0), Path.GetTempPath());
        if (!DrawsItsOwn.Contains(name))
        {
            // An example that starts drawing its own goes in the set, or
            // nothing checks that its textures arrive.
            Assert.Empty(own);
            return;
        }

        Assert.NotEmpty(own);
        Assert.All(own, path => Assert.True(File.Exists(path) && new FileInfo(path).Length > 0, $"{path} was not written, or was written empty."));
    }

    [Theory]
    [MemberData(nameof(Names))]
    public Task ShowsItsSettings(string name) => Headless.Run(() =>
    {
        // In the panel the gallery puts them in, a coloured box in a
        // scrolling list, so a setting that only breaks there breaks here.
        var example = Example(name);
        var window = new Window
        {
            Width = 420,
            Height = 900,
            Content = new Border
            {
                Background = GalleryColours.Side,
                Child = new ScrollViewer { Content = new SettingsView(example) { Margin = new Thickness(16) } },
            },
        };
        window.Show();
        Headless.Render();

        foreach (var setting in example.Settings().Where(one => one.Label.Length > 0))
        {
            Assert.Contains(Headless.Shown<TextBlock>(window), one => one.Text == setting.Label);
        }

        window.Close();
    });

    /// <summary>The gallery's own picture folder, with nothing set in the environment.</summary>
    private static IReadOnlyList<Example> All() => GalleryExamples.All(new TempPictureFolder(), _ => null);

    private static Example Example(string name) => All().Single(example => example.Name == name);

    private static List<string> SelfDrawn(RenderScene scene, string temp) =>
    [
        .. scene.Materials
            .SelectMany(material => material.Maps)
            .OfType<Texture>()
            .Select(map => map.Path)
            .Where(path => path.StartsWith(temp, StringComparison.Ordinal))
            .Distinct(),
    ];
}
