using System;

namespace AvaAdwaita.CssEngine.Properties;

/// <summary>What parsing a value needs: how to resolve a relative <c>url()</c>.</summary>
internal sealed class CssParseContext
{
    public CssParseContext(Func<string, string>? resolveUrl = null)
    {
        ResolveUrl = resolveUrl ?? (u => u);
    }

    public static CssParseContext Default { get; } = new();

    public Func<string, string> ResolveUrl { get; }
}
