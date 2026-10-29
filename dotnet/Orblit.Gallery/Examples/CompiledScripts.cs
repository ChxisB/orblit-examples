namespace Orblit.Gallery;

/// <summary>
/// The gallery's own TypeScript, as <c>npm run build</c> compiles <c>gallery/script/*.tsx</c>. Each is
/// one compiled file, ready for <see cref="Script.ScriptModule.Around"/>.
/// </summary>
internal static class CompiledScripts
{
    public static string Hud { get; } = Read("hud.js");

    public static string Menu { get; } = Read("menu.js");

    public static string World { get; } = Read("world.js");

    private static string Read(string name)
    {
        using var stream = typeof(CompiledScripts).Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"{name} is not in the gallery.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
