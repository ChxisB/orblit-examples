using Avalonia.Controls;
using Orblit.Json;
using Orblit.Ui.Avalonia;

namespace Orblit.Gallery;

/// <summary>
/// The interface the C# example writes out, over its scene. Asked again every frame, since a setting
/// changes what it says, and built again only when it says something new.
/// </summary>
internal sealed class NativeInterfaceOverlay : Decorator, IStageLayer
{
    private readonly NativeInterfaceExample _example;
    private readonly UiSurface _surface = new();
    private string? _shown;

    public NativeInterfaceOverlay(NativeInterfaceExample example)
    {
        _example = example;
        Child = _surface;
        Refresh(0);
    }

    public void Refresh(double seconds)
    {
        var description = _example.Description;
        var json = JsonText.Encode(description.ToJson());
        if (json != _shown)
        {
            _shown = json;
            _surface.Description = description;
        }
    }
}
