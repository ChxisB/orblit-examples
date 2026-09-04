# viewport

A Filament scene as an ordinary Flutter widget.

The panel beside it is not decoration. A platform view would sit in its own
window on top of everything; a texture takes part in layout, so the panel can
overlap the scene, the slider resizes the render target rather than just its
box, and the whole thing clips to a rounded rectangle.

```sh
flutter run -d macos
```

macOS only so far. The first run downloads the Filament SDK.

## What it does not show yet

The cube lives inside the renderer rather than in a scene this example
describes. Wiring the renderer to the entity-component world is the engine's
next milestone; until then this example demonstrates the surface handoff, which
was the part nobody had proven.
