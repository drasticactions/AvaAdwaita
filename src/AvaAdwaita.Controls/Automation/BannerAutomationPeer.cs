using Avalonia.Automation.Peers;

namespace AvaAdwaita.Controls.Automation;

/// <summary>A banner: a group named by its message, shown only while revealed.</summary>
public class BannerAutomationPeer : ControlAutomationPeer
{
    /// <summary>Creates the peer.</summary>
    public BannerAutomationPeer(Banner owner)
        : base(owner)
    {
    }

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

    /// <inheritdoc/>
    protected override string? GetNameCore() => base.GetNameCore() is { Length: > 0 } name ? name : ((Banner)Owner).Title;

    /// <inheritdoc/>
    protected override bool IsOffscreenCore() => !((Banner)Owner).IsRevealed || base.IsOffscreenCore();
}
