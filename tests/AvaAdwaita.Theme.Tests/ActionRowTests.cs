using System.Linq;
using Avalonia;
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

/// <summary>AvaAdwaita.Controls' ActionRow and ButtonRow: their nodes, their parts, activation and their automation peers.</summary>
public class ActionRowTests
{
    private static Point Center(Visual v, Visual root) => v.TranslatePoint(new Point(v.Bounds.Width / 2, v.Bounds.Height / 2), root)!.Value;

    [AvaloniaFact]
    public void Row_Shows_Only_The_Parts_It_Has()
    {
        var row = new ActionRow { Title = "Title" };
        var list = new ListBox { Classes = { "boxed-list" }, Width = 360, Items = { row } };
        var w = Shots.Show(list);
        var nodes = row.GetVisualDescendants().OfType<StyledElement>().Select(Css.GetCssNode).Where(n => n is { Visible: true }).ToList();
        Assert.Equal("row", Css.GetCssNode(row)!.Name);
        Assert.DoesNotContain("activatable", Css.GetCssNode(row)!.Classes);
        Assert.DoesNotContain(nodes, n => n!.Classes.Contains("prefixes") || n.Classes.Contains("suffixes") || n.Classes.Contains("subtitle"));

        row.Subtitle = "Subtitle";
        row.Prefixes.Add(new Presenters.AdwIcon { IconName = "folder-symbolic" });
        row.Content = new Button { Content = "Edit" };
        Shots.Pump(w);
        nodes = row.GetVisualDescendants().OfType<StyledElement>().Select(Css.GetCssNode).Where(n => n is { Visible: true }).ToList();
        Assert.Contains(nodes, n => n!.Classes.Contains("prefixes"));
        Assert.Contains(nodes, n => n!.Classes.Contains("subtitle"));
        var suffixes = nodes.Single(n => n!.Classes.Contains("suffixes"));
        Assert.Equal("button", Assert.Single(suffixes!.Children, c => c.Visible).Name);
        w.Close();
    }

    [AvaloniaFact]
    public void Activatable_Row_Activates_On_Click_And_Keys()
    {
        var count = 0;
        var commanded = 0;
        var row = new ActionRow { Title = "Title", IsActivatable = true, Command = new Command(() => commanded++) };
        row.Activated += (_, _) => count++;
        var list = new ListBox { Classes = { "boxed-list" }, Width = 360, Items = { row } };
        var w = Shots.Show(list);
        Assert.Contains("activatable", Css.GetCssNode(row)!.Classes);

        w.MouseDown(Center(row, w), MouseButton.Left);
        w.MouseUp(Center(row, w), MouseButton.Left);
        Assert.Equal(1, count);
        Assert.Equal(1, commanded);

        // A boxed list has no selection.
        Assert.False(row.IsSelected);
        Assert.Equal(-1, list.SelectedIndex);

        row.Focus();
        w.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        Assert.Equal(2, count);

        row.IsActivatable = false;
        Shots.Pump(w);
        Assert.DoesNotContain("activatable", Css.GetCssNode(row)!.Classes);
        w.MouseDown(Center(row, w), MouseButton.Left);
        w.MouseUp(Center(row, w), MouseButton.Left);
        Assert.Equal(2, count);
        w.Close();
    }

    [AvaloniaFact]
    public void Peer_Is_Named_By_The_Title()
    {
        var count = 0;
        var row = new ActionRow { Title = "_Wi-Fi", UseUnderline = true, Subtitle = "Connected", IsActivatable = true, Content = new Button { Content = "Edit" } };
        row.Activated += (_, _) => count++;
        var w = Shots.Show(new ListBox { Classes = { "boxed-list" }, Items = { row } });
        var peer = ControlAutomationPeer.CreatePeerForElement(row);
        Assert.Equal("Wi-Fi", peer.GetName());
        Assert.Equal("Connected", peer.GetHelpText());
        Assert.Equal(AutomationControlType.ListItem, peer.GetAutomationControlType());
        peer.GetProvider<IInvokeProvider>()!.Invoke();
        Assert.Equal(1, count);
        w.Close();
    }

    [AvaloniaFact]
    public void Button_Row_Is_An_Activatable_Button()
    {
        var count = 0;
        var row = new ButtonRow { Title = "Add", StartIconName = "list-add-symbolic", Classes = { "suggested-action" } };
        row.Activated += (_, _) => count++;
        var w = Shots.Show(new ListBox { Classes = { "boxed-list" }, Width = 360, Items = { row } });
        var node = Css.GetCssNode(row)!;
        Assert.Equal(["activatable", "button", "suggested-action"], node.Classes.Order());
        var icons = row.GetVisualDescendants().OfType<Presenters.AdwIcon>().Where(i => i.IsEffectivelyVisible).ToList();
        Assert.Equal("list-add-symbolic", Assert.Single(icons).IconName);

        w.MouseDown(Center(row, w), MouseButton.Left);
        w.MouseUp(Center(row, w), MouseButton.Left);
        Assert.Equal(1, count);
        var peer = ControlAutomationPeer.CreatePeerForElement(row);
        Assert.Equal(AutomationControlType.Button, peer.GetAutomationControlType());
        Assert.Equal("Add", peer.GetName());
        peer.GetProvider<IInvokeProvider>()!.Invoke();
        Assert.Equal(2, count);
        w.Close();
    }

    private sealed class Command(System.Action run) : System.Windows.Input.ICommand
    {
        public event System.EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => run();
    }
}
