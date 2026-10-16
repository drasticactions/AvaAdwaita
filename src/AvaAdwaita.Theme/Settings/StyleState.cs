using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using AvaAdwaita.CssEngine.Cascade;
using AvaAdwaita.CssEngine.Values;

namespace AvaAdwaita.Settings;

/// <summary>
/// The resolved style state: dark, high contrast, accent, fonts, motion, icon theme and button layout,
/// merged from the settings sources in order and the theme's overrides.
/// </summary>
/// <remarks>Follows adw-settings.c for the merge and adw-style-manager.c for the overrides.</remarks>
public sealed class StyleState
{
    private readonly List<ISettingsSource> _sources = new();
    private readonly Dictionary<ISettingsSource, EventHandler> _handlers = new();
    private AdwColorScheme _colorScheme;
    private AdwAccentColor _accentOverride;
    private AdwHighContrast _highContrastOverride;
    private bool? _variantDark;

    /// <summary>Creates a state with the defaults: light, blue, Sans 10.</summary>
    public StyleState()
    {
        Recompute(raise: false);
    }

    /// <summary>Raised after the resolved state changes, on the thread that caused it.</summary>
    public event EventHandler<StyleStateChange>? Changed;

    /// <summary>The sources, highest priority first.</summary>
    public IReadOnlyList<ISettingsSource> Sources => _sources;

    /// <summary>The merged values of all sources.</summary>
    public SettingsValues Merged { get; private set; } = SettingsValues.Empty;

    /// <summary>The application's color scheme preference.</summary>
    public AdwColorScheme ColorScheme
    {
        get => _colorScheme;
        set
        {
            _colorScheme = value;
            Recompute();
        }
    }

    /// <summary>The accent color override; <see cref="AdwAccentColor.System"/> follows the sources.</summary>
    public AdwAccentColor AccentOverride
    {
        get => _accentOverride;
        set
        {
            _accentOverride = value;
            Recompute();
        }
    }

    /// <summary>The high contrast override.</summary>
    public AdwHighContrast HighContrastOverride
    {
        get => _highContrastOverride;
        set
        {
            _highContrastOverride = value;
            Recompute();
        }
    }

    /// <summary>
    /// A theme variant the application set directly, which acts as <see cref="AdwColorScheme.ForceDark"/>
    /// or <see cref="AdwColorScheme.ForceLight"/> while <see cref="ColorScheme"/> is <see cref="AdwColorScheme.Default"/>.
    /// </summary>
    public bool? RequestedDark
    {
        get => _variantDark;
        set
        {
            _variantDark = value;
            Recompute();
        }
    }

    /// <summary>True when the resolved scheme is dark.</summary>
    public bool Dark { get; private set; }

    /// <summary>True when high contrast is on.</summary>
    public bool HighContrast { get; private set; }

    /// <summary>The resolved accent preset.</summary>
    public AdwAccentColor Accent { get; private set; } = AdwAccentColor.Blue;

    /// <summary>The accent background color.</summary>
    public CssColor AccentColor => AccentColors.ToColor(Accent);

    /// <summary>The interface font.</summary>
    public PangoFontDescription InterfaceFont { get; private set; } = PangoFontDescription.Parse("Sans 10");

    /// <summary>The document font.</summary>
    public PangoFontDescription DocumentFont { get; private set; } = PangoFontDescription.Parse("Sans 10");

    /// <summary>The monospace font.</summary>
    public PangoFontDescription MonospaceFont { get; private set; } = PangoFontDescription.Parse("Monospace 10");

    /// <summary>The window button layout, as <c>appmenu:close</c>.</summary>
    public string ButtonLayout { get; private set; } = "appmenu:close";

    /// <summary>Whether status shapes show, as GTK's <c>gtk-show-status-shapes</c>: a switch's on and off marks.</summary>
    public bool ShowStatusShapes { get; private set; }

    /// <summary>False when animations are off or motion is reduced.</summary>
    public bool AnimationsEnabled { get; private set; } = true;

    /// <summary>True when the system asks for reduced motion.</summary>
    public bool ReducedMotion { get; private set; }

    /// <summary>The freedesktop icon theme, or null.</summary>
    public string? IconTheme { get; private set; }

    /// <summary>True when a source reported a color scheme, as <c>adw_style_manager_get_system_supports_color_schemes</c>.</summary>
    public bool SupportsColorSchemes { get; private set; }

    /// <summary>True when a source reported an accent color.</summary>
    public bool SupportsAccentColors { get; private set; }

    /// <summary>The media state <c>@media</c> queries test.</summary>
    public CssMediaState Media => new(Dark, HighContrast ? CssContrast.More : CssContrast.NoPreference, ReducedMotion);

    /// <summary>The name of the first source that supplied a value, for diagnostics.</summary>
    public string SourceName { get; private set; } = "defaults";

    /// <summary>Adds a source after the existing ones; earlier sources win.</summary>
    public void AddSource(ISettingsSource source)
    {
        _sources.Add(source);
        EventHandler handler = (_, _) => Recompute();
        _handlers[source] = handler;
        source.Changed += handler;
        Recompute();
    }

    /// <summary>Removes a source without disposing it.</summary>
    public bool RemoveSource(ISettingsSource source)
    {
        if (!_sources.Remove(source))
        {
            return false;
        }

        if (_handlers.Remove(source, out var handler))
        {
            source.Changed -= handler;
        }

        Recompute();
        return true;
    }

    /// <summary>Removes and disposes every source.</summary>
    public void ClearSources()
    {
        foreach (var s in _sources)
        {
            if (_handlers.Remove(s, out var handler))
            {
                s.Changed -= handler;
            }

            s.Dispose();
        }

        _sources.Clear();
        Recompute();
    }

    private void Recompute(bool raise = true)
    {
        var merged = SettingsValues.Empty;
        string? name = null;
        foreach (var s in _sources)
        {
            var c = s.Current;
            if (name is null && c != SettingsValues.Empty)
            {
                name = s.Name;
            }

            merged = merged.Or(c);
        }

        Merged = merged;
        SourceName = name ?? "defaults";
        var before = (Dark, HighContrast, Accent, InterfaceFont, DocumentFont, MonospaceFont, ButtonLayout, AnimationsEnabled, ReducedMotion, IconTheme, ShowStatusShapes);

        var system = merged.ColorScheme ?? SystemColorScheme.Default;
        SupportsColorSchemes = merged.ColorScheme is not null;
        var scheme = _colorScheme;
        if (scheme == AdwColorScheme.Default && _variantDark is { } vd)
        {
            scheme = vd ? AdwColorScheme.ForceDark : AdwColorScheme.ForceLight;
        }

        Dark = scheme switch
        {
            AdwColorScheme.ForceLight => false,
            AdwColorScheme.PreferLight => system == SystemColorScheme.PreferDark,
            AdwColorScheme.PreferDark => system != SystemColorScheme.PreferLight,
            AdwColorScheme.ForceDark => true,
            _ => system == SystemColorScheme.PreferDark,
        };

        HighContrast = _highContrastOverride switch
        {
            AdwHighContrast.On => true,
            AdwHighContrast.Off => false,
            _ => merged.HighContrast ?? false,
        };

        SupportsAccentColors = merged.AccentRgb is not null || merged.AccentPreset is not null;
        Accent = _accentOverride != AdwAccentColor.System
            ? _accentOverride
            : merged.AccentPreset ?? (merged.AccentRgb is { } rgb ? AccentColors.Nearest(rgb.R, rgb.G, rgb.B) : AdwAccentColor.Blue);

        var iface = merged.FontName ?? "Sans 10";
        InterfaceFont = PangoFontDescription.Parse(iface);
        DocumentFont = PangoFontDescription.Parse(merged.DocumentFontName ?? iface);
        var mono = merged.MonospaceFontName;
        MonospaceFont = mono is not null ? PangoFontDescription.Parse(mono) : InterfaceFont with { Family = "Monospace" };
        ButtonLayout = merged.ButtonLayout ?? "appmenu:close";
        ReducedMotion = merged.ReducedMotion ?? false;
        AnimationsEnabled = (merged.EnableAnimations ?? true) && !ReducedMotion;
        IconTheme = merged.IconTheme;
        ShowStatusShapes = merged.ShowStatusShapes ?? false;

        if (!raise)
        {
            return;
        }

        var change = StyleStateChange.None;
        if (before.Dark != Dark)
        {
            change |= StyleStateChange.ColorScheme;
        }

        if (before.HighContrast != HighContrast)
        {
            change |= StyleStateChange.Contrast;
        }

        if (before.Accent != Accent)
        {
            change |= StyleStateChange.Accent;
        }

        if (before.InterfaceFont != InterfaceFont || before.DocumentFont != DocumentFont || before.MonospaceFont != MonospaceFont)
        {
            change |= StyleStateChange.Fonts;
        }

        if (before.ShowStatusShapes != ShowStatusShapes)
        {
            change |= StyleStateChange.StatusShapes;
        }

        if (before.ButtonLayout != ButtonLayout)
        {
            change |= StyleStateChange.ButtonLayout;
        }

        if (before.AnimationsEnabled != AnimationsEnabled || before.ReducedMotion != ReducedMotion)
        {
            change |= StyleStateChange.Motion;
        }

        if (before.IconTheme != IconTheme)
        {
            change |= StyleStateChange.Icons;
        }

        if (change != StyleStateChange.None)
        {
            Changed?.Invoke(this, change);
        }
    }

    /// <summary>The accent provider's CSS, as generate_accent_css.</summary>
    public string AccentCss()
    {
        var (r, g, b, _) = AccentColor.ToRgba8();
        return string.Create(CultureInfo.InvariantCulture, $"@define-color accent_bg_color rgb({r},{g},{b});\n@define-color accent_fg_color white;\n");
    }

    /// <summary>The fonts provider's CSS, as generate_fonts_css.</summary>
    public string FontsCss()
    {
        static string Size(PangoFontDescription f) => f.Size is { } s
            ? s.ToString("R", CultureInfo.InvariantCulture) + (f.SizeIsAbsolute ? "px" : "pt")
            : "10pt";

        var sb = new StringBuilder(":root {\n");
        sb.Append("  --document-font-family: ").Append(DocumentFont.Family ?? "Sans").Append(";\n");
        sb.Append("  --document-font-size: ").Append(Size(DocumentFont)).Append(";\n");
        sb.Append("  --monospace-font-family: ").Append(MonospaceFont.Family ?? "Monospace").Append(";\n");
        sb.Append("  --monospace-font-size: ").Append(Size(MonospaceFont)).Append(";\n");
        sb.Append('}');
        return sb.ToString();
    }
}
