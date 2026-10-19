using Avalonia.Automation.Peers;
using Avalonia.Controls;

namespace AvaAdwaita.Controls.Automation;

/// <summary>A search entry: an edit box, named by its placeholder when nothing else names it.</summary>
public class SearchEntryAutomationPeer : TextBoxAutomationPeer
{
    /// <summary>Creates the peer.</summary>
    public SearchEntryAutomationPeer(SearchEntry owner)
        : base(owner)
    {
    }

    /// <inheritdoc/>
    protected override string? GetNameCore() => base.GetNameCore() is { Length: > 0 } name ? name : ((TextBox)Owner).PlaceholderText;

    /// <inheritdoc/>
    protected override string GetLocalizedControlTypeCore() => "search";
}
