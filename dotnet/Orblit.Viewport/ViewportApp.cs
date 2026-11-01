using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;

namespace Orblit.Viewport;

/// <summary>The application: the dark theme with the engine's accent, and the window it opens with.</summary>
internal sealed class ViewportApp(Func<Window> mainWindow) : Application
{
    public override void Initialize()
    {
        RequestedThemeVariant = ThemeVariant.Dark;
        Styles.Add(new FluentTheme());
        Resources["SystemAccentColor"] = Color.FromUInt32(0xFFC25E22);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = mainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
