using Avalonia.Automation.Peers;

namespace AvaAdwaita.Controls.Automation;

/// <summary>A link row: a hyperlink named by its title.</summary>
public class LinkRowAutomationPeer : PreferencesRowAutomationPeer
{
    /// <summary>Creates the peer.</summary>
    public LinkRowAutomationPeer(LinkRow owner)
        : base(owner)
    {
    }

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Hyperlink;

    /// <inheritdoc/>
    protected override string? GetHelpTextCore() => base.GetHelpTextCore() is { Length: > 0 } help ? help : ((LinkRow)Owner).Uri;
}
