using Avalonia.Automation.Peers;

namespace AvaAdwaita.Controls.Automation;

/// <summary>An overlay split view: a pane around its sidebar and content.</summary>
public class OverlaySplitViewAutomationPeer : ControlAutomationPeer
{
    /// <summary>Creates the peer.</summary>
    public OverlaySplitViewAutomationPeer(OverlaySplitView owner)
        : base(owner)
    {
    }

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Pane;

    /// <inheritdoc/>
    protected override bool IsControlElementCore() => false;
}
