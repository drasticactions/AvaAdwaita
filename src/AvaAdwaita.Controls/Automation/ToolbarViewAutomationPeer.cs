using Avalonia.Automation.Peers;

namespace AvaAdwaita.Controls.Automation;

/// <summary>A toolbar view: a pane around its bars and content.</summary>
public class ToolbarViewAutomationPeer : ControlAutomationPeer
{
    /// <summary>Creates the peer.</summary>
    public ToolbarViewAutomationPeer(ToolbarView owner)
        : base(owner)
    {
    }

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Pane;

    /// <inheritdoc/>
    protected override bool IsControlElementCore() => false;
}
