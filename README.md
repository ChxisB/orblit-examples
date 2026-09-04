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
| [`viewport`](viewport) | A Filament scene composited as an ordinary Flutter widget — it lays out, clips, and sits beside a panel that resizes it. macOS. |
| [`simulation`](simulation) | The engine with no window: an entity-component world, a transform hierarchy, a system written in Dart over column views, and the result replicated to a second world. |

```sh
cd viewport && flutter run -d macos
dart run simulation/bin/simulation.dart
```

## Why these run against git dependencies

The engine, networking and examples are separate repositories, so each example
declares what it needs by git reference. `tool/link_local.sh` swaps those for
sibling checkouts while you work, and the overrides it writes are not
committed — so nothing ships wired to a path on one machine.

## Licence

MIT.
