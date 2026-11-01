namespace Orblit.Gallery.Tests;

/// <summary>The pictures an example hands the renderer and takes back, in order, with no renderer to take them.</summary>
internal sealed class RecordedPictures
{
    public List<string> Provided { get; } = [];

    public List<string> Released { get; } = [];

    /// <summary>How many pictures of the wall's screen were handed over.</summary>
    public int Screens => Provided.Count(name => name.Contains("world_ui", StringComparison.Ordinal));

    /// <summary>How many pictures of the names were handed over.</summary>
    public int Names => Provided.Count(name => name.Contains("nameplates", StringComparison.Ordinal));

    public void Provide(string name, byte[] bytes) => Provided.Add(name);

    public bool Release(string name)
    {
        Released.Add(name);
        return true;
    }
}
