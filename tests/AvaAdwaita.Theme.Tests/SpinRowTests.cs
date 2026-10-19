using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.VisualTree;
using AvaAdwaita.Controls;
using AvaAdwaita.Nodes;
using Xunit;

namespace AvaAdwaita.Theme.Tests;

/// <summary>AvaAdwaita.Controls' SpinRow: the spin button takes the row's width and follows the row's value.</summary>
public class SpinRowTests
{
    [AvaloniaFact]
    public void Spin_Button_Fills_The_Row_And_Follows_The_Value()
    {
        var row = new SpinRow { Title = "Size", Minimum = 0, Maximum = 100, Increment = 5, Value = 40 };
        var w = Shots.Show(new ListBox { Classes = { "boxed-list" }, Width = 360, Items = { row } });
        Assert.Contains("spin", Css.GetCssNode(row)!.Classes);
        Assert.Contains("activatable", Css.GetCssNode(row)!.Classes);
        Assert.Equal(40m, row.SpinButton.Value);
        Assert.True(row.SpinButton.Bounds.Width > 250, $"spin button {row.SpinButton.Bounds.Width} wide");

        row.Value = 55;
        Assert.Equal(55m, row.SpinButton.Value);
        row.SpinButton.Value = 60;
        Assert.Equal(60m, row.Value);

        row.Activate();
        Shots.Pump(w);
        Assert.True(row.SpinButton.IsKeyboardFocusWithin);
        w.KeyPressQwerty(PhysicalKey.ArrowUp, RawInputModifiers.None);
        Assert.Equal(65m, row.Value);
        w.Close();
    }

    [AvaloniaFact]
    public void Value_Shows_The_Digits_Whatever_The_Decimal_Scale()
    {
        var row = new SpinRow { Title = "Copies", Minimum = 1, Maximum = 99, Increment = 1, Value = 1 };
        var w = Shots.Show(new ListBox { Classes = { "boxed-list" }, Width = 360, Items = { row } });
        row.SpinButton.Focus();
        w.KeyPressQwerty(PhysicalKey.ArrowUp, RawInputModifiers.None);
        Shots.Pump(w);
        Assert.Equal(2m, row.Value);
        Assert.Equal("2", row.SpinButton.Text);

        row.Digits = 2;
        Assert.Equal("2.00", row.SpinButton.Text);
        row.FormatString = "0.0";
        Assert.Equal("2.0", row.SpinButton.Text);
        w.Close();
    }

    [AvaloniaFact]
    public void Its_Buttons_Do_Not_Activate_The_Row()
    {
        var row = new SpinRow { Title = "Copies", Minimum = 1, Maximum = 99, Increment = 1, Value = 1 };
        var w = Shots.Show(new ListBox { Classes = { "boxed-list" }, Width = 360, Items = { row } });
        var activations = 0;
        row.Activated += (_, _) => activations++;
        var plus = row.SpinButton.GetVisualDescendants().OfType<RepeatButton>().Last();
        var center = plus.TranslatePoint(new Point(plus.Bounds.Width / 2, plus.Bounds.Height / 2), w)!.Value;
        w.MouseDown(center, MouseButton.Left);
        w.MouseUp(center, MouseButton.Left);
        Shots.Pump(w);
        Assert.Equal(2m, row.Value);
        Assert.Equal(0, activations);

        // A click on the row itself activates it.
        var title = row.TranslatePoint(new Point(20, row.Bounds.Height / 2), w)!.Value;
        w.MouseDown(title, MouseButton.Left);
        w.MouseUp(title, MouseButton.Left);
        Assert.Equal(1, activations);
        w.Close();
    }
}
