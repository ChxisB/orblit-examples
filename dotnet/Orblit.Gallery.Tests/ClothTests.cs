using Orblit.Examples;
using Orblit.Filament;
using Orblit.Stage;
using Orblit.VectorMath;

namespace Orblit.Gallery.Tests;

/// <summary>
/// The flag on its pole, read from the scene it hands the renderer. The joints are where the
/// particles are, less the corner the sheet is drawn from.
/// </summary>
public sealed class ClothTests
{
    private const int Columns = 20;
    private const int Rows = 12;
    private const double Spacing = 0.08;
    private const double Step = 1 / 60.0;

    [Fact]
    public void ThePoleHoldsTheFirstColumnWhileTheRestIsBlownOut()
    {
        var cloth = new ClothExample((_, _) => { });
        var hung = Particles(cloth, 0);
        var blown = Particles(Run(cloth, 2), 2);

        for (var row = 0; row < Rows; row++)
        {
            Assert.True(blown[row * Columns].DistanceTo(new Vector3(0, -Spacing * row, 0)) < 1e-12, $"Row {row} left the pole.");
        }

        var tip = Columns - 1;
        Assert.Equal(new Vector3(Spacing * tip, 0, 0), hung[tip]);
        Assert.True(blown[tip].DistanceTo(hung[tip]) > 0.1, $"The far corner is still at {blown[tip]}.");
    }

    /// <summary>
    /// A sheet held along one edge stays in its own plane until something pushes across it. Only the
    /// gusts do, and they rock with the time the air is told.
    /// </summary>
    [Fact]
    public void TheGustsFlapTheFlagAndWithoutWindItHangsFlat()
    {
        var windy = Run(new ClothExample((_, _) => { }), 3);
        var calm = new ClothExample((_, _) => { });
        Wind(calm).Change(0);
        Run(calm, 3);

        Assert.Equal(7, Wind(windy).Value);
        Assert.Equal(0, Wind(calm).Value);
        Assert.Contains(Particles(windy, 3), at => Math.Abs(at.Z) > 0.01);
        Assert.All(Particles(calm, 3), at => Assert.True(Math.Abs(at.Z) < 1e-9, $"A still flag moved across to {at}."));
    }

    [Fact]
    public void SixStepsAreCaughtUpAndAnyMoreOrABackwardJumpTakeNone()
    {
        var still = Particles(new ClothExample((_, _) => { }), 0);
        Assert.NotEqual(still, After(6.5 * Step));
        Assert.Equal(still, After(7.5 * Step));

        var opened = new ClothExample((_, _) => { });
        Run(opened, 1);
        var left = Particles(opened, 1);
        Assert.Equal(left, Particles(opened, 0));
        Assert.NotEqual(left, Particles(opened, 1.5 * Step));

        static List<Vector3> After(double seconds)
        {
            var cloth = new ClothExample((_, _) => { });
            cloth.Scene(cloth.ViewPoint.ToRenderCamera(), 0);
            return Particles(cloth, seconds);
        }
    }

    /// <summary>
    /// The cloth steps on a fixed clock, so a slow display shows the same flag a fast one does. Half a
    /// step past the second is where both have owed the same sixty steps.
    /// </summary>
    [Fact]
    public void TheFlagIsTheSameHoweverTheFramesFall()
    {
        var (smooth, slow) = (new ClothExample((_, _) => { }), new ClothExample((_, _) => { }));
        for (var frame = 0; frame <= 60; frame++)
        {
            smooth.Scene(smooth.ViewPoint.ToRenderCamera(), frame * Step);
        }

        for (var frame = 0; frame <= 60; frame += 3)
        {
            slow.Scene(slow.ViewPoint.ToRenderCamera(), frame * Step);
        }

        Assert.Equal(Particles(smooth, 60.5 * Step), Particles(slow, 60.5 * Step));
    }

    [Fact]
    public void TheSheetIsHandedToTheRendererOnceUnderItsVersionedName()
    {
        var handed = new List<(string Name, byte[] Bytes)>();
        var cloth = new ClothExample((name, bytes) => handed.Add((name, bytes)));

        var flag = Flag(cloth.Scene(cloth.ViewPoint.ToRenderCamera(), 0));
        cloth.Scene(cloth.ViewPoint.ToRenderCamera(), Step);

        var (name, bytes) = Assert.Single(handed);
        Assert.Equal("orblit:resource/cloth/v1/flag.glb", name);
        Assert.Equal(new ParticleSheet(Columns, Rows, Spacing, [0.62, 0.06, 0.05]).ToGlb(), bytes);
        Assert.Equal(name, flag.Mesh);
        Assert.Equal(new Vector3(0, 3.4, 0), flag.Transform.GetTranslation());
        Assert.Equal(Columns * Rows, flag.Joints?.Count);
        Assert.Null(cloth.Note);
    }

    [Fact]
    public void WithoutTheRendererThereIsNoFlagAndANoteSaysWhy()
    {
        var tries = 0;
        var cloth = new ClothExample((_, _) =>
        {
            tries++;
            throw new DllNotFoundException("There is no renderer.");
        });

        var scene = cloth.Scene(cloth.ViewPoint.ToRenderCamera(), 0);
        cloth.Scene(cloth.ViewPoint.ToRenderCamera(), Step);

        Assert.Equal(1, tries);
        Assert.Equal([1L, 2L], scene.Objects.Select(one => one.Key));
        Assert.Equal("The flag could not be handed to the renderer: There is no renderer.", cloth.Note);
    }

    /// <summary>
    /// The gallery's cloth puts its sheet in the table every renderer in the process reads. Letting it
    /// go is the one way to ask whether it is there.
    /// </summary>
    [RendererFact]
    public void TheGalleryListsTheFlagUnderEffectsAndHandsItToTheRenderer()
    {
        var cloth = Assert.Single(Gallery.Examples().OfType<ClothExample>());
        Assert.Equal(ExampleSection.Effects, cloth.Section);

        Flag(cloth.Scene(cloth.ViewPoint.ToRenderCamera(), 0));

        Assert.True(RenderResources.Release("orblit:resource/cloth/v1/flag.glb"));
    }

    /// <summary>Frames a sixtieth of a second apart, from the start to <paramref name="seconds"/>.</summary>
    private static ClothExample Run(ClothExample cloth, double seconds)
    {
        for (var frame = 0; frame * Step <= seconds; frame++)
        {
            cloth.Scene(cloth.ViewPoint.ToRenderCamera(), frame * Step);
        }

        return cloth;
    }

    private static List<Vector3> Particles(Example cloth, double seconds) =>
        [.. Flag(cloth.Scene(cloth.ViewPoint.ToRenderCamera(), seconds)).Joints!.Select(pose => pose.Transform.GetTranslation())]; // the flag always has joints

    private static RenderObject Flag(RenderScene scene) => scene.Objects.Single(one => one.Key == 3);

    private static Setting Wind(Example cloth) => OverlayTests.Control<Setting>(cloth, "Wind");
}
