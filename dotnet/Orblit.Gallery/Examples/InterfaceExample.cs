using System.Globalization;
using Orblit.Examples;
using Orblit.Filament;
using Orblit.Json;
using Orblit.Light;
using Orblit.Script;
using Orblit.Script.Ui;
using Orblit.Ui;
using Orblit.VectorMath;

namespace Orblit.Gallery;

/// <summary>
/// An interface over a running scene, written in TypeScript. <c>script/hud.tsx</c> is the whole of
/// it: components, props and JSX. It is compiled by <c>npm run build</c>, loaded into the engine's own
/// script host, and asked to describe the interface. The gallery builds real controls from what comes
/// back. The borrowed class-name vocabulary is a way in rather than a second box model pretending to
/// be the web's.
/// </summary>
public sealed class InterfaceExample : Example, IDisposable
{
    private ScriptHost? _host;
    private string? _loaded;

    public override string Name => "An interface in TypeScript";

    public override string Blurb => "Two interfaces written in .tsx, running in the engine, drawn by Avalonia.";

    public override ExampleSection Section => ExampleSection.Scripting;

    public override ViewPoint ViewPoint => new() { Distance = 12, Pitch = 0.22 };

    /// <summary>
    /// Which of the two interfaces is loaded. Two, because one proves nothing about whether the first
    /// was a special case. They share no code: each is a whole <c>.tsx</c> of its own.
    /// </summary>
    public string Showing { get; set; } = "HUD";

    // The script owns the hull, not this class. A HUD written in TypeScript keeps its own state, and a
    // button in it changes that state without asking. A copy held here and pushed in before every
    // render would write over whatever the script had just done.
    internal double Health
    {
        get => double.TryParse(Read("hull"), CultureInfo.InvariantCulture, out var hull) ? hull : 0;
        set => Write("hull", value.ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// The engine running whichever interface is <see cref="Showing"/>. Made at first use rather than
    /// with the example, because QuickJS takes its stack limit from the thread that made it, and a
    /// host called from another thread says its stack is full.
    /// </summary>
    internal ScriptHost Host
    {
        get
        {
            if (_host is { } running && _loaded == Showing)
            {
                return running;
            }

            // A replaced script should not leave its state behind, so it gets a fresh engine rather
            // than a second mount into the old one.
            _host?.Dispose();
            _host = new ScriptHost();
            _loaded = Showing;
            var (script, file) = Showing == "Menu" ? (CompiledScripts.Menu, "script/menu.tsx") : (CompiledScripts.Hud, "script/hud.tsx");
            // The elements, both JSX factories, and the module registry a file compiled straight from
            // TypeScript needs.
            _host.Eval(UiRuntime.Source, "orblit/ui.js");
            // Kept under a name, because its state lives in a module and the settings write into it.
            _host.Eval(ScriptModule.Around(script, "hud"), file);
            return _host;
        }
    }

    /// <summary>
    /// What the interface looks like now, asked of the script every time. Nothing on this side builds
    /// an element, and nothing here holds the state it is built from.
    /// </summary>
    internal UiNode Describe()
    {
        try
        {
            return UiNode.Decode(Host.Eval("__orblit_ui.render()"));
        }
        catch (ScriptException error)
        {
            // Said where somebody can read it, rather than leaving a blank corner and no reason.
            return new UiNode
            {
                Type = "column",
                Classes = "p-4 gap-2 m-5 rounded-lg bg-rose-900 border border-rose-600",
                Children =
                [
                    new UiNode { Type = "text", Classes = "text-sm font-semibold text-rose-100", Text = "The interface script stopped" },
                    new UiNode { Type = "text", Classes = "text-xs text-rose-200", Text = error.Message },
                ],
            };
        }
    }

    /// <summary>Hands a press back to the script that drew what was pressed.</summary>
    internal void Press(string handler)
    {
        try
        {
            Host.Eval($"__orblit_ui.dispatch({JsonText.Encode(handler)})");
        }
        catch (ScriptException error)
        {
            Note = $"The interface script stopped: {error.Message}";
        }
    }

    public void Dispose() => _host?.Dispose();

    public override RenderScene Scene(RenderCamera camera, double seconds) => new()
    {
        Objects = [.. Enumerable.Range(0, 4).Select(i => Orbiting(i, seconds)), Ground],
        Lights =
        [
            new RenderLight
            {
                Key = 710,
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

    public override IReadOnlyList<ExampleSetting> Settings()
    {
        var written = new Choice("Written", ["HUD", "Menu"], Showing, option => Showing = option);
        return Showing == "Menu"
            ? [written, new Caption("The menu keeps its own state. Press the choices in it.")]
            :
            [
                written,
                new Setting("Hull", Health, 0, 1, value => Health = value),
                new Choice("Accent", ["ember", "steel", "moss", "rose"], Read("accent"), option => Write("accent", JsonText.Encode(option))),
                new Toggle("Buttons", Read("panel") == "true", value => Write("panel", value ? "true" : "false")),
            ];
    }

    public override string Code => """
        // script/hud.tsx is the whole of the interface. Compiled by `npm run build`
        // and loaded into the engine's script host; nothing on the C# side builds
        // an element.

        import { mount } from "orblit";

        const state = { hull: 0.72, score: 1840, accent: "ember" };

        /// A bar is two boxes: the track, and as much of it as is left. No progress
        /// widget, and no second component set.
        function Bar({ part }: { part: number }) {
          return (
            <box class="w-full h-2 rounded-full bg-slate-700 clip">
              <box class={`h-2 rounded-full bg-${state.accent}-500`}
                   style={`width: ${Math.round(part * 224)}px`} />
            </box>
          );
        }

        function Hud() {
          return (
            <column class="p-5 gap-3 items-start">
              <text class="text-lg font-semibold text-slate-100">Sector 12</text>
              <Bar part={state.hull} />
              <button class="px-3 py-2 rounded-md bg-ember-500 text-white"
                      key="repair"
                      onPressed={() => { state.hull = Math.min(1, state.hull + 0.12); }}>
                Repair
              </button>
            </column>
          );
        }

        mount(() => <Hud />);
        """;

    private static RenderObject Ground
    {
        get
        {
            var transform = Matrix4.Identity();
            transform.SetTranslation(new Vector3(0, -1.7, 0));
            transform.ScaleByDouble(10, 0.06, 10, 1);
            return new RenderObject { Key = 690, Transform = transform, Colour = Tint.Hex(0x39404A).Linear, CastShadows = false };
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
        return new RenderObject { Key = 700 + i, Transform = transform, Colour = Tint.Hex(i % 2 == 0 ? 0xD9634F : 0x5FA8D3).Linear };
    }

    private string Read(string name) => Host.Eval($"String(require(\"hud\").state.{name})");

    private void Write(string name, string value) => Host.Eval($"require(\"hud\").state.{name} = {value};");
}
