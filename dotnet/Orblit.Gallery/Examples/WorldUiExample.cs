using Avalonia;
using Avalonia.Media;
using Orblit.Examples;
using Orblit.Filament;
using Orblit.Light;
using Orblit.Stage.Avalonia;
using Orblit.Ui.Avalonia;
using Orblit.VectorMath;

namespace Orblit.Gallery;

/// <summary>
/// An interface that belongs to a place in the scene: a screen on a wall that takes a click and a drag,
/// and two hundred names over a crowd. <see cref="WorldUiOverlay"/> opens the window the screen is drawn
/// in and hands it to <see cref="Mount"/>.
/// </summary>
/// <remarks>
/// The screen is a tree of Avalonia controls drawn into a picture and shown on a quad, and a click on the
/// quad is a click in the tree. Each distinct name is drawn once into one picture, and a label is a quad
/// that shows its rectangle of it. Before <see cref="Mount"/> there is no window to draw in, so the scene
/// is the room and the crowd alone.
/// </remarks>
public sealed class WorldUiExample : Example, IDisposable
{
    private const int Crowd = 200;
    private const int Columns = 20;
    private const int WallObject = 1;
    private const int FloorObject = 2;
    private const int ScreenKey = 3;
    private const int Figures = 1000;

    private static readonly string[] Given =
    [
        "Ada", "Bram", "Cleo", "Dov", "Edda", "Finn", "Gia", "Hugo", "Ines", "Jonas",
        "Kira", "Leo", "Mina", "Nils", "Orla", "Pavel", "Quinn", "Rosa", "Sven", "Tilda",
    ];

    private static readonly int[] Palette = [0xD9634F, 0xE5B84F, 0x5FA8D3, 0x6FBF73, 0xE8E4DC];

    private static readonly NameplateBatch NoNames = new([], []);

    private readonly Action<string, byte[]> _provide;
    private readonly Func<string, bool> _release;
    private Nameplates? _plates;

    /// <param name="provide">Hands bytes to the renderer under a name, as <see cref="RenderResources.Provide"/> does.</param>
    /// <param name="release">Lets go of the bytes under a name, as <see cref="RenderResources.Release"/> does.</param>
    public WorldUiExample(Action<string, byte[]> provide, Func<string, bool> release)
    {
        _provide = provide;
        _release = release;
    }

    public override string Name => "Interface in the world";

    public override string Blurb => "A screen on a wall that takes a click and a drag, and two hundred names that face the camera.";

    public override ExampleSection Section => ExampleSection.Content;

    public override ViewPoint ViewPoint => new() { Yaw = 0, Pitch = 0.3, Distance = 9, Height = 2 };

    /// <summary>How big the screen is in pixels, which its controls lay out against.</summary>
    internal static Size Screen { get; } = new(800, 450);

    /// <summary>How many times the button on the wall was pressed.</summary>
    internal int Presses { get; set; }

    /// <summary>The slider on the wall. It sets how bright the sun is.</summary>
    internal double Level { get; set; } = 0.5;

    /// <summary>How many names float over the crowd, up to the crowd's size.</summary>
    internal int Labels { get; set; } = Crowd;

    /// <summary>
    /// Whether every name is its own text. Off, twenty names are shared by the crowd and the picture
    /// holds twenty rectangles. On, it holds two hundred.
    /// </summary>
    internal bool Distinct { get; set; }

    /// <summary>The screen on the wall, or null until it is mounted.</summary>
    internal WorldPanel? Wall { get; private set; }

    public override RenderScene Scene(RenderCamera camera, double seconds)
    {
        var stands = Enumerable.Range(0, Crowd).Select(i => Stand(i, seconds)).ToList();
        var names = _plates?.Build(camera, Enumerable.Range(0, Labels).Select(i => new Nameplate(i, Text(i), stands[i] + new Vector3(0, 1.3, 0)))) ?? NoNames;
        RenderObject[] screen = Wall is { } wall ? [wall.Quad] : [];
        RenderMaterial[] screenMaterial = Wall is { } shown ? [shown.Material] : [];
        return new RenderScene
        {
            Objects =
            [
                Box(FloorObject, new Vector3(0, -0.05, 2), new Vector3(14, 0.05, 8), 0x3B424C),
                Box(WallObject, new Vector3(0, 2.25, -2.6), new Vector3(5, 2.25, 0.1), 0xB8B2A7),
                .. screen,
                .. stands.Select((at, i) => Box(Figures + i, at + new Vector3(0, 0.45, 0), new Vector3(0.2, 0.45, 0.2), Palette[i % Palette.Length])),
                .. names.Objects,
            ],
            Materials = [.. screenMaterial, .. names.Materials],
            Lights =
            [
                new RenderLight
                {
                    Key = 10,
                    Kind = RenderLightKind.Directional,
                    Intensity = 20000 + (100000 * Level),
                    Colour = Tint.Hex(0xFFF3E0).Linear,
                    Direction = new Vector3(-0.3, -1, -0.6).Normalized(),
                },
            ],
            Sky = new Sky { Colour = Tint.Hex(0x1B222C).Linear, Ambient = 9000 },
            Camera = camera,
        };
    }

    public override IReadOnlyList<ExampleSetting> Settings() =>
    [
        new Setting("Names", Labels, 0, Crowd, value => Labels = (int)Math.Round(value, MidpointRounding.AwayFromZero)) { Decimals = 0 },
        new Toggle("Distinct text", Distinct, value => Distinct = value) { Note = "Off: twenty rectangles. On: every name has its own." },
    ];

    public override string Code => """
        // A screen is a tree of controls, drawn in a window nobody sees.
        using var host = new PanelHost();
        var panel = host.Open(face, new Size(800, 450));
        var wall = new WorldPanel(
            panel,
            // The cube's front face, squashed thin.
            onWall,
            new WorldPanelOptions { Id = "booth", ObjectKey = 3, MaterialKey = 3 });

        // Draw again after a change, then put it in the scene.
        wall.Sync();
        var scene = new RenderScene { Objects = [wall.Quad], Materials = [wall.Material], Camera = camera };

        // A click is a ray from the camera. It reaches the controls only if it lands on the face.
        var ray = PointerRay.FromCamera(camera, point, view);
        wall.Down(ray);
        wall.Move(ray);
        wall.Up(ray);
        // Or wall.Cancel() when the system takes the touch back.

        // Two hundred names share one picture.
        var plates = new Nameplates(new LabelAtlas(), new NameplatesOptions { FadeStart = 6, FadeEnd = 14 });
        plates.Sync(texts);
        var named = plates.Build(camera, [new Nameplate(1, "Ada", position)]);

        // The renderer loads a name once, so each new picture has a new name, and it
        // keeps the texture of every one until it is disposed. That suits a screen
        // somebody presses. It does not suit a screen that changes every frame.
        """;

    /// <summary>
    /// Stands <paramref name="face"/> on the wall and hangs the names, once there is a window to draw them
    /// in. Whatever was mounted before is taken down.
    /// </summary>
    internal void Mount(IPanel face)
    {
        Unmount();
        var style = new LabelAtlasOptions { Typeface = new Typeface(FontFamily.Default, FontStyle.Normal, FontWeight.SemiBold) };
        Wall = new WorldPanel(face, OnWall(), new WorldPanelOptions { Id = "world_ui", ObjectKey = ScreenKey, MaterialKey = ScreenKey, Provide = _provide, Release = _release });
        _plates = new Nameplates(new LabelAtlas(style), new NameplatesOptions { FadeStart = 6, FadeEnd = 14, Provide = _provide, Release = _release });
    }

    /// <summary>Lets go of the pictures. The gallery keeps its examples as long as it runs, so only a test calls this.</summary>
    public void Dispose() => Unmount();

    /// <summary>Lets go of the pictures. The panel stays with whoever mounted it.</summary>
    internal void Unmount()
    {
        Wall?.Dispose();
        _plates?.Dispose();
        Wall = null;
        _plates = null;
    }

    /// <summary>Draws the screen and the names if they changed. The scene names the new pictures from its next frame.</summary>
    internal void Sync()
    {
        Wall?.Sync();
        _plates?.Sync(Enumerable.Range(0, Labels).Select(Text));
    }

    // The screen's place on the wall: the cube's front face, six metres by three and a bit and a hair thick.
    private static Matrix4 OnWall()
    {
        var transform = Matrix4.Identity();
        transform.SetTranslation(new Vector3(0, 2.25, -2.49));
        transform.ScaleByDouble(3, 1.6875, 0.005, 1);
        return transform;
    }

    private static RenderObject Box(int key, Vector3 at, Vector3 half, int colour)
    {
        var transform = Matrix4.Identity();
        transform.SetTranslation(at);
        transform.ScaleByDouble(half.X, half.Y, half.Z, 1);
        return new RenderObject { Key = key, Transform = transform, Colour = Tint.Hex(colour).Linear };
    }

    // Where the figure at i stands. It bobs a little, so the names over it move.
    private static Vector3 Stand(int i, double seconds) =>
        new(((i % Columns) - 9.5) * 0.8, 0.06 * Math.Sin((seconds * 2) + i), (i / Columns) * 0.55);

    private string Text(int i)
    {
        var name = Given[i % Columns];
        return Distinct ? $"{name} {(i / Columns) + 1}" : name;
    }
}
