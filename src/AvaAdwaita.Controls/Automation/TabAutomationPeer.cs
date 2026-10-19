using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using AvaAdwaita.Controls.Primitives;

namespace AvaAdwaita.Controls.Automation;

/// <summary>A tab of a tab bar: a tab item named by its page's title, selected with its page.</summary>
public class TabAutomationPeer : ControlAutomationPeer, ISelectionItemProvider
{
    /// <summary>Creates the peer.</summary>
    public TabAutomationPeer(TabBoxChild owner)
        : base(owner)
    {
    }

    private TabPage? Page => (Owner as TabBoxChild)?.Child is Tab tab ? tab.Page : null;

    /// <inheritdoc/>
    public bool IsSelected => Page?.IsSelected == true;

    /// <inheritdoc/>
    public ISelectionProvider? SelectionContainer => null;

    /// <inheritdoc/>
    public void AddToSelection() => Select();

    /// <inheritdoc/>
    public void RemoveFromSelection()
    {
    }

    /// <inheritdoc/>
    public void Select()
    {
        if ((Owner as TabBoxChild)?.Child is Tab { Page: { } page } tab)
        {
            tab.View.SelectedPage = page;
        }
    }

    /// <inheritdoc/>
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.TabItem;

    /// <inheritdoc/>
    protected override string? GetNameCore() => base.GetNameCore() is { Length: > 0 } name ? name : Page?.Title;
}
