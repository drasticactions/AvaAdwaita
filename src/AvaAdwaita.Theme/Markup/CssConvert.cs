using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using AvaAdwaita.CssEngine.Values;

namespace AvaAdwaita.Markup;

/// <summary>Converts computed CSS values to Avalonia values. Brushes are shared per color.</summary>
public static class CssConvert
{
    private static readonly ConcurrentDictionary<uint, IImmutableSolidColorBrush> s_brushes = new();
    private static readonly ConcurrentDictionary<string, FontFamily> s_families = new(StringComparer.Ordinal);
    private static IReadOnlyList<string>? s_fallback;
    private static IReadOnlyList<string>? s_monoFallback;

    /// <summary>
    /// The family names to fall back to when a CSS family is not installed. Defaults to Adwaita Sans when the
    /// app called <c>UseAdwaitaFonts()</c>.
    /// </summary>
    public static IReadOnlyList<string> FallbackFamilies
    {
        get => s_fallback ?? RegisteredFallback(0);
        set => s_fallback = value;
    }

    /// <summary>The family names monospace text falls back to. Defaults to Adwaita Mono when the app called <c>UseAdwaitaFonts()</c>.</summary>
    public static IReadOnlyList<string> MonospaceFallbackFamilies
    {
        get => s_monoFallback ?? RegisteredFallback(1);
        set => s_monoFallback = value;
    }

    private static IReadOnlyList<string> RegisteredFallback(int index) =>
        AppContext.GetData("AvaAdwaita.Fonts.Fallback") is string s && s.Split('|') is { } parts && parts.Length > index ? [parts[index]] : [];

    /// <summary>The Avalonia color, clamped to 8-bit sRGB.</summary>
    public static Color Color(CssColor c)
    {
        var (r, g, b, a) = c.ToRgba8();
        return Avalonia.Media.Color.FromArgb(a, r, g, b);
    }

    /// <summary>A shared immutable brush for the color.</summary>
    public static IImmutableSolidColorBrush Brush(CssColor c)
    {
        var argb = c.ToArgb();
        return s_brushes.GetOrAdd(argb, static v => new ImmutableSolidColorBrush(v));
    }

    /// <summary>An Avalonia font family from a CSS family list, with <see cref="FallbackFamilies"/> appended.</summary>
    public static FontFamily FontFamily(IReadOnlyList<string> families)
    {
        var parts = new List<string>();
        var mono = false;
        var registered = FallbackFamilies.Concat(MonospaceFallbackFamilies).ToArray();
        foreach (var f in families)
        {
            // A bundled face wins over an installed one of the same name: the installed Adwaita Sans is a
            // variable font, and Avalonia cannot pick its weights.
            var bundled = registered.FirstOrDefault(r => r.EndsWith("#" + f, StringComparison.Ordinal));
            parts.Add(bundled ?? GenericFamily(f));
            mono |= f is "Monospace" or "monospace";
        }

        foreach (var f in mono ? [.. MonospaceFallbackFamilies, .. FallbackFamilies] : FallbackFamilies)
        {
            if (!parts.Contains(f))
            {
                parts.Add(f);
            }
        }

        var key = string.Join(", ", parts);
        return s_families.GetOrAdd(key, static k => new FontFamily(k));
    }

    // GTK's generic names: Pango resolves them through fontconfig, and so does Skia on Linux.
    private static string GenericFamily(string f) => f switch
    {
        "Sans" or "sans" or "sans-serif" => OperatingSystem.IsLinux() ? "sans-serif" : Avalonia.Media.FontFamily.DefaultFontFamilyName,
        "Monospace" or "monospace" => OperatingSystem.IsLinux() ? "monospace" : "Consolas, Menlo, monospace",
        _ => f,
    };

    /// <summary>The nearest Avalonia weight.</summary>
    public static FontWeight FontWeight(double w) => (FontWeight)(int)Math.Clamp(Math.Round(w), 1, 1000);

    /// <summary>The Avalonia font style for <c>normal</c>, <c>italic</c> or <c>oblique</c>.</summary>
    public static FontStyle FontStyle(string s) => s switch
    {
        "italic" => Avalonia.Media.FontStyle.Italic,
        "oblique" => Avalonia.Media.FontStyle.Oblique,
        _ => Avalonia.Media.FontStyle.Normal,
    };

    internal static Thickness Thickness(CssBoxSides s) => new(s.Left, s.Top, s.Right, s.Bottom);

    internal static CornerRadius CornerRadius(CssCorners c, double width = 0, double height = 0) =>
        new(c.TopLeft.X.Resolve(width), c.TopRight.X.Resolve(width), c.BottomRight.X.Resolve(width), c.BottomLeft.X.Resolve(width));

    /// <summary>Converts a computed value to <paramref name="target"/>, or returns null when it cannot.</summary>
    internal static object? To(object? value, Type target)
    {
        switch (value)
        {
            case null:
                return null;
            case CssColor c when target == typeof(Color):
                return Color(c);
            case CssColor c when target.IsAssignableFrom(typeof(ImmutableSolidColorBrush)):
                return Brush(c);
            case double d when target == typeof(double):
                return d;
            case double d when target == typeof(FontWeight):
                return FontWeight(d);
            case double d when target == typeof(Thickness):
                return new Thickness(d);
            case double d when target == typeof(CornerRadius):
                return new CornerRadius(d);
            case double d when target == typeof(GridLength):
                return new GridLength(d);
            case CssBoxSides s when target == typeof(Thickness):
                return Thickness(s);
            case CssCorners c when target == typeof(CornerRadius):
                return CornerRadius(c);
            case CssCorner c when target == typeof(CornerRadius):
                return new CornerRadius(c.X.Value);
            case CssCorner c when target == typeof(double):
                return c.X.Value;
            case CssNumber n when target == typeof(double):
                return n.Value;
            case CssList<string> f when target == typeof(FontFamily):
                return FontFamily(f);
            case string s when target == typeof(FontStyle):
                return FontStyle(s);
            case string s when target == typeof(FontFeatureCollection):
                return Features(s);
            case CssPosition p when target == typeof(double):
                return p.X.Value;
            case CssLineHeight lh when target == typeof(double):
                return lh.IsNormal ? double.NaN : lh.Px > 0 ? lh.Px : lh.Factor;
            default:
                if (target.IsInstanceOfType(value))
                {
                    return value;
                }

                if (target == typeof(string) || target == typeof(object))
                {
                    return value.ToString();
                }

                return null;
        }
    }

    /// <summary>OpenType features for <c>font-variant-numeric</c>.</summary>
    internal static FontFeatureCollection? Features(string numeric)
    {
        if (numeric is "normal" or "")
        {
            return null;
        }

        var features = new FontFeatureCollection();
        foreach (var word in numeric.Split(' '))
        {
            var tag = word switch
            {
                "tabular-nums" => "tnum",
                "proportional-nums" => "pnum",
                "lining-nums" => "lnum",
                "oldstyle-nums" => "onum",
                "slashed-zero" => "zero",
                "ordinal" => "ordn",
                "diagonal-fractions" => "frac",
                "stacked-fractions" => "afrc",
                _ => null,
            };
            if (tag is not null)
            {
                features.Add(new FontFeature { Tag = tag, Value = 1 });
            }
        }

        return features;
    }
}
