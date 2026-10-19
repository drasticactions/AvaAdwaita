using Avalonia.Automation.Peers;

namespace AvaAdwaita.Controls.Automation;

/// <summary>A button row: a button named by its title.</summary>
public class ButtonRowAutomationPeer : PreferencesRowAutomationPeer
{
    /// <summary>Creates the peer.</summary>
    public ButtonRowAutomationPeer(ButtonRow owner)
        : base(owner)
    {
    }

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Button;
}
