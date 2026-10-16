using System;
using AvaAdwaita.CssEngine.Values;

namespace AvaAdwaita.Settings;

/// <summary>libadwaita's accent color presets, snapping and standalone colors.</summary>
/// <remarks>Port of adw-accent-color.c.</remarks>
public static class AccentColors
{
    /// <summary>The preset's color, as adw_accent_color_to_rgba.</summary>
    public static CssColor ToColor(AdwAccentColor accent) => accent switch
    {
        AdwAccentColor.Teal => CssColor.FromRgb8(0x21, 0x90, 0xa4),
        AdwAccentColor.Green => CssColor.FromRgb8(0x3a, 0x94, 0x4a),
        AdwAccentColor.Yellow => CssColor.FromRgb8(0xc8, 0x88, 0x00),
        AdwAccentColor.Orange => CssColor.FromRgb8(0xed, 0x5b, 0x00),
        AdwAccentColor.Red => CssColor.FromRgb8(0xe6, 0x2d, 0x42),
        AdwAccentColor.Pink => CssColor.FromRgb8(0xd5, 0x61, 0x99),
        AdwAccentColor.Purple => CssColor.FromRgb8(0x91, 0x41, 0xac),
        AdwAccentColor.Slate => CssColor.FromRgb8(0x6f, 0x83, 0x96),
        _ => CssColor.FromRgb8(0x35, 0x84, 0xe4),
    };

    /// <summary>The preset nearest to a color, as adw_accent_color_nearest_from_rgba.</summary>
    public static AdwAccentColor Nearest(double r, double g, double b)
    {
        var lin = CssColorMath.Convert(CssColor.FromSrgb((float)r, (float)g, (float)b), CssColorSpace.Oklch);
        var c = lin.C1;
        var h = lin.C2;
        if (c < 0.04)
        {
            return AdwAccentColor.Slate;
        }

        if (h > 345)
        {
            return AdwAccentColor.Pink;
        }

        if (h > 280)
        {
            return AdwAccentColor.Purple;
        }

        if (h > 230)
        {
            return AdwAccentColor.Blue;
        }

        if (h > 175)
        {
            return AdwAccentColor.Teal;
        }

        if (h > 115)
        {
            return AdwAccentColor.Green;
        }

        if (h > 75.5)
        {
            return AdwAccentColor.Yellow;
        }

        if (h > 35)
        {
            return AdwAccentColor.Orange;
        }

        if (h > 10)
        {
            return AdwAccentColor.Red;
        }

        return AdwAccentColor.Pink;
    }

    /// <summary>
    /// A color usable as text on the window background: OKLab lightness capped at 0.5 in light mode,
    /// raised to at least 0.85 in dark mode, as adw_rgba_to_standalone.
    /// </summary>
    public static CssColor ToStandalone(CssColor color, bool dark)
    {
        var lab = CssColorMath.Convert(color, CssColorSpace.Oklab);
        var l = dark ? Math.Max(lab.C0, 0.85f) : Math.Min(lab.C0, 0.5f);
        var srgb = CssColorMath.Convert(CssColorMath.Init(CssColorSpace.Oklab, l, lab.C1, lab.C2, lab.Alpha), CssColorSpace.Srgb);
        return CssColor.FromSrgb(Math.Clamp(srgb.C0, 0, 1), Math.Clamp(srgb.C1, 0, 1), Math.Clamp(srgb.C2, 0, 1), color.Alpha);
    }

    /// <summary>Parses a preset name, as <c>ADW_DEBUG_ACCENT_COLOR</c> and GSettings' <c>accent-color</c> use.</summary>
    public static AdwAccentColor? ParsePreset(string? name) => name?.Trim().ToLowerInvariant() switch
    {
        "blue" => AdwAccentColor.Blue,
        "teal" => AdwAccentColor.Teal,
        "green" => AdwAccentColor.Green,
        "yellow" => AdwAccentColor.Yellow,
        "orange" => AdwAccentColor.Orange,
        "red" => AdwAccentColor.Red,
        "pink" => AdwAccentColor.Pink,
        "purple" => AdwAccentColor.Purple,
        "slate" => AdwAccentColor.Slate,
        _ => null,
    };
}
