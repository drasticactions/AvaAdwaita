using System.Linq;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using AvaAdwaita.Controls;
using AvaAdwaita.Nodes;
using Xunit;

namespace AvaAdwaita.Theme.Tests;

/// <summary>AvaAdwaita.Controls' PreferencesGroup: its header, where children go, and rows from data.</summary>
public class PreferencesGroupTests
{
    private static CssNode Header(PreferencesGroup g) =>
        g.GetVisualDescendants().OfType<Control>().Select(c => Css.GetCssNode(c)).First(n => n is not null && n.Classes.Contains("header"))!;

    [AvaloniaFact]
    public void Rows_Go_In_The_List_And_Others_Under_It()
    {
        var note = new Label { Content = "Note" };
        var group = new PreferencesGroup { Title = "Network", Children = { new ActionRow { Title = "Wi-Fi" }, note, new SwitchRow { Title = "VPN" } } };
        var w = Shots.Show(group, 480, 400);
        Assert.Equal(2, group.List!.ItemCount);
        Assert.Contains("boxed-list", Css.GetCssNode(group.List)!.Classes);
        Assert.Same(group.List.Parent, note.Parent);
        Assert.True(note.Bounds.Top >= group.List.Bounds.Bottom);
        Assert.Contains("single-line", Header(group).Classes);

        group.Description = "Connections";
        group.SeparateRows = true;
        Shots.Pump(w);
        Assert.DoesNotContain("single-line", Header(group).Classes);
        Assert.Contains("boxed-list-separate", Css.GetCssNode(group.List)!.Classes);

        group.Title = null;
        group.Description = null;
        Shots.Pump(w);
        Assert.False(Header(group).Visible);

        var peer = ControlAutomationPeer.CreatePeerForElement(group);
        Assert.Equal(AutomationControlType.Group, peer.GetAutomationControlType());
        w.Close();
    }

    [AvaloniaFact]
    public void Rows_Come_From_Data()
    {
        var group = new PreferencesGroup
        {
            Title = "Fruit",
            ItemsSource = new[] { "Apple", "Pear" },
            ItemTemplate = new FuncDataTemplate<string>((s, _) => new ActionRow { Title = s! }),
        };
        var w = Shots.Show(group, 480, 400);
        var rows = group.List!.GetVisualDescendants().OfType<ActionRow>().ToList();
        Assert.Equal(["Apple", "Pear"], rows.Select(r => r.Title));
        Assert.True(group.List!.IsVisible);
        w.Close();
    }
}
