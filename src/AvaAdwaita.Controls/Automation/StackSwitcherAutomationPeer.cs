using Avalonia.Automation.Peers;

namespace AvaAdwaita.Controls.Automation;

/// <summary>A stack switcher: a tab list whose buttons select the stack's pages.</summary>
public class StackSwitcherAutomationPeer : ControlAutomationPeer
{
    /// <summary>Creates the peer.</summary>
    public StackSwitcherAutomationPeer(StackSwitcher owner)
        : base(owner)
    {
    }

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Tab;
}
