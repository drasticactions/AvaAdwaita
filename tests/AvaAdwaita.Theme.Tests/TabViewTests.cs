using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using AvaAdwaita.Controls;
using AvaAdwaita.Controls.Primitives;
using AvaAdwaita.Nodes;
using Xunit;

namespace AvaAdwaita.Theme.Tests;

/// <summary>AvaAdwaita.Controls' TabView and TabBar: the pages' order, selection and closing, shortcuts and the tabs.</summary>
public class TabViewTests
{
    private static Point Center(Visual v, Visual root) => v.TranslatePoint(new Point(v.Bounds.Width / 2, v.Bounds.Height / 2), root)!.Value;

    private static (Window, TabBar, TabView) Show(int pages)
    {
        var view = new TabView();
        for (var i = 0; i < pages; i++)
        {
            view.Append(new TextBox { Text = $"Page {i + 1}" }).Title = $"Page {i + 1}";
        }

        var bar = new TabBar { View = view, Autohide = false };
        DockPanel.SetDock(bar, Dock.Top);
        var w = Shots.Show(new DockPanel { Children = { bar, view } }, 500, 300);
        AdwaitaEnvironment.SuppressTransitions(w);
        Shots.Pump(w);
        return (w, bar, view);
    }

    private static Tab TabOf(TabBar bar, TabPage page) => bar.GetVisualDescendants().OfType<Tab>().Single(t => t.Page == page);

    [AvaloniaFact]
    public void Pinned_Pages_Come_First_And_Closing_Selects_A_Neighbor()
    {
        var view = new TabView();
        var a = view.Append(new Border());
        var b = view.Append(new Border());
        var pinned = view.AppendPinned(new Border());
        Assert.Equal([pinned, a, b], view.Pages);
        Assert.Equal(1, view.PinnedPageCount);
        Assert.Same(a, view.SelectedPage);
        Assert.False(view.ReorderPage(a, 0));

        view.SetPagePinned(b, true);
        Assert.Equal([pinned, b, a], view.Pages);
        Assert.True(b.IsPinned);

        // A page opened from another goes after it and the pages opened from it, and closing it goes back.
        view.SetPagePinned(b, false);
        var child = view.AddPage(new Border(), a);
        var grandchild = view.AddPage(new Border(), child);
        Assert.Equal([pinned, b, a, child, grandchild], view.Pages);
        view.SelectedPage = grandchild;
        view.ClosePage(grandchild);
        Assert.Same(child, view.SelectedPage);

        // A pinned page does not close unless the request is answered.
        view.ClosePage(pinned);
        Assert.Contains(pinned, view.Pages);
        view.ClosePageRequested += (_, e) =>
        {
            e.Handled = true;
            view.ClosePageFinish(e.Page!, true);
        };
        view.ClosePage(pinned);
        Assert.DoesNotContain(pinned, view.Pages);
        Assert.Equal(0, view.PinnedPageCount);
    }

    [AvaloniaFact]
    public void Shortcuts_Switch_And_Move_Pages()
    {
        var (w, _, view) = Show(3);
        var pages = view.Pages.ToList();
        w.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.Control);
        Assert.Same(pages[1], view.SelectedPage);
        w.KeyPressQwerty(PhysicalKey.PageDown, RawInputModifiers.Control);
        w.KeyPressQwerty(PhysicalKey.PageDown, RawInputModifiers.Control);
        Assert.Same(pages[0], view.SelectedPage);
        w.KeyPressQwerty(PhysicalKey.Digit3, RawInputModifiers.Alt);
        Assert.Same(pages[2], view.SelectedPage);
        w.KeyPressQwerty(PhysicalKey.Home, RawInputModifiers.Control | RawInputModifiers.Shift);
        Assert.Same(pages[2], view.Pages[0]);
        view.Shortcuts = TabViewShortcuts.None;
        w.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.Control);
        Assert.Same(pages[2], view.SelectedPage);
        w.Close();
    }

    [AvaloniaFact]
    public void Tabs_Select_Close_And_Hide_Separators()
    {
        var (w, bar, view) = Show(3);
        var pages = view.Pages.ToList();
        var tabs = pages.Select(p => TabOf(bar, p)).ToList();
        Assert.True(tabs[0].Bounds.Width > 100);
        Assert.Contains(":selected", tabs[0].Classes);

        // The separators beside the selected tab hide.
        string Separator(int i) => string.Join(' ', Css.GetCssNode(bar.TabBox.GetVisualChildren().OfType<Presenters.AdwBox>()
            .Where(b => Css.GetNode(b) == "separator").ElementAt(2 - i))!.Classes);
        Assert.Contains("hidden", Separator(0));
        Assert.Contains("hidden", Separator(1));
        Assert.DoesNotContain("hidden", Separator(2));

        w.MouseDown(Center(tabs[2], w), MouseButton.Left);
        w.MouseUp(Center(tabs[2], w), MouseButton.Left);
        Shots.Pump(w);
        Assert.Same(pages[2], view.SelectedPage);

        // A middle click closes a tab.
        w.MouseDown(Center(tabs[1], w), MouseButton.Middle);
        w.MouseUp(Center(tabs[1], w), MouseButton.Middle);
        Shots.Pump(w);
        Assert.Equal(2, view.PageCount);
        Assert.Equal(2, bar.GetVisualDescendants().OfType<Tab>().Count(t => t.Page is not null));

        // The selected tab's close button closes it.
        var close = tabs[2].GetVisualDescendants().OfType<Button>().Single(b => b.Classes.Contains("tab-close-button"));
        Assert.Equal(1, close.Opacity);
        close.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        Shots.Pump(w);
        Assert.Equal([pages[0]], view.Pages);

        // Closing tabs under the pointer keeps the widths until it leaves; then a lone tab takes the single-tab style.
        Assert.DoesNotContain("single-tab", Css.GetCssNode(bar.TabBox)!.Classes ?? []);
        w.MouseMove(new Point(250, 250));
        Shots.Pump(w);
        Assert.Contains("single-tab", Css.GetCssNode(bar.TabBox)!.Classes);
        w.Close();
    }

    [AvaloniaFact]
    public void Autohide_Shows_The_Bar_For_Two_Pages()
    {
        var (w, bar, view) = Show(1);
        bar.Autohide = true;
        Shots.Pump(w);
        Assert.False(bar.TabsRevealed);
        view.Append(new Border()).Title = "Two";
        Shots.Pump(w);
        Assert.True(bar.TabsRevealed);
        Assert.True(bar.Bounds.Height > 0);
        w.Close();
    }

    [AvaloniaFact]
    public void Dragging_A_Tab_Moves_Its_Page()
    {
        var (w, bar, view) = Show(3);
        var pages = view.Pages.ToList();
        var first = TabOf(bar, pages[0]);
        var start = Center(first, w);
        w.MouseDown(start, MouseButton.Left);
        w.MouseMove(start + new Point(60, 0), RawInputModifiers.LeftMouseButton);
        w.MouseMove(start + new Point(200, 0), RawInputModifiers.LeftMouseButton);
        Assert.True(first.IsDragging);
        w.MouseUp(start + new Point(200, 0), MouseButton.Left);
        Shots.Pump(w);
        Assert.False(first.IsDragging);
        Assert.Equal([pages[1], pages[0], pages[2]], view.Pages);
        w.Close();
    }

    [AvaloniaFact]
    public void A_Dragged_Tab_Covers_The_Tabs_Under_It()
    {
        var (w, bar, view) = Show(3);
        var first = TabOf(bar, view.Pages[0]);
        var start = Center(first, w);
        w.MouseDown(start, MouseButton.Left);
        w.MouseMove(start + new Point(30, 0), RawInputModifiers.LeftMouseButton);
        w.MouseMove(start + new Point(60, 0), RawInputModifiers.LeftMouseButton);
        Shots.Pump(w);

        // As snapshot_tabs: it draws over the others, which are cut away where it is; one that goes on past it shows from its end.
        var dragged = first.FindAncestorOfType<TabBoxChild>()!;
        var under = TabOf(bar, view.Pages[1]).FindAncestorOfType<TabBoxChild>()!;
        Assert.Equal(1, dragged.ZIndex);
        Assert.Equal(0, under.ZIndex);
        Assert.True(dragged.Bounds.Right > under.Bounds.Left);
        var visible = Assert.IsType<RectangleGeometry>(under.Clip).Rect;
        Assert.Equal(dragged.Bounds.Right - under.Bounds.Left, visible.Left, 3);
        Assert.Equal(under.Bounds.Right - dragged.Bounds.Right, visible.Width, 3);

        w.MouseUp(start + new Point(60, 0), MouseButton.Left);
        Shots.Pump(w);
        Assert.Null(under.Clip);
        Assert.Equal(0, dragged.ZIndex);
        w.Close();
    }

    [AvaloniaFact]
    public void Tabs_Slide_Aside_For_A_Dragged_Tab_And_It_Slides_Into_Place()
    {
        var (w, bar, view) = Show(3);
        var pages = view.Pages.ToList();
        Animate(w);
        var first = TabOf(bar, pages[0]).FindAncestorOfType<TabBoxChild>()!;
        var second = TabOf(bar, pages[1]).FindAncestorOfType<TabBoxChild>()!;
        var (firstSlot, secondSlot) = (first.Bounds.X, second.Bounds.X);
        var start = Center(first, w);
        w.MouseDown(start, MouseButton.Left);
        w.MouseMove(start + new Point(30, 0), RawInputModifiers.LeftMouseButton);
        w.MouseMove(start + new Point(first.Bounds.Width * 0.6, 0), RawInputModifiers.LeftMouseButton);

        // update_drag_reodering: the tab it passed slides into its place; the pages move only on the drop.
        var (aside, slid) = Until(() => second.Bounds.X <= firstSlot, () => second.Bounds.X > firstSlot && second.Bounds.X < secondSlot);
        Assert.True(aside);
        Assert.True(slid, "the tab jumped aside instead of sliding");
        Assert.Equal(firstSlot, second.Bounds.X);
        Assert.Equal(pages, view.Pages);

        // end_drag_reodering: the dropped tab slides on from where it was let go to its place.
        w.MouseUp(start + new Point(first.Bounds.Width * 0.6, 0), MouseButton.Left);
        Assert.Equal([pages[1], pages[0], pages[2]], view.Pages);
        var dropped = first.Bounds.X;
        Assert.NotEqual(secondSlot, dropped);
        Assert.True(Until(() => first.Bounds.X == secondSlot && first.ZIndex == 0, () => false).Done);
        Assert.Null(second.Clip);
        w.Close();
    }

    [AvaloniaFact]
    public void A_Reordered_Page_Slides_Its_Tab_Over()
    {
        var (w, bar, view) = Show(3);
        var pages = view.Pages.ToList();
        Animate(w);
        var first = TabOf(bar, pages[0]).FindAncestorOfType<TabBoxChild>()!;
        var third = TabOf(bar, pages[2]).FindAncestorOfType<TabBoxChild>()!;
        var (firstSlot, thirdSlot) = (first.Bounds.X, third.Bounds.X);

        // page_reordered_cb: the tab slides to its place and those it passes slide the other way.
        view.ReorderLast(pages[0]);
        Assert.Equal([pages[1], pages[2], pages[0]], view.Pages);
        var (placed, slid) = Until(
            () => first.Bounds.X == thirdSlot && first.ZIndex == 0,
            () => first.Bounds.X > firstSlot && first.Bounds.X < thirdSlot);
        Assert.True(placed);
        Assert.True(slid, "the tab jumped instead of sliding");
        Assert.Equal(TabOf(bar, pages[1]).FindAncestorOfType<TabBoxChild>()!.Bounds.X, firstSlot);
        w.Close();
    }

    // Animations on in the window, once another test's settings change stops holding them back.
    private static void Animate(Window w)
    {
        AdwaitaEnvironment.ResumeTransitions(w);
        for (var i = 0; i < 100 && AdwaitaEnvironment.TransitionsSuppressedFor(w); i++)
        {
            Frames.Run(16);
        }
    }

    // Runs frames until done, for at most two seconds: whether it got there, and whether a frame on the way was between.
    private static (bool Done, bool SawBetween) Until(System.Func<bool> done, System.Func<bool> between)
    {
        var start = System.Environment.TickCount64;
        var sawBetween = false;
        while (!done() && System.Environment.TickCount64 - start < 2000)
        {
            Frames.Run(16);
            sawBetween |= between();
        }

        return (done(), sawBetween);
    }

    [AvaloniaFact]
    public void Context_Click_Sets_Up_And_Opens_The_Menu()
    {
        var (w, bar, view) = Show(2);
        TabPage? setUp = null;
        view.SetupMenu += (_, e) => setUp ??= e.Page;
        view.Menu = new MenuFlyout { Items = { new MenuItem { Header = "Close" } } };
        var tab = TabOf(bar, view.Pages[1]);
        w.MouseDown(Center(tab, w), MouseButton.Right);
        w.MouseUp(Center(tab, w), MouseButton.Right);
        Shots.Pump(w);
        Assert.Same(view.Pages[1], setUp);
        Assert.True(view.Menu.IsOpen);
        view.Menu.Hide();
        w.Close();
    }
}
