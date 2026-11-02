# orblit-examples

Worked examples of what [Orblit](https://github.com/ChxisB/orblit) can do,
and how. Each one is small enough to read in a sitting, and does one thing
properly rather than being a game with the technique buried in it.

The examples are C# on Avalonia. You need the .NET 10 SDK.

```sh
# Clone orblit, orblit-net and orblit-physics beside this one, then fill the
# local feed the packages restore from:
orblit/tool/build_native.sh
orblit/tool/pack_local.sh
dotnet pack orblit-net/dotnet/Orblit.Net -c Release -o ~/.orblit/feed
orblit-physics/tool/build_native.sh
dotnet pack orblit-physics/dotnet/Orblit.Physics -c Release -o ~/.orblit/feed
dotnet pack orblit-physics/dotnet/Orblit.Physics.Soft -c Release -o ~/.orblit/feed
```

| Example | What it shows |
| --- | --- |
| [`Orblit.Gallery`](dotnet/Orblit.Gallery) | Every rendering technique, one at a time: pick from the list, change it with the settings, and read the lines that do it. macOS. |
| [`Orblit.Viewport`](dotnet/Orblit.Viewport) | A Filament scene drawn as an ordinary Avalonia control. It lays out, it clips, and it sits beside a panel that resizes it. macOS. |
| [`Orblit.Simulation`](dotnet/Orblit.Simulation) | The engine with no window: an entity-component world, a transform hierarchy, a system written over column views, and the result replicated to a second world. |
| [`Orblit.Crate`](dotnet/Orblit.Crate) | A game as a project: one scene and one `IGame` class. The editor builds it and plays it, and `dotnet run` plays the same class in a window of its own. |

```sh
dotnet run --project dotnet/Orblit.Gallery -c Release
dotnet run --project dotnet/Orblit.Viewport -c Release
dotnet run --project dotnet/Orblit.Simulation -c Release
dotnet run --project dotnet/Orblit.Crate -c Release
```

`./tool/check_dotnet.sh` builds, tests and checks the format.

## The crate

`Orblit.Crate` is laid out the way the editor expects a game project to be.
`scenes/main.oscene` holds the objects. `game/CrateGame.cs` is the one
`IGame` class, and `orblit.project.json` names it. The arrow keys move the
crate, it turns on its own, and a click puts the marker on the tile under the
pointer.

Open the `dotnet/Orblit.Crate` folder in the editor and press Play. The editor
asks once whether to trust the project, because building it runs its code. It
then builds `game/Game.csproj` and runs the class on a copy of the scene.

The game keeps nothing but the camera's distance from the crate. Everything
else is read from the scene each frame, so a change made in the editor while it
plays shows up on the next frame. `Orblit.Crate.Tests` plays the game under a
host with no window and says what one frame of input does.

## The gallery

Each example is one file and stands on its own. What it needs to work is what
is written in it, and that is what the panel on the right shows.

Most of them live in the engine's own `Orblit.Examples` package rather than
here, because the editor shows the same ones beside the projects somebody is
working on. An example written twice is an example that drifts. What stays
here are the three that need more than that package gives: the cloth, which
uses the physics solver, the interface in a window and the interface in the
world. They are not steps in a tutorial, and they share nothing but the
surface they draw on.

| | |
| --- | --- |
| Lights | Sun, point and spot, in lux and lumens, with the shadows each casts. |
| Day and night | A sun and a moon crossing the sky, with the camera metered for both. |
| Weather | Haze, banks of cloud and falling weather, carried by one wind. |
| A thousand objects | A whole scene sent every frame. You only pay for what moved. |
| Meshes | A glTF file, loaded once and instanced, with failures reported back. |

`ORBLIT_EXAMPLE=weather` opens on one of them by name, which is handy for a
screenshot or a demo that should start where it means to.

The C# gallery reads more switches, so a sweep is a shell loop. `ORBLIT_YAW`,
`ORBLIT_PITCH` and `ORBLIT_DISTANCE` frame the camera. `ORBLIT_SECONDS` holds
the clock at one time. `ORBLIT_EXAMPLE=blocks ORBLIT_SET="Relief=9; Trees=off"` sets
settings of the opening example by label, and `ORBLIT_WALK=1` is its `Walk`
toggle. A switch that cannot be applied is named on stderr and the rest still
run.

## Where the packages come from

The engine, networking, physics and examples are separate repositories, and
there is no public NuGet feed yet. Each repository packs its libraries into
`~/.orblit/feed`, and `dotnet/Directory.Packages.props` pins the versions the
examples restore from there. `ORBLIT_FEED` names another feed folder for the
engine's `tool/pack_local.sh`.

## Licence

MPL-2.0, © 2026 Chris Beckett. That is the Mozilla Public License, and it is
open source. Use it, fork it and ship games with it, including commercial ones.
Your game stays yours, and the licence does not reach into it. What it asks is
that changes to this repository's own files ship under the same licence, so
engine work stays in the open.

The gallery and the viewport link the renderer, so builds carry Filament's
Apache 2.0 licence too. See [LICENSE](LICENSE).
