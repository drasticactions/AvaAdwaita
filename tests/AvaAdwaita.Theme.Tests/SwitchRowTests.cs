using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using AvaAdwaita.Controls;
using AvaAdwaita.Nodes;
using Xunit;

namespace AvaAdwaita.Theme.Tests;

/// <summary>AvaAdwaita.Controls' SwitchRow: the switch is the first suffix, and activating the row toggles it.</summary>
public class SwitchRowTests
{
    private static Point Center(Visual v, Visual root) => v.TranslatePoint(new Point(v.Bounds.Width / 2, v.Bounds.Height / 2), root)!.Value;

    private static void Click(Window w, Point p)
    {
        w.MouseDown(p, MouseButton.Left);
        w.MouseUp(p, MouseButton.Left);
        Shots.Pump(w);
    }

    [AvaloniaFact]
    public void Row_And_Switch_Toggle_Once()
    {
        var changes = 0;
        var row = new SwitchRow { Title = "Wi-Fi", Suffixes = { new Button { Content = "Edit" } } };
        row.IsCheckedChanged += (_, _) => changes++;
        var w = Shots.Show(new ListBox { Classes = { "boxed-list" }, Width = 360, Items = { row } });
        Assert.Contains("activatable", Css.GetCssNode(row)!.Classes);
        Assert.Same(row.Switch, row.Suffixes[0]);

        Click(w, row.TranslatePoint(new Point(40, row.Bounds.Height / 2), w)!.Value);
        Assert.True(row.IsChecked);
        Assert.True(row.Switch.IsChecked);

        Click(w, Center(row.Switch, w));
        Assert.False(row.IsChecked);

        row.Focus();
        w.KeyPressQwerty(PhysicalKey.Space, RawInputModifiers.None);
        Assert.True(row.IsChecked);
        Assert.Equal(3, changes);

        row.IsChecked = false;
        Assert.False(row.Switch.IsChecked);
        w.Close();
    }

    [AvaloniaFact]
    public void Peer_Toggles()
    {
        var row = new SwitchRow { Title = "Bluetooth" };
        var w = Shots.Show(new ListBox { Classes = { "boxed-list" }, Items = { row } });
        var toggle = ControlAutomationPeer.CreatePeerForElement(row).GetProvider<IToggleProvider>()!;
        Assert.Equal(ToggleState.Off, toggle.ToggleState);
        toggle.Toggle();
        Assert.True(row.IsChecked);
        Assert.Equal(ToggleState.On, toggle.ToggleState);
        w.Close();
    }
}
