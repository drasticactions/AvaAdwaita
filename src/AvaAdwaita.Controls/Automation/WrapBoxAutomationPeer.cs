using Avalonia.Automation.Peers;

namespace AvaAdwaita.Controls.Automation;

/// <summary>A wrap box: a group of its children.</summary>
public class WrapBoxAutomationPeer : ControlAutomationPeer
{
    /// <summary>Creates the peer.</summary>
    public WrapBoxAutomationPeer(WrapBox owner)
        : base(owner)
    {
    }

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

    /// <inheritdoc/>
    protected override bool IsControlElementCore() => false;
}
