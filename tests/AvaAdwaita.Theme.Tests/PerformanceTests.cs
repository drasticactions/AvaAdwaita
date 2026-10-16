using System;
using System.Diagnostics;
using AvaAdwaita.CssEngine.Cascade;
using AvaAdwaita.CssEngine.Selectors;
using AvaAdwaita.Nodes;
using Xunit;

namespace AvaAdwaita.Theme.Tests;

[Collection(nameof(PerformanceCollection))]
public class PerformanceTests(ITestOutputHelper output)
{
    [Fact]
    public void Parse_Resolve_And_Hover_Costs()
    {
        var css = TestData.BundledCss;
        CssStyleSheet.Parse(css, CssOrigin.Theme);
        var sw = Stopwatch.StartNew();
        const int Runs = 5;
        for (var i = 0; i < Runs; i++)
        {
            CssStyleSheet.Parse(css, CssOrigin.Theme);
        }

        var parseMs = sw.Elapsed.TotalMilliseconds / Runs;

        var h = new EngineHarness();
        var buttons = new System.Collections.Generic.List<CssNode>();
        string[] names = ["button", "label", "entry", "checkbutton", "scale", "row", "box", "image"];
        var box = h.Add(h.Window, "box", "vertical");
        for (var i = 0; i < 250; i++)
        {
            var row = h.Add(box, "box", "horizontal");
            for (var k = 0; k < 7; k++)
            {
                var n = h.Add(row, names[(i + k) % names.Length], k % 2 == 0 ? "flat" : "text-button");
                if (n.Name == "button")
                {
                    buttons.Add(n);
                }
            }
        }

        sw.Restart();
        h.Engine.ResolveNow();
        var firstMs = sw.Elapsed.TotalMilliseconds;

        h.Engine.InvalidateAll();
        sw.Restart();
        h.Engine.ResolveNow();
        var fullMs = sw.Elapsed.TotalMilliseconds;

        // The window going to the background restyles every node, with the rule index kept.
        var restyleMs = double.MaxValue;
        for (var i = 0; i < 6; i++)
        {
            h.Window.States ^= CssStates.Backdrop;
            sw.Restart();
            h.Engine.ResolveNow();
            restyleMs = Math.Min(restyleMs, sw.Elapsed.TotalMilliseconds);
        }

        var b = buttons[buttons.Count / 2];
        b.States |= CssStates.Hover;
        h.Engine.ResolveNow();
        b.States &= ~CssStates.Hover;
        h.Engine.ResolveNow();
        sw.Restart();
        const int Hovers = 200;
        for (var i = 0; i < Hovers; i++)
        {
            b.States ^= CssStates.Hover;
            h.Engine.ResolveNow();
        }

        var hoverMs = sw.Elapsed.TotalMilliseconds / Hovers;
        output.WriteLine($"parse {parseMs:F1} ms; first resolve of 2,001 nodes {firstMs:F1} ms; full re-resolve {fullMs:F1} ms; window restyle {restyleMs:F1} ms; hover {hoverMs:F3} ms");

        // The budgets are 10 ms for the window and 0.2 ms for a hover; shared CI machines get twice that.
        Assert.True(restyleMs < 20, $"restyling 2,001 nodes took {restyleMs} ms");
        Assert.True(hoverMs < 0.4, $"hover took {hoverMs} ms");
    }
}

[CollectionDefinition(nameof(PerformanceCollection), DisableParallelization = true)]
public class PerformanceCollection;
