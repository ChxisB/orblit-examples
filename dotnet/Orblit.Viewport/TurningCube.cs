using Orblit.Filament;
using Orblit.VectorMath;

namespace Orblit.Viewport;

/// <summary>What the viewport draws: a cube turning over a floor, under one light.</summary>
internal static class TurningCube
{
    private static readonly RenderCamera Camera = new()
    {
        Position = new Vector3(4.3, 2.7, 5.1),
        Target = new Vector3(0, -0.2, 0),
    };

    /// <summary>The scene <paramref name="seconds"/> after the window opened.</summary>
    public static RenderScene At(double seconds)
    {
        var turn = seconds * 0.35;
        var spun = Matrix4.Identity();
        spun.RotateY(turn);
        spun.RotateX(turn * 0.42);

        // A floor, so the light has somewhere to land and the shadow has somewhere to fall.
        var floor = Matrix4.Identity();
        floor.SetTranslation(new Vector3(0, -1.6, 0));
        floor.ScaleByDouble(6, 0.06, 6, 1);

        // Colours are linear, as the renderer takes them.
        return new RenderScene
        {
            Objects =
            [
                new RenderObject { Key = 1, Transform = spun, Colour = new Vector3(0.69, 0.12, 0.08) },
                new RenderObject { Key = 2, Transform = floor, Colour = new Vector3(0.044, 0.054, 0.073), CastShadows = false },
            ],
            Lights =
            [
                new RenderLight
                {
                    Key = 10,
                    Kind = RenderLightKind.Directional,
                    Intensity = 82000,
                    Colour = new Vector3(1, 0.9, 0.75),
                    Direction = new Vector3(-0.4, -1, -0.55).Normalized(),
                },
            ],
            Sky = new Sky { Colour = new Vector3(0.01, 0.016, 0.025), Ambient = 9000 },
            Camera = Camera,
        };
    }
}
