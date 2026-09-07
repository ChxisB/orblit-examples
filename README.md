# orbis-examples

Worked examples of what [Orbis](https://github.com/Orbis-Engine/orbis) can do,
and how. Each one is small enough to read in a sitting and does one thing
properly, rather than being a game with the technique buried in it.

```sh
# Clone the engine and networking beside this one, then:
./tool/link_local.sh
```

| Example | What it shows |
| --- | --- |
| [`gallery`](gallery) | Every rendering technique, one at a time: pick from the list, change it with the settings, and read the lines that do it. macOS. |
| [`viewport`](viewport) | A Filament scene composited as an ordinary Flutter widget — it lays out, clips, and sits beside a panel that resizes it. macOS. |
| [`simulation`](simulation) | The engine with no window: an entity-component world, a transform hierarchy, a system written in Dart over column views, and the result replicated to a second world. |

```sh
cd gallery && flutter run -d macos
cd viewport && flutter run -d macos
dart run simulation/bin/simulation.dart
```

## The gallery

Each example in it is one file and stands on its own: what it needs to work
is what is written in it, and that is what the panel on the right shows.

Most of them live in the engine's own `orbis_examples` package rather than
here, because the editor shows the same ones beside the projects somebody is
working on — an example written twice is an example that drifts. What stays
here are the three that run TypeScript, beside the scripting runtime they
need: in the shared package they would mean every host of it building QuickJS
to show eleven examples that never touch it. They
are not steps in a tutorial and nothing is shared between them but the
surface they draw on.

| | |
| --- | --- |
| A scene as a widget | A lit surface composited by Flutter, laid out like any other widget. |
| Lights | Sun, point and spot, in lux and lumens, with the shadows each casts. |
| Day and night | A sun and a moon crossing the sky, with the camera metered for both. |
| Weather | Haze, banks of cloud and falling weather, carried by one wind. |
| A thousand objects | A whole scene sent every frame, and only what moved paid for. |
| Meshes | A glTF file, loaded once and instanced, with failures reported back. |

`ORBIS_EXAMPLE=weather` opens on one of them by name, for a screenshot or a
demo that should start where it means to.

## Why these run against git dependencies

The engine, networking and examples are separate repositories, so each example
declares what it needs by git reference. `tool/link_local.sh` swaps those for
sibling checkouts while you work, and the overrides it writes are not
committed — so nothing ships wired to a path on one machine.

## Licence

MIT.
