using Avalonia.Automation.Peers;

namespace AvaAdwaita.Controls.Automation;

/// <summary>A view switcher bar: no element of its own; its view switcher stands in its place.</summary>
public class ViewSwitcherBarAutomationPeer : ControlAutomationPeer
{
    /// <summary>Creates the peer.</summary>
    public ViewSwitcherBarAutomationPeer(ViewSwitcherBar owner)
        : base(owner)
    {
    }

    /// <inheritdoc/>
    protected override bool IsControlElementCore() => false;
}
