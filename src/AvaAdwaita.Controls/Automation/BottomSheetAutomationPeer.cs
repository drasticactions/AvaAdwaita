using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;

namespace AvaAdwaita.Controls.Automation;

/// <summary>A bottom sheet: a pane that expands and collapses as its sheet opens and closes.</summary>
public class BottomSheetAutomationPeer : ControlAutomationPeer, IExpandCollapseProvider
{
    /// <summary>Creates the peer.</summary>
    public BottomSheetAutomationPeer(BottomSheet owner)
        : base(owner)
    {
        owner.PropertyChanged += (_, e) =>
        {
            if (e.Property == BottomSheet.IsOpenProperty)
            {
                RaisePropertyChangedEvent(
                    ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,
                    (bool)e.OldValue! ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed,
                    (bool)e.NewValue! ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed);
            }
        };
    }

    private new BottomSheet Owner => (BottomSheet)base.Owner;

    /// <inheritdoc/>
    public ExpandCollapseState ExpandCollapseState => Owner.IsOpen ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;

    /// <inheritdoc/>
    public bool ShowsMenu => false;

    /// <inheritdoc/>
    public void Expand() => Owner.IsOpen = true;

    /// <inheritdoc/>
    public void Collapse() => Owner.IsOpen = false;

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Pane;
}
