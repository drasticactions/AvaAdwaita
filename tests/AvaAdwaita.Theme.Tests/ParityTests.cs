using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaAdwaita.CssEngine.Selectors;
using AvaAdwaita.Nodes;
using AvaAdwaita.Presenters;
using Xunit;

namespace AvaAdwaita.Theme.Tests;

/// <summary>
/// Builds every scene of the parity corpus and compares the node tree, the allocations and the draws with
/// what GTK and libadwaita produced for the same scene (tests/Data/parity/expected-gtk.json, written by
/// tools/adw-oracle). Scenes listed in parity/known-differences.txt are expected to differ.
/// </summary>
public class ParityTests
{
    [AvaloniaFact]
    public void Scenes_Match_Gtk() =>
        ParityRunner.Run("parity", Path.Combine(ParityRunner.DataDir, "parity", "known-differences.txt"),
            (_, _, gtk, control, _, problems) => problems.AddRange(Check(control, gtk)));

    private static List<string> Check(Control control, JsonObject gtk)
    {
        var problems = new List<string>();
        var node = Css.GetCssNode(control);
        if (node is null || node.Style is not { } style)
        {
            problems.Add("tree: the control has no node");
            return problems;
        }

        var border = new CssBoxGeometry(style, control.Bounds.Size).BorderBox;
        var size = gtk["size"]!.AsArray().Select(x => (double)x!).ToArray();
        if (Math.Abs(border.Width - size[0]) > 1 || Math.Abs(border.Height - size[1]) > 1)
        {
            problems.Add($"size: expected {size[0]}x{size[1]}, got {border.Width:0.##}x{border.Height:0.##}");
        }

        var expectedTree = ((string)gtk["tree"]!).TrimEnd('\n').Split('\n');
        var actualTree = new List<(string Line, Rect Bounds)>();
        Tree(node, control, border.Position, 0, actualTree);
        if (!expectedTree.Select(StripBounds).SequenceEqual(actualTree.Select(t => t.Line)))
        {
            problems.Add("tree: expected\n" + string.Join('\n', expectedTree.Select(l => "        " + Normalize(l)))
                + "\n      got\n" + string.Join('\n', actualTree.Select(t => "        " + t.Line + " " + F(t.Bounds))));
        }
        else
        {
            for (var i = 0; i < expectedTree.Length; i++)
            {
                var b = Bounds(expectedTree[i]);
                var a = actualTree[i].Bounds;
                if (Math.Abs(a.X - b.X) > 1 || Math.Abs(a.Y - b.Y) > 1 || Math.Abs(a.Width - b.Width) > 1 || Math.Abs(a.Height - b.Height) > 1)
                {
                    problems.Add($"allocation: {actualTree[i].Line.Trim()} expected {F(b)}, got {F(a)}");
                }
            }
        }

        using (var rec = DrawRecorder.Start(control))
        {
            Render(control);
            AddEditableText(control, rec);
            var draws = rec.Draws.Select(d => d with { Rect = d.Rect.Translate(-(Vector)border.Position) }).ToList();
            var sceneSize = new Size((double)gtk["size"]![0]!, (double)gtk["size"]![1]!);
            foreach (var p in ParityComparer.Compare(gtk["draws"]!.AsArray(), draws, sceneSize))
            {
                problems.Add("draw: " + p);
            }
        }

        return problems;
    }

    // An entry's text is drawn by Avalonia's TextPresenter, and a data grid's cell text by a plain TextBlock that
    // is a label node; neither can report itself.
    private static void AddEditableText(Control root, DrawRecorder rec)
    {
        var plain = root.GetVisualDescendants().OfType<TextBlock>().Where(t => t is not AdwLabel && AvaAdwaita.Nodes.Css.GetNode(t) is not null)
            .Select(t => (Control: (Control)t, t.Text, t.Foreground, t.FontFamily, t.FontStyle, t.FontWeight, t.FontSize));
        var presenters = root.GetVisualDescendants().OfType<Avalonia.Controls.Presenters.TextPresenter>()
            .Select(t => (Control: (Control)t, t.Text, t.Foreground, t.FontFamily, t.FontStyle, t.FontWeight, t.FontSize));
        foreach (var p in presenters.Concat(plain))
        {
            if (string.IsNullOrEmpty(p.Text) || !p.Control.IsEffectivelyVisible || p.Foreground is not Avalonia.Media.ISolidColorBrush fg)
            {
                continue;
            }

            var face = Avalonia.Media.FontManager.Current.TryGetGlyphTypeface(new Avalonia.Media.Typeface(p.FontFamily, p.FontStyle, p.FontWeight), out var g) ? g : null;
            rec.Add(p.Control, new DrawRecord("text", new Rect(p.Control.Bounds.Size))
            {
                Color = Avalonia.Media.Color.FromArgb((byte)Math.Round(fg.Color.A * fg.Opacity), fg.Color.R, fg.Color.G, fg.Color.B),
                Family = p.FontFamily.FamilyNames.FirstOrDefault(n => face?.FamilyName.StartsWith(n, StringComparison.Ordinal) == true) ?? face?.FamilyName,
                Weight = (int)p.FontWeight,
                Size = p.FontSize,
            });
        }
    }

    private static void Render(Control control)
    {
        // Draw the control's subtree into an offscreen bitmap so the presenters report their draws.
        var size = new PixelSize(Math.Max(1, (int)Math.Ceiling(control.Bounds.Width)), Math.Max(1, (int)Math.Ceiling(control.Bounds.Height)));
        using var bmp = new Avalonia.Media.Imaging.RenderTargetBitmap(size);
        bmp.Render(control);
    }

    private static void Tree(CssNode node, Visual root, Point origin, int depth, List<(string, Rect)> lines)
    {
        if (!node.Visible)
        {
            return;
        }

        var bounds = default(Rect);
        if (node.Owner is NodeBinder { Owner: Visual v } && node.Style is { } s)
        {
            // A control's node is drawn, with its transform, by the box at the root of its template.
            var drawn = v is Avalonia.Controls.Primitives.TemplatedControl && v.GetVisualChildren().FirstOrDefault() is AdwBox b && Css.GetCssNode(b) is null ? (Visual)b : v;
            // In a right-to-left subtree the box lays out with the mirrored style it draws.
            var box = new CssBoxGeometry(Css.GetStyle(drawn) ?? s, drawn.Bounds.Size).BorderBox;
            bounds = box.TransformToAABB(DrawRecorder.ToRoot(drawn, root)).Translate(-(Vector)origin);
        }

        // The oracle lists widgets; a scrolled window's undershoots and a popover's arrow are CSS nodes without widgets.
        if ((node.Name == "undershoot" && node.Parent?.Name == "scrolledwindow") || (node.Name == "arrow" && node.Parent?.Name == "popover"))
        {
            return;
        }

        // A text view's text is a CSS node of GtkTextView without a widget.
        if (node.Name == "text" && node.Parent?.Name == "textview")
        {
            foreach (var c in node.Children)
            {
                Tree(c, root, origin, depth, lines);
            }

            return;
        }

        lines.Add((new string(' ', depth * 2) + Normalize(node.Name + string.Concat(node.Classes.Select(c => "." + c))), bounds));
        // Siblings with item positions, as a data grid's rows, are in that order rather than the panel's.
        var children = node.Children.All(c => c.ItemPosition is not null) ? node.Children.OrderBy(c => c.ItemPosition!.Value.Index).ToList() : node.Children.ToList();
        foreach (var c in children)
        {
            Tree(c, root, origin, depth + 1, lines);
        }
    }

    private static string StripBounds(string line)
    {
        var i = line.IndexOf(" [", StringComparison.Ordinal);
        return Normalize(i < 0 ? line : line[..i]);
    }

    // Classes in a stable order, so GTK's insertion order does not matter.
    private static string Normalize(string line)
    {
        var bracket = line.IndexOf(" [", StringComparison.Ordinal);
        if (bracket >= 0)
        {
            line = line[..bracket];
        }

        var indent = line.Length - line.TrimStart().Length;
        var parts = line.Trim().Split('.');
        return new string(' ', indent) + parts[0] + string.Concat(parts.Skip(1).Order(StringComparer.Ordinal).Select(c => "." + c));
    }

    private static Rect Bounds(string line)
    {
        var i = line.IndexOf(" [", StringComparison.Ordinal);
        var v = line[(i + 2)..^1].Split(',').Select(x => double.Parse(x, System.Globalization.CultureInfo.InvariantCulture)).ToArray();
        return new Rect(v[0], v[1], v[2], v[3]);
    }

    private static string F(Rect r) => FormattableString.Invariant($"[{r.X:0.##},{r.Y:0.##},{r.Width:0.##},{r.Height:0.##}]");
}
