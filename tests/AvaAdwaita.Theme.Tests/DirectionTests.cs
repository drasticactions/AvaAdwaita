using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using AvaAdwaita.CssEngine.Computed;
using AvaAdwaita.Nodes;
using Xunit;

namespace AvaAdwaita.Theme.Tests;

/// <summary>Right-to-left layout, which Avalonia mirrors and GTK styles with :dir(rtl).</summary>
public class DirectionTests
{
    private static StackPanel Linked(FlowDirection direction)
    {
        var box = new StackPanel { Orientation = Orientation.Horizontal, FlowDirection = direction, HorizontalAlignment = HorizontalAlignment.Left };
        Css.SetNode(box, "box");
        Css.SetClasses(box, "linked horizontal");
        box.Children.Add(new Button { Content = "First" });
        box.Children.Add(new Button { Content = "Last" });
        return box;
    }

    [AvaloniaFact]
    public void Right_To_Left_Draws_The_Style_Mirrored()
    {
        var box = Linked(FlowDirection.RightToLeft);
        Shots.Show(box);
        var first = (Button)box.Children[0];
        var computed = Css.GetCssNode(first)!.Style!;
        var drawn = first.GetValue(Css.StyleProperty)!;

        // .linked > button:dir(rtl):not(:last-child) squares the left corners: the screen's left, where the next
        // button is. Avalonia lays the button out left to right and flips it, so the drawn style squares its right.
        Assert.Equal(0, computed.Radii.TopLeft.X.Resolve(100));
        Assert.NotEqual(0, computed.Radii.TopRight.X.Resolve(100));
        Assert.Equal(computed.Radii.TopLeft, drawn.Radii.TopRight);
        Assert.Equal(computed.Radii.TopRight, drawn.Radii.TopLeft);
        Assert.Equal(computed.Margin.Left, drawn.Margin.Right);
        Assert.Equal(computed.Margin.Right, drawn.Margin.Left);
    }

    [AvaloniaFact]
    public void Left_To_Right_Draws_The_Computed_Style()
    {
        var box = Linked(FlowDirection.LeftToRight);
        Shots.Show(box);
        var first = (Button)box.Children[0];
        Assert.Same(Css.GetCssNode(first)!.Style, first.GetValue(Css.StyleProperty));
    }

    [AvaloniaFact]
    public void Changing_Direction_Redraws_Mirrored()
    {
        var box = Linked(FlowDirection.LeftToRight);
        var w = Shots.Show(box);
        var first = (Button)box.Children[0];
        box.FlowDirection = FlowDirection.RightToLeft;
        Shots.Pump(w);
        Assert.Equal(Css.GetCssNode(first)!.Style!.Radii.TopLeft, first.GetValue(Css.StyleProperty)!.Radii.TopRight);
    }

    [AvaloniaFact]
    public void Mirroring_Swaps_Sides_And_Flips_Offsets()
    {
        var styles = new CssEngine.Cascade.CssStyleSet();
        styles.Add(CssEngine.Cascade.CssStyleSheet.Parse(
            "box { margin-left: 3px; padding-right: 5px; border-left: 2px solid red; border-top-left-radius: 7px; box-shadow: 4px 1px 2px black; background-image: linear-gradient(to right, red, blue); }",
            CssEngine.Cascade.CssOrigin.Application, "test"));
        var engine = new CssStyleEngine(styles);
        var node = new CssNode("box");
        engine.AddRoot(node);
        var style = engine.Resolve(node);
        var mirrored = style.Mirrored();
        Assert.Equal(3, mirrored.Margin.Right);
        Assert.Equal(0, mirrored.Margin.Left);
        Assert.Equal(5, mirrored.Padding.Left);
        Assert.Equal(2, mirrored.BorderWidths.Right);
        Assert.Equal(style.Radii.TopLeft, mirrored.Radii.TopRight);
        Assert.Equal(-4, mirrored.Background.BoxShadow[0].X);
        Assert.Equal(CssEngine.Values.CssLinearGradient.Left, ((CssEngine.Values.CssLinearGradient)mirrored.Background.Image[0]!).Side);
        Assert.Same(mirrored, style.Mirrored());
    }

    [AvaloniaFact]
    public void Labels_Start_At_The_Reading_Side()
    {
        // GtkLabel's xalign flips right to left; a stretched label's text sits at the right.
        var label = new Label { Content = "Heading", HorizontalAlignment = HorizontalAlignment.Stretch, FlowDirection = FlowDirection.RightToLeft };
        Shots.Show(label);
        var text = label.GetVisualDescendants().OfType<Presenters.AdwLabel>().Single();
        Assert.Equal(TextAlignment.Start, text.TextAlignment);
    }

    [AvaloniaFact]
    public void Menu_Indicators_Are_Named_By_Their_Side()
    {
        foreach (var (direction, check, arrow) in new[] { (FlowDirection.LeftToRight, "left", "right"), (FlowDirection.RightToLeft, "right", "left") })
        {
            var toggle = new MenuItem { Header = "Toggle", ToggleType = MenuItemToggleType.CheckBox, IsChecked = true };
            var sub = new MenuItem { Header = "More" };
            sub.Items.Add(new MenuItem { Header = "Item" });
            var flyout = new MenuFlyout { Items = { toggle, sub } };
            var button = new Button { Content = "Menu", Flyout = flyout, FlowDirection = direction };
            var w = Shots.Show(button);
            flyout.ShowAt(button);
            Shots.Pump(w);
            var checkNode = Css.GetCssNode(toggle.GetVisualDescendants().OfType<Control>().Single(c => c.Name == "PART_Check"))!;
            var arrowNode = Css.GetCssNode(sub.GetVisualDescendants().OfType<Control>().Single(c => c.Name == "PART_Arrow"))!;
            Assert.Contains(check, checkNode.Classes);
            Assert.Contains(arrow, arrowNode.Classes);
            flyout.Hide();
            w.Close();
        }
    }
}
