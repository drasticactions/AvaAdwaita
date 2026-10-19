using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using AvaAdwaita.Chrome;
using AvaAdwaita.Controls;
using AvaAdwaita.Nodes;
using Xunit;

namespace AvaAdwaita.Theme.Tests;

/// <summary>AvaAdwaita.Controls' BottomSheet: opening and closing, its bottom bar, its heights and header bars in it.</summary>
public class BottomSheetTests
{
    private static Point Center(Visual v, Visual root) => v.TranslatePoint(new Point(v.Bounds.Width / 2, v.Bounds.Height / 2), root)!.Value;

    private static (Window, BottomSheet) Show(bool bar)
    {
        var sheet = new BottomSheet
        {
            Content = new Button { Content = "Content" },
            Sheet = new ToolbarView { TopBars = { new HeaderBar() }, Content = new TextBox() },
            BottomBar = bar ? new Label { Content = "Now Playing" } : null,
        };
        var w = Shots.Show(sheet, 400, 300);
        AdwaitaEnvironment.SuppressTransitions(w);
        return (w, sheet);
    }

    [AvaloniaFact]
    public void Bottom_Bar_Opens_The_Sheet_And_The_Dimming_Closes_It()
    {
        var (w, sheet) = Show(bar: true);
        var node = sheet.SheetNode;
        Assert.True(node.IsVisible);
        Assert.Contains("bottom-bar", Css.GetCssNode(node)!.Classes);
        Assert.Equal(sheet.Bounds.Height - node.Bounds.Top, sheet.BottomBarHeight);
        Assert.Equal(sheet.BottomBarHeight, sheet.SheetHeight);

        var button = node.GetVisualDescendants().OfType<Button>().First(b => b.Content is Label);
        w.MouseDown(Center(button, w), MouseButton.Left);
        w.MouseUp(Center(button, w), MouseButton.Left);
        Shots.Pump(w);
        Assert.True(sheet.IsOpen);
        Assert.DoesNotContain("bottom-bar", Css.GetCssNode(node)!.Classes);
        Assert.Equal(sheet.Bounds.Height - node.Bounds.Top, sheet.SheetHeight);
        Assert.True(sheet.SheetHeight > sheet.BottomBarHeight);

        // The dimmed content above the sheet closes it.
        var above = sheet.TranslatePoint(new Point(sheet.Bounds.Width / 2, 10), w)!.Value;
        w.MouseDown(above, MouseButton.Left);
        w.MouseUp(above, MouseButton.Left);
        Shots.Pump(w);
        Assert.False(sheet.IsOpen);
        Assert.Contains("bottom-bar", Css.GetCssNode(node)!.Classes);

        // Hiding the bottom bar takes the sheet out of view.
        sheet.RevealBottomBar = false;
        Shots.Pump(w);
        Assert.Equal(0, sheet.BottomBarHeight);
        Assert.False(node.IsVisible);
        w.Close();
    }

    [AvaloniaFact]
    public void Escape_And_The_Close_Button_Close_Unless_It_Cannot()
    {
        var (w, sheet) = Show(bar: false);
        Assert.False(sheet.SheetNode.IsVisible);
        sheet.IsOpen = true;
        Shots.Pump(w);
        Assert.True(sheet.SheetNode.IsVisible);
        var textBox = sheet.GetVisualDescendants().OfType<TextBox>().Single();
        textBox.Focus();
        w.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);
        Shots.Pump(w);
        Assert.False(sheet.IsOpen);
        Assert.False(sheet.SheetNode.IsVisible);

        var attempts = 0;
        sheet.CloseAttempt += (_, _) => attempts++;
        sheet.CanClose = false;
        sheet.IsOpen = true;
        Shots.Pump(w);
        var bar = sheet.GetVisualDescendants().OfType<HeaderBar>().Single();
        var close = bar.GetVisualDescendants().OfType<WindowControls>().Single(c => !c.IsStart);
        Assert.Equal(["close"], close.Children.Select(b => b.Classes.First(c => !c.StartsWith(':'))));
        ((Button)close.Children[0]).RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Shots.Pump(w);
        Assert.Equal(1, attempts);
        Assert.True(sheet.IsOpen);

        sheet.CanClose = true;
        ((Button)close.Children[0]).RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Shots.Pump(w);
        Assert.False(sheet.IsOpen);
        w.Close();
    }

    [AvaloniaFact]
    public void Header_Bar_Shows_No_Title_Under_The_Drag_Handle()
    {
        var (w, sheet) = Show(bar: false);
        sheet.IsOpen = true;
        Shots.Pump(w);
        var bar = sheet.GetVisualDescendants().OfType<HeaderBar>().Single();
        Assert.Null(bar.WindowTitle);
        Assert.Contains("has-drag-handle", Css.GetCssNode(sheet.SheetNode)!.Classes);

        sheet.ShowDragHandle = false;
        Shots.Pump(w);
        Assert.Equal(w.Title, bar.WindowTitle);
        Assert.DoesNotContain("has-drag-handle", Css.GetCssNode(sheet.SheetNode)!.Classes);
        w.Close();
    }

    [AvaloniaFact]
    public void Narrow_Sheet_Sits_At_Its_Align()
    {
        var (w, sheet) = Show(bar: false);
        sheet.FullWidth = false;
        sheet.Align = 0;
        sheet.IsOpen = true;
        Shots.Pump(w);
        var node = sheet.SheetNode;
        Assert.Equal(0, node.Bounds.Left);
        Assert.True(node.Bounds.Width < sheet.Bounds.Width);
        Assert.Contains("flush-left", Css.GetCssNode(node)!.Classes);
        Assert.DoesNotContain("flush-right", Css.GetCssNode(node)!.Classes);
        w.Close();
    }

    [AvaloniaFact]
    public void Peer_Expands_And_Collapses()
    {
        var (w, sheet) = Show(bar: true);
        var peer = ControlAutomationPeer.CreatePeerForElement(sheet);
        var expand = peer.GetProvider<IExpandCollapseProvider>()!;
        Assert.Equal(ExpandCollapseState.Collapsed, expand.ExpandCollapseState);
        expand.Expand();
        Assert.True(sheet.IsOpen);
        expand.Collapse();
        Assert.False(sheet.IsOpen);
        w.Close();
    }

    private static void Drag(Window w, Point from, double dy)
    {
        w.MouseDown(from, MouseButton.Left);
        for (var i = 1; i <= 10; i++)
        {
            w.MouseMove(from + new Point(0, dy * i / 10));
        }

        w.MouseUp(from + new Point(0, dy), MouseButton.Left);
        Shots.Pump(w);
    }

    [AvaloniaFact]
    public void Dragging_The_Sheet_Down_Closes_It_And_A_Short_Drag_Goes_Back()
    {
        var (w, sheet) = Show(bar: false);
        sheet.IsOpen = true;
        Shots.Pump(w);
        var top = sheet.SheetNode.TranslatePoint(new Point(sheet.SheetNode.Bounds.Width / 2, 6), w)!.Value;

        // A drag shorter than half the sheet leaves it open.
        Drag(w, top, 30);
        Assert.True(sheet.IsOpen);
        Assert.Equal(sheet.Bounds.Height - sheet.SheetNode.Bounds.Top, sheet.SheetHeight);

        // Past halfway, it closes.
        Drag(w, top, sheet.SheetHeight * 0.75);
        Assert.False(sheet.IsOpen);
        w.Close();
    }

    [AvaloniaFact]
    public void Dragging_The_Bottom_Bar_Up_Opens_The_Sheet()
    {
        var (w, sheet) = Show(bar: true);
        // Where the bar shows, at the bottom of the sheet.
        var bar = sheet.TranslatePoint(new Point(sheet.Bounds.Width / 2, sheet.Bounds.Height - (sheet.BottomBarHeight / 2)), w)!.Value;
        Drag(w, bar, -200);
        Assert.True(sheet.IsOpen);
        w.Close();
    }

    [AvaloniaFact]
    public void Header_Bar_In_A_Draggable_Sheet_Does_Not_Move_The_Window()
    {
        // allow-window-handle: the sheet takes drags from its header bar while a pointer can drag it.
        var (w, sheet) = Show(bar: false);
        sheet.IsOpen = true;
        Shots.Pump(w);
        var handle = sheet.SheetNode.GetVisualDescendants().OfType<HeaderBarHandle>().First();
        Assert.Equal(WindowDecorationsElementRole.None, Avalonia.Controls.Chrome.WindowDecorationProperties.GetElementRole(handle));

        // Without the drag handle or a bottom bar, only touch drags the sheet, so the header bar moves the window.
        sheet.ShowDragHandle = false;
        Assert.Equal(WindowDecorationsElementRole.TitleBar, Avalonia.Controls.Chrome.WindowDecorationProperties.GetElementRole(handle));
        w.Close();
    }
}
