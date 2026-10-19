using Avalonia.Automation.Peers;

namespace AvaAdwaita.Controls.Automation;

/// <summary>A status page: a group named by its title and described by its description.</summary>
public class StatusPageAutomationPeer : ControlAutomationPeer
{
    /// <summary>Creates the peer.</summary>
    public StatusPageAutomationPeer(StatusPage owner)
        : base(owner)
    {
    }

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

    /// <inheritdoc/>
    protected override string? GetNameCore() => base.GetNameCore() is { Length: > 0 } name ? name : ((StatusPage)Owner).Title;

    /// <inheritdoc/>
    protected override string? GetHelpTextCore() => base.GetHelpTextCore() is { Length: > 0 } help ? help : ((StatusPage)Owner).Description;
}
