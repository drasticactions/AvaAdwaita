using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;

namespace AvaAdwaita.Controls.Automation;

/// <summary>An expander row: a list item named by its title that expands and collapses its nested rows.</summary>
public class ExpanderRowAutomationPeer : PreferencesRowAutomationPeer, IExpandCollapseProvider
{
    /// <summary>Creates the peer.</summary>
    public ExpanderRowAutomationPeer(ExpanderRow owner)
        : base(owner)
    {
    }

    /// <inheritdoc/>
    public ExpandCollapseState ExpandCollapseState => ((ExpanderRow)Owner).IsExpanded ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;

    /// <inheritdoc/>
    public bool ShowsMenu => false;

    /// <inheritdoc/>
    public void Collapse() => ((ExpanderRow)Owner).IsExpanded = false;

    /// <inheritdoc/>
    public void Expand()
    {
        EnsureEnabled();
        ((ExpanderRow)Owner).IsExpanded = true;
    }

    /// <inheritdoc/>
    protected override string? GetHelpTextCore() => base.GetHelpTextCore() is { Length: > 0 } help ? help : ((ExpanderRow)Owner).Subtitle;
}
