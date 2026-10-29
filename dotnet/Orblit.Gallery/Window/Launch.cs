using System.Globalization;
using Orblit.Examples;

namespace Orblit.Gallery;

/// <summary>
/// How the gallery opens, named by environment variables so that a screenshot, a bug report or a
/// demo can start where it means to.
/// </summary>
/// <param name="Example">The start of the name of the example to open on, in any case. <c>ORBLIT_EXAMPLE</c>.</param>
/// <param name="Bare">
/// Whether the window is the stage and nothing else, edge to edge, for a picture of what the
/// renderer draws. <c>ORBLIT_BARE=1</c>.
/// </param>
/// <param name="Orbit">How fast the camera circles the scene on its own, in radians a second. <c>ORBLIT_ORBIT</c>.</param>
internal sealed record Launch(string? Example, bool Bare, double Orbit)
{
    public static Launch From(Func<string, string?> environment) =>
        new(
            environment("ORBLIT_EXAMPLE"),
            environment("ORBLIT_BARE") == "1",
            double.TryParse(environment("ORBLIT_ORBIT"), NumberStyles.Float, CultureInfo.InvariantCulture, out var orbit) ? orbit : 0);

    /// <summary>The first of <paramref name="examples"/> whose name starts with <see cref="Example"/>, or the first.</summary>
    public Example Opening(IReadOnlyList<Example> examples) =>
        examples.FirstOrDefault(one => Example is { } wanted && one.Name.StartsWith(wanted, StringComparison.OrdinalIgnoreCase))
        ?? examples[0];
}
