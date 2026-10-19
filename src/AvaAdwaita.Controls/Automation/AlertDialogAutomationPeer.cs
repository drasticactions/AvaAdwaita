namespace AvaAdwaita.Controls.Automation;

/// <summary>An alert dialog: a window named by its heading and described by its body.</summary>
public class AlertDialogAutomationPeer : DialogAutomationPeer
{
    /// <summary>Creates the peer.</summary>
    public AlertDialogAutomationPeer(AlertDialog owner)
        : base(owner)
    {
    }

    /// <inheritdoc/>
    protected override string? GetNameCore() => base.GetNameCore() is { Length: > 0 } name ? name : ((AlertDialog)Owner).Heading;

    /// <inheritdoc/>
    protected override string? GetHelpTextCore() => base.GetHelpTextCore() is { Length: > 0 } help ? help : ((AlertDialog)Owner).Body;
}
