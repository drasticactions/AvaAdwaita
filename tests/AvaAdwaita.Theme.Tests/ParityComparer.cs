using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using Avalonia;
using Avalonia.Media;
using AvaAdwaita.Presenters;

namespace AvaAdwaita.Theme.Tests;

/// <summary>
/// Compares AvaAdwaita's draws with GTK's: colors exact in 8 bits, geometry within half a pixel, text by
/// color, family, weight and size only, icons within 1.5 px (their ink bounds come from different path code).
/// Draws are matched by kind in any order; a transformed subtree is left to the pixel goldens.
/// </summary>
internal static class ParityComparer
{
    private const double Geometry = 0.5;

    public static List<string> Compare(JsonArray expected, IReadOnlyList<DrawRecord> actual)
    {
        var problems = new List<string>();
        var left = actual.Where(Relevant).ToList();
        foreach (var e in expected.Select(x => x!.AsObject()))
        {
            var kind = (string)e["k"]!;

            // The oracle does not walk into a rotated or scaled subtree (libadwaita's diamond slider beside marks);
            // the pixel goldens compare it. Its draws here are the ones within its bounds.
            if (kind == "transform")
            {
                var area = Rect(e).Inflate(1);
                left.RemoveAll(a => area.Contains(a.Rect));
                continue;
            }

            // A popover's arrow and border are drawn with cairo; the pixel goldens compare them.
            if (kind == "cairo")
            {
                continue;
            }

            if (kind == "other")
            {
                problems.Add($"GTK drew a {kind} node at {Rect(e)}");
                continue;
            }

            // GTK emits a border node even when every side it draws is transparent.
            if (kind == "border" && Widths(e).Select((w, i) => (w, c: Color(e["c"]!.AsArray()[i]))).All(s => s.w <= 0 || s.c.A == 0))
            {
                continue;
            }

            // GTK draws a symbolic icon that is one axis-aligned rectangle as a plain color node.
            var match = left.FirstOrDefault(a => a.Kind == kind && Matches(e, a))
                ?? (kind == "color" && e["rad"] is null ? left.FirstOrDefault(a => a.Kind == "icon" && a.Color == Color(e["c"]) && Near(Rect(e), a.Rect, Geometry)) : null);
            if (match is not null)
            {
                left.Remove(match);
                continue;
            }

            var near = left.FirstOrDefault(a => a.Kind == kind);
            problems.Add(near is null ? $"missing {Describe(e)}" : $"expected {Describe(e)}\n      got {Describe(near)}");
            if (near is not null)
            {
                left.Remove(near);
            }
        }

        foreach (var a in left)
        {
            problems.Add($"extra {Describe(a)}");
        }

        return problems;
    }

    private static bool Relevant(DrawRecord d) => d.Kind switch
    {
        "border" => d.Widths!.Any(w => w > 0) && d.Colors!.Where((c, i) => d.Widths![i] > 0).Any(c => c.A > 0),
        "color" or "shadow" or "inset-shadow" => d.Color is { A: > 0 },
        "icon" => d.Color is null || d.Color.Value.A > 0,
        _ => true,
    };

    private static bool Matches(JsonObject e, DrawRecord a)
    {
        switch (a.Kind)
        {
            case "text":
                return Color(e["c"]) == a.Color
                    && string.Equals((string?)e["family"], a.Family, StringComparison.Ordinal)
                    && (int)e["weight"]! == a.Weight
                    && Math.Abs((double)e["size"]! - a.Size) < 0.02;
            case "icon":
                return (e["c"] is null || a.Color is null || Color(e["c"]) == a.Color) && Near(Rect(e), a.Rect, 1.5);
            case "gradient":
                return Near(Rect(e), a.Rect, Geometry);
            case "border":
                {
                    var w = Widths(e);
                    var cs = e["c"]!.AsArray().Select(Color).ToArray();
                    for (var i = 0; i < 4; i++)
                    {
                        if (Math.Abs(w[i] - a.Widths![i]) > Geometry || (w[i] > 0 && cs[i] != a.Colors![i]))
                        {
                            return false;
                        }
                    }

                    return Near(Rect(e), a.Rect, Geometry) && NearRadii(e, a);
                }

            case "shadow":
            case "inset-shadow":
                {
                    var s = e["s"]!.AsArray().Select(x => (double)x!).ToArray();
                    return Color(e["c"]) == a.Color && Near(Rect(e), a.Rect, Geometry) && NearRadii(e, a)
                        && s.Zip(a.Shadow!).All(p => Math.Abs(p.First - p.Second) <= Geometry);
                }

            default:
                return Color(e["c"]) == a.Color && Near(Rect(e), a.Rect, Geometry) && NearRadii(e, a);
        }
    }

    private static bool NearRadii(JsonObject e, DrawRecord a)
    {
        var r = e["rad"]?.AsArray().Select(x => (double)x!).ToArray() ?? [0, 0, 0, 0];
        var ar = a.Radii ?? [0, 0, 0, 0];
        return r.Zip(ar).All(p => Math.Abs(p.First - p.Second) <= Geometry);
    }

    private static bool Near(Rect a, Rect b, double tolerance)
    {
        tolerance += 1e-6;
        return Math.Abs(a.X - b.X) <= tolerance && Math.Abs(a.Y - b.Y) <= tolerance
            && Math.Abs(a.Width - b.Width) <= tolerance && Math.Abs(a.Height - b.Height) <= tolerance;
    }

    private static double[] Widths(JsonObject e) => e["w"]!.AsArray().Select(x => (double)x!).ToArray();

    public static Rect Rect(JsonObject e)
    {
        var r = e["r"]!.AsArray().Select(x => (double)x!).ToArray();
        return new Rect(r[0], r[1], r[2], r[3]);
    }

    public static Color Color(JsonNode? n)
    {
        var s = (string)n!;
        var v = uint.Parse(s.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        return Avalonia.Media.Color.FromArgb((byte)(v & 0xff), (byte)(v >> 24), (byte)(v >> 16), (byte)(v >> 8));
    }

    private static string Hex(Color c) => $"#{c.R:x2}{c.G:x2}{c.B:x2}{c.A:x2}";

    private static string F(Rect r) => FormattableString.Invariant($"[{r.X:0.###},{r.Y:0.###},{r.Width:0.###},{r.Height:0.###}]");

    private static string Describe(JsonObject e)
    {
        var sb = new StringBuilder((string)e["k"]!).Append(' ').Append(F(Rect(e)));
        foreach (var key in new[] { "rad", "w", "c", "s", "family", "weight", "size" })
        {
            if (e[key] is { } v)
            {
                sb.Append(' ').Append(key).Append('=').Append(v.ToJsonString().Replace("\"", string.Empty, StringComparison.Ordinal));
            }
        }

        return sb.ToString();
    }

    private static string Describe(DrawRecord a)
    {
        var sb = new StringBuilder(a.Kind).Append(' ').Append(F(a.Rect));
        if (a.Radii is { } r)
        {
            sb.Append(" rad=[").Append(string.Join(',', r.Select(x => x.ToString("0.##", CultureInfo.InvariantCulture)))).Append(']');
        }

        if (a.Widths is { } w)
        {
            sb.Append(" w=[").Append(string.Join(',', w.Select(x => x.ToString("0.##", CultureInfo.InvariantCulture)))).Append(']');
        }

        if (a.Color is { } c)
        {
            sb.Append(" c=").Append(Hex(c));
        }

        if (a.Colors is { } cs)
        {
            sb.Append(" c=[").Append(string.Join(',', cs.Select(Hex))).Append(']');
        }

        if (a.Shadow is { } s)
        {
            sb.Append(" s=[").Append(string.Join(',', s.Select(x => x.ToString("0.##", CultureInfo.InvariantCulture)))).Append(']');
        }

        if (a.Family is not null)
        {
            sb.Append(" family=").Append(a.Family).Append(" weight=").Append(a.Weight).Append(" size=").Append(a.Size.ToString("0.##", CultureInfo.InvariantCulture));
        }

        return sb.ToString();
    }
}
