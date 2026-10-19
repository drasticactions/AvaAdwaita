namespace AvaAdwaita.Controls.Automation;

/// <summary>An action row: a preference row described by its subtitle.</summary>
public class ActionRowAutomationPeer : PreferencesRowAutomationPeer
{
    /// <summary>Creates the peer.</summary>
    public ActionRowAutomationPeer(ActionRow owner)
        : base(owner)
    {
    }

    /// <inheritdoc/>
    protected override string? GetHelpTextCore() => base.GetHelpTextCore() is { Length: > 0 } help ? help : ((ActionRow)Owner).Subtitle;
}
