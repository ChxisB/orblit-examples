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
    /// <summary>Where the camera starts, in place of the example's own. <c>ORBLIT_YAW</c>.</summary>
    public double? Yaw { get; init; }

    /// <summary>Where the camera starts, in place of the example's own. <c>ORBLIT_PITCH</c>.</summary>
    public double? Pitch { get; init; }

    /// <summary>Where the camera starts, in place of the example's own. <c>ORBLIT_DISTANCE</c>.</summary>
    public double? Distance { get; init; }

    /// <summary>
    /// The moment to hold the clock at, so that two runs of an animating scene draw one picture.
    /// <c>ORBLIT_SECONDS</c>.
    /// </summary>
    public double? Seconds { get; init; }

    /// <summary>
    /// The settings to set on the example that opens, each written <c>Label=value</c>, in the order
    /// they were named. <c>ORBLIT_SET</c> holds several apart by semicolons, and <c>ORBLIT_WALK</c>
    /// is the one named <c>Walk</c>.
    /// </summary>
    public IReadOnlyList<string> Settings { get; init; } = [];

    public static Launch From(Func<string, string?> environment) =>
        new(environment("ORBLIT_EXAMPLE"), environment("ORBLIT_BARE") == "1", Number(environment, "ORBLIT_ORBIT") ?? 0)
        {
            Yaw = Number(environment, "ORBLIT_YAW"),
            Pitch = Number(environment, "ORBLIT_PITCH"),
            Distance = Number(environment, "ORBLIT_DISTANCE"),
            Seconds = Number(environment, "ORBLIT_SECONDS"),
            Settings = [.. Walk(environment("ORBLIT_WALK")), .. Items(environment("ORBLIT_SET"))],
        };

    /// <summary>The first of <paramref name="examples"/> whose name starts with <see cref="Example"/>, or the first.</summary>
    public Example Opening(IReadOnlyList<Example> examples) =>
        examples.FirstOrDefault(one => Example is { } wanted && one.Name.StartsWith(wanted, StringComparison.OrdinalIgnoreCase))
        ?? examples[0];

    /// <summary><paramref name="view"/> with the angle and distance this launch names put over its own.</summary>
    public ViewPoint Framed(ViewPoint view) =>
        view with { Yaw = Yaw ?? view.Yaw, Pitch = Pitch ?? view.Pitch, Distance = Distance ?? view.Distance };

    /// <summary>The time to draw at, which is <see cref="Seconds"/> when one is held and otherwise the time that has passed.</summary>
    public double Clock(double elapsed) => Seconds ?? elapsed;

    /// <summary>
    /// Sets <see cref="Settings"/> on the example that opens, and tells <paramref name="complain"/> of
    /// each that cannot be set, so that a typo in a shell loop is seen rather than drawn.
    /// </summary>
    public void Preset(IReadOnlyList<Example> examples, Action<string> complain)
    {
        var opening = Opening(examples);
        foreach (var item in Settings)
        {
            SettingByLabel.Set(opening, item, complain);
        }
    }

    private static double? Number(Func<string, string?> environment, string name) =>
        double.TryParse(environment(name), NumberStyles.Float, CultureInfo.InvariantCulture, out var number) ? number : null;

    // Held with the other settings, since the voxel walk's switch is the setting called Walk.
    private static IEnumerable<string> Walk(string? value) =>
        string.IsNullOrWhiteSpace(value) ? [] : [$"Walk={value}"];

    private static string[] Items(string? value) =>
        (value ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
