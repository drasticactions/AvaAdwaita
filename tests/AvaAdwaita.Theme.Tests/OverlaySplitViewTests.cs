using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using AvaAdwaita.Controls;
using AvaAdwaita.Nodes;
using Xunit;

namespace AvaAdwaita.Theme.Tests;

/// <summary>AvaAdwaita.Controls' OverlaySplitView: the sidebar's width, collapsing, and hiding a sidebar over the content.</summary>
public class OverlaySplitViewTests
{
    [AvaloniaFact]
    public void Expanded_Sidebar_Takes_A_Share_Of_The_Width()
    {
        var sidebar = new Border();
        var content = new Border();
        var view = new OverlaySplitView { Sidebar = sidebar, Content = content, Width = 1000, Height = 300 };
        var w = Shots.Show(view, 1100, 400);
        AdwaitaEnvironment.SuppressTransitions(w);

        // A quarter of 1000, within 180 to 280.
        Assert.Equal(250, sidebar.Bounds.Width);
        Assert.Equal(750, content.Bounds.Width);
        view.ShowSidebar = false;
        Shots.Pump(w);
        Assert.Equal(1000, content.Bounds.Width);
        Assert.False(sidebar.IsEffectivelyVisible);
        w.Close();
    }

    [AvaloniaFact]
    public void Collapsed_Sidebar_Hides_On_Click_And_Escape()
    {
        var sidebar = new Button { Content = "In the sidebar" };
        var view = new OverlaySplitView { Sidebar = sidebar, Content = new Border(), Width = 500, Height = 300 };
        var w = Shots.Show(view, 600, 400);
        AdwaitaEnvironment.SuppressTransitions(w);

        view.IsCollapsed = true;
        Shots.Pump(w);
        Assert.False(view.ShowSidebar);
        view.ShowSidebar = true;
        Shots.Pump(w);
        Assert.Contains("overlay-pane", Css.GetCssNode(sidebar.Parent!.Parent as StyledElement ?? sidebar)?.Classes ?? []);
        var right = view.TranslatePoint(new Point(450, 150), w)!.Value;
        w.MouseDown(right, MouseButton.Left);
        w.MouseUp(right, MouseButton.Left);
        Assert.False(view.ShowSidebar);

        view.ShowSidebar = true;
        Shots.Pump(w);
        sidebar.Focus();
        w.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);
        Assert.False(view.ShowSidebar);

        view.PinSidebar = true;
        view.ShowSidebar = true;
        view.IsCollapsed = false;
        view.IsCollapsed = true;
        Assert.True(view.ShowSidebar);
        w.Close();
    }
}
