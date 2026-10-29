using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Orblit.Gallery.Tests;

/// <summary>
/// One headless Avalonia for every test in a process. This is a third copy, after the engine's
/// <c>Orblit.HeadlessSupport</c> and the editor's. Packing it is decision 93.
/// </summary>
public static class Headless
{
    private static readonly HeadlessUnitTestSession Session = HeadlessUnitTestSession.StartNew(typeof(TestApp));

    /// <summary>
    /// Runs <paramref name="test"/> on the UI thread and finishes when it does. The session's own
    /// dispatch returns before an async body is done and drops what it throws, which would make
    /// every failed assertion after an await pass.
    /// </summary>
    public static Task Run(Func<Task> test)
    {
        var done = new TaskCompletionSource();
        _ = Session.Dispatch(
            async () =>
            {
                try
                {
                    await test();
                    done.SetResult();
                }
#pragma warning disable CA1031 // The failure is handed to the test that awaits it.
                catch (Exception error)
#pragma warning restore CA1031
                {
                    done.SetException(error);
                }
            },
            CancellationToken.None);
        return done.Task;
    }

    /// <summary>Runs <paramref name="test"/> on the UI thread, for a test with nothing to await.</summary>
    public static Task Run(Action test) =>
        Run(() =>
        {
            test();
            return Task.CompletedTask;
        });

    /// <summary>Lets the window draw what has been invalidated since it last did.</summary>
    public static void Render()
    {
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
    }

    /// <summary>
    /// What is on screen under <paramref name="root"/>. Visual rather than logical, because what opens
    /// over the window is in its overlay layer, which only the visual tree reaches.
    /// </summary>
    public static IEnumerable<T> Shown<T>(Visual root)
        where T : Visual => root.GetVisualDescendants().OfType<T>().Where(one => one.IsEffectivelyVisible);

    public static void Click(Window window, Control control)
    {
        // Null only for a control off the window, and a test that clicks one has a bug to see.
        var at = control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value;
        window.MouseDown(at, MouseButton.Left);
        window.MouseUp(at, MouseButton.Left);
        Render();
    }
}

/// <summary>An application with a theme, so that a window shows its content, and no screen to draw on.</summary>
public sealed class TestApp : Application
{
    public override void Initialize() => Styles.Add(new FluentTheme());

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<TestApp>().UseSkia().UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}
