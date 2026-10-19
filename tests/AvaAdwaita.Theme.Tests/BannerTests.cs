using System.Linq;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaAdwaita.Controls;
using AvaAdwaita.Nodes;
using Xunit;

namespace AvaAdwaita.Theme.Tests;

/// <summary>AvaAdwaita.Controls' Revealer and Banner: revealing, the button's place and its click.</summary>
public class BannerTests
{
    [AvaloniaFact]
    public void Revealer_Slides_Its_Child_In()
    {
        var child = new Border { Height = 40 };
        var revealer = new Revealer { Child = child };
        var w = Shots.Show(new StackPanel { Children = { revealer } });
        Assert.Equal(0, revealer.Bounds.Height);
        Assert.False(child.IsVisible);

        // Another test's settings change can hold transitions back for a moment.
        for (var i = 0; i < 100 && AdwaitaEnvironment.TransitionsSuppressedFor(revealer); i++)
        {
            Frames.Run(16);
        }

        revealer.IsRevealed = true;
        var start = System.Environment.TickCount64;
        var sawMiddle = false;
        while (!revealer.IsChildRevealed && System.Environment.TickCount64 - start < 2000)
        {
            Frames.Run(16);
            sawMiddle |= revealer.Bounds.Height is > 0 and < 40;
        }

        Assert.True(revealer.IsChildRevealed);
        Assert.True(sawMiddle, "the revealer jumped instead of sliding");
        Assert.Equal(40, revealer.Bounds.Height);
        Assert.True(child.IsVisible);

        AdwaitaEnvironment.SuppressTransitions(w);
        revealer.IsRevealed = false;
        Shots.Pump(w);
        Assert.Equal(0, revealer.Bounds.Height);
        Assert.False(child.IsVisible);
        w.Close();
    }

    [AvaloniaFact]
    public void Button_Moves_Under_The_Title_When_Narrow()
    {
        var clicks = 0;
        var banner = new Banner { Title = "Saved to the cloud", ButtonLabel = "_Undo", IsRevealed = true, Width = 400 };
        banner.ButtonClicked += (_, _) => clicks++;
        var w = Shots.Show(banner);
        var title = banner.GetVisualDescendants().OfType<TextBlock>().First(t => t.Name == "PART_Title");
        var button = banner.GetVisualDescendants().OfType<Button>().Single();
        Assert.Equal(title.Bounds.Center.Y, button.Bounds.Center.Y, tolerance: 1);

        banner.Width = 200;
        Shots.Pump(w);
        Assert.True(button.Bounds.Top > title.Bounds.Bottom);

        button.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Assert.Equal(1, clicks);
        banner.ButtonStyle = BannerButtonStyle.Suggested;
        Assert.Contains("suggested-action", Css.GetCssNode(button)!.Classes);
        Assert.Equal("Saved to the cloud", ControlAutomationPeer.CreatePeerForElement(banner).GetName());
        w.Close();
    }
}
