using System.Globalization;
using Orblit.Examples;
using Orblit.Filament;
using Orblit.Light;
using Orblit.Script;
using Orblit.Script.Scene;
using Orblit.VectorMath;

namespace Orblit.Gallery;

/// <summary>
/// A world put together in TypeScript. Every object here is spawned by <c>script/world.tsx</c>, and
/// nothing in this file decides where anything is. The engine asks the script for a frame, the
/// script's own step runs, and what comes back describes the whole world.
/// </summary>
/// <remarks>
/// A description rather than a stream of commands, for the same reason the interface is one. A
/// message that says everything cannot go stale, and a script reloaded halfway through cannot leave
/// the world half built.
/// </remarks>
public sealed class SpawningExample : Example, IDisposable
{
    private ScriptHost? _host;

    public override string Name => "Spawning from script";

    public override string Blurb => "Every object put there by TypeScript, and moved by it every frame.";

    public override ExampleSection Section => ExampleSection.Scripting;

    public override ViewPoint ViewPoint => new() { Distance = 26, Pitch = 0.42, Height = 1 };

    /// <summary>
    /// The engine running <c>script/world.tsx</c>. Made at first use rather than with the example,
    /// because QuickJS takes its stack limit from the thread that made it. The list of examples can be
    /// made on one thread and drawn on another, and a host called from the second says its stack is full.
    /// </summary>
    internal ScriptHost Host
    {
        get
        {
            if (_host is null)
            {
                _host = new ScriptHost();
                // The scene runtime: spawn, destroy, and the __orblit_scene object a frame is asked of.
                _host.Eval(SceneRuntime.Source, "orblit/scene.js");
                // Kept under a name, so that its settings can be reached.
                _host.Eval(ScriptModule.Around(CompiledScripts.World, "world"), "script/world.tsx");
            }

            return _host;
        }
    }

    public void Dispose() => _host?.Dispose();

    public override RenderScene Scene(RenderCamera camera, double seconds) => new()
    {
        Objects = [.. World(seconds).Select(Drawn), Ground],
        Lights =
        [
            new RenderLight
            {
                Key = 810,
                Kind = RenderLightKind.Directional,
                Intensity = 76000,
                Direction = new Vector3(-0.4, -0.92, -0.35).Normalized(),
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
            BodyDirection = new Vector3(0.4, 0.92, 0.35).Normalized(),
            BodyColour = Tint.Hex(0xFFF6E8).Linear,
            BodySize = 0.011,
            Quality = SkyQuality.Fair,
            Clouds = Clouds.Cumulus with { Cover = 0.3, Wind = new Vector2(4, 1.5) },
        },
        Camera = camera,
    };

    public override IReadOnlyList<ExampleSetting> Settings() =>
    [
        new Setting("Rings", Number("rings"), 1, 8, value =>
        {
            Write("rings", Math.Round(value, MidpointRounding.AwayFromZero));
            // A different arrangement, so the script lays the whole thing out again.
            Host.Eval("require(\"world\").build();");
        }) { Decimals = 0 },
        new Setting("Spin", Number("spin"), 0, 2, value => Write("spin", value)),
        new Toggle("Bob", Host.Eval("String(require(\"world\").settings.bob)") == "true", value => Host.Eval($"require(\"world\").settings.bob = {(value ? "true" : "false")};")),
        new Buttons("Drop one", ["Drop", "Clear"], option => Host.Eval(option == "Drop" ? "require(\"world\").drop();" : "require(\"world\").clearDropped();")),
    ];

    public override string Code => """
        // script/world.tsx holds every object in the scene, and everything that moves.

        import { spawn, all, clear } from "orblit/scene";

        export const settings = { rings: 4, spin: 0.35, bob: true };

        export function build() {
          clear();
          spawn({ id: "core", at: [0, 0.4, 0], size: [0.8, 2.4, 0.8],
                  colour: "#F2F4F7" });

          for (let ring = 0; ring < settings.rings; ring++) {
            const radius = 2.4 + ring * 1.9;
            const many = 6 + ring * 4;

            for (let i = 0; i < many; i++) {
              const angle = (i / many) * Math.PI * 2;
              spawn({
                id: `r${ring}-${i}`,
                at: [Math.cos(angle) * radius, -0.6, Math.sin(angle) * radius],
                size: [0.5, 0.5 + ring * 0.25, 0.5],
                turn: (angle * 180) / Math.PI,
                colour: colours[(ring + i) % colours.length],
              });
            }
          }
        }

        // The step. Everything that moves, moves here. The host only asks for a
        // frame and draws whatever it is told.
        export function step(seconds: number) {
          for (const thing of all()) { /* ... */ }
        }
        """;

    private static RenderObject Ground
    {
        get
        {
            var transform = Matrix4.Identity();
            transform.SetTranslation(new Vector3(0, -1.15, 0));
            transform.ScaleByDouble(26, 0.06, 26, 1);
            return new RenderObject { Key = 790, Transform = transform, Colour = Tint.Hex(0x39404A).Linear, CastShadows = false };
        }
    }

    // The renderer keeps what it built against a number, and script names things with words. One
    // stands for the other while the world holds still, which between two frames it does.
    private static RenderObject Drawn(ScriptedThing thing, int index)
    {
        var transform = Matrix4.Identity();
        transform.SetTranslation(thing.Position);
        transform.RotateY(thing.Turn * Math.PI / 180);
        transform.ScaleByDouble(thing.Scale.X, thing.Scale.Y, thing.Scale.Z, 1);
        return new RenderObject { Key = 800 + index, Transform = transform, Colour = Tint.Hex(thing.Colour).Linear, Mesh = thing.Mesh };
    }

    /// <summary>
    /// Runs the script's step and takes back everything in the world, in one call. Nothing on this
    /// side knows what moved.
    /// </summary>
    private IReadOnlyList<ScriptedThing> World(double seconds)
    {
        try
        {
            return ScriptedThing.DecodeAll(Host.Eval($"__orblit_scene.frame({seconds.ToString(CultureInfo.InvariantCulture)})"));
        }
        catch (ScriptException error)
        {
            Note = $"The world script stopped: {error.Message}";
            return [];
        }
    }

    private double Number(string name) =>
        double.TryParse(Host.Eval($"String(require(\"world\").settings.{name})"), CultureInfo.InvariantCulture, out var value) ? value : 0;

    private void Write(string name, double value) =>
        Host.Eval($"require(\"world\").settings.{name} = {value.ToString(CultureInfo.InvariantCulture)};");
}
