using System.Linq;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using AvaAdwaita.Controls;
using AvaAdwaita.Nodes;
using AvaAdwaita.Presenters;
using Xunit;

namespace AvaAdwaita.Theme.Tests;

/// <summary>AvaAdwaita.Controls' StatusPage: the parts it shows and the content under them.</summary>
public class StatusPageTests
{
    [AvaloniaFact]
    public void Page_Shows_Only_The_Parts_It_Has()
    {
        var page = new StatusPage { Title = "Empty Folder" };
        var w = Shots.Show(page, 480, 700);
        Assert.Equal("statuspage", Css.GetCssNode(page)!.Name);
        Assert.False(page.GetVisualDescendants().OfType<AdwIcon>().Single().IsVisible);

        var retry = new Button { Content = "Retry" };
        page.IconName = "folder-symbolic";
        page.Description = "Nothing here yet";
        page.Content = retry;
        Shots.Pump(w);
        var icon = page.GetVisualDescendants().OfType<AdwIcon>().Single();
        Assert.True(icon.IsVisible);
        Assert.Equal(128, icon.IconSize);
        Assert.True(retry.TranslatePoint(default, page)!.Value.Y > icon.TranslatePoint(default, page)!.Value.Y);

        // The page centers its column in the height it has.
        var column = page.GetVisualDescendants().OfType<AdwBox>().First(b => Css.GetCssNode(b)?.Name == "box");
        var top = column.TranslatePoint(default, page)!.Value.Y;
        Assert.Equal(page.Bounds.Height - top - column.Bounds.Height, top, tolerance: 1);

        var peer = ControlAutomationPeer.CreatePeerForElement(page);
        Assert.Equal("Empty Folder", peer.GetName());
        Assert.Equal("Nothing here yet", peer.GetHelpText());
        w.Close();
    }
}
