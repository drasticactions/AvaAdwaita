using System.Linq;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using AvaAdwaita.Controls;
using AvaAdwaita.Nodes;
using Xunit;

namespace AvaAdwaita.Theme.Tests;

/// <summary>AvaAdwaita.Controls' Clamp and PreferencesPage: the clamped width and its classes, and the page's parts.</summary>
public class PreferencesPageTests
{
    [AvaloniaTheory]
    [InlineData(360, 360, "small")]
    [InlineData(500, 484, "medium")]
    [InlineData(1200, 600, "large")]
    public void Clamp_Narrows_Its_Child(double width, double childWidth, string size)
    {
        var child = new Border { Height = 20 };
        var clamp = new Clamp { Width = width, Child = child };
        var w = Shots.Show(clamp, 1300, 200);
        Assert.Equal(childWidth, child.Bounds.Width);
        Assert.Equal(System.Math.Floor((width - childWidth) / 2), child.Bounds.X);
        Assert.Contains(size, child.Classes);
        Assert.Equal("clamp", Css.GetCssNode(clamp)!.Name);
        w.Close();
    }

    [AvaloniaFact]
    public void Page_Scrolls_Its_Groups_Under_The_Banner()
    {
        var banner = new Border { Height = 40 };
        var group = new PreferencesGroup { Title = "General", Children = { new ActionRow { Title = "Row" } } };
        var page = new PreferencesPage { Title = "Appearance", Description = "About the page", Banner = banner, Children = { group } };
        var w = Shots.Show(page, 640, 400);
        Assert.Equal("preferencespage", Css.GetCssNode(page)!.Name);
        Assert.Equal(0, banner.Bounds.Y);
        Assert.NotNull(group.FindAncestorOfType<ScrollViewer>());
        Assert.Null(banner.FindAncestorOfType<ScrollViewer>());
        Assert.True(group.Bounds.Width <= 600);
        Assert.Equal("Appearance", ControlAutomationPeer.CreatePeerForElement(page).GetName());

        // The parity check leaves text positions to Pango and Skia, so the centering is checked here.
        var description = page.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Name == "PART_Description");
        Assert.Equal(Avalonia.Media.TextAlignment.Start, description.TextAlignment);
        page.DescriptionCentered = true;
        Assert.Equal(Avalonia.Media.TextAlignment.Center, description.TextAlignment);
        w.Close();
    }
}
