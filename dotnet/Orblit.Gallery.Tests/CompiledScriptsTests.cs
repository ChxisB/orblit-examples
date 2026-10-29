using System.Text.RegularExpressions;
using Orblit.Json;

namespace Orblit.Gallery.Tests;

/// <summary>
/// The gallery ships a copy of what <c>gallery/script/*.tsx</c> compile to. <c>npm run build</c>
/// writes only the Dart gallery's <c>scripts.g.dart</c>, so this is what says the copy is stale.
/// </summary>
public sealed partial class CompiledScriptsTests
{
    [Theory]
    [InlineData("hud")]
    [InlineData("menu")]
    [InlineData("world")]
    public void EachIsTheScriptTheDartGalleryRuns(string name)
    {
        var shipped = name switch
        {
            "hud" => CompiledScripts.Hud,
            "menu" => CompiledScripts.Menu,
            _ => CompiledScripts.World,
        };

        Assert.Equal(DartString(name + "Script"), shipped);
    }

    [GeneratedRegex("""const String (\w+) = ("(?:[^"\\]|\\.)*");""")]
    private static partial Regex Literal();

    private static string DartString(string constant)
    {
        var dart = File.ReadAllText(Path.Combine(RepositoryRoot(), "gallery", "lib", "src", "examples", "scripts.g.dart"));
        var quoted = Literal().Matches(dart).Single(match => match.Groups[1].Value == constant).Groups[2].Value;
        // Dart escapes a dollar sign in a quoted string. JSON does not know that escape.
        return (string)JsonText.Decode(quoted.Replace("\\$", "$", StringComparison.Ordinal))!; // a quoted literal decodes to a string
    }

    private static string RepositoryRoot()
    {
        var root = AppContext.BaseDirectory;
        while (!File.Exists(Path.Combine(root, "gallery", "lib", "src", "examples", "scripts.g.dart")))
        {
            root = Path.GetDirectoryName(root) ?? throw new InvalidOperationException("The repository root was not found");
        }

        return root;
    }
}
