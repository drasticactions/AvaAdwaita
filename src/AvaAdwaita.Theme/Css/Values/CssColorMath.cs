// Ported from GTK 4.24: gtk/gtkcsscolor.c, gtk/gtkcolorutils.c, gdk/gdkhsla.c.
using System;

namespace AvaAdwaita.CssEngine.Values;

/// <summary>Color space conversion and interpolation.</summary>
/// <remarks>Port of gtk_css_color_convert and gtk_css_color_interpolate (gtkcsscolor.c).</remarks>
internal static class CssColorMath
{
    /// <summary>Creates a color and marks its powerless components missing, as gtk_css_color_init.</summary>
    public static CssColor Init(CssColorSpace space, float c0, float c1, float c2, float alpha)
    {
        byte missing = 0;
        switch (space)
        {
            case CssColorSpace.Hsl:
                if (MathF.Abs(c2) < 0.001f)
                {
                    missing |= 1;
                }

                break;
            case CssColorSpace.Hwb:
                if (c1 + c2 > 99.999f)
                {
                    missing |= 1;
                }

                break;
            case CssColorSpace.Oklch:
                if (MathF.Abs(c1) < 0.001f)
                {
                    missing |= 4;
                }

                break;
        }

        return new CssColor(space, c0, c1, c2, alpha, missing);
    }

    public static CssColor WithMissing(CssColorSpace space, float c0, float c1, float c2, float alpha, byte missing)
    {
        // gtk_css_color_init_with_missing zeroes missing components.
        if ((missing & 1) != 0)
        {
            c0 = 0;
        }

        if ((missing & 2) != 0)
        {
            c1 = 0;
        }

        if ((missing & 4) != 0)
        {
            c2 = 0;
        }

        if ((missing & 8) != 0)
        {
            alpha = 0;
        }

        return new CssColor(space, c0, c1, c2, alpha, missing);
    }

    public static bool IsPolar(CssColorSpace space) => space is CssColorSpace.Hsl or CssColorSpace.Hwb or CssColorSpace.Oklch;

    /// <summary>The range a percentage maps onto, as gtk_css_color_space_get_coord_range.</summary>
    public static (float Lower, float Upper) CoordRange(CssColorSpace space, bool legacyRgbScale, int coord)
    {
        if (coord == 3)
        {
            return (0, 1);
        }

        return space switch
        {
            CssColorSpace.Srgb => (0, legacyRgbScale ? 255 : 1),
            CssColorSpace.Hsl or CssColorSpace.Hwb => coord == 0 ? (float.NaN, float.NaN) : (0, 100),
            CssColorSpace.Oklab => coord == 0 ? (0, 1) : (-0.4f, 0.4f),
            CssColorSpace.Oklch => coord switch
            {
                0 => (0, 1),
                1 => (0, 0.4f),
                _ => (float.NaN, float.NaN),
            },
            _ => (0, 1),
        };
    }

    /// <summary>The channel keywords of relative color syntax.</summary>
    public static string[] ChannelNames(CssColorSpace space) => space switch
    {
        CssColorSpace.Srgb or CssColorSpace.SrgbLinear or CssColorSpace.DisplayP3 or CssColorSpace.Rec2020 => ["r", "g", "b", "alpha"],
        CssColorSpace.Hsl => ["h", "s", "l", "alpha"],
        CssColorSpace.Hwb => ["h", "w", "b", "alpha"],
        CssColorSpace.Oklab => ["l", "a", "b", "alpha"],
        CssColorSpace.Oklch => ["l", "c", "h", "alpha"],
        CssColorSpace.Xyz => ["x", "y", "z", "alpha"],
        _ => ["alpha"],
    };

    /// <summary>The coordinate of <paramref name="color"/> in <paramref name="space"/>, as gtk_css_color_value_get_coord.</summary>
    public static float Coord(CssColor color, CssColorSpace space, bool legacyRgbScale, int coord)
    {
        var c = Convert(color, space);
        var v = c[coord];
        if (space == CssColorSpace.Srgb && legacyRgbScale && coord < 3)
        {
            v *= 255f;
        }

        return v;
    }

    /// <summary>Converts a color between spaces (CSS Color 4, §17).</summary>
    public static CssColor Convert(CssColor input, CssColorSpace dest)
    {
        var o = input;
        o = ToRectangular(o);
        o = ToLinear(o);
        o = LinearToLinear(o, dest);
        o = FromLinear(o, dest);
        o = FromRectangular(o, dest);
        return o;
    }

    private static CssColor ToRectangular(CssColor c)
    {
        switch (c.Space)
        {
            case CssColorSpace.Hsl:
                {
                    var (r, g, b) = HslToRgb(c.C0, c.C1 / 100, c.C2 / 100);
                    return new CssColor(CssColorSpace.Srgb, r, g, b, c.Alpha, 0);
                }

            case CssColorSpace.Hwb:
                {
                    var (r, g, b) = HwbToRgb(c.C0, c.C1 / 100, c.C2 / 100);
                    return new CssColor(CssColorSpace.Srgb, r, g, b, c.Alpha, 0);
                }

            case CssColorSpace.Oklch:
                {
                    var (l, a, b) = OklchToOklab(c.C0, c.C1, c.C2);
                    return new CssColor(CssColorSpace.Oklab, l, a, b, c.Alpha, 0);
                }

            default:
                return c;
        }
    }

    private static CssColor ToLinear(CssColor c)
    {
        switch (c.Space)
        {
            case CssColorSpace.Srgb:
                return Init(CssColorSpace.SrgbLinear, UnapplyGamma(c.C0), UnapplyGamma(c.C1), UnapplyGamma(c.C2), c.Alpha);
            case CssColorSpace.DisplayP3:
                {
                    var (r, g, b) = P3ToRgb(c.C0, c.C1, c.C2);
                    return Init(CssColorSpace.SrgbLinear, UnapplyGamma(r), UnapplyGamma(g), UnapplyGamma(b), c.Alpha);
                }

            case CssColorSpace.Xyz:
                {
                    var (r, g, b) = XyzToLinearSrgb(c.C0, c.C1, c.C2);
                    return Init(CssColorSpace.SrgbLinear, r, g, b, c.Alpha);
                }

            case CssColorSpace.Rec2020:
                {
                    var (x, y, z) = Rec2020ToXyz(c.C0, c.C1, c.C2);
                    var (r, g, b) = XyzToLinearSrgb(x, y, z);
                    return Init(CssColorSpace.SrgbLinear, r, g, b, c.Alpha);
                }

            default:
                return c;
        }
    }

    private static CssColor LinearToLinear(CssColor c, CssColorSpace dest)
    {
        var destLinear = dest is CssColorSpace.Oklch or CssColorSpace.Oklab ? CssColorSpace.Oklab : CssColorSpace.SrgbLinear;
        if (destLinear == CssColorSpace.SrgbLinear && c.Space == CssColorSpace.Oklab)
        {
            var (r, g, b) = OklabToLinearSrgb(c.C0, c.C1, c.C2);
            return Init(destLinear, r, g, b, c.Alpha);
        }

        if (destLinear == CssColorSpace.Oklab && c.Space == CssColorSpace.SrgbLinear)
        {
            var (l, a, b) = LinearSrgbToOklab(c.C0, c.C1, c.C2);
            return Init(destLinear, l, a, b, c.Alpha);
        }

        return c;
    }

    private static CssColor FromLinear(CssColor c, CssColorSpace dest)
    {
        switch (dest)
        {
            case CssColorSpace.Srgb:
            case CssColorSpace.Hsl:
            case CssColorSpace.Hwb:
                return Init(CssColorSpace.Srgb, ApplyGamma(c.C0), ApplyGamma(c.C1), ApplyGamma(c.C2), c.Alpha);
            case CssColorSpace.DisplayP3:
                {
                    var (r, g, b) = RgbToP3(ApplyGamma(c.C0), ApplyGamma(c.C1), ApplyGamma(c.C2));
                    return Init(CssColorSpace.DisplayP3, r, g, b, c.Alpha);
                }

            case CssColorSpace.Xyz:
                {
                    var (x, y, z) = LinearSrgbToXyz(c.C0, c.C1, c.C2);
                    return Init(CssColorSpace.Xyz, x, y, z, c.Alpha);
                }

            case CssColorSpace.Rec2020:
                {
                    var (x, y, z) = LinearSrgbToXyz(c.C0, c.C1, c.C2);
                    var (r, g, b) = XyzToRec2020(x, y, z);
                    return Init(CssColorSpace.Rec2020, r, g, b, c.Alpha);
                }

            default:
                return c;
        }
    }

    private static CssColor FromRectangular(CssColor c, CssColorSpace dest)
    {
        switch (dest)
        {
            case CssColorSpace.Hsl:
                {
                    var (h, s, l) = RgbToHsl(c.C0, c.C1, c.C2);
                    return Init(dest, h, s * 100, l * 100, c.Alpha);
                }

            case CssColorSpace.Hwb:
                {
                    var (h, w, b) = RgbToHwb(c.C0, c.C1, c.C2);
                    return Init(dest, h, w * 100, b * 100, c.Alpha);
                }

            case CssColorSpace.Oklch:
                {
                    var (l, ch, h) = OklabToOklch(c.C0, c.C1, c.C2);
                    return Init(dest, l, ch, h, c.Alpha);
                }

            default:
                return c;
        }
    }

    private static readonly int[][] s_analogous =
    [
        [0, 1, 2, -1, -1, -1, -1, -1, 3], // srgb
        [0, 1, 2, -1, -1, -1, -1, -1, 3], // srgb-linear
        [-1, -1, -1, 2, 1, 0, -1, -1, 3], // hsl
        [-1, -1, -1, -1, -1, 0, -1, -1, 3], // hwb
        [-1, -1, -1, 0, -1, -1, 1, 2, 3], // oklab
        [-1, -1, -1, 0, 1, 2, -1, -1, 3], // oklch
        [0, 1, 2, -1, -1, -1, -1, -1, 3], // display-p3
        [0, 1, 2, -1, -1, -1, -1, -1, 3], // xyz
        [0, 1, 2, -1, -1, -1, -1, -1, 3], // rec2020
    ];

    private static byte AnalogousMissing(CssColor color, CssColorSpace space)
    {
        var src = s_analogous[(int)color.Space];
        var dest = s_analogous[(int)space];
        byte missing = 0;
        for (var i = 0; i < 4; i++)
        {
            if (!color.IsMissing(i))
            {
                continue;
            }

            for (var j = 0; j < 9; j++)
            {
                if (src[j] == i)
                {
                    var idx = dest[j];
                    if (idx != -1)
                    {
                        missing |= (byte)(1 << idx);
                    }

                    break;
                }
            }
        }

        return missing;
    }

    /// <summary>Interpolates two colors (CSS Color 4, §12), premultiplied, in <paramref name="space"/>.</summary>
    public static CssColor Interpolate(CssColor from, CssColor to, float progress, CssColorSpace space, CssHueInterpolation interp)
    {
        var fromMissing = AnalogousMissing(from, space);
        var toMissing = AnalogousMissing(to, space);
        var f = Convert(from, space);
        var t = Convert(to, space);
        Span<float> fv = [f.C0, f.C1, f.C2, f.Alpha];
        Span<float> tv = [t.C0, t.C1, t.C2, t.Alpha];
        byte missing = 0;
        for (var i = 0; i < 4; i++)
        {
            var m1 = (fromMissing & (1 << i)) != 0;
            var m2 = (toMissing & (1 << i)) != 0;
            if (m1 && !m2)
            {
                fv[i] = tv[i];
            }
            else if (!m1 && m2)
            {
                tv[i] = fv[i];
            }

            if (m1 && m2)
            {
                missing |= (byte)(1 << i);
            }
        }

        var hueIdx = space switch
        {
            CssColorSpace.Hsl or CssColorSpace.Hwb => 0,
            CssColorSpace.Oklch => 2,
            _ => -1,
        };
        if (hueIdx >= 0)
        {
            AdjustHue(ref fv[hueIdx], ref tv[hueIdx], interp);
        }

        Premultiply(fv, space);
        Premultiply(tv, space);

        Span<float> v = stackalloc float[4];
        for (var i = 0; i < 4; i++)
        {
            v[i] = (fv[i] * (1 - progress)) + (tv[i] * progress);
        }

        var outColor = WithMissing(space, v[0], v[1], v[2], v[3], missing);
        Span<float> o = [outColor.C0, outColor.C1, outColor.C2, outColor.Alpha];
        if (hueIdx >= 0)
        {
            o[hueIdx] %= 360;
            if (o[hueIdx] < 0)
            {
                o[hueIdx] += 360;
            }
        }

        if ((missing & 8) == 0 && o[3] != 0)
        {
            for (var i = 0; i < 3; i++)
            {
                if (i == hueIdx || (missing & (1 << i)) != 0)
                {
                    continue;
                }

                o[i] /= o[3];
            }
        }

        return new CssColor(space, o[0], o[1], o[2], o[3], missing);
    }

    private static void Premultiply(Span<float> v, CssColorSpace space)
    {
        var hueIdx = space switch
        {
            CssColorSpace.Hsl or CssColorSpace.Hwb => 0,
            CssColorSpace.Oklch => 2,
            _ => -1,
        };
        for (var i = 0; i < 3; i++)
        {
            if (i != hueIdx)
            {
                v[i] *= v[3];
            }
        }
    }

    private static void AdjustHue(ref float h1, ref float h2, CssHueInterpolation interp)
    {
        switch (interp)
        {
            case CssHueInterpolation.Shorter:
                {
                    var d = h2 - h1;
                    if (d > 180)
                    {
                        h1 += 360;
                    }
                    else if (d < -180)
                    {
                        h2 += 360;
                    }

                    break;
                }

            case CssHueInterpolation.Longer:
                {
                    var d = h2 - h1;
                    if (d > 0 && d < 180)
                    {
                        h1 += 360;
                    }
                    else if (d > -180 && d <= 0)
                    {
                        h2 += 360;
                    }

                    break;
                }

            case CssHueInterpolation.Increasing:
                if (h2 < h1)
                {
                    h2 += 360;
                }

                break;
            case CssHueInterpolation.Decreasing:
                if (h1 < h2)
                {
                    h1 += 360;
                }

                break;
        }
    }

    /// <summary>color-mix() percentage normalization; a negative percentage means "not given".</summary>
    public static (float P1, float P2, float AlphaMultiplier) NormalizeMixPercentages(float p1, float p2)
    {
        if (p1 < -0.5f && p2 < -0.5f)
        {
            return (50, 50, 1);
        }

        if (p1 < -0.5f)
        {
            return (100 - p2, p2, 1);
        }

        if (p2 < -0.5f)
        {
            return (p1, 100 - p1, 1);
        }

        return (p1 / (p1 + p2) * 100, p2 / (p1 + p2) * 100, MathF.Min(1, (p1 + p2) * 0.01f));
    }

    /// <summary>Port of apply_color_mix (gtkcsscolorvalue.c).</summary>
    public static CssColor ColorMix(CssColorSpace space, CssHueInterpolation interp, CssColor c1, CssColor c2, float p1, float p2)
    {
        var (_, np2, alphaMul) = NormalizeMixPercentages(p1, p2);
        var o = Interpolate(c1, c2, np2 * 0.01f, space, interp);
        return WithMissing(o.Space, o.C0, o.C1, o.C2, o.Alpha * alphaMul, o.Missing);
    }

    /// <summary>Port of apply_mix, the deprecated <c>mix()</c> and the transition of colors.</summary>
    public static CssColor LegacyMix(CssColor c1, CssColor c2, double factor)
    {
        var (r1, g1, b1, a1) = c1.ToSrgb();
        var (r2, g2, b2, a2) = c2.ToSrgb();
        var a = Math.Clamp(a1 + ((a2 - a1) * factor), 0, 1);
        if (a <= 0)
        {
            return CssColor.Transparent;
        }

        double Ch(float x1, float x2) => Math.Clamp((x1 * a1) + (((x2 * a2) - (x1 * a1)) * factor), 0, 1) / a;
        return FromRgbaDouble(Ch(r1, r2), Ch(g1, g2), Ch(b1, b2), a);
    }

    /// <summary>Port of apply_shade.</summary>
    public static CssColor Shade(CssColor c, double factor)
    {
        var (r, g, b, a) = c.ToSrgb();
        var (h, s, l) = RgbToHsl(r, g, b);
        l = Math.Clamp(l * (float)factor, 0, 1);
        s = Math.Clamp(s * (float)factor, 0, 1);
        var (r2, g2, b2) = HslToRgb(h, s, l);
        return FromRgbaDouble(r2, g2, b2, a);
    }

    /// <summary>Port of apply_alpha.</summary>
    public static CssColor ScaleAlpha(CssColor c, double factor)
    {
        var (r, g, b, a) = c.ToSrgb();
        return FromRgbaDouble(r, g, b, Math.Clamp(a * factor, 0, 1));
    }

    // GdkRGBA keeps floats too.
    private static CssColor FromRgbaDouble(double r, double g, double b, double a) =>
        Init(CssColorSpace.Srgb, (float)r, (float)g, (float)b, (float)a);

    private static float ApplyGamma(float v) => v > 0.0031308 ? (float)((1.055 * Math.Pow(v, 1 / 2.4)) - 0.055) : (float)(12.92 * v);

    private static float UnapplyGamma(float v) => v >= 0.04045 ? (float)Math.Pow((v + 0.055) / (1 + 0.055), 2.4) : (float)(v / 12.92);

    public static (float L, float A, float B) LinearSrgbToOklab(float red, float green, float blue)
    {
        var l = (0.4122214708f * red) + (0.5363325363f * green) + (0.0514459929f * blue);
        var m = (0.2119034982f * red) + (0.6806995451f * green) + (0.1073969566f * blue);
        var s = (0.0883024619f * red) + (0.2817188376f * green) + (0.6299787005f * blue);
        l = MathF.Cbrt(l);
        m = MathF.Cbrt(m);
        s = MathF.Cbrt(s);
        return ((0.2104542553f * l) + (0.7936177850f * m) - (0.0040720468f * s),
                (1.9779984951f * l) - (2.4285922050f * m) + (0.4505937099f * s),
                (0.0259040371f * l) + (0.7827717662f * m) - (0.8086757660f * s));
    }

    public static (float R, float G, float B) OklabToLinearSrgb(float L, float a, float b)
    {
        var l = L + (0.3963377774f * a) + (0.2158037573f * b);
        var m = L - (0.1055613458f * a) - (0.0638541728f * b);
        var s = L - (0.0894841775f * a) - (1.2914855480f * b);
        l = MathF.Pow(l, 3);
        m = MathF.Pow(m, 3);
        s = MathF.Pow(s, 3);
        return ((+4.0767416621f * l) - (3.3077115913f * m) + (0.2309699292f * s),
                (-1.2684380046f * l) + (2.6097574011f * m) - (0.3413193965f * s),
                (-0.0041960863f * l) - (0.7034186147f * m) + (1.7076147010f * s));
    }

    public static (float L, float C, float H) OklabToOklch(float L, float a, float b)
    {
        var c = MathF.Sqrt((a * a) + (b * b));
        var h = (float)(Math.Atan2(b, a) * 180 / Math.PI);
        h %= 360;
        if (h < 0)
        {
            h += 360;
        }

        return (L, c, h);
    }

    public static (float L, float A, float B) OklchToOklab(float L, float C, float H)
    {
        var rad = (float)(H * Math.PI / 180);
        return (L, MathF.Cos(rad) * C, MathF.Sin(rad) * C);
    }

    /// <summary>Port of _gdk_hsla_init_from_rgba; hue in degrees, saturation and lightness 0–1.</summary>
    public static (float H, float S, float L) RgbToHsl(float red, float green, float blue)
    {
        float max, min;
        if (red > green)
        {
            max = red > blue ? red : blue;
            min = green < blue ? green : blue;
        }
        else
        {
            max = green > blue ? green : blue;
            min = red < blue ? red : blue;
        }

        var lightness = (max + min) / 2;
        float saturation = 0, hue = 0;
        if (max != min)
        {
            saturation = lightness <= 0.5f ? (max - min) / (max + min) : (max - min) / (2 - max - min);
            var delta = max - min;
            if (red == max)
            {
                hue = (green - blue) / delta;
            }
            else if (green == max)
            {
                hue = 2 + ((blue - red) / delta);
            }
            else if (blue == max)
            {
                hue = 4 + ((red - green) / delta);
            }

            hue *= 60;
            if (hue < 0)
            {
                hue += 360;
            }
        }

        return (hue, saturation, lightness);
    }

    /// <summary>Port of _gdk_rgba_init_from_hsla.</summary>
    public static (float R, float G, float B) HslToRgb(float hue, float saturation, float lightness)
    {
        var m2 = lightness <= 0.5f ? lightness * (1 + saturation) : lightness + saturation - (lightness * saturation);
        var m1 = (2 * lightness) - m2;
        if (saturation == 0)
        {
            return (lightness, lightness, lightness);
        }

        float Channel(float h)
        {
            h %= 360;
            if (h < 0)
            {
                h += 360;
            }

            if (h < 60)
            {
                return m1 + ((m2 - m1) * h / 60);
            }

            if (h < 180)
            {
                return m2;
            }

            if (h < 240)
            {
                return m1 + ((m2 - m1) * (240 - h) / 60);
            }

            return m1;
        }

        return (Channel(hue + 120), Channel(hue), Channel(hue - 120));
    }

    public static (float H, float W, float B) RgbToHwb(float red, float green, float blue)
    {
        var (h, _, _) = RgbToHsl(red, green, blue);
        return (h, MathF.Min(MathF.Min(red, green), blue), 1 - MathF.Max(MathF.Max(red, green), blue));
    }

    public static (float R, float G, float B) HwbToRgb(float hue, float white, float black)
    {
        if (white + black >= 1)
        {
            var gray = white / (white + black);
            return (gray, gray, gray);
        }

        var (r, g, b) = HslToRgb(hue, 1, 0.5f);
        return ((r * (1 - white - black)) + white, (g * (1 - white - black)) + white, (b * (1 - white - black)) + white);
    }

    private static (float, float, float) LinearSrgbToXyz(float r, float g, float b) =>
        ((float)((506752.0 / 1228815.0 * r) + (87881.0 / 245763.0 * g) + (12673.0 / 70218.0 * b)),
         (float)((87098.0 / 409605.0 * r) + (175762.0 / 245763.0 * g) + (12673.0 / 175545.0 * b)),
         (float)((7918.0 / 409605.0 * r) + (87881.0 / 737289.0 * g) + (1001167.0 / 1053270.0 * b)));

    private static (float, float, float) XyzToLinearSrgb(float x, float y, float z) =>
        ((float)((12831.0 / 3959.0 * x) - (329.0 / 214.0 * y) - (1974.0 / 3959.0 * z)),
         (float)((-(851781.0 / 878810.0) * x) + (1648619.0 / 878810.0 * y) + (36519.0 / 878810.0 * z)),
         (float)((705.0 / 12673.0 * x) - (2585.0 / 12673.0 * y) + (705.0 / 667.0 * z)));

    private static (float, float, float) LinP3ToXyz(float r, float g, float b) =>
        ((float)((608311.0 / 1250200.0 * r) + (189793.0 / 714400.0 * g) + (198249.0 / 1000160.0 * b)),
         (float)((35783.0 / 156275.0 * r) + (247089.0 / 357200.0 * g) + (198249.0 / 2500400.0 * b)),
         (float)((0 * r) + (32229.0 / 714400.0 * g) + (5220557.0 / 5000800.0 * b)));

    private static (float, float, float) XyzToLinP3(float x, float y, float z) =>
        ((float)((446124.0 / 178915.0 * x) - (333277.0 / 357830.0 * y) - (72051.0 / 178915.0 * z)),
         (float)((-(14852.0 / 17905.0) * x) + (63121.0 / 35810.0 * y) + (423.0 / 17905.0 * z)),
         (float)((11844.0 / 330415.0 * x) - (50337.0 / 660830.0 * y) + (316169.0 / 330415.0 * z)));

    private static (float, float, float) P3ToRgb(float pr, float pg, float pb)
    {
        var (x, y, z) = LinP3ToXyz(UnapplyGamma(pr), UnapplyGamma(pg), UnapplyGamma(pb));
        var (r, g, b) = XyzToLinearSrgb(x, y, z);
        return (ApplyGamma(r), ApplyGamma(g), ApplyGamma(b));
    }

    private static (float, float, float) RgbToP3(float red, float green, float blue)
    {
        var (x, y, z) = LinearSrgbToXyz(UnapplyGamma(red), UnapplyGamma(green), UnapplyGamma(blue));
        var (r, g, b) = XyzToLinP3(x, y, z);
        return (ApplyGamma(r), ApplyGamma(g), ApplyGamma(b));
    }

    private static float Rec2020Eotf(float v)
    {
        const float alpha = 1.09929682680944f;
        const float beta = 0.018053968510807f;
        var sign = v < 0 ? -1f : 1f;
        var abs = MathF.Abs(v);
        return abs < beta * 4.5f ? sign * abs / 4.5f : sign * MathF.Pow((abs + alpha - 1) / alpha, 1 / 0.45f);
    }

    private static float Rec2020Oetf(float v)
    {
        const float alpha = 1.09929682680944f;
        const float beta = 0.018053968510807f;
        var sign = v < 0 ? -1f : 1f;
        var abs = MathF.Abs(v);
        return abs > beta ? sign * ((alpha * MathF.Pow(abs, 0.45f)) - (alpha - 1)) : sign * 4.5f * abs;
    }

    private static (float, float, float) Rec2020ToXyz(float r, float g, float b)
    {
        r = Rec2020Eotf(r);
        g = Rec2020Eotf(g);
        b = Rec2020Eotf(b);
        return ((float)((63426534.0 / 99577255.0 * r) + (20160776.0 / 139408157.0 * g) + (47086771.0 / 278816314.0 * b)),
                (float)((26158966.0 / 99577255.0 * r) + (472592308.0 / 697040785.0 * g) + (8267143.0 / 139408157.0 * b)),
                (float)((0 * r) + (19567812.0 / 697040785.0 * g) + (295819943.0 / 278816314.0 * b)));
    }

    private static (float, float, float) XyzToRec2020(float x, float y, float z)
    {
        var r = (float)((30757411.0 / 17917100.0 * x) - (6372589.0 / 17917100.0 * y) - (4539589.0 / 17917100.0 * z));
        var g = (float)((-(19765991.0 / 29648200.0) * x) + (47925759.0 / 29648200.0 * y) + (467509.0 / 29648200.0 * z));
        var b = (float)((792561.0 / 44930125.0 * x) - (1921689.0 / 44930125.0 * y) + (42328811.0 / 44930125.0 * z));
        return (Rec2020Oetf(r), Rec2020Oetf(g), Rec2020Oetf(b));
    }
}
