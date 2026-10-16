using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using AvaAdwaita.Nodes;
using Xunit;

namespace AvaAdwaita.Theme.Tests;

public class TransitionTests
{
    [AvaloniaFact]
    public void Hover_Background_Transitions_And_Settles()
    {
        AdwaitaEnvironment.ResumeTransitions();
        var b = new Button { Content = "Hover me" };
        var w = Shots.Show(b);
        var node = Css.GetCssNode(b)!;
        var normal = node.Style!.Background.Color;
        node.States |= CssEngine.Selectors.CssStates.Hover;
        node.Engine!.ResolveNow();
        var target = node.Style!.Background.Color;
        Assert.NotEqual(normal, target);

        // Right after the change the shown style is still near the old color.
        var shown = Css.GetStyle(b)!.Background.Color;
        Assert.NotEqual(target, shown);

        for (var i = 0; i < 40; i++)
        {
            Frames.Run(10);
        }

        Assert.Equal(target, Css.GetStyle(b)!.Background.Color);
        w.Close();
    }

    [AvaloniaFact]
    public void Keyframes_Run_Over_The_Style_And_End()
    {
        AdwaitaEnvironment.ResumeTransitions();
        var sheet = CssEngine.Cascade.CssStyleSheet.Parse(
            "@keyframes flash { from { background-color: rgb(255,0,0); } to { background-color: rgb(0,0,255); } }\n"
            + "button.flash { animation: flash 300ms linear; }",
            CssEngine.Cascade.CssOrigin.Application,
            "test.css");
        AdwaitaEnvironment.SetAppStyleSheets([sheet]);
        try
        {
            var b = new Button { Content = "Flash" };
            var w = Shots.Show(b);
            var settled = Css.GetCssNode(b)!.Style!.Background.Color;
            b.Classes.Add("flash");
            Dispatcher.UIThread.RunJobs();
            Css.GetCssNode(b)!.Engine!.ResolveNow();

            // Part way through, the color is between the keyframes: more blue than at the start.
            Frames.Run(150);
            var (r, _, bl, _) = Css.GetStyle(b)!.Background.Color.ToRgba8();
            Assert.InRange(r, 40, 215);
            Assert.InRange(bl, 40, 215);

            // Without a fill mode the animation ends on the underlying style.
            for (var i = 0; i < 30; i++)
            {
                Frames.Run(10);
            }

            Assert.Equal(settled, Css.GetStyle(b)!.Background.Color);
            w.Close();
        }
        finally
        {
            AdwaitaEnvironment.SetAppStyleSheets([]);
        }
    }

    [AvaloniaFact]
    public void Indeterminate_Progress_Pulses_A_Fifth_Of_The_Trough()
    {
        var bar = new ProgressBar { IsIndeterminate = true, Width = 200 };
        var w = Shots.Show(bar);
        var progress = System.Linq.Enumerable.Single(
            Avalonia.VisualTree.VisualExtensions.GetVisualDescendants(bar),
            v => v is Avalonia.StyledElement e && Css.GetNode(e) == "progress");
        var node = Css.GetCssNode((Avalonia.StyledElement)progress)!;
        Assert.Contains("pulse", node.Classes);
        var start = progress.Bounds.X;
        Assert.InRange(progress.Bounds.Width, 200 / 5.0 - 1, 200 / 5.0 + 9);
        for (var i = 0; i < 20; i++)
        {
            Frames.Run(10);
        }

        Assert.NotEqual(start, progress.Bounds.X);
        w.Close();
    }
}
