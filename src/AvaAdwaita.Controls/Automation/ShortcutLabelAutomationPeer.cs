using Avalonia.Automation.Peers;

namespace AvaAdwaita.Controls.Automation;

/// <summary>A shortcut label: text named by its shortcut, its keycaps hidden.</summary>
public class ShortcutLabelAutomationPeer : ControlAutomationPeer
{
    /// <summary>Creates the peer.</summary>
    public ShortcutLabelAutomationPeer(ShortcutLabel owner)
        : base(owner)
    {
    }

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Text;

    /// <inheritdoc/>
    protected override System.Collections.Generic.IReadOnlyList<AutomationPeer>? GetChildrenCore() => null;
}
