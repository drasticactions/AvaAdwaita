using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using AvaAdwaita.Controls;
using AvaAdwaita.Presenters;
using Xunit;

namespace AvaAdwaita.Theme.Tests;

/// <summary>AvaAdwaita.Controls' WrapBox: lines of children, aligned, justified, reversed and expanded.</summary>
public class WrapBoxTests
{
    private static (Window, WrapBox) Show(double width, params double[] sizes)
    {
        var box = new WrapBox { ChildSpacing = 6, LineSpacing = 4, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Width = width };
        foreach (var s in sizes)
        {
            box.Children.Add(new Border { MinWidth = s, Height = 20 });
        }

        return (Shots.Show(box, 400, 300), box);
    }

    private static Rect[] Rects(WrapBox box) => box.Children.Select(c => c.Bounds).ToArray();

    [AvaloniaFact]
    public void Children_Wrap_When_The_Line_Is_Full()
    {
        var (w, box) = Show(100, 40, 40, 40, 90);
        Assert.Equal([new Rect(0, 0, 40, 20), new Rect(46, 0, 40, 20), new Rect(0, 24, 40, 20), new Rect(0, 48, 90, 20)], Rects(box));
        Assert.Equal(68, box.Bounds.Height);
        w.Close();
    }

    [AvaloniaFact]
    public void Align_Places_Short_Lines()
    {
        var (w, box) = Show(100, 40, 40);
        box.Align = 1;
        Shots.Pump(w);
        Assert.Equal([new Rect(14, 0, 40, 20), new Rect(60, 0, 40, 20)], Rects(box));
        w.Close();
    }

    [AvaloniaFact]
    public void Justify_Fills_Every_Line_But_The_Last()
    {
        var (w, box) = Show(100, 40, 40, 40);
        box.Justify = JustifyMode.Fill;
        Shots.Pump(w);
        Assert.Equal([new Rect(0, 0, 47, 20), new Rect(53, 0, 47, 20), new Rect(0, 24, 40, 20)], Rects(box));

        box.Justify = JustifyMode.Spread;
        box.JustifyLastLine = true;
        Shots.Pump(w);

        // A child alone on a spread line stretches.
        Assert.Equal([new Rect(0, 0, 40, 20), new Rect(60, 0, 40, 20), new Rect(0, 24, 100, 20)], Rects(box));
        w.Close();
    }

    [AvaloniaFact]
    public void Expanding_Children_Take_The_Spare_Room()
    {
        var (w, box) = Show(100, 40, 40);
        AdwStack.SetExpand(box.Children[1], true);
        Shots.Pump(w);
        Assert.Equal([new Rect(0, 0, 40, 20), new Rect(46, 0, 54, 20)], Rects(box));
        w.Close();
    }

    [AvaloniaFact]
    public void Reversed_Lines_And_Packing()
    {
        // Lines go upwards, and each line's children right to left within the line's place.
        var (w, box) = Show(100, 40, 40, 40);
        box.WrapReverse = true;
        box.PackDirection = PackDirection.EndToStart;
        Shots.Pump(w);
        Assert.Equal([new Rect(46, 24, 40, 20), new Rect(0, 24, 40, 20), new Rect(0, 0, 40, 20)], Rects(box));
        w.Close();
    }

    [AvaloniaFact]
    public void Natural_Line_Length_Sets_The_Natural_Width()
    {
        var (w, box) = Show(double.NaN, 40, 40, 40);
        Assert.Equal(132, box.Bounds.Width);
        box.NaturalLineLength = 90;
        Shots.Pump(w);
        Assert.Equal(90, box.Bounds.Width);
        Assert.Equal(44, box.Bounds.Height);
        w.Close();
    }
}
