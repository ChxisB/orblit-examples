using Avalonia.Controls;
using Orblit.Json;
using Orblit.Ui.Avalonia;

namespace Orblit.Gallery;

/// <summary>
/// The interface the example's script describes, over its scene. The script is asked every frame,
/// since a setting can change what it describes. The surface builds again only when the description
/// does, so that a button is not replaced under the pointer pressing it.
/// </summary>
internal sealed class ScriptOverlay : Decorator, IStageLayer
{
    private readonly InterfaceExample _example;
    private readonly UiSurface _surface = new();
    private string? _shown;

    public ScriptOverlay(InterfaceExample example)
    {
        _example = example;
        // A press goes back into the script that drew the button, and the script says what changed.
        _surface.OnEvent = (handler, _) =>
        {
            example.Press(handler);
            Show();
        };
        Child = _surface;
        Show();
    }

    public void Refresh(double seconds) => Show();

    private void Show()
    {
        var description = _example.Describe();
        var json = JsonText.Encode(description.ToJson());
        if (json != _shown)
        {
            _shown = json;
            _surface.Description = description;
        }
    }
}
