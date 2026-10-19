using Avalonia.Automation.Peers;

namespace AvaAdwaita.Controls.Automation;

/// <summary>A preferences group: a group named by its title and described by its description.</summary>
public class PreferencesGroupAutomationPeer : ControlAutomationPeer
{
    /// <summary>Creates the peer.</summary>
    public PreferencesGroupAutomationPeer(PreferencesGroup owner)
        : base(owner)
    {
    }

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

    /// <inheritdoc/>
    protected override string? GetNameCore() => base.GetNameCore() is { Length: > 0 } name ? name : ((PreferencesGroup)Owner).Title;

    /// <inheritdoc/>
    protected override string? GetHelpTextCore() => base.GetHelpTextCore() is { Length: > 0 } help ? help : ((PreferencesGroup)Owner).Description;
}
