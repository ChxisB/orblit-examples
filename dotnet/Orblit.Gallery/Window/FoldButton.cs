using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Orblit.Gallery;

/// <summary>
/// A flat button that opens and folds what is under it, and tells a screen reader which it is now.
/// </summary>
internal sealed class FoldButton : Button
{
    private readonly Action _toggle;

    public FoldButton(Action toggle)
    {
        _toggle = toggle;
        Background = Brushes.Transparent;
        BorderThickness = default;
        CornerRadius = default;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        HorizontalContentAlignment = HorizontalAlignment.Left;
        VerticalAlignment = VerticalAlignment.Stretch;
        VerticalContentAlignment = VerticalAlignment.Center;
        Click += (_, _) => toggle();
    }

    public bool IsOpen { get; set; }

    protected override AutomationPeer OnCreateAutomationPeer() => new Peer(this);

    private sealed class Peer(FoldButton owner) : ButtonAutomationPeer(owner), IExpandCollapseProvider
    {
        public ExpandCollapseState ExpandCollapseState =>
            owner.IsOpen ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;

        public bool ShowsMenu => false;

        public void Expand()
        {
            if (!owner.IsOpen)
            {
                owner._toggle();
            }
        }

        public void Collapse()
        {
            if (owner.IsOpen)
            {
                owner._toggle();
            }
        }
    }
}
