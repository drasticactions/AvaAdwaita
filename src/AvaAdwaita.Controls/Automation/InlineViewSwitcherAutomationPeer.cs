using Avalonia.Automation.Peers;

namespace AvaAdwaita.Controls.Automation;

/// <summary>An inline view switcher: a tab list whose toggles select the stack's pages.</summary>
public class InlineViewSwitcherAutomationPeer : ControlAutomationPeer
{
    /// <summary>Creates the peer.</summary>
    public InlineViewSwitcherAutomationPeer(InlineViewSwitcher owner)
        : base(owner)
    {
    }

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Tab;
}
