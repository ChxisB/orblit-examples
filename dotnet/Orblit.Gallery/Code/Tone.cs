using System.Diagnostics.CodeAnalysis;

namespace Orblit.Gallery;

/// <summary>What a character of code is, for the colour it is drawn in.</summary>
public enum Tone
{
    Plain,
    Comment,

    /// <summary>Declarations and literals: <c>var</c>, <c>class</c>, <c>true</c>, <c>null</c>.</summary>
    Keyword,

    /// <summary>What decides where execution goes next: <c>if</c>, <c>foreach</c>, <c>return</c>, <c>using</c>.</summary>
    Control,

    /// <summary>Variables, members being set and named arguments.</summary>
    Name,
    Type,
    Call,

    [SuppressMessage("Naming", "CA1720:Identifier contains type name", Justification = "A string literal is what it colours.")]
    String,
    Number,
}
