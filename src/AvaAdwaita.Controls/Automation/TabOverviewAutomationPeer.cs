using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;

namespace AvaAdwaita.Controls.Automation;

/// <summary>A tab overview: a pane that expands into the overview and collapses back to its content.</summary>
public class TabOverviewAutomationPeer : ControlAutomationPeer, IExpandCollapseProvider
{
    /// <summary>Creates the peer.</summary>
    public TabOverviewAutomationPeer(TabOverview owner)
        : base(owner)
    {
    }

    /// <inheritdoc/>
    public ExpandCollapseState ExpandCollapseState => ((TabOverview)Owner).IsOpen ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;

    /// <inheritdoc/>
    public bool ShowsMenu => false;

    /// <inheritdoc/>
    public void Expand() => ((TabOverview)Owner).Open();

    /// <inheritdoc/>
    public void Collapse() => ((TabOverview)Owner).Close();

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Pane;
}
