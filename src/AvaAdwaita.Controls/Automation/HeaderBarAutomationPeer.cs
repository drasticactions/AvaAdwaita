using Avalonia.Automation.Peers;

namespace AvaAdwaita.Controls.Automation;

/// <summary>A header bar: a title bar, as GTK's header bars report themselves.</summary>
public class HeaderBarAutomationPeer : ControlAutomationPeer
{
    /// <summary>Creates the peer.</summary>
    public HeaderBarAutomationPeer(HeaderBar owner)
        : base(owner)
    {
    }

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.TitleBar;
}
