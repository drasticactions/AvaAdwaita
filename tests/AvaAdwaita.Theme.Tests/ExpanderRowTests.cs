using System.Linq;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using AvaAdwaita.Controls;
using AvaAdwaita.CssEngine.Selectors;
using AvaAdwaita.Nodes;
using Xunit;

namespace AvaAdwaita.Theme.Tests;

/// <summary>AvaAdwaita.Controls' ExpanderRow: expanding by the header, the enable switch, its states and classes.</summary>
public class ExpanderRowTests
{
    private static bool Shown(Avalonia.Visual v) => v.IsAttachedToVisualTree() && v.IsEffectivelyVisible;

    [AvaloniaFact]
    public void Header_Expands_And_Collapses_The_Rows()
    {
        var expanded = 0;
        var row = new ExpanderRow { Title = "Advanced" };
        row.Expanded += (_, _) => expanded++;
        var w = Shots.Show(new ListBox { Classes = { "boxed-list" }, Width = 360, Items = { row } });
        AdwaitaEnvironment.SuppressTransitions(w);
        var node = Css.GetCssNode(row)!;
        Assert.Contains("empty", node.Classes);

        var nested = new ActionRow { Title = "Nested" };
        row.Rows.Add(nested);
        Shots.Pump(w);
        Assert.DoesNotContain("empty", node.Classes);
        Assert.False(Shown(nested));

        row.Header.Activate();
        Shots.Pump(w);
        Assert.True(row.IsExpanded);
        Assert.Equal(1, expanded);
        Assert.True(Shown(nested));
        Assert.True(node.States.HasFlag(CssStates.Checked));
        Assert.Equal("list", Css.GetCssNode(nested)!.Parent!.Name);
        Assert.Contains("nested", Css.GetCssNode(nested)!.Parent!.Classes);

        row.Header.Activate();
        Shots.Pump(w);
        Assert.False(row.IsExpanded);
        Assert.False(node.States.HasFlag(CssStates.Checked));
        w.Close();
    }

    [AvaloniaFact]
    public void Enable_Switch_Turns_The_Expansion_On_And_Off()
    {
        var row = new ExpanderRow { Title = "Proxy", ShowEnableSwitch = true, Rows = { new ActionRow { Title = "Host" } } };
        var w = Shots.Show(new ListBox { Classes = { "boxed-list" }, Width = 360, Items = { row } });
        var toggle = row.Header.GetVisualDescendants().OfType<ToggleSwitch>().Single();
        Assert.True(toggle.IsVisible);
        Assert.True(toggle.IsChecked);

        toggle.IsChecked = false;
        Assert.False(row.EnableExpansion);
        row.IsExpanded = true;
        Assert.False(row.IsExpanded);

        toggle.IsChecked = true;
        Assert.True(row.EnableExpansion);
        Assert.True(row.IsExpanded);

        var peer = ControlAutomationPeer.CreatePeerForElement(row).GetProvider<IExpandCollapseProvider>()!;
        Assert.Equal(ExpandCollapseState.Expanded, peer.ExpandCollapseState);
        peer.Collapse();
        Assert.False(row.IsExpanded);
        w.Close();
    }
}
