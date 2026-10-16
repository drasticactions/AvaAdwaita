using System;
using System.Linq;

namespace AvaAdwaita.CssEngine.Values;

/// <summary>
/// A color matrix and offset, as GTK applies the color <c>filter</c> functions to everything a widget draws: a
/// color (r, g, b, a) becomes (r, g, b, a) × M + offset, unpremultiplied and clamped.
/// </summary>
/// <remarks>Follows gtk_css_filter_get_matrix and gtk_css_filter_value_compute_matrix (gtkcssfiltervalue.c).</remarks>
internal sealed class CssColorMatrix : IEquatable<CssColorMatrix>
{
    private const double R = 0.2126;
    private const double G = 0.7152;
    private const double B = 0.0722;

    private readonly double[] _m;
    private readonly double[] _o;

    private CssColorMatrix(double[] m, double[] o)
    {
        _m = m;
        _o = o;
    }

    /// <summary>The matrix of a color filter function, or null for one that is not a color matrix.</summary>
    public static CssColorMatrix? Of(CssFilterFunction f)
    {
        var v = f.Amount;
        double[] Scale(double s) => [s, 0, 0, 0, 0, s, 0, 0, 0, 0, s, 0, 0, 0, 0, 1];
        switch (f.Kind)
        {
            case CssFilterKind.Brightness:
                return new(Scale(v), [0, 0, 0, 0]);
            case CssFilterKind.Contrast:
                return new(Scale(v), [0.5 - (0.5 * v), 0.5 - (0.5 * v), 0.5 - (0.5 * v), 0]);
            case CssFilterKind.Grayscale:
                return new([1 - ((1 - R) * v), R * v, R * v, 0, G * v, 1 - ((1 - G) * v), G * v, 0, B * v, B * v, 1 - ((1 - B) * v), 0, 0, 0, 0, 1], [0, 0, 0, 0]);
            case CssFilterKind.HueRotate:
                {
                    var a = v * Math.PI / 180;
                    var c = Math.Cos(a);
                    var s = Math.Sin(a);
                    return new(
                        [
                            0.213 + (0.787 * c) - (0.213 * s), 0.213 - (0.213 * c) + (0.143 * s), 0.213 - (0.213 * c) - (0.787 * s), 0,
                            0.715 - (0.715 * c) - (0.715 * s), 0.715 + (0.285 * c) + (0.140 * s), 0.715 - (0.715 * c) + (0.715 * s), 0,
                            0.072 - (0.072 * c) + (0.928 * s), 0.072 - (0.072 * c) - (0.283 * s), 0.072 + (0.928 * c) + (0.072 * s), 0,
                            0, 0, 0, 1,
                        ],
                        [0, 0, 0, 0]);
                }

            case CssFilterKind.Invert:
                return new(Scale(1 - (2 * v)), [v, v, v, 0]);
            case CssFilterKind.Saturate:
                return new([R + ((1 - R) * v), R - (R * v), R - (R * v), 0, G - (G * v), G + ((1 - G) * v), G - (G * v), 0, B - (B * v), B - (B * v), B + ((1 - B) * v), 0, 0, 0, 0, 1], [0, 0, 0, 0]);
            case CssFilterKind.Sepia:
                return new([1 - (0.607 * v), 0.349 * v, 0.272 * v, 0, 0.769 * v, 1 - (0.314 * v), 0.534 * v, 0, 0.189 * v, 0.168 * v, 1 - (0.869 * v), 0, 0, 0, 0, 1], [0, 0, 0, 0]);
            default:
                return null;
        }
    }

    /// <summary>This matrix, then <paramref name="next"/>: (c × M₁ + o₁) × M₂ + o₂.</summary>
    public CssColorMatrix Then(CssColorMatrix next)
    {
        var m = new double[16];
        for (var i = 0; i < 4; i++)
        {
            for (var j = 0; j < 4; j++)
            {
                double sum = 0;
                for (var k = 0; k < 4; k++)
                {
                    sum += _m[(i * 4) + k] * next._m[(k * 4) + j];
                }

                m[(i * 4) + j] = sum;
            }
        }

        var o = new double[4];
        for (var j = 0; j < 4; j++)
        {
            double sum = next._o[j];
            for (var k = 0; k < 4; k++)
            {
                sum += _o[k] * next._m[(k * 4) + j];
            }

            o[j] = sum;
        }

        return new(m, o);
    }

    /// <summary>The color through the matrix, in sRGB.</summary>
    public CssColor Apply(CssColor c)
    {
        var (r, g, b, a) = c.ToSrgb();
        double[] v = [r, g, b, a];
        var o = new float[4];
        for (var j = 0; j < 4; j++)
        {
            var sum = _o[j];
            for (var k = 0; k < 4; k++)
            {
                sum += v[k] * _m[(k * 4) + j];
            }

            o[j] = (float)Math.Clamp(sum, 0, 1);
        }

        return CssColor.FromSrgb(o[0], o[1], o[2], o[3]);
    }

    /// <inheritdoc/>
    public bool Equals(CssColorMatrix? other) => other is not null && _m.SequenceEqual(other._m) && _o.SequenceEqual(other._o);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is CssColorMatrix m && Equals(m);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(_m[0], _m[5], _m[10], _o[0]);
}
