using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;

namespace AvaAdwaita.Controls.Automation;

/// <summary>A combo row: a list item named by its title whose list expands and collapses.</summary>
public class ComboRowAutomationPeer : ActionRowAutomationPeer, IExpandCollapseProvider, IValueProvider
{
    /// <summary>Creates the peer.</summary>
    public ComboRowAutomationPeer(ComboRow owner)
        : base(owner)
    {
    }

    /// <inheritdoc/>
    public ExpandCollapseState ExpandCollapseState => ((ComboRow)Owner).IsDropDownOpen ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;

    /// <inheritdoc/>
    public bool ShowsMenu => false;

    /// <inheritdoc/>
    public bool IsReadOnly => true;

    /// <inheritdoc/>
    public string? Value => ((ComboRow)Owner).SelectedItem?.ToString();

    /// <inheritdoc/>
    public void Collapse() => ((ComboRow)Owner).IsDropDownOpen = false;

    /// <inheritdoc/>
    public void Expand()
    {
        EnsureEnabled();
        ((ComboRow)Owner).IsDropDownOpen = true;
    }

    /// <inheritdoc/>
    public void SetValue(string? value) => throw new System.InvalidOperationException("A combo row's value is chosen from its list.");

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ComboBox;
}
