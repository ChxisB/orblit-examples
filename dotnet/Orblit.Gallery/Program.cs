using Avalonia;
using Orblit.Examples;

namespace Orblit.Gallery;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        var examples = GalleryExamples.All(new TempPictureFolder(), Environment.GetEnvironmentVariable);
        var launch = Launch.From(Environment.GetEnvironmentVariable);
        launch.Preset(examples, problem => Console.Error.WriteLine($"orblit gallery: {problem}"));
        return AppBuilder.Configure(() => new GalleryApp(() => new GalleryWindow(examples, launch, renderer: OperatingSystem.IsMacOS())))
            .UsePlatformDetect()
            .StartWithClassicDesktopLifetime(args);
    }
}
