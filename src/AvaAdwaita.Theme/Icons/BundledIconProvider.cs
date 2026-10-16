using System.Collections.Concurrent;
using System.IO;

namespace AvaAdwaita.Icons;

/// <summary>The symbolic icons AvaAdwaita bundles, from adwaita-icon-theme (CC-BY-SA-3.0) and libadwaita (CC0-1.0).</summary>
public sealed class BundledIconProvider : IIconProvider
{
    private readonly ConcurrentDictionary<string, SymbolicSvg?> _cache = new();

    /// <inheritdoc/>
    public IconData? Lookup(string name, int size, bool rtl)
    {
        if (rtl && Load(name + "-rtl") is { } r)
        {
            return new IconData(name + "-rtl", r, null, true, "bundled");
        }

        return Load(name) is { } svg ? new IconData(name, svg, null, true, "bundled") : null;
    }

    private SymbolicSvg? Load(string name) => _cache.GetOrAdd(name, static n =>
    {
        using var s = typeof(BundledIconProvider).Assembly.GetManifestResourceStream("AvaAdwaita.Icons." + n + ".svg");
        if (s is null)
        {
            return null;
        }

        using var m = new MemoryStream();
        s.CopyTo(m);
        return SymbolicSvg.Parse(m.ToArray());
    });
}
