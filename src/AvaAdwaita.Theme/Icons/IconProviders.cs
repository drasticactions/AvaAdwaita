using System;
using System.Collections.Generic;
using AvaAdwaita.Settings;

namespace AvaAdwaita.Icons;

/// <summary>The icon providers in lookup order: on Linux the freedesktop theme, then the bundled icons.</summary>
public static class IconProviders
{
    private static IReadOnlyList<IIconProvider>? s_chain;
    private static string? s_theme;

    /// <summary>Raised after the icon theme changes.</summary>
    public static event EventHandler? Changed;

    /// <summary>Extra providers searched first, for applications.</summary>
    public static List<IIconProvider> Custom { get; } = new();

    /// <summary>Looks up an icon in every provider.</summary>
    public static IconData? Lookup(string name, int size, bool rtl)
    {
        foreach (var p in Custom)
        {
            if (p.Lookup(name, size, rtl) is { } c)
            {
                return c;
            }
        }

        foreach (var p in Chain())
        {
            if (p.Lookup(name, size, rtl) is { } d)
            {
                return d;
            }
        }

        return null;
    }

    private static IReadOnlyList<IIconProvider> Chain()
    {
        var theme = AdwaitaEnvironment.IsInitialized ? AdwaitaEnvironment.State.IconTheme : null;
        if (s_chain is not null && theme == s_theme)
        {
            return s_chain;
        }

        s_theme = theme;
        var list = new List<IIconProvider>();
        if (OperatingSystem.IsLinux() && AdwaitaEnvironment.Options.UseSystemSettings)
        {
            list.Add(new FreedesktopIconProvider(theme));
        }

        list.Add(new BundledIconProvider());
        s_chain = list;
        return list;
    }

    /// <summary>Drops the cached providers so the next lookup follows <see cref="StyleState.IconTheme"/>.</summary>
    public static void Reset()
    {
        s_chain = null;
        Changed?.Invoke(null, EventArgs.Empty);
    }
}
