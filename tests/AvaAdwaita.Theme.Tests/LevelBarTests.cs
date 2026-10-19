using System.Linq;
using Avalonia.Automation.Peers;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using AvaAdwaita.Controls;
using AvaAdwaita.Nodes;
using AvaAdwaita.Presenters;
using Xunit;

namespace AvaAdwaita.Theme.Tests;

/// <summary>AvaAdwaita.Controls' LevelBar: the blocks, their offset classes and their sizes.</summary>
public class LevelBarTests
{
    private static string[] Blocks(LevelBar bar) =>
        bar.GetVisualDescendants().OfType<AdwBox>().Select(b => Css.GetCssNode(b)).Where(n => n?.Name == "block")
            .Select(n => string.Join(".", n!.Classes)).ToArray();

    [AvaloniaFact]
    public void Filled_Block_Takes_The_Offset_Class()
    {
        var bar = new LevelBar { Width = 200, Value = 0.2 };
        var w = Shots.Show(bar);
        Assert.Equal(["filled.low", "empty"], Blocks(bar));
        Assert.Contains("continuous", Css.GetCssNode(bar)!.Classes);

        bar.Value = 0.9;
        Shots.Pump(w);
        Assert.Equal(["filled.full", "empty"], Blocks(bar));

        bar.SetOffset("critical", 0.1);
        bar.Value = 0.05;
        Shots.Pump(w);
        Assert.Equal(["filled.critical", "empty"], Blocks(bar));

        Assert.Equal(AutomationControlType.ProgressBar, ControlAutomationPeer.CreatePeerForElement(bar).GetAutomationControlType());
        w.Close();
    }

    [AvaloniaFact]
    public void Discrete_Bar_Has_A_Block_Per_Unit()
    {
        var bar = new LevelBar { Width = 200, Maximum = 5, Value = 3, Mode = LevelBarMode.Discrete };
        var w = Shots.Show(bar);
        Assert.Equal(["filled", "filled", "filled", "empty", "empty"], Blocks(bar));
        Assert.Contains("discrete", Css.GetCssNode(bar)!.Classes);

        bar.IsInverted = true;
        Shots.Pump(w);
        Assert.Equal(["empty", "empty", "filled", "filled", "filled"], Blocks(bar));
        w.Close();
    }
}
