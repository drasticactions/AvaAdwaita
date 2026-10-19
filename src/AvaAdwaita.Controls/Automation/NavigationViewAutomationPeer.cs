using Avalonia.Automation.Peers;

namespace AvaAdwaita.Controls.Automation;

/// <summary>A navigation view: no element of its own; its visible page stands in its place.</summary>
public class NavigationViewAutomationPeer : ControlAutomationPeer
{
    /// <summary>Creates the peer.</summary>
    public NavigationViewAutomationPeer(NavigationView owner)
        : base(owner)
    {
    }

    /// <inheritdoc/>
    protected override bool IsControlElementCore() => false;
}
