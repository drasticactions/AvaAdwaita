using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaAdwaita.Nodes;
using Xunit;

namespace AvaAdwaita.Theme.Tests;

/// <summary>A scroll viewer's indicators and undershoots behave as GtkScrolledWindow's.</summary>
public class ScrollViewerTests
{
    private static (ScrollViewer View, Window Window) Show(bool overlay = true)
    {
        var sv = new ScrollViewer { Width = 200, Height = 120, AllowAutoHide = overlay, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, Content = new Border { Width = 400, Height = 300 } };
        var w = Shots.Show(sv);
        return (sv, w);
    }

    private static ScrollBar[] Bars(ScrollViewer sv) => sv.GetVisualDescendants().OfType<ScrollBar>().ToArray();

    private static Control Undershoot(ScrollViewer sv, string side) =>
        sv.GetVisualDescendants().OfType<Control>().Single(c => Css.GetCssNode(c) is { Name: "undershoot" } n && n.HasClass(side));

    [AvaloniaFact]
    public void Overlay_Indicators_Hide_At_Rest_And_Show_When_Scrolled()
    {
        var (sv, w) = Show();
        AdwaitaEnvironment.SuppressTransitions(w);
        try
        {
            Assert.All(Bars(sv), b => Assert.Equal(0, b.Opacity));
            sv.Offset = new Vector(0, 50);
            Dispatcher.UIThread.RunJobs();
            Assert.All(Bars(sv), b => Assert.Equal(1, b.Opacity));
        }
        finally
        {
            AdwaitaEnvironment.ResumeTransitions(w);
            w.Close();
        }
    }

    [AvaloniaFact]
    public void Classic_Scroll_Bars_Always_Show_Beside_The_Content()
    {
        var (sv, w) = Show(overlay: false);
        Assert.All(Bars(sv), b => Assert.Equal(1, b.Opacity));
        var viewport = sv.GetVisualDescendants().OfType<Control>().Single(c => Css.GetCssNode(c)?.Name == "viewport");
        Assert.True(viewport.Bounds.Width < 200 && viewport.Bounds.Height < 120);
        w.Close();
    }

    [AvaloniaFact]
    public void Undershoots_Show_Where_There_Is_More_Content()
    {
        var (sv, w) = Show();
        Assert.False(Undershoot(sv, "top").IsVisible);
        Assert.False(Undershoot(sv, "left").IsVisible);
        Assert.True(Undershoot(sv, "bottom").IsVisible);
        Assert.True(Undershoot(sv, "right").IsVisible);

        sv.Offset = new Vector(sv.Extent.Width - sv.Viewport.Width, sv.Extent.Height - sv.Viewport.Height);
        Dispatcher.UIThread.RunJobs();
        Assert.True(Undershoot(sv, "top").IsVisible);
        Assert.True(Undershoot(sv, "left").IsVisible);
        Assert.False(Undershoot(sv, "bottom").IsVisible);
        Assert.False(Undershoot(sv, "right").IsVisible);
        Assert.Equal(40, Undershoot(sv, "top").Bounds.Height);
        w.Close();
    }
}
