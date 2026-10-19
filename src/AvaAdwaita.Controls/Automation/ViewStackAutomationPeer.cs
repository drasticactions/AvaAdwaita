using Avalonia.Automation.Peers;

namespace AvaAdwaita.Controls.Automation;

/// <summary>A view stack: no element of its own; its visible page's content stands in its place.</summary>
public class ViewStackAutomationPeer : ControlAutomationPeer
{
    /// <summary>Creates the peer.</summary>
    public ViewStackAutomationPeer(ViewStack owner)
        : base(owner)
    {
    }

    /// <inheritdoc/>
    protected override bool IsControlElementCore() => false;
}
