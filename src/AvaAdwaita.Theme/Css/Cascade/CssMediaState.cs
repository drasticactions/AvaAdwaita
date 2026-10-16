namespace AvaAdwaita.CssEngine.Cascade;

/// <summary>The values <c>@media</c> queries test.</summary>
/// <param name="Dark"><c>prefers-color-scheme: dark</c>.</param>
/// <param name="Contrast"><c>prefers-contrast</c>.</param>
/// <param name="ReducedMotion"><c>prefers-reduced-motion: reduce</c>.</param>
public readonly record struct CssMediaState(bool Dark, CssContrast Contrast, bool ReducedMotion)
{
    /// <summary>Light, no contrast preference, motion allowed.</summary>
    public static CssMediaState Default { get; } = new(false, CssContrast.NoPreference, false);
}
