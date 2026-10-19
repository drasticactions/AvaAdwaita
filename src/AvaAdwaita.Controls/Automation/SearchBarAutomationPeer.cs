using Avalonia.Automation.Peers;

namespace AvaAdwaita.Controls.Automation;

/// <summary>A search bar: a search landmark, shown only while searching.</summary>
public class SearchBarAutomationPeer : ControlAutomationPeer
{
    /// <summary>Creates the peer.</summary>
    public SearchBarAutomationPeer(SearchBar owner)
        : base(owner)
    {
    }

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

    /// <inheritdoc/>
    protected override AutomationLandmarkType? GetLandmarkTypeCore() => AutomationLandmarkType.Search;

    /// <inheritdoc/>
    protected override bool IsOffscreenCore() => !((SearchBar)Owner).IsSearchMode || base.IsOffscreenCore();
}
