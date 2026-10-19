using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;

namespace AvaAdwaita.Controls.Automation;

/// <summary>A preference row: a list item named by its title, invokable when it is activatable.</summary>
public class PreferencesRowAutomationPeer : ListItemAutomationPeer, IInvokeProvider
{
    /// <summary>Creates the peer.</summary>
    public PreferencesRowAutomationPeer(PreferencesRow owner)
        : base(owner)
    {
    }

    /// <inheritdoc/>
    public void Invoke()
    {
        EnsureEnabled();
        if (Owner is PreferencesRow { IsActivatable: true } row)
        {
            row.Activate();
        }
    }

    /// <inheritdoc/>
    protected override string? GetNameCore() =>
        Avalonia.Automation.AutomationProperties.GetName(Owner) is { Length: > 0 } name ? name
        : Owner is PreferencesRow { Title: { Length: > 0 } title } row ? (row.UseUnderline ? title.Replace("_", string.Empty) : title)
        : base.GetNameCore();
}
