using Avalonia;
using Orblit.Examples;

namespace Orblit.Gallery;

/// <summary>What a drag and a scroll on the stage do to where the camera looks from.</summary>
internal static class ViewMoves
{
    /// <summary>
    /// <paramref name="view"/> turned by a drag of <paramref name="by"/> pixels. The pitch stops
    /// short of straight up and straight down, where the camera's up would be undefined.
    /// </summary>
    public static ViewPoint Orbited(ViewPoint view, Vector by) =>
        view with { Yaw = view.Yaw - (by.X * 0.008), Pitch = Math.Clamp(view.Pitch + (by.Y * 0.008), -1.5, 1.5) };

    /// <summary><paramref name="view"/> moved out by a scroll of <paramref name="amount"/> pixels, or in when it is less than zero.</summary>
    public static ViewPoint Zoomed(ViewPoint view, double amount) =>
        view with { Distance = Math.Clamp(view.Distance * (1 + (amount * 0.0016)), 1.5, 400) };
}
