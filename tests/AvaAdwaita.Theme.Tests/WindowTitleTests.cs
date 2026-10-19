using System.Linq;
using Avalonia.Automation.Peers;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using AvaAdwaita.Controls;
using AvaAdwaita.Presenters;
using Xunit;

namespace AvaAdwaita.Theme.Tests;

/// <summary>AvaAdwaita.Controls' WindowTitle: its lines and its minimum width.</summary>
public class WindowTitleTests
{
    [AvaloniaFact]
    public void Title_Keeps_Five_Characters_And_Shows_The_Subtitle_When_Set()
    {
        var title = new WindowTitle { Title = "A", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left };
        var w = Shots.Show(title);
        var labels = title.GetVisualDescendants().OfType<AdwLabel>().ToList();
        Assert.False(labels[1].IsVisible);

        // width-chars 5 in the bold title font: five 10 px digits and the 12 px paddings, as GTK's 74.
        Assert.Equal(74, title.Bounds.Width);

        title.Subtitle = "Subtitle";
        Shots.Pump(w);
        Assert.True(labels[1].IsVisible);
        Assert.Equal("A", ControlAutomationPeer.CreatePeerForElement(title).GetName());
        w.Close();
    }
}
