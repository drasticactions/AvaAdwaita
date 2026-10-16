using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using AvaAdwaita.CssEngine.Computed;
using AvaAdwaita.CssEngine.Values;

namespace AvaAdwaita.Markup;

/// <summary>The paths <c>{adw:Css}</c> accepts: every property, flat (<c>FontWeight</c>) or grouped (<c>Font.Weight</c>), plus composites.</summary>
internal static partial class CssPaths
{
    private static readonly FrozenDictionary<string, CssPath> s_paths = Build();

    public static CssPath? Get(string path) => s_paths.GetValueOrDefault(path);

    public static IEnumerable<string> Names => s_paths.Keys;

    private static FrozenDictionary<string, CssPath> Build()
    {
        var d = new Dictionary<string, CssPath>(StringComparer.Ordinal);
        AddGenerated(d);
        d["Padding"] = new("Padding", -1, typeof(CssBoxSides), s => s.Padding);
        d["Margin"] = new("Margin", -1, typeof(CssBoxSides), s => s.Margin);
        d["BorderWidth"] = new("BorderWidth", -1, typeof(CssBoxSides), s => s.BorderWidths);
        d["BorderRadius"] = new("BorderRadius", -1, typeof(CssCorners), s => s.Radii);
        d["FontFamilyName"] = new("FontFamilyName", -1, typeof(string), s => string.Join(", ", s.Font.Family));
        return d.ToFrozenDictionary(StringComparer.Ordinal);
    }
}
