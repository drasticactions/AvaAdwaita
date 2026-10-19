using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;

namespace AvaAdwaita.Controls.Automation;

/// <summary>A switch row: a list item named by its title that toggles.</summary>
public class SwitchRowAutomationPeer : ActionRowAutomationPeer, IToggleProvider
{
    /// <summary>Creates the peer.</summary>
    public SwitchRowAutomationPeer(SwitchRow owner)
        : base(owner)
    {
    }

    /// <inheritdoc/>
    public ToggleState ToggleState => ((SwitchRow)Owner).IsChecked ? ToggleState.On : ToggleState.Off;

    /// <inheritdoc/>
    public void Toggle()
    {
        EnsureEnabled();
        ((SwitchRow)Owner).IsChecked = !((SwitchRow)Owner).IsChecked;
    }
}
