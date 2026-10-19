using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using AvaAdwaita.Controls.Primitives;

namespace AvaAdwaita.Controls.Automation;

/// <summary>A tab overview's thumbnail: a list item named by its page's title; invoking it opens the page.</summary>
public class TabThumbnailAutomationPeer : ControlAutomationPeer, IInvokeProvider
{
    /// <summary>Creates the peer.</summary>
    public TabThumbnailAutomationPeer(TabGridChild owner)
        : base(owner)
    {
    }

    private TabThumbnail? Thumbnail => (Owner as TabGridChild)?.Child as TabThumbnail;

    /// <inheritdoc/>
    public void Invoke()
    {
        if (Thumbnail is { Page: { } page } thumbnail)
        {
            thumbnail.View.SelectedPage = page;
            (thumbnail.Parent as TabGrid)?.Overview?.Close();
        }
    }

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ListItem;

    /// <inheritdoc/>
    protected override string? GetNameCore() => base.GetNameCore() is { Length: > 0 } name ? name : Thumbnail?.Page?.Title;
}
