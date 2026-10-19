using Avalonia.Automation.Peers;
using Avalonia.Controls;

namespace AvaAdwaita.Controls.Automation;

/// <summary>An avatar: an image named after the person, as "Avatar of %s".</summary>
public class AvatarAutomationPeer : ControlAutomationPeer
{
    /// <summary>Creates the peer.</summary>
    public AvatarAutomationPeer(Avatar owner)
        : base(owner)
    {
    }

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Image;

    /// <inheritdoc/>
    protected override string? GetNameCore() =>
        base.GetNameCore() is { Length: > 0 } name ? name
        : ((Avatar)Owner).Text is { Length: > 0 } text ? string.Format(System.Globalization.CultureInfo.CurrentCulture, Owner.FindResource("AdwStringAvatarOf") as string ?? "Avatar of {0}", text)
        : null;
}
