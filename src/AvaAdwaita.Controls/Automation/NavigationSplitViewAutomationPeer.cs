using Avalonia.Automation.Peers;

namespace AvaAdwaita.Controls.Automation;

/// <summary>A navigation split view: no element of its own; its pages stand in its place.</summary>
public class NavigationSplitViewAutomationPeer : ControlAutomationPeer
{
    /// <summary>Creates the peer.</summary>
    public NavigationSplitViewAutomationPeer(NavigationSplitView owner)
        : base(owner)
    {
    }

    /// <inheritdoc/>
    protected override bool IsControlElementCore() => false;
}
