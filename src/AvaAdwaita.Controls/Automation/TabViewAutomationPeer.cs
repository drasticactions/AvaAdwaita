using Avalonia.Automation.Peers;

namespace AvaAdwaita.Controls.Automation;

/// <summary>A tab view: a pane showing the selected page.</summary>
public class TabViewAutomationPeer : ControlAutomationPeer
{
    /// <summary>Creates the peer.</summary>
    public TabViewAutomationPeer(TabView owner)
        : base(owner)
    {
    }

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Pane;

    /// <inheritdoc/>
    protected override string? GetNameCore() => base.GetNameCore() is { Length: > 0 } name ? name : ((TabView)Owner).SelectedPage?.Title;
}
