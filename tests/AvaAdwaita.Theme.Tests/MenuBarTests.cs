using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.VisualTree;
using Xunit;

namespace AvaAdwaita.Theme.Tests;

/// <summary>A menu bar's menus, whose shadows take no input, as GTK's popovers' do not.</summary>
public class MenuBarTests
{
    private static (Menu Bar, MenuItem File, MenuItem Edit, Window Window) Show()
    {
        var file = new MenuItem { Header = "File", Items = { new MenuItem { Header = "New" }, new MenuItem { Header = "Open" } } };
        var edit = new MenuItem { Header = "Edit", Items = { new MenuItem { Header = "Cut" } } };
        var bar = new Menu { Items = { file, edit }, VerticalAlignment = VerticalAlignment.Top };
        var window = Shots.Show(bar, 400, 300);
        return (bar, file, edit, window);
    }

    private static Point Center(Visual v, Visual root) => v.TranslatePoint(new Point(v.Bounds.Width / 2, v.Bounds.Height / 2), root)!.Value;

    [AvaloniaFact]
    public void A_Menu_Leaves_The_Bar_Free_And_Hovering_Another_Item_Switches_Menus()
    {
        var (_, file, edit, window) = Show();
        window.MouseDown(Center(file, window), MouseButton.Left);
        window.MouseUp(Center(file, window), MouseButton.Left);
        Shots.Pump(window);
        Assert.True(file.IsSubMenuOpen);

        // No shadow on the side facing the item: the popup's surface starts where the menu does.
        var popup = file.GetVisualDescendants().OfType<Popup>().Single();
        var child = (Control)popup.Child!;
        Assert.Equal(0, child.Margin.Top);
        Assert.True(child.Margin.Bottom > 0);

        window.MouseMove(Center(edit, window));
        Shots.Pump(window);
        Assert.True(edit.IsSubMenuOpen);
        Assert.False(file.IsSubMenuOpen);
        window.Close();
    }

    [AvaloniaFact]
    public void A_Press_In_A_Menus_Shadow_Closes_It()
    {
        var (bar, file, _, window) = Show();
        window.MouseDown(Center(file, window), MouseButton.Left);
        window.MouseUp(Center(file, window), MouseButton.Left);
        Shots.Pump(window);
        var popup = file.GetVisualDescendants().OfType<Popup>().Single();
        var child = (Control)popup.Child!;
        var root = TopLevel.GetTopLevel(child)!;

        // A Wayland popup is a surface that takes every press on it, the shadow's too; the headless overlay host
        // lets a press through where nothing is drawn, so give it a background to stand for the surface.
        if (child.FindAncestorOfType<OverlayPopupHost>() is { } host)
        {
            host.Background = Avalonia.Media.Brushes.Transparent;
        }

        // Below the menu, in the shadow's margin.
        var shadow = child.TranslatePoint(new Point(child.Bounds.Width / 2, child.Bounds.Height + (child.Margin.Bottom / 2)), root)!.Value;
        root.MouseDown(shadow, MouseButton.Left);
        root.MouseUp(shadow, MouseButton.Left);
        Shots.Pump(window);
        Assert.False(bar.IsOpen);
        Assert.False(file.IsSubMenuOpen);
        window.Close();
    }
}
