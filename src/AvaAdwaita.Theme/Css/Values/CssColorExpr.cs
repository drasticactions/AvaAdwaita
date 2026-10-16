using System;
using System.Collections.Generic;
using System.Globalization;

namespace AvaAdwaita.CssEngine.Values;

/// <summary>A specified color: a literal, <c>currentColor</c>, a named color or a color function over others.</summary>
/// <remarks>Port of the value tree in gtkcsscolorvalue.c.</remarks>
internal abstract class CssColorExpr : IEquatable<CssColorExpr>
{
    public static readonly CssColorExpr Current = new CurrentExpr();
    public static readonly CssColorExpr TransparentLiteral = new Literal(CssColor.Transparent);

    /// <summary>True when resolving needs the element's color.</summary>
    public abstract bool UsesCurrentColor { get; }

    /// <summary>True when resolving needs the named color table.</summary>
    public abstract bool UsesNames { get; }

    /// <summary>Resolves the color, or returns null when a name is unknown or forms a cycle.</summary>
    public CssColor? Resolve(ICssColorScope scope, CssColor current) => Resolve(scope, current, null);

    internal abstract CssColor? Resolve(ICssColorScope scope, CssColor current, List<string>? cycle);

    public abstract bool Equals(CssColorExpr? other);

    public override bool Equals(object? obj) => obj is CssColorExpr e && Equals(e);

    public override int GetHashCode() => GetType().GetHashCode();

    /// <summary>A resolved color.</summary>
    public sealed class Literal(CssColor color) : CssColorExpr
    {
        public CssColor Color { get; } = color;

        public override bool UsesCurrentColor => false;

        public override bool UsesNames => false;

        internal override CssColor? Resolve(ICssColorScope scope, CssColor current, List<string>? cycle) => Color;

        public override bool Equals(CssColorExpr? other) => other is Literal l && l.Color.Equals(Color);

        public override int GetHashCode() => Color.GetHashCode();

        public override string ToString() => Color.ToString();
    }

    /// <summary><c>currentColor</c>.</summary>
    private sealed class CurrentExpr : CssColorExpr
    {
        public override bool UsesCurrentColor => true;

        public override bool UsesNames => false;

        internal override CssColor? Resolve(ICssColorScope scope, CssColor current, List<string>? cycle) => current;

        public override bool Equals(CssColorExpr? other) => other is CurrentExpr;

        public override string ToString() => "currentcolor";
    }

    /// <summary>A <c>@name</c> reference to a <c>@define-color</c>.</summary>
    public sealed class Named(string name) : CssColorExpr
    {
        public string Name { get; } = name;

        public override bool UsesCurrentColor => true;

        public override bool UsesNames => true;

        internal override CssColor? Resolve(ICssColorScope scope, CssColor current, List<string>? cycle)
        {
            if (cycle is not null && cycle.Contains(Name))
            {
                return null;
            }

            var def = scope.LookupColor(Name);
            if (def is null)
            {
                return null;
            }

            cycle ??= new List<string>();
            cycle.Add(Name);
            var r = def.Resolve(scope, current, cycle);
            cycle.RemoveAt(cycle.Count - 1);
            return r;
        }

        public override bool Equals(CssColorExpr? other) => other is Named n && n.Name == Name;

        public override int GetHashCode() => Name.GetHashCode(StringComparison.Ordinal);

        public override string ToString() => "@" + Name;
    }

    /// <summary><c>color-mix(in space, a p1, b p2)</c>. A negative percentage means it was not given.</summary>
    public sealed class Mix(CssColorSpace space, CssHueInterpolation hue, CssColorExpr a, float p1, CssColorExpr b, float p2) : CssColorExpr
    {
        public CssColorSpace Space { get; } = space;

        public CssHueInterpolation Hue { get; } = hue;

        public CssColorExpr A { get; } = a;

        public float P1 { get; } = p1;

        public CssColorExpr B { get; } = b;

        public float P2 { get; } = p2;

        public override bool UsesCurrentColor => A.UsesCurrentColor || B.UsesCurrentColor;

        public override bool UsesNames => A.UsesNames || B.UsesNames;

        internal override CssColor? Resolve(ICssColorScope scope, CssColor current, List<string>? cycle)
        {
            if (A.Resolve(scope, current, cycle) is not { } ca || B.Resolve(scope, current, cycle) is not { } cb)
            {
                return null;
            }

            return CssColorMath.ColorMix(Space, Hue, ca, cb, P1, P2);
        }

        public override bool Equals(CssColorExpr? other) =>
            other is Mix m && m.Space == Space && m.Hue == Hue && m.P1 == P1 && m.P2 == P2 && m.A.Equals(A) && m.B.Equals(B);

        public override int GetHashCode() => HashCode.Combine(Space, A, B, P1, P2);

        public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"color-mix(in {Space}, {A} {P1}%, {B} {P2}%)");
    }

    /// <summary>A color function, with or without <c>from</c>. Null channels are <c>none</c>.</summary>
    public sealed class Function(CssColorSpace space, bool legacyRgb, CssColorExpr? origin, CssNumeric?[] channels, bool alphaGiven) : CssColorExpr
    {
        public CssColorSpace Space { get; } = space;

        public bool LegacyRgb { get; } = legacyRgb;

        public CssColorExpr? Origin { get; } = origin;

        public CssNumeric?[] Channels { get; } = channels;

        public bool AlphaGiven { get; } = alphaGiven;

        public override bool UsesCurrentColor => Origin?.UsesCurrentColor == true;

        public override bool UsesNames => Origin?.UsesNames == true;

        internal override CssColor? Resolve(ICssColorScope scope, CssColor current, List<string>? cycle)
        {
            float[]? originChannels = null;
            if (Origin is not null)
            {
                if (Origin.Resolve(scope, current, cycle) is not { } o)
                {
                    return null;
                }

                var conv = CssColorMath.Convert(o, Space);
                originChannels = [conv.C0, conv.C1, conv.C2, conv.Alpha];
                if (Space == CssColorSpace.Srgb && LegacyRgb)
                {
                    originChannels[0] *= 255;
                    originChannels[1] *= 255;
                    originChannels[2] *= 255;
                }
            }

            var ctx = CssNumericContext.Default with { Channels = originChannels };
            Span<float> v = stackalloc float[4];
            byte missing = 0;
            for (var i = 0; i < 4; i++)
            {
                var n = Channels[i];
                if (n is null)
                {
                    if (i == 3 && !AlphaGiven)
                    {
                        v[3] = originChannels?[3] ?? 1;
                        continue;
                    }

                    missing |= (byte)(1 << i);
                    v[i] = 0;
                    continue;
                }

                var (lower, upper) = CssColorMath.CoordRange(Space, LegacyRgb, i);
                var num = n.Compute(ctx);
                float value;
                if (num.HasPercent)
                {
                    // As GTK, a percentage spans the coordinate range from its lower end, so oklab's 0% a is -0.4.
                    value = float.IsNaN(lower) ? (float)num.Value : (float)(num.Value + (num.Percent * (upper - lower) / 100.0) + lower);
                }
                else
                {
                    value = (float)num.Value;
                }

                if (Origin is null)
                {
                    value = ClampAbsolute(Space, LegacyRgb, i, value);
                }

                v[i] = value;
            }

            if (Space == CssColorSpace.Srgb && LegacyRgb)
            {
                v[0] /= 255f;
                v[1] /= 255f;
                v[2] /= 255f;
            }

            v[3] = Math.Clamp(v[3], 0, 1);
            return CssColorMath.WithMissing(Space, v[0], v[1], v[2], v[3], missing);
        }

        // The clamping GTK's channel parsers do for absolute colors.
        private static float ClampAbsolute(CssColorSpace space, bool legacy, int i, float v)
        {
            if (i == 3)
            {
                return Math.Clamp(v, 0, 1);
            }

            switch (space)
            {
                case CssColorSpace.Srgb when legacy:
                    return Math.Clamp(v, 0, 255);
                case CssColorSpace.Hsl:
                case CssColorSpace.Hwb:
                    if (i == 0)
                    {
                        v %= 360;
                        return v < 0 ? v + 360 : v;
                    }

                    return Math.Clamp(v, 0, 100);
                case CssColorSpace.Oklab:
                    return i == 0 ? Math.Clamp(v, 0, 1) : v;
                case CssColorSpace.Oklch:
                    if (i == 0)
                    {
                        return Math.Clamp(v, 0, 1);
                    }

                    if (i == 1)
                    {
                        return Math.Max(v, 0);
                    }

                    v %= 360;
                    return v < 0 ? v + 360 : v;
                default:
                    return v;
            }
        }

        public override bool Equals(CssColorExpr? other)
        {
            if (other is not Function f || f.Space != Space || f.LegacyRgb != LegacyRgb || f.AlphaGiven != AlphaGiven)
            {
                return false;
            }

            if (!Equals(f.Origin, Origin))
            {
                return false;
            }

            for (var i = 0; i < 4; i++)
            {
                if (!Equals(f.Channels[i], Channels[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public override int GetHashCode() => HashCode.Combine(Space, Origin, Channels[0], Channels[1], Channels[2]);

        public override string ToString() => $"{Space}({(Origin is null ? string.Empty : "from " + Origin + " ")}{string.Join(" ", (IEnumerable<CssNumeric?>)Channels)})";
    }

    /// <summary>The deprecated <c>shade()</c>, <c>lighter()</c>, <c>darker()</c>, <c>alpha()</c> and <c>mix()</c>.</summary>
    public sealed class Legacy(string op, CssColorExpr a, CssColorExpr? b, double factor) : CssColorExpr
    {
        public string Op { get; } = op;

        public CssColorExpr A { get; } = a;

        public CssColorExpr? B { get; } = b;

        public double Factor { get; } = factor;

        public override bool UsesCurrentColor => A.UsesCurrentColor || B?.UsesCurrentColor == true;

        public override bool UsesNames => A.UsesNames || B?.UsesNames == true;

        internal override CssColor? Resolve(ICssColorScope scope, CssColor current, List<string>? cycle)
        {
            if (A.Resolve(scope, current, cycle) is not { } ca)
            {
                return null;
            }

            switch (Op)
            {
                case "alpha":
                    return CssColorMath.ScaleAlpha(ca, Factor);
                case "mix":
                    if (B!.Resolve(scope, current, cycle) is not { } cb)
                    {
                        return null;
                    }

                    return CssColorMath.LegacyMix(ca, cb, Factor);
                default:
                    return CssColorMath.Shade(ca, Factor);
            }
        }

        public override bool Equals(CssColorExpr? other) =>
            other is Legacy l && l.Op == Op && l.Factor == Factor && l.A.Equals(A) && Equals(l.B, B);

        public override int GetHashCode() => HashCode.Combine(Op, A, Factor);

        public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{Op}({A}, {B}, {Factor})");
    }
}
