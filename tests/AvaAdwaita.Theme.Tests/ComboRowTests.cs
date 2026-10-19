using System.Linq;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using AvaAdwaita.Controls;
using AvaAdwaita.Nodes;
using Xunit;

namespace AvaAdwaita.Theme.Tests;

/// <summary>AvaAdwaita.Controls' ComboRow: the selected item, the popover the row opens and the subtitle mode.</summary>
public class ComboRowTests
{
    private static readonly string[] Items = ["Small", "Medium", "Large"];

    [AvaloniaFact]
    public void Activating_Opens_The_List_And_Keys_Choose()
    {
        var row = new ComboRow { Title = "Size", ItemsSource = Items, SelectedIndex = 1 };
        var w = Shots.Show(new ListBox { Classes = { "boxed-list" }, Width = 360, Items = { row } });
        Assert.Equal("Medium", row.SelectedItem);
        Assert.Equal("Medium", row.ComboBox.SelectedItem);
        Assert.Contains("combo", Css.GetCssNode(row)!.Classes);

        row.Activate();
        Shots.Pump(w);
        Assert.True(row.IsDropDownOpen);
        var rows = TopLevel.GetTopLevel(row)!.GetVisualDescendants().OfType<ComboBoxItem>().ToList();
        Assert.Equal(3, rows.Count);
        Assert.All(rows, r => Assert.Contains("activatable", Css.GetCssNode(r)!.Classes));

        w.KeyPressQwerty(PhysicalKey.ArrowDown, RawInputModifiers.None);
        w.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        Shots.Pump(w);
        Assert.False(row.IsDropDownOpen);
        Assert.Equal(2, row.SelectedIndex);
        Assert.Equal("Large", row.SelectedItem);
        w.Close();
    }

    [AvaloniaFact]
    public void Subtitle_Mode_Shows_The_Selection_As_The_Subtitle()
    {
        // The selection comes before the items, as XAML sets an attribute before a property element.
        var row = new ComboRow { Title = "Size", UseSubtitle = true, SelectedIndex = 0 };
        row.ItemsSource = Items;
        var w = Shots.Show(new ListBox { Classes = { "boxed-list" }, Width = 360, Items = { row } });
        Assert.Equal("Small", row.Subtitle);
        Assert.True(row.ComboBox.IsVisible);
        Assert.DoesNotContain(row.ComboBox.GetVisualDescendants().OfType<Avalonia.Controls.Control>(), c => c.Name == "PART_Current" && c.IsVisible);
        row.SelectedItem = "Large";
        Assert.Equal(2, row.SelectedIndex);
        Assert.Equal("Large", row.Subtitle);

        var peer = ControlAutomationPeer.CreatePeerForElement(row);
        Assert.Equal(AutomationControlType.ComboBox, peer.GetAutomationControlType());
        Assert.Equal("Large", peer.GetProvider<IValueProvider>()!.Value);
        peer.GetProvider<IExpandCollapseProvider>()!.Expand();
        Assert.True(row.IsDropDownOpen);
        Assert.Equal(ExpandCollapseState.Expanded, peer.GetProvider<IExpandCollapseProvider>()!.ExpandCollapseState);
        w.Close();
    }
}
