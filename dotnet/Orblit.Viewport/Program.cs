using Avalonia;

namespace Orblit.Viewport;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args) =>
        AppBuilder.Configure(() => new ViewportApp(() => new ViewportWindow(renderer: OperatingSystem.IsMacOS())))
            .UsePlatformDetect()
            .StartWithClassicDesktopLifetime(args);
}
