using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;

namespace AvaAdwaita.Controls.Automation;

/// <summary>An entry row: an edit named by its title, whose value is the text.</summary>
public class EntryRowAutomationPeer : PreferencesRowAutomationPeer, IValueProvider
{
    /// <summary>Creates the peer.</summary>
    public EntryRowAutomationPeer(EntryRow owner)
        : base(owner)
    {
    }

    /// <inheritdoc/>
    public bool IsReadOnly => !((EntryRow)Owner).IsEditable;

    /// <inheritdoc/>
    public string? Value => Owner is PasswordEntryRow ? null : ((EntryRow)Owner).Text;

    /// <inheritdoc/>
    public void SetValue(string? value)
    {
        EnsureEnabled();
        ((EntryRow)Owner).Text = value;
    }

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Edit;
}
