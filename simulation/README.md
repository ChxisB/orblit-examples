# simulation

The engine with no window.

An entity-component world, a transform hierarchy, a system written in Dart that
runs over column views, and the result replicated to a second world. It is the
smallest thing that exercises all four at once, which makes it the first place
a change to any of them shows up wrong.

```sh
dart run bin/simulation.dart   # prints a hierarchy moving
dart test                      # asserts it moved correctly
```

The movement pass is the shape every Dart system should have: fetch the columns
once, then loop entirely in Dart. Nothing in it calls into the engine per
entity, and a thousand bodies simulate, propagate and replicate for ten ticks
in about forty milliseconds.
