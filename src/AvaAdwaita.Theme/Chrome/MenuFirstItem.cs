using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace AvaAdwaita.Chrome;

/// <summary>
/// A menu that opens selects its first item, as GtkPopoverMenu focuses its first item when it shows: a menu bar's
/// menu, a submenu, a context menu and a menu button's menu, however they are opened.
/// </summary>
internal static class MenuFirstItem
{
    public static void Register()
    {
        MenuItem.SubmenuOpenedEvent.AddClassHandler<MenuItem>((item, e) =>
        {
            if (ReferenceEquals(e.Source, item))
            {
                Later(item);
            }
        });
        MenuBase.OpenedEvent.AddClassHandler<ContextMenu>((menu, e) =>
        {
            if (ReferenceEquals(e.Source, menu))
            {
                Later(menu);
            }
        });
        Control.LoadedEvent.AddClassHandler<MenuFlyoutPresenter>((presenter, _) => Later(presenter));
    }

    // After the popup lays its items out, so they have containers.
    private static void Later(ItemsControl menu) => Dispatcher.UIThread.Post(() => Select(menu), DispatcherPriority.Loaded);

    private static void Select(ItemsControl menu)
    {
        for (var i = 0; i < menu.ItemCount; i++)
        {
            if (menu.ContainerFromIndex(i) is MenuItem { IsEffectivelyEnabled: true, IsEffectivelyVisible: true } item
                && item.Header is not "-")
            {
                // The open menu's selection is the item the keyboard moves from, as GTK's focus is.
                item.IsSelected = true;
                item.Focus();
                return;
            }
        }
    }
}
