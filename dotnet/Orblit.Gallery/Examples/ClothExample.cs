using Orblit.Examples;
using Orblit.Filament;
using Orblit.Light;
using Orblit.Physics.Soft;
using Orblit.Stage;
using Orblit.VectorMath;

namespace Orblit.Gallery;

/// <summary>
/// A flag on a pole, moved by a position-based cloth and drawn as a skinned sheet.
/// </summary>
/// <remarks>
/// The renderer takes new joint poses every frame and no new vertices, so the sheet has a joint for
/// each particle. Each frame the solver moves the particles, and the sheet's joints are set to where
/// they are.
/// </remarks>
public sealed class ClothExample : Example
{
    private const int Columns = 20;
    private const int Rows = 12;
    private const double Spacing = 0.08;
    private const double StepLength = 1.0 / 60;

    /// <summary>
    /// The renderer's name for the sheet. The bytes behind a name must not change, so a change to the
    /// sheet is a change to the version here.
    /// </summary>
    private static readonly string Resource = RenderResources.NameFor("cloth/v1/flag.glb");

    /// <summary>Where the top of the flag's pole edge is.</summary>
    private static readonly Vector3 Corner = new(0, 3.4, 0);

    private readonly Action<string, byte[]> _provide;
    private readonly Gusts _air = new();
    private readonly ParticleSheet _sheet = new(Columns, Rows, Spacing, [0.62, 0.06, 0.05]);
    private readonly SoftBody _flag;
    private bool _requested;
    private bool _ready;
    private double _clock;

    /// <param name="provide">Hands bytes to the renderer under a name, as <see cref="RenderResources.Provide"/> does.</param>
    public ClothExample(Action<string, byte[]> provide)
    {
        _provide = provide;
        _flag = Hung(_air);
    }

    public override string Name => "Cloth";

    public override string Blurb => "A flag on a pole. The wind gusts and the cloth answers.";

    public override ExampleSection Section => ExampleSection.Effects;

    public override ViewPoint ViewPoint => new() { Yaw = 0, Distance = 9, Pitch = 0.1, Height = 2.6 };

    public override RenderScene Scene(RenderCamera camera, double seconds)
    {
        Provide();
        Advance(seconds);
        return new RenderScene
        {
            Objects = _ready ? [Pole, Ground, Flag()] : [Pole, Ground],
            Lights =
            [
                new RenderLight
                {
                    Key = 10,
                    Kind = RenderLightKind.Directional,
                    Intensity = 76000,
                    Direction = new Vector3(-0.4, -0.7, -0.6).Normalized(),
                    Colour = Tint.Hex(0xFFF3E0).Linear,
                    SunAngularRadius = 1.2,
                },
            ],
            Sky = new Sky
            {
                Colour = Tint.Hex(0x6E8DB4).Linear,
                Zenith = Tint.Hex(0x2F5F97).Linear,
                Horizon = Tint.Hex(0xB9CBDD).Linear,
                Ambient = 22000,
                BodyDirection = new Vector3(0.4, 0.7, 0.6).Normalized(),
                BodyColour = Tint.Hex(0xFFF6E8).Linear,
                BodySize = 0.011,
                Quality = SkyQuality.Fair,
            },
            Camera = camera,
        };
    }

    public override IReadOnlyList<ExampleSetting> Settings() =>
    [
        new Setting("Wind", _air.Strength, 0, 14, value => _air.Strength = value) { Decimals = 1, Unit = " m/s" },
    ];

    public override string Code => """
        // The solver moves particles. The renderer takes joints, not vertices, so the
        // sheet has a vertex and a joint for every particle.
        var sheet = new ParticleSheet(columns: 20, rows: 12, spacing: 0.08);
        RenderResources.Provide(name, sheet.ToGlb());

        var flag = SoftBody.Cloth(
            columns: 20, rows: 12, origin: corner,
            across: new Vector3(0.08, 0, 0), down: new Vector3(0, -0.08, 0),
            air: gusts); // an ISoftAir: the wind at a point
        for (var row = 0; row < 12; row++)
        {
            flag.Pin(row * 20); // the pole holds the first column
        }

        // Every frame: step the cloth, then set each joint to its particle.
        flag.Step(1.0 / 60);
        new RenderObject
        {
            Key = 3, Mesh = name, Transform = Matrix4.TranslationValues(0, 3.4, 0),
            Colour = Vector3.All(1),
            Joints = sheet.Poses(i => flag.PositionOf(i) - corner),
        };
        """;

    // The cube is two metres wide, so a scale is a half-size.
    private static RenderObject Pole => new()
    {
        Key = 1,
        Transform = Placed(new Vector3(-0.05, 1.9, 0), new Vector3(0.04, 1.9, 0.04)),
        Colour = Tint.Hex(0xB9BEC6).Linear,
    };

    private static RenderObject Ground => new()
    {
        Key = 2,
        Transform = Placed(new Vector3(0, -0.03, 0), new Vector3(14, 0.03, 14)),
        Colour = Tint.Hex(0x39404A).Linear,
        CastShadows = false,
    };

    private RenderObject Flag() => new()
    {
        Key = 3,
        Mesh = Resource,
        Transform = Matrix4.TranslationValues(Corner.X, Corner.Y, Corner.Z),
        Colour = Vector3.All(1),
        Joints = _sheet.Poses(i => _flag.PositionOf(i) - Corner),
    };

    private static SoftBody Hung(ISoftAir air)
    {
        var cloth = SoftBody.Cloth(
            Columns,
            Rows,
            Corner,
            new Vector3(Spacing, 0, 0),
            new Vector3(0, -Spacing, 0),
            // A flag lifts when the pull of the wind beats its weight, which the default drag does
            // not reach at a breeze.
            spec: new SoftSpec(windDrag: 3),
            air: air);
        // The pole holds the whole first column.
        for (var row = 0; row < Rows; row++)
        {
            cloth.Pin(row * Columns);
        }

        return cloth;
    }

    private static Matrix4 Placed(Vector3 at, Vector3 halfSize)
    {
        var transform = Matrix4.Identity();
        transform.SetTranslation(at);
        transform.ScaleByDouble(halfSize.X, halfSize.Y, halfSize.Z, 1);
        return transform;
    }

    /// <summary>Hands the sheet to the renderer, once.</summary>
    private void Provide()
    {
        if (_requested)
        {
            return;
        }

        _requested = true;
        try
        {
            _provide(Resource, _sheet.ToGlb());
            _ready = true;
        }
        catch (Exception error) when (error is RendererException or DllNotFoundException)
        {
            Note = $"The flag could not be handed to the renderer: {error.Message}";
        }
    }

    /// <summary>
    /// Steps the cloth to <paramref name="seconds"/> in fixed steps. A jump in the clock, such as the
    /// example opening again, takes no steps rather than a hundred.
    /// </summary>
    private void Advance(double seconds)
    {
        var owed = (int)Math.Floor((seconds - _clock) / StepLength);
        if (owed is < 0 or > 6)
        {
            _clock = seconds;
            return;
        }

        for (var n = 0; n < owed; n++)
        {
            _air.Seconds = _clock;
            _flag.Step(StepLength);
            _clock += StepLength;
        }
    }

    /// <summary>
    /// A wind that blows along the flag and rocks across it, in gusts. A function of the time it is
    /// told, so the same seconds always blow the same wind and the cloth can be stepped as often as
    /// the clock says.
    /// </summary>
    private sealed class Gusts : ISoftAir
    {
        /// <summary>In metres a second, along the flag.</summary>
        public double Strength { get; set; } = 7;

        public double Seconds { get; set; }

        public Vector3 WindAt(Vector3 point)
        {
            var gust = 1 + (0.45 * Math.Sin(Seconds * 1.3)) + (0.25 * Math.Sin((Seconds * 3.1) + 1));
            var rock = 0.4 * Math.Sin(Seconds * 2.2);
            return new Vector3(gust, 0, rock) * Strength;
        }
    }
}
