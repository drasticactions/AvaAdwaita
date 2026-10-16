using System;
using System.Collections.Generic;

namespace AvaAdwaita.Chrome;

/// <summary>The window buttons at the start and at the end of the header bar.</summary>
/// <param name="Start">The buttons before the ':'.</param>
/// <param name="End">The buttons after it.</param>
public sealed record ButtonLayout(IReadOnlyList<string> Start, IReadOnlyList<string> End)
{
    private static readonly string[] s_known = ["minimize", "maximize", "close"];

    /// <summary>Parses a layout as GTK parses <c>gtk-decoration-layout</c>, as <c>appmenu:minimize,maximize,close</c>.</summary>
    /// <remarks>Unknown names, <c>appmenu</c> and <c>icon</c> are dropped: there is no window menu.</remarks>
    public static ButtonLayout Parse(string? layout)
    {
        var parts = (layout ?? "appmenu:close").Split(':');
        return new ButtonLayout(Side(parts[0]), parts.Length > 1 ? Side(parts[1]) : []);
    }

    private static List<string> Side(string s)
    {
        var list = new List<string>();
        foreach (var raw in s.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (Array.IndexOf(s_known, raw) >= 0 && !list.Contains(raw))
            {
                list.Add(raw);
            }
        }

        return list;
    }
}
