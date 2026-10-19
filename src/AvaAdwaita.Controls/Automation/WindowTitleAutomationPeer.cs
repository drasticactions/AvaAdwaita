using Avalonia.Automation.Peers;

namespace AvaAdwaita.Controls.Automation;

/// <summary>A window title: text named by the title and described by the subtitle.</summary>
public class WindowTitleAutomationPeer : ControlAutomationPeer
{
    /// <summary>Creates the peer.</summary>
    public WindowTitleAutomationPeer(WindowTitle owner)
        : base(owner)
    {
    }

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Text;

    /// <inheritdoc/>
    protected override string? GetNameCore() => base.GetNameCore() is { Length: > 0 } name ? name : ((WindowTitle)Owner).Title;

    /// <inheritdoc/>
    protected override string? GetHelpTextCore() => base.GetHelpTextCore() is { Length: > 0 } help ? help : ((WindowTitle)Owner).Subtitle;
}
