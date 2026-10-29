using System.Diagnostics;

namespace Orblit.Gallery;

/// <summary>What a snippet is written in.</summary>
public enum CodeLanguage
{
    CSharp,
    Tsx,
}

public static class CodeLanguageExtensions
{
    /// <summary>What the code block says it is.</summary>
    public static string Label(this CodeLanguage language) => language switch
    {
        CodeLanguage.CSharp => "C#",
        CodeLanguage.Tsx => "TSX",
        _ => throw new UnreachableException(),
    };
}
