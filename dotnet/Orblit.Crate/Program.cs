using Crate;
using Orblit.Game.Avalonia;
using Orblit.Scene;

namespace Orblit.Crate;

/// <summary>Plays the crate game in a window of its own. The editor plays the same game from the same folder.</summary>
internal static class Program
{
    public static int Main()
    {
        var folder = AppContext.BaseDirectory;
        var load = SceneDocument.Decode(File.ReadAllText(Path.Combine(folder, "scenes", "main.oscene")));
        if (load.Problems.Count > 0)
        {
            Console.Error.WriteLine($"The scene did not load: {string.Join(" ", load.Problems)}");
            return 1;
        }

        return GameRunner.Run(new CrateGame(), load.Document, folder);
    }
}
