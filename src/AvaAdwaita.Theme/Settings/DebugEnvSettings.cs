using System;
using AvaAdwaita.CssEngine.Values;

namespace AvaAdwaita.Settings;

/// <summary>
/// libadwaita's debug environment variables: <c>ADW_DEBUG_COLOR_SCHEME</c>,
/// <c>ADW_DEBUG_HIGH_CONTRAST</c> and <c>ADW_DEBUG_ACCENT_COLOR</c>.
/// </summary>
public sealed class DebugEnvSettings : ISettingsSource
{
    /// <summary>Reads the variables.</summary>
    public DebugEnvSettings(Func<string, string?>? getenv = null)
    {
        getenv ??= Environment.GetEnvironmentVariable;
        SystemColorScheme? scheme = getenv("ADW_DEBUG_COLOR_SCHEME") switch
        {
            "default" => SystemColorScheme.Default,
            "prefer-dark" => SystemColorScheme.PreferDark,
            "prefer-light" => SystemColorScheme.PreferLight,
            _ => null,
        };

        bool? hc = getenv("ADW_DEBUG_HIGH_CONTRAST") switch
        {
            "1" => true,
            "0" => false,
            _ => null,
        };

        var accentText = getenv("ADW_DEBUG_ACCENT_COLOR");
        AdwAccentColor? preset = AccentColors.ParsePreset(accentText);
        (double, double, double)? rgb = null;
        if (preset is null && !string.IsNullOrEmpty(accentText))
        {
            var r = new CssEngine.Syntax.CssReader(CssEngine.Syntax.CssComponentParser.Parse(accentText));
            if (CssColorParser.Parse(r) is CssColorExpr.Literal lit)
            {
                var (cr, cg, cb, _) = lit.Color.ToSrgb();
                rgb = (cr, cg, cb);
            }
        }

        Current = new SettingsValues { ColorScheme = scheme, HighContrast = hc, AccentPreset = preset, AccentRgb = rgb };
    }

    /// <inheritdoc/>
    public string Name => "environment";

    /// <inheritdoc/>
    public SettingsValues Current { get; }

    /// <inheritdoc/>
    public event EventHandler? Changed
    {
        add { }
        remove { }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
    }
}
