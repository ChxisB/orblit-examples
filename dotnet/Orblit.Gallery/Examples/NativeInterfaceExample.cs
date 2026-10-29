using System.Globalization;
using Orblit.Examples;
using Orblit.Filament;
using Orblit.Light;
using Orblit.Ui;
using Orblit.VectorMath;

namespace Orblit.Gallery;

/// <summary>
/// The same kind of interface, described in C#. There is no script here and no engine. The
/// description is built directly.
/// </summary>
/// <remarks>
/// <para>
/// Worth having as well as the TypeScript one, for two reasons. A game written entirely in C# should
/// not have to embed a script engine to draw a heads-up display. The vocabulary and the builder are
/// the useful part, and neither needs a script.
/// </para>
/// <para>
/// And it shows where the boundary is. Everything below <see cref="Description"/> is the same in both
/// examples, because the shape a script sends and the shape C# builds are one shape. That is why a
/// script can send one at all.
/// </para>
/// </remarks>
public sealed class NativeInterfaceExample : Example
{
    public override string Name => "An interface in C#";

    public override string Blurb => "The same description, built directly, with no script in the way.";

    public override ExampleSection Section => ExampleSection.Scripting;

    public override ViewPoint ViewPoint => new() { Distance = 12, Pitch = 0.22 };

    internal double Health { get; set; } = 0.72;

    internal int Shields { get; set; } = 3;

    internal string Accent { get; set; } = "steel";

    /// <summary>
    /// Written out rather than sent. Every node is one the TypeScript example's script produces as
    /// JSON, with the same element names and the same class lists.
    /// </summary>
    internal UiNode Description => new()
    {
        Type = "stack",
        Classes = "full",
        Children =
        [
            new UiNode
            {
                Type = "column",
                Classes = "p-5 gap-3 items-start top-0 left-0",
                Children = [Chip, Readout],
            },
        ],
    };

    public override RenderScene Scene(RenderCamera camera, double seconds) => new()
    {
        Objects = [.. Enumerable.Range(0, 4).Select(i => Orbiting(i, seconds)), Ground],
        Lights =
        [
            new RenderLight
            {
                Key = 860,
                Kind = RenderLightKind.Directional,
                Intensity = 74000,
                Direction = new Vector3(-0.4, -1, -0.45).Normalized(),
                Colour = Tint.Hex(0xFFF3E0).Linear,
                SunAngularRadius = 1.5,
            },
        ],
        Sky = new Sky { Colour = Tint.Hex(0x161C25).Linear, Ambient = 11000 },
        Camera = camera,
    };

    public override IReadOnlyList<ExampleSetting> Settings() =>
    [
        new Setting("Power", Health, 0, 1, value => Health = value),
        new Setting("Shields", Shields, 0, 5, value => Shields = (int)Math.Round(value, MidpointRounding.AwayFromZero)) { Decimals = 0 },
        new Choice("Accent", ["ember", "steel", "moss", "rose"], Accent, option => Accent = option),
    ];

    public override string Code => """
        // The same description, written rather than sent. No engine, no script.

        UiNode Description => new()
        {
            Type = "column",
            Classes = "gap-2 px-4 py-3 rounded-lg bg-slate-900 border border-slate-700",
            Children =
            [
                new UiNode { Type = "text", Classes = "text-xs uppercase text-slate-400", Text = "Power" },
                // A bar is two boxes: the track, and as much of it as is left.
                new UiNode
                {
                    Type = "box",
                    Classes = "w-full h-2 rounded-full bg-slate-700 clip",
                    Children =
                    [
                        new UiNode
                        {
                            Type = "box",
                            Classes = $"h-2 rounded-full bg-{Accent}-500",
                            Css = $"width: {Math.Round(Health * 224)}px",
                        },
                    ],
                },
            ],
        };

        // And then, exactly as in the TypeScript example:
        new UiBuilder(new UiBuilderOptions()).Build(Description);
        """;

    private UiNode Chip => new()
    {
        Type = "row",
        Classes = "gap-3 items-center px-4 py-3 rounded-lg bg-slate-900 border border-slate-700 shadow",
        Css = "opacity: 0.94",
        Children =
        [
            new UiNode { Type = "box", Classes = $"w-3 h-3 rounded-full bg-{Accent}-500" },
            new UiNode { Type = "text", Classes = "text-lg font-semibold text-slate-100", Text = "Docking bay" },
            new UiNode { Type = "text", Classes = "text-sm text-slate-400", Text = "· clear" },
        ],
    };

    private UiNode Readout => new()
    {
        Type = "column",
        Classes = "gap-2 px-4 py-3 rounded-lg bg-slate-900 border border-slate-700 w-64",
        Css = "opacity: 0.94",
        Children =
        [
            new UiNode
            {
                Type = "row",
                Classes = "justify-between items-center",
                Children =
                [
                    new UiNode { Type = "text", Classes = "text-xs uppercase text-slate-400", Text = "Power" },
                    new UiNode { Type = "text", Classes = "text-xs text-slate-300", Text = $"{Whole(Health * 100)}%" },
                ],
            },
            // A bar is two boxes: the track, and as much of it as is left. No progress control, and
            // no second component set.
            new UiNode
            {
                Type = "box",
                Classes = "w-full h-2 rounded-full bg-slate-700 clip",
                Children = [new UiNode { Type = "box", Classes = $"h-2 rounded-full bg-{Accent}-500", Css = $"width: {Whole(Health * 224)}px" }],
            },
            // A row of pips, which in C# is a query and in TypeScript is a map. The description does
            // not know or care.
            new UiNode
            {
                Type = "row",
                Classes = "gap-1 pt-2",
                Children = [.. Enumerable.Range(0, 5).Select(i => new UiNode { Type = "box", Classes = $"w-6 h-1 rounded-full {(i < Shields ? $"bg-{Accent}-400" : "bg-slate-700")}" })],
            },
        ],
    };

    private static RenderObject Ground
    {
        get
        {
            var transform = Matrix4.Identity();
            transform.SetTranslation(new Vector3(0, -1.7, 0));
            transform.ScaleByDouble(10, 0.06, 10, 1);
            return new RenderObject { Key = 840, Transform = transform, Colour = Tint.Hex(0x39404A).Linear, CastShadows = false };
        }
    }

    private static RenderObject Orbiting(int i, double seconds)
    {
        var transform = Matrix4.Identity();
        transform.SetTranslation(new Vector3(
            Math.Cos((seconds * 0.3) + (i * 1.6)) * 2.6,
            -0.4 + (Math.Sin((seconds * 0.5) + i) * 0.35),
            Math.Sin((seconds * 0.3) + (i * 1.6)) * 2.6));
        transform.RotateY((seconds * 0.4) + i);
        transform.ScaleByDouble(0.6, 0.6, 0.6, 1);
        return new RenderObject { Key = 850 + i, Transform = transform, Colour = Tint.Hex(i % 2 == 0 ? 0x5FA8D3 : 0x7FB069).Linear };
    }

    private static string Whole(double value) => Math.Round(value, MidpointRounding.AwayFromZero).ToString(CultureInfo.InvariantCulture);
}
