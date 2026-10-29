using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Orblit.Examples;
using Orblit.Filament;
using Orblit.Interop;

namespace Orblit.Gallery.Tests;

/// <summary>What the renderer reports back to the example on show, and what the stage draws over it.</summary>
public sealed class StageTests
{
    private const string Fox = """
        {"clips":[{"name":"Survey","seconds":3.41666675}],"skins":[],"variants":[],"materials":[],
        "lights":[],"cameras":[],"bounds":{"min":[-8,0,-60],"max":[8,80,70]},"unsupported":[]}
        """;

    [Fact]
    public void TheRendererSaysWhatAModelHoldsAndTheFirstThingThatWentWrong()
    {
        var example = new CamerasExample();

        StageView.Hear(example, [new($"{AssetInfo.NotePrefix}/fox.glb", Fox), new("/a.glb", "No file at /a.glb"), new("/b.glb", "No file at /b.glb")]);

        var fox = Assert.Single(example.Models);
        Assert.Equal(("/fox.glb", "Survey"), (fox.Key, Assert.Single(fox.Value.Clips).Name));
        Assert.Equal("No file at /a.glb", example.Note);

        // A later report replaces what a model holds, and leaves a note standing until the example clears it.
        StageView.Hear(example, [new($"{AssetInfo.NotePrefix}/fox.glb", Fox.Replace("Survey", "Walk", StringComparison.Ordinal)), new("/c.glb", "Later")]);
        Assert.Equal("Walk", Assert.Single(Assert.Single(example.Models).Value.Clips).Name);
        Assert.Equal("No file at /a.glb", example.Note);

        example.Note = null;
        StageView.Hear(example, [new($"{AssetInfo.NotePrefix}/fox.glb", Fox)]);
        Assert.Null(example.Note);
    }

    [Fact]
    public Task OnlyTheCamerasTheBenchmarkAndTheBlocksDrawOverTheirScene() => Headless.Run(() =>
    {
        var drawn = Gallery.Examples()
            .Select(example => (example.Name, StageView.OverlayFor(example, () => 0)?.GetType().Name))
            .Where(pair => pair.Item2 is not null);

        Assert.Equal([("Virtual cameras", "FramingOverlay"), ("Benchmark", "Readout"), ("Blocks", "WalkOverlay")], drawn);
    });

    [Fact]
    public Task WithoutTheRendererNothingIsDrawnOverTheReasonWhy() => Headless.Run(() =>
    {
        var cameras = new CamerasExample { Note = "Something to say" };
        var stage = new StageView(cameras, renderer: false, orbit: 0);
        var window = new Window { Width = 400, Height = 300, Content = stage };
        window.Show();

        stage.Draw(0);
        stage.Show(new BenchmarkExample());
        stage.Draw(0);
        Headless.Render();

        Assert.Empty(stage.GetVisualDescendants().OfType<IStageLayer>());
        Assert.Equal(["The renderer runs on macOS so far."], Headless.Shown<TextBlock>(stage).Select(text => text.Text));
        window.Close();
    });

    /// <summary>
    /// The read finishes on another thread, and the hand-over after it writes the example's tables.
    /// The gallery asks for a scene on the UI thread, so the hand-over must come back to it.
    /// </summary>
    [Fact]
    public Task AFileReadInTheBackgroundIsHandedOverOnTheUIThread() => Headless.Run(async () =>
    {
        var files = new BackgroundFiles();
        var imported = new ImportedExample(_ => null, files);

        imported.Scene(imported.ViewPoint.ToRenderCamera(), 0);
        await files.Handed.Task.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal([true], files.OnUIThread);
    });

    [RendererFact]
    public Task TheRendererGivesTheExampleItsDeviceAndSaysWhatItCouldNotLoad() => Headless.Run(async () =>
    {
        var meshes = Gallery.Examples().OfType<MeshesExample>().Single();
        OverlayTests.Control<TextEntry>(meshes, "Path to a .glb or .gltf").Change("/nowhere/crate.glb");
        var stage = new StageView(meshes, renderer: true, orbit: 0);
        var window = new Window { Width = 320, Height = 200, Content = stage };
        window.Show();

        // The viewport chooses how to show frames after it is shown, and draws nothing before.
        for (var tick = 0; tick < 300 && meshes.Note is null; tick++)
        {
            stage.Draw(tick / 60.0);
            await Task.Delay(10);
        }

        Headless.Render();
        Assert.NotNull(meshes.Device);
        Assert.Equal("The file could not be read.", meshes.Note);
        Assert.Single(Headless.Shown<NoteCard>(window));
        window.Close();
    });

    private sealed class BackgroundFiles : ISampleFiles
    {
        public List<bool> OnUIThread { get; } = [];

        public TaskCompletionSource Handed { get; } = new();

        public Task<byte[]?> ReadAsync(string path) => Task.Run(() => (byte[]?)[1, 2, 3]);

        public void Provide(string name, byte[] bytes)
        {
            OnUIThread.Add(Dispatcher.UIThread.CheckAccess());
            Handed.TrySetResult();
        }
    }
}

/// <summary>A test that draws, skipped where the renderer library is not in the feed's package.</summary>
public sealed class RendererFactAttribute : FactAttribute
{
    private static readonly bool Present =
        NativeLoader.SearchPaths("orblit_filament", AppContext.BaseDirectory).Any(File.Exists);

    public RendererFactAttribute([CallerFilePath] string? sourceFilePath = null, [CallerLineNumber] int sourceLineNumber = -1)
        : base(sourceFilePath, sourceLineNumber)
    {
        if (!Present)
        {
            Skip = "The renderer library is not in this build. Pack the engine with its natives built.";
        }
    }
}
