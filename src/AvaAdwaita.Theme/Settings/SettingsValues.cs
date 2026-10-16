namespace AvaAdwaita.Settings;

/// <summary>The settings one source found. Null means the source does not know the value.</summary>
public sealed record SettingsValues
{
    /// <summary>No values.</summary>
    public static SettingsValues Empty { get; } = new();

    /// <summary><c>org.freedesktop.appearance color-scheme</c>.</summary>
    public SystemColorScheme? ColorScheme { get; init; }

    /// <summary>The accent color as sRGB 0–1, before snapping to a preset.</summary>
    public (double R, double G, double B)? AccentRgb { get; init; }

    /// <summary>A preset accent, when the source names one (as GSettings' <c>accent-color</c>).</summary>
    public AdwAccentColor? AccentPreset { get; init; }

    /// <summary>High contrast.</summary>
    public bool? HighContrast { get; init; }

    /// <summary>Reduced motion.</summary>
    public bool? ReducedMotion { get; init; }

    /// <summary><c>org.gnome.desktop.interface enable-animations</c>.</summary>
    public bool? EnableAnimations { get; init; }

    /// <summary>The interface font, as a Pango description.</summary>
    public string? FontName { get; init; }

    /// <summary>The document font, as a Pango description.</summary>
    public string? DocumentFontName { get; init; }

    /// <summary>The monospace font, as a Pango description.</summary>
    public string? MonospaceFontName { get; init; }

    /// <summary>The freedesktop icon theme.</summary>
    public string? IconTheme { get; init; }

    /// <summary><c>org.gnome.desktop.wm.preferences button-layout</c>.</summary>
    public string? ButtonLayout { get; init; }

    /// <summary><c>org.gnome.desktop.a11y.interface show-status-shapes</c>: GTK's <c>gtk-show-status-shapes</c>.</summary>
    public bool? ShowStatusShapes { get; init; }

    /// <summary>Fills the values this one lacks from <paramref name="fallback"/>.</summary>
    public SettingsValues Or(SettingsValues fallback) => new()
    {
        ColorScheme = ColorScheme ?? fallback.ColorScheme,
        AccentRgb = AccentRgb ?? (AccentPreset is null ? fallback.AccentRgb : null),
        AccentPreset = AccentPreset ?? (AccentRgb is null ? fallback.AccentPreset : null),
        HighContrast = HighContrast ?? fallback.HighContrast,
        ReducedMotion = ReducedMotion ?? fallback.ReducedMotion,
        EnableAnimations = EnableAnimations ?? fallback.EnableAnimations,
        FontName = FontName ?? fallback.FontName,
        DocumentFontName = DocumentFontName ?? fallback.DocumentFontName,
        MonospaceFontName = MonospaceFontName ?? fallback.MonospaceFontName,
        IconTheme = IconTheme ?? fallback.IconTheme,
        ButtonLayout = ButtonLayout ?? fallback.ButtonLayout,
        ShowStatusShapes = ShowStatusShapes ?? fallback.ShowStatusShapes,
    };
}
