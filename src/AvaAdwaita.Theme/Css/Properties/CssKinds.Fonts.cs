using System;
using System.Collections.Generic;
using System.Text;
using AvaAdwaita.CssEngine.Syntax;
using AvaAdwaita.CssEngine.Values;

namespace AvaAdwaita.CssEngine.Properties;

/// <content>The font kinds.</content>
internal static partial class CssKinds
{
    internal sealed class FontSizeKind : CssKind
    {
        private static readonly (string Name, double Factor)[] s_keywords =
        [
            ("xx-small", 3.0 / 5), ("x-small", 3.0 / 4), ("small", 8.0 / 9), ("medium", 1), ("large", 6.0 / 5), ("x-large", 3.0 / 2), ("xx-large", 2),
        ];

        public override object? Parse(CssReader r, CssParseContext pc)
        {
            foreach (var (n, f) in s_keywords)
            {
                if (r.TryIdent(n))
                {
                    return new FontSizeKeyword(f, 0);
                }
            }

            if (r.TryIdent("smaller"))
            {
                return new FontSizeKeyword(0, -1);
            }

            if (r.TryIdent("larger"))
            {
                return new FontSizeKeyword(0, 1);
            }

            return CssNumeric.Parse(r, CssValueParsers.LengthPercent | CssNumberFlags.PositiveOnly);
        }

        /// <remarks>Port of gtk_css_value_font_size_compute (gtkcssenumvalue.c).</remarks>
        public override object Compute(object specified, CssComputeContext ctx)
        {
            if (specified is FontSizeKeyword k)
            {
                return k.Relative switch
                {
                    -1 => ctx.ParentFontSize / 1.2,
                    1 => ctx.ParentFontSize * 1.2,
                    _ => ctx.Env.FontSizePx * k.Factor,
                };
            }

            var n = ((CssNumeric)specified).Compute(ctx.NumericForFontSize);
            return Math.Max(0, n.Value + (n.Percent * ctx.ParentFontSize / 100));
        }

        public override string Print(object specified)
        {
            if (specified is not FontSizeKeyword k)
            {
                return CssSerializer.Print(specified);
            }

            if (k.Relative != 0)
            {
                return k.Relative < 0 ? "smaller" : "larger";
            }

            return Array.Find(s_keywords, w => w.Factor == k.Factor).Name ?? throw new InvalidOperationException($"no font-size keyword has factor {k.Factor}");
        }

        public override object? Interpolate(object from, object to, double t) => LerpValue(from, to, t);

        private sealed record FontSizeKeyword(double Factor, int Relative);
    }

    internal sealed class FontWeightKind : CssKind
    {
        private const double Bolder = -1;
        private const double Lighter = -2;

        public override object? Parse(CssReader r, CssParseContext pc)
        {
            if (TryParseKeyword(r) is { } k)
            {
                return k;
            }

            var n = CssNumeric.Parse(r, CssNumberFlags.Number | CssNumberFlags.PositiveOnly);
            if (n is null)
            {
                return null;
            }

            var v = n.Compute(CssNumericContext.Default).Value;
            if (v < 1 || v > 1000)
            {
                r.Fail("Font weight values must be between 1 and 1000");
                return null;
            }

            return v;
        }

        public static object? TryParseKeyword(CssReader r)
        {
            if (r.TryIdent("normal"))
            {
                return 400.0;
            }

            if (r.TryIdent("bold"))
            {
                return 700.0;
            }

            if (r.TryIdent("bolder"))
            {
                return Bolder;
            }

            if (r.TryIdent("lighter"))
            {
                return Lighter;
            }

            return null;
        }

        // bolder and lighter are stored as negative weights.
        public override string Print(object specified) => (double)specified switch
        {
            Bolder => "bolder",
            Lighter => "lighter",
            var w => CssSerializer.Number(w),
        };

        /// <remarks>Port of gtk_css_value_font_weight_compute (gtkcssenumvalue.c).</remarks>
        public override object Compute(object specified, CssComputeContext ctx)
        {
            var v = (double)specified;
            if (v >= 0)
            {
                return v;
            }

            var parent = ctx.Parent?.GetValue(ctx.PropertyId) is double p ? p : 400;
            if (v == Bolder)
            {
                return parent < 350 ? 400.0 : parent < 550 ? 700.0 : 900.0;
            }

            return parent > 750 ? 700.0 : parent > 550 ? 400.0 : 100.0;
        }

        public override object? Interpolate(object from, object to, double t) => LerpValue(from, to, t);
    }

    internal sealed class FontFamilyKind : CssKind
    {
        public override object? Parse(CssReader r, CssParseContext pc)
        {
            var list = new List<string>();
            do
            {
                if (r.Peek() is { Type: CssTokenType.String } s)
                {
                    r.Next();
                    list.Add(s.Token.Text);
                    continue;
                }

                var sb = new StringBuilder();
                while (r.Peek() is { Type: CssTokenType.Ident } id)
                {
                    if (sb.Length > 0)
                    {
                        sb.Append(' ');
                    }

                    sb.Append(id.Token.Text);
                    r.Next();
                }

                if (sb.Length == 0)
                {
                    r.Fail("Expected a font family");
                    return null;
                }

                list.Add(sb.ToString());
            }
            while (r.TryComma());

            return new CssList<string>(list.ToArray());
        }

        // A family of plain words prints as identifiers, as it was most likely written; anything else as a string.
        public override string Print(object specified) =>
            CssSerializer.Join((CssList<string>)specified, f => Array.TrueForAll(f.Split(' '), CssSerializer.IsPlainIdent) ? f : CssSerializer.String(f));

        public override object Compute(object specified, CssComputeContext ctx)
        {
            // The initial value is the settings font, as _gtk_css_initial_value_new_compute.
            if (ReferenceEquals(specified, CssPropertyTable.All[ctx.PropertyId].Initial))
            {
                return new CssList<string>([ctx.Env.FontFamily]);
            }

            return specified;
        }
    }

    internal sealed class LineHeightKind : CssKind
    {
        public override object? Parse(CssReader r, CssParseContext pc)
        {
            if (r.TryIdent("normal"))
            {
                return CssLineHeight.Normal;
            }

            return CssNumeric.Parse(r, CssNumberFlags.Number | CssNumberFlags.Percent | CssNumberFlags.Length | CssNumberFlags.PositiveOnly);
        }

        /// <remarks>Port of gtk_css_value_line_height_compute: a percentage multiplies the font size.</remarks>
        public override object Compute(object specified, CssComputeContext ctx)
        {
            if (specified is CssLineHeight lh)
            {
                return lh;
            }

            var n = ((CssNumeric)specified).Compute(ctx.Numeric);
            return n.Class switch
            {
                CssDimensionClass.Number => new CssLineHeight(false, n.Value, 0),
                CssDimensionClass.Percent => new CssLineHeight(false, 0, n.Percent / 100 * ctx.FontSize),
                _ => new CssLineHeight(false, 0, n.Value + (n.Percent / 100 * ctx.FontSize)),
            };
        }

        public override object? Interpolate(object from, object to, double t)
        {
            var a = (CssLineHeight)from;
            var b = (CssLineHeight)to;
            if (a.IsNormal || b.IsNormal || (a.Px > 0) != (b.Px > 0))
            {
                return null;
            }

            return new CssLineHeight(false, Lerp(a.Factor, b.Factor, t), Lerp(a.Px, b.Px, t));
        }
    }

    internal sealed class FontSettingsKind(bool features) : CssKind
    {
        public override object? Parse(CssReader r, CssParseContext pc)
        {
            if (r.TryIdent("normal"))
            {
                return CssList<CssFontSetting>.Empty;
            }

            var list = new List<CssFontSetting>();
            do
            {
                if (r.Peek() is not { Type: CssTokenType.String } s || s.Token.Text.Length != 4)
                {
                    r.Fail("Expected a four-letter tag");
                    return null;
                }

                r.Next();
                double value;
                if (features)
                {
                    value = 1;
                    if (r.TryIdent("on"))
                    {
                        value = 1;
                    }
                    else if (r.TryIdent("off"))
                    {
                        value = 0;
                    }
                    else if (r.Peek() is { Type: CssTokenType.Number, Token.IsInteger: true } n && n.Token.Number >= 0)
                    {
                        r.Next();
                        value = n.Token.Number;
                    }
                }
                else
                {
                    if (r.Peek() is not { Type: CssTokenType.Number } n)
                    {
                        r.Fail("Expected a number");
                        return null;
                    }

                    r.Next();
                    value = n.Token.Number;
                }

                list.Add(new CssFontSetting(s.Token.Text, value));
            }
            while (r.TryComma());

            return new CssList<CssFontSetting>(list.ToArray());
        }
    }
}
