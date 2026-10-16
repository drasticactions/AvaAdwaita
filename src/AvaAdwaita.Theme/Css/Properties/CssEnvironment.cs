namespace AvaAdwaita.CssEngine.Properties;

/// <summary>The settings computed values depend on, as GTK reads them from <c>GtkSettings</c>.</summary>
/// <param name="FontFamily">The interface font family, the initial <c>font-family</c>.</param>
/// <param name="FontSizePx">The interface font size in px, which <c>medium</c> and <c>rem</c> mean.</param>
internal sealed record CssEnvironment(string FontFamily, double FontSizePx)
{
    /// <summary>GTK's defaults without settings: Sans at 10pt.</summary>
    public static CssEnvironment Default { get; } = new("Sans", 10 * 96.0 / 72.0);
}
