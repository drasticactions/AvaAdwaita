using Avalonia.Automation.Peers;

namespace AvaAdwaita.Controls.Automation;

/// <summary>A navigation page: a group named by its title.</summary>
public class NavigationPageAutomationPeer : ContentControlAutomationPeer
{
    /// <summary>Creates the peer.</summary>
    public NavigationPageAutomationPeer(NavigationPage owner)
        : base(owner)
    {
    }

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

    /// <inheritdoc/>
    protected override string? GetNameCore() => Avalonia.Automation.AutomationProperties.GetName(Owner) is { Length: > 0 } name ? name : ((NavigationPage)Owner).Title;
}
