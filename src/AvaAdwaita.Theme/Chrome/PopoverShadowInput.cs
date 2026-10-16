using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;

namespace AvaAdwaita.Chrome;

/// <summary>
/// A press in a popover's shadow is a press outside it, as GTK's popover surface takes input only where the popover
/// is drawn. Avalonia's popup surface spans the shadow too, so without this a click there would do nothing.
/// </summary>
internal static class PopoverShadowInput
{
    public static void Register()
    {
        InputElement.PointerPressedEvent.AddClassHandler<PopupRoot>(OnPointerPressed, RoutingStrategies.Tunnel);
        InputElement.PointerPressedEvent.AddClassHandler<OverlayPopupHost>(OnPointerPressed, RoutingStrategies.Tunnel);
    }

    private static void OnPointerPressed(Control host, PointerPressedEventArgs e)
    {
        if (e.Handled || host.Parent is not Popup popup
            || host.GetVisualDescendants().OfType<PopoverContents>().FirstOrDefault() is not { } contents)
        {
            return;
        }

        // The popover's outline: the contents and the arrow's tail on its side.
        var tail = PopoverArrow.WithTail(default, contents.TailSide, PopoverArrow.TailHeight);
        var outline = new Rect(contents.Bounds.Size).Inflate(tail);
        if (outline.Contains(e.GetPosition(contents)))
        {
            return;
        }

        // A menu's popovers close together, as a click outside a GTK menu closes it and its submenus.
        var menu = popup.TemplatedParent is MenuItem item ? item.FindLogicalAncestorOfType<MenuBase>() : popup.Child as MenuBase;
        if (menu is not null)
        {
            e.Handled = true;
            menu.Close();
        }
        else if (popup.IsLightDismissEnabled)
        {
            e.Handled = true;
            popup.Close();
        }
    }
}
