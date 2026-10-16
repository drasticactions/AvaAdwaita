using System.Collections.Generic;

namespace AvaAdwaita.Sources;

/// <summary>The libadwaita stylesheet the theme runs: its text, its assets and where it came from.</summary>
/// <param name="IsInstalled">True for the installed library's stylesheet, false for the bundled copy.</param>
/// <param name="LibraryPath">The library it was read from.</param>
/// <param name="Version">The libadwaita version, or null when unknown.</param>
/// <param name="Css">The stylesheet.</param>
/// <param name="Assets">The asset files by name, as <c>check.svg</c>.</param>
/// <param name="IsKnownClean">True when the CSS is known to parse cleanly, so it can be parsed lazily.</param>
/// <param name="FallbackReason">Why the installed stylesheet was not used, or null.</param>
public sealed record StyleSheetSource(bool IsInstalled, string? LibraryPath, string? Version, string Css, IReadOnlyDictionary<string, byte[]> Assets, bool IsKnownClean, string? FallbackReason)
{
    /// <summary>The base URI of <c>url()</c>s in the stylesheet.</summary>
    public const string BaseUri = "resource:///org/gnome/Adwaita/styles/";

    /// <summary>A label for diagnostics: "installed 1.10.0" or "bundled 1.10.0".</summary>
    public string Description => IsInstalled ? $"installed {Version ?? "(unknown version)"}" : $"bundled {StyleSheetLoader.BundledVersion}";
}
