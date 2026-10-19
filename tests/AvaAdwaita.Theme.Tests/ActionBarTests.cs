using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using AvaAdwaita.Controls;
using Xunit;

namespace AvaAdwaita.Theme.Tests;

/// <summary>AvaAdwaita.Controls' ActionBar: packing and revealing.</summary>
public class ActionBarTests
{
    [AvaloniaFact]
    public void Widgets_Pack_From_Their_Ends()
    {
        var start1 = new Button { Content = "A" };
        var start2 = new Button { Content = "B" };
        var end1 = new Button { Content = "Y" };
        var end2 = new Button { Content = "Z" };
        var center = new Label { Content = "Center" };
        var bar = new ActionBar { Start = { start1, start2 }, End = { end1, end2 }, CenterWidget = center };
        var w = Shots.Show(bar, 480, 200);
        AdwaitaEnvironment.SuppressTransitions(w);
        double X(Visual v) => v.TranslatePoint(default, bar)!.Value.X;
        Assert.True(X(start1) < X(start2));
        Assert.True(X(end2) < X(end1));
        Assert.InRange(X(center) + (center.Bounds.Width / 2) - (bar.Bounds.Width / 2), -1, 1);
        Assert.Equal(AutomationControlType.ToolBar, ControlAutomationPeer.CreatePeerForElement(bar).GetAutomationControlType());

        bar.IsRevealed = false;
        Shots.Pump(w);
        Assert.Equal(0, bar.DesiredSize.Height);
        w.Close();
    }
}
