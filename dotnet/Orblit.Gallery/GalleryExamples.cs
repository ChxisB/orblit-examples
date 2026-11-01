using Orblit.Examples;
using Orblit.Filament;

namespace Orblit.Gallery;

/// <summary>Every example the gallery lists, in the order it lists them.</summary>
public static class GalleryExamples
{
    /// <summary>
    /// A fresh set each call: the engine's, made as <see cref="EngineExamples.All"/> makes them, then
    /// the gallery's own. The cloth is here rather than in <c>Orblit.Examples</c>, so that every host
    /// of those examples does not ship the soft body solver for one of them. The interfaces are here
    /// because the gallery's own controls draw them. Where an example is listed follows its section, not
    /// its place here.
    /// </summary>
    public static IReadOnlyList<Example> All(IPictureFolder pictures, Func<string, string?> environment) =>
    [
        .. EngineExamples.All(pictures, environment, (name, bytes) => RenderResources.Provide(name, bytes)),
        new ClothExample((name, bytes) => RenderResources.Provide(name, bytes)),
        new NativeInterfaceExample(),
        new WorldUiExample((name, bytes) => RenderResources.Provide(name, bytes), RenderResources.Release),
    ];
}
