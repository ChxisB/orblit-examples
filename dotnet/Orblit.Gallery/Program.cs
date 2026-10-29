using Avalonia;
using Orblit.Examples;

namespace Orblit.Gallery;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args) =>
        AppBuilder.Configure(() => new GalleryApp(() => new GalleryWindow(
                GalleryExamples.All(new TempPictureFolder(), Environment.GetEnvironmentVariable),
                Launch.From(Environment.GetEnvironmentVariable),
                renderer: OperatingSystem.IsMacOS())))
            .UsePlatformDetect()
            .StartWithClassicDesktopLifetime(args);
}
