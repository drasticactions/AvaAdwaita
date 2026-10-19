using Avalonia.Automation.Peers;

namespace AvaAdwaita.Controls.Automation;

/// <summary>A preferences page: a pane named by its title.</summary>
public class PreferencesPageAutomationPeer : ControlAutomationPeer
{
    /// <summary>Creates the peer.</summary>
    public PreferencesPageAutomationPeer(PreferencesPage owner)
        : base(owner)
    {
    }

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Pane;

    /// <inheritdoc/>
    protected override string? GetNameCore() => base.GetNameCore() is { Length: > 0 } name ? name : ((PreferencesPage)Owner).Title;
}
