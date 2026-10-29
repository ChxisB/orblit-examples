using Orblit.Examples;

namespace Orblit.Gallery;

/// <summary>Every example the gallery lists, in the order it lists them.</summary>
public static class GalleryExamples
{
    /// <summary>A fresh set each call, made as <see cref="EngineExamples.All"/> makes it.</summary>
    public static IReadOnlyList<Example> All(IPictureFolder pictures, Func<string, string?> environment) =>
        EngineExamples.All(pictures, environment);
}
