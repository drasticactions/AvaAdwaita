using System.Linq;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using AvaAdwaita.Chrome;
using AvaAdwaita.Controls;
using AvaAdwaita.Presenters;
using Xunit;

namespace AvaAdwaita.Theme.Tests;

/// <summary>AvaAdwaita.Controls' HeaderBar: packing, the window's title and the window buttons.</summary>
public class HeaderBarTests
{
    [AvaloniaFact]
    public void End_Widgets_Pack_From_The_End()
    {
        var first = new Button { Content = "First" };
        var second = new Button { Content = "Second" };
        var bar = new HeaderBar { End = { first, second } };
        var w = Shots.Show(bar, 480, 200, "Files");
        Assert.True(second.TranslatePoint(default, bar)!.Value.X < first.TranslatePoint(default, bar)!.Value.X);
        var title = bar.GetVisualDescendants().OfType<AdwLabel>().Single(l => l.IsVisible && Nodes.Css.GetCssNode(l)?.Classes.Contains("title") == true);
        Assert.Equal("Files", title.Text);
        w.Title = "Pictures";
        Assert.Equal("Pictures", title.Text);
        Assert.Equal(AutomationControlType.TitleBar, ControlAutomationPeer.CreatePeerForElement(bar).GetAutomationControlType());
        w.Close();
    }

    [AvaloniaFact]
    public void Window_Buttons_Hide_On_Request()
    {
        var bar = new HeaderBar();
        var w = Shots.Show(bar);
        var end = bar.GetVisualDescendants().OfType<WindowControls>().Single(c => !c.IsStart);
        Assert.NotEmpty(end.Children);
        bar.ShowEndTitleButtons = false;
        Assert.Empty(end.Children);
        w.Close();
    }
}
