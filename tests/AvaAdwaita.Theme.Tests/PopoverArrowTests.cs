using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using AvaAdwaita.Chrome;
using AvaAdwaita.Presenters;
using Xunit;

namespace AvaAdwaita.Theme.Tests;

/// <summary>A popover's arrow and the header bar it often hangs from.</summary>
public class PopoverArrowTests
{
    private static PopoverArrow Arrow(double tip)
    {
        var arrow = new PopoverArrow();
        arrow.Measure(new Size(300, 200));
        arrow.Arrange(new Rect(0, 0, 300, 200));
        arrow.Update(PopoverSide.Top, tip, new Thickness(15), 15, new Thickness(1));
        return arrow;
    }

    [AvaloniaFact]
    public void The_Arrow_Points_At_The_Anchor_From_The_Middle_Of_Its_Base()
    {
        var (initial, tip, final) = Arrow(150).Gap()!.Value;
        Assert.Equal(new Point(150, 15), tip);
        Assert.Equal(138, initial.X);
        Assert.Equal(162, final.X);
    }

    [AvaloniaFact]
    public void Near_A_Corner_The_Base_Stays_On_The_Contents()
    {
        // The anchor is 10 px from the contents' right edge, inside its 15 px corner: the base stays left of the
        // corner, on the contents, and the tail leans to the tip.
        var (initial, tip, final) = Arrow(275).Gap()!.Value;
        var contentsRight = 300 - 15;
        Assert.Equal(275, tip.X);
        Assert.True(final.X <= contentsRight - 15, $"base ends at {final.X}");
        Assert.Equal(24, final.X - initial.X);
    }

    [AvaloniaFact]
    public void Header_Bar_Buttons_Sit_Six_Pixels_From_The_Window_Controls()
    {
        // AdwHeaderBar's end box is gtk_box_new (GTK_ORIENTATION_HORIZONTAL, 6), and GtkBox adds its CSS
        // border-spacing to that.
        var window = new Window { Width = 600, Height = 300 };
        WindowHeaderBar.SetEnd(window, new Button { Content = "Menu" });
        window.Show();
        Shots.Pump(window);
        var end = window.GetVisualDescendants().OfType<AdwStack>().Single(s => s.Children.OfType<WindowControls>().Any(c => !c.IsStart));
        Assert.Equal(6, end.BoxSpacing);
        Assert.Equal(6 + (Nodes.Css.GetCssNode(end)?.Style?.Size.BorderSpacing.X.Value ?? 0), end.Spacing);
        window.Close();
    }
}
