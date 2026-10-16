using System;
using System.Collections.Frozen;
using System.Collections.Generic;

namespace AvaAdwaita.CssEngine.Properties;

/// <summary>Looks up longhands and shorthands by name.</summary>
internal static partial class CssPropertyTable
{
    private static FrozenDictionary<string, CssProperty>? s_byName;
    private static FrozenDictionary<string, CssShorthand>? s_shorthandsByName;

    public static CssProperty? Lookup(string name)
    {
        s_byName ??= BuildLonghands();
        return s_byName.GetValueOrDefault(name);
    }

    public static CssShorthand? LookupShorthand(string name)
    {
        s_shorthandsByName ??= BuildShorthands();
        return s_shorthandsByName.GetValueOrDefault(name);
    }

    private static FrozenDictionary<string, CssProperty> BuildLonghands()
    {
        var d = new Dictionary<string, CssProperty>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in All)
        {
            d[p.Name] = p;
        }

        return d.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    private static FrozenDictionary<string, CssShorthand> BuildShorthands()
    {
        var d = new Dictionary<string, CssShorthand>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in Shorthands)
        {
            d[s.Name] = s;
        }

        return d.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    private static int[] AllIds()
    {
        var ids = new int[Count];
        for (var i = 0; i < ids.Length; i++)
        {
            ids[i] = i;
        }

        return ids;
    }
}
