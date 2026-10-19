using Avalonia.Automation.Peers;

namespace AvaAdwaita.Controls.Automation;

/// <summary>An action bar: a tool bar.</summary>
public class ActionBarAutomationPeer : ControlAutomationPeer
{
    /// <summary>Creates the peer.</summary>
    public ActionBarAutomationPeer(ActionBar owner)
        : base(owner)
    {
    }

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ToolBar;
}
