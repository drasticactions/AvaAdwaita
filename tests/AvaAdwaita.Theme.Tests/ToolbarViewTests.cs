using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using AvaAdwaita.Chrome;
using AvaAdwaita.Controls;
using AvaAdwaita.Nodes;
using Xunit;

namespace AvaAdwaita.Theme.Tests;

/// <summary>AvaAdwaita.Controls' ToolbarView: its bars, the undershoot, extending content, and the window's header bar.</summary>
public class ToolbarViewTests
{
    [AvaloniaFact]
    public void Content_Sits_Between_The_Bars()
    {
        var content = new Border();
        var view = new ToolbarView
        {
            Content = content,
            TopBars = { new HeaderBar { ShowEndTitleButtons = false } },
            BottomBars = { new ActionBar { Start = { new Button { Content = "Action" } } } },
        };
        var w = Shots.Show(view, 480, 400);
        AdwaitaEnvironment.SuppressTransitions(w);
        Assert.Equal(view.TopBarHeight, content.TranslatePoint(default, view)!.Value.Y);
        Assert.Equal(view.Bounds.Height - view.TopBarHeight - view.BottomBarHeight, content.Bounds.Height);
        Assert.Contains("undershoot-top", Css.GetCssNode(view)!.Classes);

        view.TopBarStyle = ToolbarStyle.Raised;
        view.ExtendContentToBottomEdge = true;
        Shots.Pump(w);
        Assert.DoesNotContain("undershoot-top", Css.GetCssNode(view)!.Classes);
        Assert.DoesNotContain("undershoot-bottom", Css.GetCssNode(view)!.Classes);
        Assert.Equal(view.Bounds.Height - view.TopBarHeight, content.Bounds.Height);

        view.RevealTopBars = false;
        Shots.Pump(w);
        Assert.Equal(0, view.TopBarHeight);
        Assert.Equal(0, content.TranslatePoint(default, view)!.Value.Y);
        w.Close();
    }

    [AvaloniaFact]
    public void As_Window_Content_It_Replaces_The_Window_Header_Bar()
    {
        var w = new Window { Width = 480, Height = 320, Content = new ToolbarView { TopBars = { new HeaderBar() } } };
        w.Show();
        Shots.Pump(w);
        Assert.False(WindowHeaderBar.GetIsVisible(w));
        var headerBars = w.GetVisualDescendants().OfType<Control>().Count(c => Css.GetCssNode(c)?.Name == "headerbar" && c.IsEffectivelyVisible);
        Assert.Equal(1, headerBars);

        w.Content = new Border();
        Shots.Pump(w);
        Assert.True(WindowHeaderBar.GetIsVisible(w));
        w.Close();
    }
}
