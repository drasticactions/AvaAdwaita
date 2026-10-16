using System;

namespace AvaAdwaita.CssEngine.Values;

/// <summary>
/// A computed numeric value: a canonical amount (px, deg, ms or a plain number) plus a percentage
/// that is resolved later against a basis, as GTK keeps percentages relative where it can.
/// </summary>
internal readonly record struct CssNumber(double Value, double Percent, CssDimensionClass Class)
{
    public static CssNumber Px(double px) => new(px, 0, CssDimensionClass.Length);

    public static CssNumber Plain(double n) => new(n, 0, CssDimensionClass.Number);

    public static CssNumber Pct(double p) => new(0, p, CssDimensionClass.Percent);

    public bool HasPercent => Percent != 0 || Class == CssDimensionClass.Percent;

    /// <summary>Resolves the value with <paramref name="basis"/> as 100%.</summary>
    public double Resolve(double basis) => Value + (Percent * basis / 100.0);

    public static CssNumber operator +(CssNumber a, CssNumber b) =>
        new(a.Value + b.Value, a.Percent + b.Percent, Merge(a.Class, b.Class));

    public static CssNumber operator *(CssNumber a, double f) => new(a.Value * f, a.Percent * f, a.Class);

    private static CssDimensionClass Merge(CssDimensionClass a, CssDimensionClass b)
    {
        if (a == b)
        {
            return a;
        }

        if (a == CssDimensionClass.Percent)
        {
            return b;
        }

        if (b == CssDimensionClass.Percent)
        {
            return a;
        }

        return a == CssDimensionClass.Number ? b : a;
    }

    public static CssNumber Lerp(CssNumber a, CssNumber b, double t) =>
        new(a.Value + ((b.Value - a.Value) * t), a.Percent + ((b.Percent - a.Percent) * t), Merge(a.Class, b.Class));

    public override string ToString()
    {
        var unit = Class switch
        {
            CssDimensionClass.Length => "px",
            CssDimensionClass.Angle => "deg",
            CssDimensionClass.Time => "ms",
            _ => string.Empty,
        };
        if (Percent == 0)
        {
            return FormattableString.Invariant($"{Value}{unit}");
        }

        if (Value == 0)
        {
            return FormattableString.Invariant($"{Percent}%");
        }

        return FormattableString.Invariant($"calc({Percent}% + {Value}{unit})");
    }
}
