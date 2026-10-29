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

    private Peer? _peer;
    private bool _open;

    public bool IsOpen
    {
        get => _open;
        set
        {
            if (value == _open)
            {
                return;
            }

            _open = value;
            _peer?.RaisePropertyChangedEvent(ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty, StateOf(!value), StateOf(value));
        }
    }

    protected override AutomationPeer OnCreateAutomationPeer() => _peer = new Peer(this);

    private static ExpandCollapseState StateOf(bool open) => open ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;

    private sealed class Peer(FoldButton owner) : ButtonAutomationPeer(owner), IExpandCollapseProvider
    {
        public ExpandCollapseState ExpandCollapseState => StateOf(owner.IsOpen);

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
