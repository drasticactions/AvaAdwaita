using System;
using System.Collections.Generic;
using System.Globalization;

namespace AvaAdwaita.Settings;

/// <summary>A parsed Pango font description, as <c>Adwaita Sans 11</c> or <c>Cantarell Bold Italic 10.5</c>.</summary>
/// <param name="Family">The family, or null when the description has none.</param>
/// <param name="Size">The size, or null.</param>
/// <param name="SizeIsAbsolute">True when the size is in px, false for points.</param>
/// <param name="Weight">The weight, 100–1000.</param>
/// <param name="Italic">True for italic or oblique.</param>
public sealed record PangoFontDescription(string? Family, double? Size, bool SizeIsAbsolute, int Weight, bool Italic)
{
    private static readonly Dictionary<string, int> s_weights = new(StringComparer.OrdinalIgnoreCase)
    {
        ["thin"] = 100,
        ["ultra-light"] = 200,
        ["ultralight"] = 200,
        ["extra-light"] = 200,
        ["light"] = 300,
        ["semi-light"] = 350,
        ["book"] = 380,
        ["regular"] = 400,
        ["normal"] = 400,
        ["medium"] = 500,
        ["semi-bold"] = 600,
        ["semibold"] = 600,
        ["demi-bold"] = 600,
        ["bold"] = 700,
        ["ultra-bold"] = 800,
        ["extra-bold"] = 800,
        ["heavy"] = 900,
        ["black"] = 900,
        ["ultra-heavy"] = 1000,
    };

    private static readonly HashSet<string> s_otherStyleWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "roman", "small-caps", "all-small-caps", "petite-caps", "all-petite-caps", "unicase", "title-caps",
        "ultra-condensed", "extra-condensed", "condensed", "semi-condensed", "semi-expanded", "expanded", "extra-expanded", "ultra-expanded",
        "not-rotated", "south", "upside-down", "north", "rotated-left", "east", "rotated-right", "west",
    };

    /// <summary>The size in px at 96 dpi, or null.</summary>
    public double? SizePx => Size is { } s ? (SizeIsAbsolute ? s : s * 96.0 / 72.0) : null;

    /// <summary>Parses a description, as pango_font_description_from_string.</summary>
    public static PangoFontDescription Parse(string text)
    {
        var words = new List<string>(text.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        double? size = null;
        var absolute = false;
        var weight = 400;
        var italic = false;
        if (words.Count > 0)
        {
            var last = words[^1];
            var px = last.EndsWith("px", StringComparison.OrdinalIgnoreCase);
            var num = px ? last[..^2] : last;
            if (double.TryParse(num, NumberStyles.Float, CultureInfo.InvariantCulture, out var s) && s >= 0)
            {
                size = s;
                absolute = px;
                words.RemoveAt(words.Count - 1);
            }
        }

        while (words.Count > 0)
        {
            var w = words[^1].TrimEnd(',');
            if (s_weights.TryGetValue(w, out var wt))
            {
                weight = wt;
            }
            else if (w.Equals("italic", StringComparison.OrdinalIgnoreCase) || w.Equals("oblique", StringComparison.OrdinalIgnoreCase))
            {
                italic = true;
            }
            else if (w.StartsWith("weight=", StringComparison.OrdinalIgnoreCase) && int.TryParse(w[7..], CultureInfo.InvariantCulture, out var nw))
            {
                weight = nw;
            }
            else if (!s_otherStyleWords.Contains(w))
            {
                break;
            }

            words.RemoveAt(words.Count - 1);
        }

        var family = string.Join(' ', words).Trim().TrimEnd(',');
        return new PangoFontDescription(family.Length == 0 ? null : family, size, absolute, weight, italic);
    }
}
