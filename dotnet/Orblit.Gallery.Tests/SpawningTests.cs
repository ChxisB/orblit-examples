using Orblit.Examples;
using Orblit.Filament;
using Orblit.Light;
using Orblit.VectorMath;

namespace Orblit.Gallery.Tests;

/// <summary>
/// The world <c>script/world.tsx</c> spawns, read from the scene the example hands the renderer. The
/// ground at key 790 is the one object the C# side adds.
/// </summary>
public sealed class SpawningTests
{
    [Fact]
    public void TheScriptSpawnsAPillarAndFourRingsAndTheGroundIsAdded()
    {
        using var example = new SpawningExample();
        var objects = Scene(example, 0).Objects;

        // The pillar, then rings of 6, 10, 14 and 18.
        Assert.Equal([.. Enumerable.Range(800, 49).Select(key => (long)key), 790L], objects.Select(one => one.Key));
        var pillar = Matrix4.Identity();
        pillar.SetTranslation(new Vector3(0, 0.4, 0));
        pillar.ScaleByDouble(0.8, 2.4, 0.8, 1);
        Assert.All(Enumerable.Range(0, 16), i => Assert.Equal(pillar[i], objects[0].Transform[i], 1e-9));
        Assert.Equal(Tint.Hex(0xF2F4F7).Linear, objects[0].Colour);

        // The second of the first ring is a sixth of the way round, and turned to face along it.
        var expected = Matrix4.Identity();
        expected.SetTranslation(new Vector3(Math.Cos(Math.PI / 3) * 2.4, -0.6, Math.Sin(Math.PI / 3) * 2.4));
        expected.RotateY(Math.PI / 3);
        expected.ScaleByDouble(0.5, 0.5, 0.5, 1);
        Assert.All(Enumerable.Range(0, 16), i => Assert.Equal(expected[i], objects[2].Transform[i], 1e-9));
        Assert.Equal(Tint.Hex(0x5FA8D3).Linear, objects[2].Colour);
    }

    [Fact]
    public void ChangingTheRingsLaysTheWorldOutAgain()
    {
        using var example = new SpawningExample();

        Rings(example).Change(1);
        Assert.Equal(1 + 6 + 1, Scene(example, 0).Objects.Count);
        Assert.Equal(1, Rings(example).Value);

        // Half a ring rounds up, as the slider shows it.
        Rings(example).Change(2.5);
        Assert.Equal(3, Rings(example).Value);
        Assert.Equal(1 + 6 + 10 + 14 + 1, Scene(example, 0).Objects.Count);
    }

    [Fact]
    public void TheRingsSpinAndBobUntilTheyAreToldNotTo()
    {
        using var moving = new SpawningExample();
        var first = Ring(moving, 0);
        Assert.NotEqual(first.X, Ring(moving, 1).X);
        Assert.NotEqual(-0.6, Ring(moving, 1).Y);

        using var still = new SpawningExample();
        OverlayTests.Control<Setting>(still, "Spin").Change(0);
        OverlayTests.Control<Toggle>(still, "Bob").Change(false);
        Assert.Equal(new Vector3(2.4, -0.6, 0), Ring(still, 0));
        Assert.Equal(new Vector3(2.4, -0.6, 0), Ring(still, 1));
        Assert.Equal(0, OverlayTests.Control<Setting>(still, "Spin").Value);
        Assert.False(OverlayTests.Control<Toggle>(still, "Bob").Value);
    }

    [Fact]
    public void DropPutsOneAboveTheMiddleAndClearTakesThemAway()
    {
        using var example = new SpawningExample();
        var drop = OverlayTests.Control<Buttons>(example, "Drop one");

        drop.Press("Drop");
        drop.Press("Drop");
        var objects = Scene(example, 0).Objects;
        Assert.Equal(49 + 2 + 1, objects.Count);
        Assert.Equal(new Vector3(0.7, 2.5, 0), objects[49].Transform.GetTranslation());

        drop.Press("Clear");
        Assert.Equal(49 + 1, Scene(example, 0).Objects.Count);
    }

    [Fact]
    public void AMeshTheScriptNamesIsTheOneDrawn()
    {
        using var example = new SpawningExample();
        example.Host.Eval("require(\"orblit/scene\").spawn({ id: \"rock\", at: [0, 0, 0], size: [1, 1, 1], colour: \"#FFFFFF\", mesh: \"rock.glb\" });");

        var objects = Scene(example, 0).Objects;
        Assert.Equal("rock.glb", objects[^2].Mesh);
        Assert.All(objects.SkipLast(2), one => Assert.Null(one.Mesh));
    }

    [Fact]
    public void AWorldScriptThatStopsLeavesTheGroundAndSaysWhy()
    {
        using var example = new SpawningExample();
        example.Host.Eval("__orblit_scene.frame = () => { throw new Error('the step broke'); };");

        Assert.Equal([790L], Scene(example, 0).Objects.Select(one => one.Key));
        Assert.StartsWith("The world script stopped: ", example.Note, StringComparison.Ordinal);
        Assert.Contains("the step broke", example.Note, StringComparison.Ordinal);
    }

    /// <summary>
    /// The list is made wherever the gallery starts and drawn on the UI thread. A script engine made
    /// with the example would be called from the second thread, and say its stack is full.
    /// </summary>
    [Fact]
    public async Task MadeOnOneThreadAndDrawnOnAnother()
    {
        var example = new SpawningExample();
        await Headless.Run(() =>
        {
            using (example)
            {
                Assert.Equal(49 + 1, Scene(example, 0).Objects.Count);
            }
        });
    }

    private static RenderScene Scene(Example example, double seconds) => example.Scene(example.ViewPoint.ToRenderCamera(), seconds);

    /// <summary>Where the first of the first ring is, <paramref name="seconds"/> in.</summary>
    private static Vector3 Ring(Example example, double seconds) => Scene(example, seconds).Objects[1].Transform.GetTranslation();

    private static Setting Rings(Example example) => OverlayTests.Control<Setting>(example, "Rings");
}
