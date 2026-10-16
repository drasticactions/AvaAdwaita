using System;
using System.Globalization;

namespace AvaAdwaita.CssEngine.Values;

/// <summary>A resolved color in its own color space, with missing components, as GTK's <c>GtkCssColor</c>.</summary>
/// <remarks>GTK computes colors in single precision. So does this type, so results match GTK's.</remarks>
public readonly struct CssColor : IEquatable<CssColor>
{
    internal CssColor(CssColorSpace space, float c0, float c1, float c2, float alpha, byte missing)
    {
        Space = space;
        C0 = c0;
        C1 = c1;
        C2 = c2;
        Alpha = alpha;
        Missing = missing;
    }

    /// <summary>Transparent black.</summary>
    public static CssColor Transparent { get; } = new(CssColorSpace.Srgb, 0, 0, 0, 0, 0);

    /// <summary>Opaque white.</summary>
    public static CssColor White { get; } = new(CssColorSpace.Srgb, 1, 1, 1, 1, 0);

    /// <summary>Opaque black.</summary>
    public static CssColor Black { get; } = new(CssColorSpace.Srgb, 0, 0, 0, 1, 0);

    internal CssColorSpace Space { get; }

    internal float C0 { get; }

    internal float C1 { get; }

    internal float C2 { get; }

    /// <summary>The alpha, 0 to 1.</summary>
    public float Alpha { get; }

    internal byte Missing { get; }

    internal float this[int i] => i switch
    {
        0 => C0,
        1 => C1,
        2 => C2,
        _ => Alpha,
    };

    internal bool IsMissing(int i) => (Missing & (1 << i)) != 0;

    /// <summary>Creates an sRGB color from 0–1 components.</summary>
    public static CssColor FromSrgb(float r, float g, float b, float a = 1) => new(CssColorSpace.Srgb, r, g, b, a, 0);

    /// <summary>Creates an sRGB color from 8-bit components.</summary>
    public static CssColor FromRgb8(byte r, byte g, byte b, byte a = 255) => FromSrgb(r / 255f, g / 255f, b / 255f, a / 255f);

    /// <summary>The color in sRGB, unclamped.</summary>
    internal (float R, float G, float B, float A) ToSrgb()
    {
        if (Space == CssColorSpace.Srgb)
        {
            return (C0, C1, C2, Alpha);
        }

        var c = CssColorMath.Convert(this, CssColorSpace.Srgb);
        return (c.C0, c.C1, c.C2, c.Alpha);
    }

    /// <summary>The color as clamped 8-bit sRGB, rounded as GTK prints it.</summary>
    public (byte R, byte G, byte B, byte A) ToRgba8()
    {
        var (r, g, b, a) = ToSrgb();
        return (To8(r), To8(g), To8(b), To8(a));
    }

    private static byte To8(float v) => (byte)(int)(0.5 + (Math.Clamp(v, 0f, 1f) * 255.0));

    /// <summary>Packs the color as 0xAARRGGBB.</summary>
    public uint ToArgb()
    {
        var (r, g, b, a) = ToRgba8();
        return ((uint)a << 24) | ((uint)r << 16) | ((uint)g << 8) | b;
    }

    /// <summary>True when the color is fully transparent.</summary>
    public bool IsTransparent => Alpha <= 0;

    /// <inheritdoc/>
    public bool Equals(CssColor other) =>
        Space == other.Space && C0.Equals(other.C0) && C1.Equals(other.C1) && C2.Equals(other.C2) && Alpha.Equals(other.Alpha) && Missing == other.Missing;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is CssColor c && Equals(c);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Space, C0, C1, C2, Alpha, Missing);

    /// <summary>Equality.</summary>
    public static bool operator ==(CssColor a, CssColor b) => a.Equals(b);

    /// <summary>Inequality.</summary>
    public static bool operator !=(CssColor a, CssColor b) => !a.Equals(b);

    /// <summary>Prints the color as <c>rgb()</c> or <c>rgba()</c> with 8-bit channels.</summary>
    public override string ToString()
    {
        var (r, g, b, a) = ToRgba8();
        if (a == 255)
        {
            return string.Create(CultureInfo.InvariantCulture, $"rgb({r},{g},{b})");
        }

        var (_, _, _, af) = ToSrgb();
        return string.Create(CultureInfo.InvariantCulture, $"rgba({r},{g},{b},{Math.Round(Math.Clamp(af, 0, 1), 3)})");
    }
}
