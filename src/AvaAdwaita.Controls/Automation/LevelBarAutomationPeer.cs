using Avalonia.Automation.Peers;

namespace AvaAdwaita.Controls.Automation;

/// <summary>A level bar: a read-only range, as a progress bar.</summary>
public class LevelBarAutomationPeer : RangeBaseAutomationPeer
{
    /// <summary>Creates the peer.</summary>
    public LevelBarAutomationPeer(LevelBar owner)
        : base(owner)
    {
    }

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ProgressBar;

    /// <inheritdoc/>
    protected override bool IsContentElementCore() => false;
}
