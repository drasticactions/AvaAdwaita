using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using AvaAdwaita.CssEngine.Selectors;
using AvaAdwaita.Nodes;
using Xunit;

namespace AvaAdwaita.Theme.Tests;

/// <summary>
/// Builds every scene of the parity corpus in one window, with the scene's settings and states, and hands it
/// to a check. Scenes a check reports problems for must be exactly the ones in its known-differences file;
/// AVAADWAITA_PARITY_UPDATE=1 rewrites the file instead, keeping its comment header. AVAADWAITA_SCENES, a
/// comma-separated list of scene id prefixes, checks only those scenes, for working on one control.
/// </summary>
internal static class ParityRunner
{
    public static string DataDir => Path.Combine(TestData.RepoRoot, "tests", "Data");

    public delegate void SceneCheck(string id, JsonObject entry, JsonObject gtk, Control control, Window window, List<string> problems);

    public static void Run(string name, string knownPath, SceneCheck check, double scaling = 1)
    {
        var corpus = JsonNode.Parse(File.ReadAllText(Path.Combine(DataDir, "parity", "corpus.json")))!.AsArray();
        var expected = JsonNode.Parse(File.ReadAllText(Path.Combine(DataDir, "parity", "expected-gtk.json")))!.AsObject();
        var theme = TestApplication.Instance.Theme;
        var host = new StackPanel { Margin = new Thickness(24) };
        var window = Shots.Show(host, 640, 480);
        if (scaling != 1)
        {
            window.SetRenderScaling(scaling);
            Settle();
        }

        var report = new StringBuilder();
        var failing = new List<string>();
        var counts = new Dictionary<string, int>();
        var only = Environment.GetEnvironmentVariable("AVAADWAITA_SCENES")?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var ran = new HashSet<string>();
        AdwaitaEnvironment.SuppressTransitions(window);
        try
        {
            foreach (var entry in corpus.Select(e => e!.AsObject()))
            {
                var id = (string)entry["id"]!;
                if (expected[id] is not JsonObject gtk || (only is not null && !only.Any(p => id.StartsWith(p, StringComparison.Ordinal))))
                {
                    continue;
                }

                ran.Add(id);

                theme.ColorScheme = (bool)entry["dark"]! ? AdwColorScheme.ForceDark : AdwColorScheme.ForceLight;
                theme.HighContrast = (bool)entry["hc"]! ? AdwHighContrast.On : AdwHighContrast.Off;
                theme.AccentColor = Enum.Parse<AdwAccentColor>((string)entry["accent"]!, ignoreCase: true);

                var spec = entry["widget"]!.AsObject();

                // A scene's own application style sheet.
                string? sceneCss = null;
                if ((string?)spec["css"] is { } css)
                {
                    var path = Path.Combine(Path.GetTempPath(), $"avaadwaita-scene-{Environment.ProcessId}-{Guid.NewGuid():N}.css");
                    File.WriteAllText(path, css);
                    sceneCss = new Uri(path).AbsoluteUri;
                    theme.StyleSheets.Add(sceneCss);
                    Settle();
                }

                var shapes = (bool?)spec["shapes"] == true ? new FixedSettings(new Settings.SettingsValues { ShowStatusShapes = true }) : null;
                if (shapes is not null)
                {
                    AdwaitaEnvironment.State.AddSource(shapes);
                }

                var control = ParityScene.Build(spec);
                control.FlowDirection = ParityScene.Direction(entry);
                host.Children.Add(control);
                Settle();

                // Controls reset some pseudo-classes when they attach, so the states go on afterwards.
                var backdrop = ParityScene.ApplyStates(control, spec);
                NodeTree.ExtraWindowStates = backdrop ? CssStates.Backdrop : CssStates.None;
                Settle();
                if (backdrop)
                {
                    NodeTree.RefreshSubtree(Css.GetCssNode(window)!);
                    Settle();
                }

                ParityScene.Open(control, spec, gtk);
                Settle();
                var problems = new List<string>();
                check(id, entry, gtk, ParityScene.Root(control, spec), window, problems);
                ParityScene.Close(control);
                Settle();
                if (problems.Count > 0)
                {
                    failing.Add(id);
                    report.Append(id).Append('\n');
                    foreach (var p in problems)
                    {
                        report.Append("    ").Append(p).Append('\n');
                        var key = p.Split(' ', 2)[0];
                        counts[key] = counts.GetValueOrDefault(key) + 1;
                    }
                }

                host.Children.Remove(control);
                NodeTree.ExtraWindowStates = CssStates.None;
                if (sceneCss is not null)
                {
                    theme.StyleSheets.Remove(sceneCss);
                    File.Delete(new Uri(sceneCss).LocalPath);
                    Settle();
                }
                if (shapes is not null)
                {
                    AdwaitaEnvironment.State.RemoveSource(shapes);
                    Settle();
                }
            }
        }
        finally
        {
            NodeTree.ExtraWindowStates = CssStates.None;
            AdwaitaEnvironment.ResumeTransitions(window);
            theme.ColorScheme = AdwColorScheme.Default;
            theme.HighContrast = AdwHighContrast.System;
            theme.AccentColor = AdwAccentColor.System;
            window.Close();
        }

        var dir = Path.Combine(TestData.RepoRoot, "artifacts", name);
        Directory.CreateDirectory(dir);
        var summary = $"{failing.Count} of {ran.Count} scenes differ from GTK\n"
            + string.Join('\n', counts.OrderByDescending(kv => kv.Value).Select(kv => $"  {kv.Value,5} {kv.Key}")) + "\n\n";
        File.WriteAllText(Path.Combine(dir, "report.txt"), summary + report);

        if (Environment.GetEnvironmentVariable("AVAADWAITA_PARITY_UPDATE") == "1" && only is null)
        {
            var header = File.Exists(knownPath) ? File.ReadAllLines(knownPath).TakeWhile(l => l.StartsWith('#')).ToArray() : [];
            File.WriteAllLines(knownPath, header.Concat(failing));
            return;
        }

        var known = File.Exists(knownPath) ? File.ReadAllLines(knownPath).Where(l => l.Length > 0 && l[0] != '#').ToHashSet() : [];
        var regressed = failing.Where(id => !known.Contains(id)).ToList();
        var fixedNow = known.Where(id => ran.Contains(id) && !failing.Contains(id)).ToList();
        Assert.True(regressed.Count == 0 && fixedNow.Count == 0,
            $"{summary}newly different: {string.Join(", ", regressed.Take(20))}\nnow matching (remove from {Path.GetFileName(knownPath)}): {string.Join(", ", fixedNow.Take(20))}\nsee artifacts/{name}/report.txt");
    }

    public static void Settle()
    {
        for (var i = 0; i < 3; i++)
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        }
    }
}
