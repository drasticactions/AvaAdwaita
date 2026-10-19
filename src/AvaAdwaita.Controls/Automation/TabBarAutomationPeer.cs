using Avalonia.Automation.Peers;

namespace AvaAdwaita.Controls.Automation;

/// <summary>A tab bar: a tab list.</summary>
public class TabBarAutomationPeer : ControlAutomationPeer
{
    /// <summary>Creates the peer.</summary>
    public TabBarAutomationPeer(TabBar owner)
        : base(owner)
    {
    }

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Tab;

    /// <inheritdoc/>
    protected override bool IsOffscreenCore() => !((TabBar)Owner).TabsRevealed || base.IsOffscreenCore();
}
