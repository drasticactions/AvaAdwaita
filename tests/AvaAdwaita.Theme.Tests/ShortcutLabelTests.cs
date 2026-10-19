using System.Linq;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using AvaAdwaita.Controls;
using AvaAdwaita.Controls.Primitives;
using AvaAdwaita.Nodes;
using Xunit;

namespace AvaAdwaita.Theme.Tests;

/// <summary>AvaAdwaita.Controls' ShortcutLabel: GTK's accelerator syntax as keycaps.</summary>
public class ShortcutLabelTests
{
    private static string[] Parts(ShortcutLabel label) => label.GetVisualDescendants()
        .Where(v => v is MarkupLabel || (v is Presenters.AdwLabel && Css.GetCssNode((Control)v)?.Classes.Contains("dimmed") == true))
        .Select(v => v is MarkupLabel m ? m.Markup! : ((Presenters.AdwLabel)v).Text!)
        .ToArray();

    [AvaloniaTheory]
    [InlineData("<Control>q", new[] { "Ctrl", "Q" })]
    [InlineData("<Shift>A Home", new[] { "Shift", "A", "/", "Home" })]
    [InlineData("<Alt>1...9", new[] { "Alt", "1", "⋯", "9" })]
    [InlineData("<Control>C+<Control>X", new[] { "Ctrl", "C", "→", "Ctrl", "X" })]
    [InlineData("<Super>Left", new[] { "Super", "←" })]
    [InlineData("<Control>less", new[] { "Ctrl", "&lt;" })]
    [InlineData("Control_L&Control_R", new[] { "Ctrl <small><b>L</b></small>", "Ctrl <small><b>R</b></small>" })]
    public void Accelerators_Become_Keycaps(string accelerator, string[] expected)
    {
        var label = new ShortcutLabel { Accelerator = accelerator };
        var w = Shots.Show(new StackPanel { Children = { label } });
        Assert.Equal(expected, Parts(label));
        w.Close();
    }

    [AvaloniaFact]
    public void Named_Keys_Take_GTKs_Keyboard_Labels()
    {
        var label = new ShortcutLabel { Accelerator = "<Primary><Shift>Page_Up" };
        var w = Shots.Show(new StackPanel { Children = { label } });
        Assert.Equal(["Ctrl", "Shift", Resources.GtkCatalog.Pgettext("keyboard label", "Page_Up")], Parts(label));
        w.Close();
    }

    [AvaloniaFact]
    public void No_Accelerator_Shows_The_Disabled_Text()
    {
        var label = new ShortcutLabel { DisabledText = "Disabled" };
        var w = Shots.Show(label);
        Assert.Equal(["Disabled"], Parts(label));
        Assert.True(label.Bounds.Height > 0);

        label.Accelerator = "<Control>w";
        Shots.Pump(w);
        Assert.Equal(["Ctrl", "W"], Parts(label));
        Assert.True(label.Bounds.Height > 0);
        w.Close();
    }

    [AvaloniaFact]
    public void Peer_Names_The_Shortcut()
    {
        var label = new ShortcutLabel { Accelerator = "<Control><Shift>z <Control>y" };
        var w = Shots.Show(new StackPanel { Children = { label } });
        var peer = ControlAutomationPeer.CreatePeerForElement(label);
        Assert.Equal(AutomationControlType.Text, peer.GetAutomationControlType());
        Assert.Equal("Shift+Ctrl+Z Ctrl+Y", peer.GetName());
        Assert.Empty(peer.GetChildren());
        w.Close();
    }
}
