using Avalonia.Automation.Peers;

namespace AvaAdwaita.Controls.Automation;

/// <summary>A dialog: a window-like element named by its title.</summary>
public class DialogAutomationPeer : ControlAutomationPeer
{
    /// <summary>Creates the peer.</summary>
    public DialogAutomationPeer(Dialog owner)
        : base(owner)
    {
    }

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Window;

    /// <inheritdoc/>
    protected override string? GetNameCore() => base.GetNameCore() is { Length: > 0 } name ? name : ((Dialog)Owner).Title;
}
