using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using AvaAdwaita.CssEngine.Values;

namespace AvaAdwaita.CssEngine.Properties;

/// <summary>
/// Prints specified values as CSS the engine's own parsers read back to an equal value, as GTK's
/// <c>gtk_css_value_print</c>. Values whose text depends on the property, such as the milliseconds of a time
/// list, are printed by <see cref="CssKind.Print"/>.
/// </summary>
internal static class CssSerializer
{
    private static readonly string[] s_wideKeywords = ["initial", "inherit", "unset", "revert", "default"];

    /// <summary>Prints a specified value, or a computed value a shorthand stored as one.</summary>
    public static string Print(object value)
    {
        var sb = new StringBuilder();
        Append(sb, value);
        return sb.ToString();
    }

    /// <summary>Prints a number with the fewest digits that read back to the same double.</summary>
    public static string Number(double v) => v.ToString("R", CultureInfo.InvariantCulture);

    /// <summary>Prints the items of a list, comma-separated.</summary>
    public static string Join<T>(IEnumerable<T> items, Func<T, string> print)
    {
        var sb = new StringBuilder();
        foreach (var i in items)
        {
            if (sb.Length > 0)
            {
                sb.Append(", ");
            }

            sb.Append(print(i));
        }

        return sb.ToString();
    }

    /// <summary>True when <paramref name="s"/> reads back as one identifier that is not a CSS-wide keyword.</summary>
    public static bool IsPlainIdent(string s)
    {
        if (s.Length == 0 || Array.Exists(s_wideKeywords, k => k.Equals(s, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        var start = s[0] == '-' ? 1 : 0;
        if (start == s.Length || !(IsNameStart(s[start]) || (start == 1 && s[1] == '-')))
        {
            return false;
        }

        for (var i = start; i < s.Length; i++)
        {
            if (!IsNameChar(s[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsNameStart(char c) => char.IsAsciiLetter(c) || c == '_' || c >= 0x80;

    private static bool IsNameChar(char c) => IsNameStart(c) || char.IsAsciiDigit(c) || c == '-';

    /// <summary>Prints an identifier, escaping what would not read back as one.</summary>
    public static string Ident(string s)
    {
        if (IsPlainIdent(s) || Array.IndexOf(s_wideKeywords, s.ToLowerInvariant()) >= 0)
        {
            return s;
        }

        var sb = new StringBuilder();
        for (var i = 0; i < s.Length; i++)
        {
            var c = s[i];
            var ok = i == 0 ? IsNameStart(c) || (c == '-' && s.Length > 1 && !char.IsAsciiDigit(s[1])) : IsNameChar(c);
            if (ok)
            {
                sb.Append(c);
            }
            else
            {
                sb.Append('\\').Append(((int)c).ToString("x", CultureInfo.InvariantCulture)).Append(' ');
            }
        }

        return sb.ToString();
    }

    /// <summary>Prints a double-quoted string.</summary>
    public static string String(string s)
    {
        var sb = new StringBuilder(s.Length + 2);
        AppendString(sb, s);
        return sb.ToString();
    }

    private static void AppendString(StringBuilder sb, string s)
    {
        sb.Append('"');
        foreach (var c in s)
        {
            if (c is '"' or '\\')
            {
                sb.Append('\\').Append(c);
            }
            else if (char.IsControl(c))
            {
                sb.Append('\\').Append(((int)c).ToString("x", CultureInfo.InvariantCulture)).Append(' ');
            }
            else
            {
                sb.Append(c);
            }
        }

        sb.Append('"');
    }

    private static void Append(StringBuilder sb, object value)
    {
        switch (value)
        {
            case string s:
                // Keyword kinds store the keywords themselves.
                sb.Append(s);
                break;
            case double d:
                sb.Append(Number(d));
                break;
            case CssNumeric n:
                AppendNumeric(sb, n, null);
                break;
            case CssNumber n:
                AppendNumber(sb, n);
                break;
            case CssColorExpr c:
                AppendColor(sb, c);
                break;
            case CssColor c:
                AppendColor(sb, c);
                break;
            case CssKinds.CornerSpec c:
                AppendPair(sb, c.X, c.Y);
                break;
            case CssValueParsers.PositionSpec p:
                AppendNumeric(sb, p.X, null);
                sb.Append(' ');
                AppendNumeric(sb, p.Y, null);
                break;
            case CssPosition p:
                AppendNumber(sb, p.X);
                sb.Append(' ');
                AppendNumber(sb, p.Y);
                break;
            case CssValueParsers.ShadowListSpec l:
                if (l.Items.Length == 0)
                {
                    sb.Append("none");
                }

                AppendJoined(sb, l.Items, s => AppendShadow(sb, s));
                break;
            case CssValueParsers.TransformSpec t:
                AppendTransform(sb, t);
                break;
            case CssValueParsers.FilterSpec f:
                AppendFilter(sb, f);
                break;
            case CssValueParsers.PaletteSpec p:
                AppendPalette(sb, p);
                break;
            case CssKinds.BackgroundImageSpec b:
                AppendJoined(sb, b.Items, i => AppendImageOrNone(sb, i));
                break;
            case CssKinds.NullableImageSpec n:
                AppendImageOrNone(sb, n.Item);
                break;
            case CssValueParsers.ImageSpec i:
                AppendImage(sb, i);
                break;
            case CssImage i:
                AppendImage(sb, i);
                break;
            case CssKinds.ISpecifiedList l:
                AppendJoined(sb, l.Items, i => Append(sb, i));
                break;
            case CssKinds.BackgroundSizeKind.SizeSpec s:
                AppendSize(sb, s.Width is null ? null : () => AppendNumeric(sb, s.Width, null), s.Height is null ? null : () => AppendNumeric(sb, s.Height, null));
                break;
            case CssBackgroundSize s:
                if (s.Cover || s.Contain)
                {
                    sb.Append(s.Cover ? "cover" : "contain");
                    break;
                }

                AppendSize(sb, s.Width is { } w ? () => AppendNumber(sb, w) : null, s.Height is { } h ? () => AppendNumber(sb, h) : null);
                break;
            case CssBackgroundRepeat r:
                sb.Append(Kebab(r.X));
                if (r.Y != r.X)
                {
                    sb.Append(' ').Append(Kebab(r.Y));
                }

                break;
            case CssKinds.BorderImageBoxKind.BoxSpec b:
                AppendBox(sb, b);
                break;
            case CssEasing e:
                AppendEasing(sb, e);
                break;
            case CssLineHeight { IsNormal: true }:
                sb.Append("normal");
                break;
            case CssList<CssFontSetting> l:
                if (l.Count == 0)
                {
                    sb.Append("normal");
                }

                AppendJoined(sb, l, f =>
                {
                    AppendString(sb, f.Tag);
                    sb.Append(' ').Append(Number(f.Value));
                });
                break;
            case CssList<string> l:
                AppendJoined(sb, l, s => sb.Append(Ident(s)));
                break;
            case Enum e:
                sb.Append(Kebab(e));
                break;
            case IEnumerable list:
                {
                    var any = false;
                    foreach (var i in list)
                    {
                        if (any)
                        {
                            sb.Append(", ");
                        }

                        Append(sb, i!);
                        any = true;
                    }

                    if (!any)
                    {
                        sb.Append("none");
                    }

                    break;
                }

            default:
                throw new NotSupportedException($"cannot print a {value.GetType()} as CSS");
        }
    }

    private static void AppendJoined<T>(StringBuilder sb, IEnumerable<T> items, Action<T> append)
    {
        var first = true;
        foreach (var i in items)
        {
            if (!first)
            {
                sb.Append(", ");
            }

            append(i);
            first = false;
        }
    }

    /// <summary>An enum member as a CSS keyword: <c>NoRepeat</c> is <c>no-repeat</c>.</summary>
    private static string Kebab(Enum e)
    {
        var name = e.ToString();
        var sb = new StringBuilder(name.Length + 4);
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsAsciiLetterUpper(c))
            {
                if (i > 0)
                {
                    sb.Append('-');
                }

                sb.Append(char.ToLowerInvariant(c));
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    // ---- numbers ----

    private static void AppendPair(StringBuilder sb, CssNumeric x, CssNumeric y)
    {
        AppendNumeric(sb, x, null);
        if (!x.Equals(y))
        {
            sb.Append(' ');
            AppendNumeric(sb, y, null);
        }
    }

    private static void AppendDimension(StringBuilder sb, double v, CssUnit unit)
    {
        sb.Append(Number(v));
        switch (unit)
        {
            case CssUnit.Number:
                break;
            case CssUnit.Percent:
                sb.Append('%');
                break;
            default:
                sb.Append(unit.ToString().ToLowerInvariant());
                break;
        }
    }

    /// <summary>Prints a numeric tree. <paramref name="channels"/> names the channel keywords of a relative color.</summary>
    private static void AppendNumeric(StringBuilder sb, CssNumeric n, string[]? channels)
    {
        switch (n.Op)
        {
            case CssCalcOp.Leaf:
                AppendDimension(sb, n.SimpleValue, n.SimpleUnit);
                break;
            case CssCalcOp.Channel:
                sb.Append(channels is null ? throw new InvalidOperationException("a channel keyword outside a relative color") : channels[(int)n.SimpleValue]);
                break;
            case CssCalcOp.Min:
            case CssCalcOp.Max:
            case CssCalcOp.Clamp:
                sb.Append(n.Op.ToString().ToLowerInvariant()).Append('(');
                for (var i = 0; i < n.Args.Length; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(", ");
                    }

                    AppendCalc(sb, n.Args[i], channels);
                }

                sb.Append(')');
                break;
            default:
                sb.Append("calc(");
                AppendCalc(sb, n, channels);
                sb.Append(')');
                break;
        }
    }

    /// <summary>Prints the inside of a <c>calc()</c>, keeping the tree's shape so it parses back the same.</summary>
    private static void AppendCalc(StringBuilder sb, CssNumeric n, string[]? channels)
    {
        var args = n.Args;
        switch (n.Op)
        {
            case CssCalcOp.Sum:
                AppendOperand(sb, args[0], channels, inSum: true);
                for (var i = 1; i < args.Length; i++)
                {
                    if (args[i].Op == CssCalcOp.Negate)
                    {
                        sb.Append(" - ");
                        AppendOperand(sb, args[i].Args[0], channels, inSum: true);
                    }
                    else
                    {
                        sb.Append(" + ");
                        AppendOperand(sb, args[i], channels, inSum: true);
                    }
                }

                break;
            case CssCalcOp.Product:
                AppendOperand(sb, args[0], channels, inSum: false);
                for (var i = 1; i < args.Length; i++)
                {
                    if (args[i].Op == CssCalcOp.Invert)
                    {
                        sb.Append(" / ");
                        AppendOperand(sb, args[i].Args[0], channels, inSum: false);
                    }
                    else
                    {
                        sb.Append(" * ");
                        AppendOperand(sb, args[i], channels, inSum: false);
                    }
                }

                break;
            case CssCalcOp.Negate:
                // The parser only makes these as terms of a sum or product; standing alone they print as one.
                sb.Append("-1 * ");
                AppendOperand(sb, args[0], channels, inSum: false);
                break;
            case CssCalcOp.Invert:
                sb.Append("1 / ");
                AppendOperand(sb, args[0], channels, inSum: false);
                break;
            default:
                AppendNumeric(sb, n, channels);
                break;
        }
    }

    // A parenthesized sum parses to the sum itself, so brackets keep the shape without adding a node.
    private static void AppendOperand(StringBuilder sb, CssNumeric n, string[]? channels, bool inSum)
    {
        var bracket = n.Op is CssCalcOp.Sum or CssCalcOp.Negate or CssCalcOp.Invert || (n.Op == CssCalcOp.Product && !inSum);
        if (bracket)
        {
            sb.Append('(');
        }

        AppendCalc(sb, n, channels);
        if (bracket)
        {
            sb.Append(')');
        }
    }

    /// <summary>Prints a computed number in its canonical unit.</summary>
    private static void AppendNumber(StringBuilder sb, CssNumber n)
    {
        var unit = n.Class switch
        {
            CssDimensionClass.Length => "px",
            CssDimensionClass.Angle => "deg",
            CssDimensionClass.Time => "ms",
            _ => string.Empty,
        };
        if (n.Class == CssDimensionClass.Percent && n.Value == 0)
        {
            sb.Append(Number(n.Percent)).Append('%');
        }
        else if (n.Percent == 0)
        {
            sb.Append(Number(n.Value)).Append(unit);
        }
        else
        {
            sb.Append("calc(").Append(Number(n.Percent)).Append("% + ").Append(Number(n.Value)).Append(unit).Append(')');
        }
    }

    private static void AppendSize(StringBuilder sb, Action? width, Action? height)
    {
        if (width is null)
        {
            sb.Append("auto");
        }
        else
        {
            width();
        }

        if (height is not null)
        {
            sb.Append(' ');
            height();
        }
    }

    private static void AppendBox(StringBuilder sb, CssKinds.BorderImageBoxKind.BoxSpec b)
    {
        // Drop the values the parser fills in from the others: right from top, bottom from top, left from right.
        var v = b.Values;
        var count = 4;
        if (Equals(v[3], v[1]))
        {
            count = 3;
            if (Equals(v[2], v[0]))
            {
                count = 2;
                if (Equals(v[1], v[0]))
                {
                    count = 1;
                }
            }
        }

        for (var i = 0; i < count; i++)
        {
            if (i > 0)
            {
                sb.Append(' ');
            }

            if (v[i] is { } n)
            {
                AppendNumeric(sb, n, null);
            }
            else
            {
                sb.Append("auto");
            }
        }

        if (b.Fill)
        {
            sb.Append(" fill");
        }
    }

    private static void AppendEasing(StringBuilder sb, CssEasing e)
    {
        if (e.IsSteps)
        {
            sb.Append("steps(").Append(e.Steps.ToString(CultureInfo.InvariantCulture)).Append(e.JumpStart ? ", start)" : ", end)");
            return;
        }

        if (e == CssEasing.Linear)
        {
            sb.Append("linear");
        }
        else if (e == CssEasing.Ease)
        {
            sb.Append("ease");
        }
        else if (e == CssEasing.Bezier(0.42, 0, 1, 1))
        {
            sb.Append("ease-in");
        }
        else if (e == CssEasing.Bezier(0, 0, 0.58, 1))
        {
            sb.Append("ease-out");
        }
        else if (e == CssEasing.Bezier(0.42, 0, 0.58, 1))
        {
            sb.Append("ease-in-out");
        }
        else
        {
            sb.Append("cubic-bezier(").Append(Number(e.X1)).Append(", ").Append(Number(e.Y1)).Append(", ")
                .Append(Number(e.X2)).Append(", ").Append(Number(e.Y2)).Append(')');
        }
    }

    // ---- colors ----

    private static string SpaceName(CssColorSpace space) => space switch
    {
        CssColorSpace.Srgb => "srgb",
        CssColorSpace.SrgbLinear => "srgb-linear",
        CssColorSpace.Hsl => "hsl",
        CssColorSpace.Hwb => "hwb",
        CssColorSpace.Oklab => "oklab",
        CssColorSpace.Oklch => "oklch",
        CssColorSpace.DisplayP3 => "display-p3",
        CssColorSpace.Xyz => "xyz",
        CssColorSpace.Rec2020 => "rec2020",
        _ => throw new ArgumentOutOfRangeException(nameof(space)),
    };

    // The spaces color() takes; the others have functions of their own.
    private static bool IsColorFunctionSpace(CssColorSpace space) =>
        space is CssColorSpace.Srgb or CssColorSpace.SrgbLinear or CssColorSpace.DisplayP3 or CssColorSpace.Xyz or CssColorSpace.Rec2020;

    private static void AppendColor(StringBuilder sb, CssColorExpr e)
    {
        switch (e)
        {
            case CssColorExpr.Literal l:
                AppendColor(sb, l.Color);
                break;
            case CssColorExpr.Named n:
                sb.Append('@').Append(Ident(n.Name));
                break;
            case CssColorExpr.Mix m:
                sb.Append("color-mix(in ").Append(SpaceName(m.Space));
                if (CssColorMath.IsPolar(m.Space) && m.Hue != CssHueInterpolation.Shorter)
                {
                    sb.Append(' ').Append(Kebab(m.Hue)).Append(" hue");
                }

                sb.Append(", ");
                AppendMixComponent(sb, m.A, m.P1);
                sb.Append(", ");
                AppendMixComponent(sb, m.B, m.P2);
                sb.Append(')');
                break;
            case CssColorExpr.Function f:
                AppendColorFunction(sb, f);
                break;
            case CssColorExpr.Legacy l:
                // lighter() and darker() parse to shade() with a fixed factor.
                sb.Append(l.Op).Append('(');
                AppendColor(sb, l.A);
                if (l.B is not null)
                {
                    sb.Append(", ");
                    AppendColor(sb, l.B);
                }

                sb.Append(", ").Append(Number(l.Factor)).Append(')');
                break;
            default:
                if (ReferenceEquals(e, CssColorExpr.Current))
                {
                    sb.Append("currentcolor");
                    break;
                }

                throw new NotSupportedException($"cannot print a {e.GetType()} as CSS");
        }
    }

    private static void AppendMixComponent(StringBuilder sb, CssColorExpr c, float percent)
    {
        AppendColor(sb, c);

        // A negative percentage was not given.
        if (percent >= 0)
        {
            sb.Append(' ').Append(percent.ToString("R", CultureInfo.InvariantCulture)).Append('%');
        }
    }

    private static void AppendColorFunction(StringBuilder sb, CssColorExpr.Function f)
    {
        // Always the modern, space-separated syntax: it takes every channel the legacy comma syntax takes.
        var colorFunction = !(f.Space == CssColorSpace.Srgb && f.LegacyRgb) && IsColorFunctionSpace(f.Space);
        sb.Append(colorFunction ? "color" : f.LegacyRgb ? "rgb" : SpaceName(f.Space)).Append('(');
        if (f.Origin is not null)
        {
            sb.Append("from ");
            AppendColor(sb, f.Origin);
            sb.Append(' ');
        }

        if (colorFunction)
        {
            sb.Append(SpaceName(f.Space)).Append(' ');
        }

        var names = f.Origin is null ? null : CssColorMath.ChannelNames(f.Space);
        for (var i = 0; i < 3; i++)
        {
            if (i > 0)
            {
                sb.Append(' ');
            }

            AppendChannel(sb, f.Channels[i], names);
        }

        if (f.AlphaGiven)
        {
            sb.Append(" / ");
            AppendChannel(sb, f.Channels[3], names);
        }

        sb.Append(')');
    }

    private static void AppendChannel(StringBuilder sb, CssNumeric? n, string[]? names)
    {
        if (n is null)
        {
            sb.Append("none");
        }
        else
        {
            AppendNumeric(sb, n, names);
        }
    }

    /// <summary>
    /// Prints a resolved color exactly: 8-bit sRGB as hex, which is how the parser makes literals, and anything else
    /// as <c>color()</c> with its float channels.
    /// </summary>
    private static void AppendColor(StringBuilder sb, CssColor c)
    {
        if (c == CssColor.Transparent)
        {
            sb.Append("transparent");
            return;
        }

        if (c.Space == CssColorSpace.Srgb && c.Missing == 0 && Is8Bit(c.C0) && Is8Bit(c.C1) && Is8Bit(c.C2) && Is8Bit(c.Alpha))
        {
            var (r, g, b, a) = c.ToRgba8();
            sb.Append('#').Append(r.ToString("x2", CultureInfo.InvariantCulture)).Append(g.ToString("x2", CultureInfo.InvariantCulture))
                .Append(b.ToString("x2", CultureInfo.InvariantCulture));
            if (a != 255)
            {
                sb.Append(a.ToString("x2", CultureInfo.InvariantCulture));
            }

            return;
        }

        if (!IsColorFunctionSpace(c.Space))
        {
            c = CssColorMath.Convert(c, CssColorSpace.Srgb);
        }

        sb.Append("color(").Append(SpaceName(c.Space));
        for (var i = 0; i < 4; i++)
        {
            sb.Append(i == 3 ? " / " : " ");
            sb.Append(c.IsMissing(i) ? "none" : c[i].ToString("R", CultureInfo.InvariantCulture));
        }

        sb.Append(')');
    }

    private static bool Is8Bit(float v) => v is >= 0 and <= 1 && MathF.Round(v * 255) / 255f == v;

    // ---- shadows, transforms, filters, palettes ----

    private static void AppendShadow(StringBuilder sb, CssValueParsers.ShadowSpec s)
    {
        if (s.Inset)
        {
            sb.Append("inset ");
        }

        AppendNumeric(sb, s.X, null);
        sb.Append(' ');
        AppendNumeric(sb, s.Y, null);
        if (s.Blur is not null)
        {
            sb.Append(' ');
            AppendNumeric(sb, s.Blur, null);
            if (s.Spread is not null)
            {
                sb.Append(' ');
                AppendNumeric(sb, s.Spread, null);
            }
        }

        sb.Append(' ');
        AppendColor(sb, s.Color);
    }

    private static void AppendTransform(StringBuilder sb, CssValueParsers.TransformSpec t)
    {
        if (t.Functions.Length == 0)
        {
            sb.Append("none");
            return;
        }

        for (var i = 0; i < t.Functions.Length; i++)
        {
            var (kind, args, axis) = t.Functions[i];
            if (i > 0)
            {
                sb.Append(' ');
            }

            sb.Append(kind switch
            {
                CssTransformKind.SkewX => "skewX",
                CssTransformKind.SkewY => "skewY",
                _ => Kebab(kind),
            });
            sb.Append(axis switch
            {
                1 => "X",
                2 => "Y",
                _ => string.Empty,
            });
            sb.Append('(');
            for (var a = 0; a < args.Length; a++)
            {
                if (a > 0)
                {
                    sb.Append(", ");
                }

                AppendNumeric(sb, args[a], null);
            }

            sb.Append(')');
        }
    }

    private static void AppendFilter(StringBuilder sb, CssValueParsers.FilterSpec f)
    {
        if (f.Functions.Length == 0)
        {
            sb.Append("none");
            return;
        }

        for (var i = 0; i < f.Functions.Length; i++)
        {
            var (kind, amount, shadow) = f.Functions[i];
            if (i > 0)
            {
                sb.Append(' ');
            }

            sb.Append(Kebab(kind)).Append('(');
            if (shadow is not null)
            {
                AppendShadow(sb, shadow);
            }
            else
            {
                AppendNumeric(sb, amount!, null);
            }

            sb.Append(')');
        }
    }

    private static void AppendPalette(StringBuilder sb, CssValueParsers.PaletteSpec p)
    {
        if (ReferenceEquals(p, CssValueParsers.PaletteSpec.Default))
        {
            sb.Append("default");
            return;
        }

        AppendJoined(sb, p.Colors, c =>
        {
            sb.Append(Ident(c.Name)).Append(' ');
            AppendColor(sb, c.Color);
        });
    }

    // ---- images ----

    private static void AppendImageOrNone(StringBuilder sb, object image)
    {
        if (ReferenceEquals(image, CssKinds.s_noImage))
        {
            sb.Append("none");
        }
        else
        {
            Append(sb, image);
        }
    }

    private static void AppendImage(StringBuilder sb, CssImage image)
    {
        switch (image)
        {
            case CssUrlImage u:
                sb.Append("url(");
                AppendString(sb, u.Url);
                sb.Append(')');
                break;
            case CssIconThemeImage i:
                sb.Append("-gtk-icontheme(");
                AppendString(sb, i.Name);
                sb.Append(')');
                break;
            default:
                throw new NotSupportedException($"cannot print a {image.GetType()} as CSS");
        }
    }

    private static readonly (int Bit, string Name)[] s_sides =
        [(CssLinearGradient.Top, "top"), (CssLinearGradient.Bottom, "bottom"), (CssLinearGradient.Left, "left"), (CssLinearGradient.Right, "right")];

    private static void AppendImage(StringBuilder sb, CssValueParsers.ImageSpec image)
    {
        switch (image)
        {
            case CssValueParsers.ImageSpec.Fixed f:
                AppendImage(sb, f.Image);
                break;
            case CssValueParsers.ImageSpec.Recolor r:
                sb.Append("-gtk-recolor(url(");
                AppendString(sb, r.Url);
                sb.Append(')');
                if (r.Palette is not null)
                {
                    sb.Append(", ");
                    AppendPalette(sb, r.Palette);
                }

                sb.Append(')');
                break;
            case CssValueParsers.ImageSpec.Linear l:
                sb.Append(l.Repeating ? "repeating-linear-gradient(" : "linear-gradient(");
                if (l.Angle is not null)
                {
                    AppendNumeric(sb, l.Angle, null);
                    sb.Append(", ");
                }
                else if (l.Side != CssLinearGradient.Bottom)
                {
                    // "to bottom" is what no direction means.
                    sb.Append("to");
                    foreach (var (bit, name) in s_sides)
                    {
                        if ((l.Side & bit) != 0)
                        {
                            sb.Append(' ').Append(name);
                        }
                    }

                    sb.Append(", ");
                }

                AppendStops(sb, l.Stops);
                sb.Append(')');
                break;
            case CssValueParsers.ImageSpec.Radial g:
                AppendRadial(sb, g);
                break;
            case CssValueParsers.ImageSpec.Fallback f:
                sb.Append("image(");
                AppendJoined(sb, f.Images, i => AppendImage(sb, i));
                if (f.Color is not null)
                {
                    if (f.Images.Length > 0)
                    {
                        sb.Append(", ");
                    }

                    AppendColor(sb, f.Color);
                }

                sb.Append(')');
                break;
            case CssValueParsers.ImageSpec.CrossFade c:
                sb.Append("cross-fade(");
                AppendJoined(sb, c.Layers, l =>
                {
                    var start = sb.Length;
                    if (l.Percent is { } p)
                    {
                        sb.Append(Number(p)).Append('%');
                    }

                    if (l.Image is not null || l.Color is not null)
                    {
                        if (sb.Length > start)
                        {
                            sb.Append(' ');
                        }

                        if (l.Image is not null)
                        {
                            AppendImage(sb, l.Image);
                        }
                        else
                        {
                            AppendColor(sb, l.Color!);
                        }
                    }
                });
                sb.Append(')');
                break;
            default:
                throw new NotSupportedException($"cannot print a {image.GetType()} as CSS");
        }
    }

    private static void AppendRadial(StringBuilder sb, CssValueParsers.ImageSpec.Radial g)
    {
        sb.Append(g.Repeating ? "repeating-radial-gradient(" : "radial-gradient(");
        var start = sb.Length;
        void Space()
        {
            if (sb.Length > start)
            {
                sb.Append(' ');
            }
        }

        // An ellipse is the default shape, and one radius alone makes a circle, so ellipse never needs saying.
        if (g.Circle)
        {
            sb.Append("circle");
        }

        if (g.RadiusX is not null)
        {
            Space();
            AppendNumeric(sb, g.RadiusX, null);
            if (g.RadiusY is not null)
            {
                sb.Append(' ');
                AppendNumeric(sb, g.RadiusY, null);
            }
        }

        // A keyword after the radii overrides them; farthest-corner is the default only without radii.
        if (g.Size != CssRadialSize.Explicit && (g.Size != CssRadialSize.FarthestCorner || g.RadiusX is not null))
        {
            Space();
            sb.Append(Kebab(g.Size));
        }

        if (g.Center is not null)
        {
            Space();
            sb.Append("at ");
            Append(sb, g.Center);
        }

        if (sb.Length > start)
        {
            sb.Append(", ");
        }

        AppendStops(sb, g.Stops);
        sb.Append(')');
    }

    private static void AppendStops(StringBuilder sb, (CssColorExpr Color, CssNumeric? Offset)[] stops) =>
        AppendJoined(sb, stops, s =>
        {
            AppendColor(sb, s.Color);
            if (s.Offset is not null)
            {
                sb.Append(' ');
                AppendNumeric(sb, s.Offset, null);
            }
        });
}
