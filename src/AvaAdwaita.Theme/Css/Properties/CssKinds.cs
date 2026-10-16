using System;
using System.Collections.Generic;
using AvaAdwaita.CssEngine.Syntax;
using AvaAdwaita.CssEngine.Values;

namespace AvaAdwaita.CssEngine.Properties;

/// <summary>The property kinds the generated property table refers to.</summary>
/// <remarks>Ports the parse and compute functions of gtkcssstylepropertyimpl.c.</remarks>
internal static partial class CssKinds
{
    public static readonly CssKind Color = new ColorKind();
    public static readonly CssKind Dpi = new NumberKind(CssNumberFlags.Number);
    public static readonly CssKind FontSize = new FontSizeKind();
    public static readonly CssKind Palette = new PaletteKind();
    public static readonly CssKind Length = new LengthKind(CssNumberFlags.Length);
    public static readonly CssKind PositiveLength = new LengthKind(CssNumberFlags.Length | CssNumberFlags.PositiveOnly);
    public static readonly CssKind LetterSpacing = new LengthKind(CssNumberFlags.Length);
    public static readonly CssKind IconSize = new LengthKind(CssNumberFlags.Length | CssNumberFlags.Percent | CssNumberFlags.PositiveOnly);
    public static readonly CssKind BorderWidth = new BorderWidthKind();
    public static readonly CssKind Corner = new CornerKind();
    public static readonly CssKind BorderSpacing = new BorderSpacingKind();
    public static readonly CssKind Position = new PositionKind();
    public static readonly CssKind Opacity = new OpacityKind();
    public static readonly CssKind BorderStyle = new EnumKind<CssBorderStyle>(
        ("none", CssBorderStyle.None), ("solid", CssBorderStyle.Solid), ("inset", CssBorderStyle.Inset), ("outset", CssBorderStyle.Outset),
        ("hidden", CssBorderStyle.Hidden), ("dotted", CssBorderStyle.Dotted), ("dashed", CssBorderStyle.Dashed), ("double", CssBorderStyle.Double),
        ("groove", CssBorderStyle.Groove), ("ridge", CssBorderStyle.Ridge));

    public static readonly CssKind AreaList = new EnumListKind<CssArea>(
        ("border-box", CssArea.BorderBox), ("padding-box", CssArea.PaddingBox), ("content-box", CssArea.ContentBox));

    public static readonly CssKind BoxShadow = new ShadowKind(box: true);
    public static readonly CssKind TextShadow = new ShadowKind(box: false);
    public static readonly CssKind Transform = new TransformKind();
    public static readonly CssKind Filter = new FilterKind();

    public static readonly CssKind IconStyle = new KeywordKind("requested", "regular", "symbolic");
    public static readonly CssKind FontStyle = new KeywordKind("normal", "oblique", "italic");
    public static readonly CssKind FontWidth = new KeywordKind("ultra-condensed", "extra-condensed", "condensed", "semi-condensed", "normal",
        "semi-expanded", "expanded", "extra-expanded", "ultra-expanded");

    public static readonly CssKind TextDecorationStyle = new KeywordKind("solid", "double", "wavy");
    public static readonly CssKind TextTransform = new KeywordKind("none", "lowercase", "uppercase", "capitalize");
    public static readonly CssKind FontKerning = new KeywordKind("auto", "normal", "none");
    public static readonly CssKind FontVariantPosition = new KeywordKind("normal", "sub", "super");
    public static readonly CssKind FontVariantCaps = new KeywordKind("normal", "small-caps", "all-small-caps", "petite-caps", "all-petite-caps", "unicase", "titling-caps");
    public static readonly CssKind FontVariantAlternates = new KeywordKind("normal", "historical-forms");

    public static readonly CssKind TextDecorationLine = new KeywordSetKind("none", ["underline"], ["overline"], ["line-through"]);
    public static readonly CssKind FontVariantLigatures = new KeywordSetKind("normal",
        ["common-ligatures", "no-common-ligatures"], ["discretionary-ligatures", "no-discretionary-ligatures"],
        ["historical-ligatures", "no-historical-ligatures"], ["contextual", "no-contextual"], ["none"]);

    public static readonly CssKind FontVariantNumeric = new KeywordSetKind("normal",
        ["lining-nums", "oldstyle-nums"], ["proportional-nums", "tabular-nums"], ["diagonal-fractions", "stacked-fractions"], ["ordinal"], ["slashed-zero"]);

    public static readonly CssKind FontVariantEastAsian = new KeywordSetKind("normal",
        ["jis78", "jis83", "jis90", "jis04", "simplified", "traditional"], ["full-width", "proportional-width"], ["ruby"]);

    public static readonly CssKind BlendModeList = new KeywordListKind("normal", "multiply", "screen", "overlay", "darken", "lighten", "color-dodge",
        "color-burn", "hard-light", "soft-light", "difference", "exclusion", "color", "hue", "saturation", "luminosity");

    public static readonly CssKind AnimationDirectionList = new KeywordListKind("normal", "reverse", "alternate", "alternate-reverse");
    public static readonly CssKind PlayStateList = new KeywordListKind("running", "paused");
    public static readonly CssKind FillModeList = new KeywordListKind("none", "forwards", "backwards", "both");
    public static readonly CssKind AnimationName = new IdentListKind();
    public static readonly CssKind TransitionProperty = new IdentListKind();
    public static readonly CssKind TimeList = new TimeListKind();
    public static readonly CssKind TimingList = new TimingListKind();
    public static readonly CssKind IterationCountList = new IterationCountListKind();

    public static readonly CssKind FontFamily = new FontFamilyKind();
    public static readonly CssKind FontWeight = new FontWeightKind();
    public static readonly CssKind LineHeight = new LineHeightKind();
    public static readonly CssKind FontFeatures = new FontSettingsKind(features: true);
    public static readonly CssKind FontVariations = new FontSettingsKind(features: false);

    public static readonly CssKind BackgroundImage = new BackgroundImageKind();
    public static readonly CssKind Image = new ImageKind();
    public static readonly CssKind BackgroundSize = new BackgroundSizeKind();
    public static readonly CssKind BackgroundPosition = new BackgroundPositionKind();
    public static readonly CssKind BackgroundRepeat = new BackgroundRepeatKind();
    public static readonly CssKind BorderImageRepeat = new BorderImageRepeatKind();
    public static readonly CssKind BorderImageSlice = new BorderImageBoxKind(slice: true);
    public static readonly CssKind BorderImageWidth = new BorderImageBoxKind(slice: false);

    private static double Lerp(double a, double b, double t) => a + ((b - a) * t);

    /// <summary>Interpolates two computed values of the same type, for kinds that are plain numbers.</summary>
    private static object? LerpValue(object from, object to, double t) => (from, to) switch
    {
        (double a, double b) => Lerp(a, b, t),
        (CssNumber a, CssNumber b) => CssNumber.Lerp(a, b, t),
        (CssCorner a, CssCorner b) => new CssCorner(CssNumber.Lerp(a.X, b.X, t), CssNumber.Lerp(a.Y, b.Y, t)),
        (CssPosition a, CssPosition b) => new CssPosition(CssNumber.Lerp(a.X, b.X, t), CssNumber.Lerp(a.Y, b.Y, t)),
        (CssColor a, CssColor b) => CssColorMath.LegacyMix(a, b, t),
        _ => null,
    };

    // ---- colors ----

    internal sealed class ColorKind : CssKind
    {
        public override object? Parse(CssReader r, CssParseContext pc) => CssColorParser.Parse(r);

        public override object Compute(object specified, CssComputeContext ctx)
        {
            var e = (CssColorExpr)specified;
            var isColor = ctx.PropertyId == CssPropertyTable.Id.Color;

            // For color itself, currentColor is the parent's color.
            var current = isColor ? ctx.ParentColor : ctx.Color;
            if (e.Resolve(ctx.Colors, current) is { } c)
            {
                return c;
            }

            // gtk_css_value_color_get_fallback: the property's initial value.
            var initial = (CssColorExpr)CssPropertyTable.All[ctx.PropertyId].Initial;
            return initial.Resolve(ctx.Colors, current) ?? CssColor.Transparent;
        }

        public override bool UsesCurrentColor(object specified) => ((CssColorExpr)specified).UsesCurrentColor;

        public override object? Interpolate(object from, object to, double t) => CssColorMath.LegacyMix((CssColor)from, (CssColor)to, t);
    }

    internal sealed class PaletteKind : CssKind
    {
        public override object? Parse(CssReader r, CssParseContext pc) => CssValueParsers.ParsePalette(r);

        public override object? Interpolate(object from, object to, double t)
        {
            var a = (CssPalette)from;
            var b = (CssPalette)to;
            var list = new List<(string, CssColor)>();
            foreach (var (name, ca) in a.Colors)
            {
                list.Add((name, b.Get(name) is { } cb ? CssColorMath.LegacyMix(ca, cb, t) : ca));
            }

            return new CssPalette(new CssList<(string, CssColor)>(list.ToArray()));
        }
    }

    // ---- numbers and lengths ----

    internal sealed class NumberKind(CssNumberFlags flags) : CssKind
    {
        public override object? Parse(CssReader r, CssParseContext pc) => CssNumeric.Parse(r, flags);

        public override object Compute(object specified, CssComputeContext ctx) => ((CssNumeric)specified).Compute(ctx.Numeric).Value;

        public override object? Interpolate(object from, object to, double t) => LerpValue(from, to, t);
    }

    internal sealed class LengthKind(CssNumberFlags flags) : CssKind
    {
        public override object? Parse(CssReader r, CssParseContext pc) => CssNumeric.Parse(r, flags);

        public override object Compute(object specified, CssComputeContext ctx)
        {
            var n = ((CssNumeric)specified).Compute(ctx.Numeric);

            // A percentage of an icon size is of 100px, as gtk_css_number_value_get (value, 100).
            var px = n.Value + n.Percent;
            if ((flags & CssNumberFlags.PositiveOnly) != 0 && px < 0)
            {
                px = 0;
            }

            return px;
        }

        public override object? Interpolate(object from, object to, double t) => LerpValue(from, to, t);
    }

    internal sealed class BorderWidthKind : CssKind
    {
        public override object? Parse(CssReader r, CssParseContext pc) => CssNumeric.Parse(r, CssValueParsers.PositiveLength);

        public override object Compute(object specified, CssComputeContext ctx)
        {
            // GTK orders each border-*-style right before its width, and a width with no style computes to 0.
            if (ctx.Values[ctx.PropertyId - 1] is CssBorderStyle s && s is CssBorderStyle.None or CssBorderStyle.Hidden)
            {
                return 0.0;
            }

            return Math.Max(0, ((CssNumeric)specified).Compute(ctx.Numeric).Value);
        }

        public override object? Interpolate(object from, object to, double t) => LerpValue(from, to, t);
    }

    internal sealed class OpacityKind : CssKind
    {
        public override object? Parse(CssReader r, CssParseContext pc) => CssNumeric.Parse(r, CssNumberFlags.Number | CssNumberFlags.Percent);

        public override object Compute(object specified, CssComputeContext ctx)
        {
            var n = ((CssNumeric)specified).Compute(ctx.Numeric);
            return Math.Clamp(n.Class == CssDimensionClass.Percent ? n.Percent / 100 : n.Value, 0, 1);
        }

        public override object? Interpolate(object from, object to, double t) => LerpValue(from, to, t);
    }

    internal sealed class CornerKind : CssKind
    {
        public override object? Parse(CssReader r, CssParseContext pc)
        {
            var x = CssNumeric.Parse(r, CssValueParsers.LengthPercent | CssNumberFlags.PositiveOnly);
            if (x is null)
            {
                return null;
            }

            var y = x;
            if (CssNumeric.CanParse(r))
            {
                y = CssNumeric.Parse(r, CssValueParsers.LengthPercent | CssNumberFlags.PositiveOnly);
                if (y is null)
                {
                    return null;
                }
            }

            return new CornerSpec(x, y);
        }

        public override object? Interpolate(object from, object to, double t) => LerpValue(from, to, t);
    }

    internal sealed class CornerSpec(CssNumeric x, CssNumeric y) : ICssSpecified
    {
        public CssNumeric X => x;

        public CssNumeric Y => y;

        public bool UsesCurrentColor => false;

        public object Compute(CssComputeContext ctx) => new CssCorner(x.Compute(ctx.Numeric), y.Compute(ctx.Numeric));
    }

    internal sealed class BorderSpacingKind : CssKind
    {
        public override object? Parse(CssReader r, CssParseContext pc)
        {
            var x = CssNumeric.Parse(r, CssValueParsers.PositiveLength);
            if (x is null)
            {
                return null;
            }

            var y = x;
            if (CssNumeric.CanParse(r))
            {
                y = CssNumeric.Parse(r, CssValueParsers.PositiveLength);
                if (y is null)
                {
                    return null;
                }
            }

            return new CssValueParsers.PositionSpec(x, y);
        }

        public override object? Interpolate(object from, object to, double t) => LerpValue(from, to, t);
    }

    internal sealed class PositionKind : CssKind
    {
        public override object? Parse(CssReader r, CssParseContext pc)
        {
            var p = CssValueParsers.TryParsePosition(r);
            if (p is null)
            {
                r.Fail("Unrecognized position value");
            }

            return p;
        }

        public override object? Interpolate(object from, object to, double t) => LerpValue(from, to, t);
    }

    internal sealed class ShadowKind(bool box) : CssKind
    {
        public override object? Parse(CssReader r, CssParseContext pc) => CssValueParsers.ParseShadows(r, box);

        public override object? Interpolate(object from, object to, double t)
        {
            // gtk_css_shadow_value_transition: pad the shorter list with transparent shadows of the other's inset.
            var a = (CssList<CssShadow>)from;
            var b = (CssList<CssShadow>)to;
            var n = Math.Max(a.Count, b.Count);
            var result = new CssShadow[n];
            for (var i = 0; i < n; i++)
            {
                var sa = i < a.Count ? a[i] : null;
                var sb = i < b.Count ? b[i] : null;
                sa ??= CssShadow.Transparent(sb!.Inset);
                sb ??= CssShadow.Transparent(sa.Inset);
                if (sa.Inset != sb.Inset)
                {
                    return null;
                }

                result[i] = new CssShadow(sa.Inset, Lerp(sa.X, sb.X, t), Lerp(sa.Y, sb.Y, t), Lerp(sa.Blur, sb.Blur, t), Lerp(sa.Spread, sb.Spread, t),
                    CssColorMath.LegacyMix(sa.Color, sb.Color, t));
            }

            return new CssList<CssShadow>(result);
        }
    }

    internal sealed class TransformKind : CssKind
    {
        public override object? Parse(CssReader r, CssParseContext pc) => CssValueParsers.ParseTransform(r);

        public override object? Interpolate(object from, object to, double t)
        {
            var a = (CssList<CssTransformFunction>)from;
            var b = (CssList<CssTransformFunction>)to;
            var n = Math.Max(a.Count, b.Count);
            var result = new CssTransformFunction[n];
            for (var i = 0; i < n; i++)
            {
                var fa = i < a.Count ? a[i] : Identity(b[i].Kind);
                var fb = i < b.Count ? b[i] : Identity(a[i].Kind);
                if (fa.Kind != fb.Kind)
                {
                    return null;
                }

                result[i] = new CssTransformFunction(fa.Kind, Lerp(fa.A, fb.A, t), Lerp(fa.B, fb.B, t), Lerp(fa.C, fb.C, t), Lerp(fa.D, fb.D, t),
                    Lerp(fa.E, fb.E, t), Lerp(fa.F, fb.F, t), CssNumber.Lerp(fa.Tx, fb.Tx, t), CssNumber.Lerp(fa.Ty, fb.Ty, t));
            }

            return new CssList<CssTransformFunction>(result);
        }

        private static CssTransformFunction Identity(CssTransformKind kind)
        {
            var zero = CssNumber.Px(0);
            return kind switch
            {
                CssTransformKind.Scale => new(kind, 1, 1, 0, 0, 0, 0, zero, zero),
                CssTransformKind.Matrix => new(kind, 1, 0, 0, 1, 0, 0, zero, zero),
                _ => new(kind, 0, 0, 0, 0, 0, 0, zero, zero),
            };
        }
    }

    internal sealed class FilterKind : CssKind
    {
        public override object? Parse(CssReader r, CssParseContext pc) => CssValueParsers.ParseFilter(r);

        public override object? Interpolate(object from, object to, double t)
        {
            var a = (CssList<CssFilterFunction>)from;
            var b = (CssList<CssFilterFunction>)to;
            var n = Math.Max(a.Count, b.Count);
            var result = new CssFilterFunction[n];
            for (var i = 0; i < n; i++)
            {
                var fa = i < a.Count ? a[i] : null;
                var fb = i < b.Count ? b[i] : null;
                fa ??= Neutral(fb!.Kind);
                fb ??= Neutral(fa.Kind);
                if (fa.Kind != fb.Kind)
                {
                    return null;
                }

                CssShadow? shadow = null;
                if (fa.Shadow is not null && fb.Shadow is not null)
                {
                    var sa = fa.Shadow;
                    var sb = fb.Shadow;
                    shadow = new CssShadow(false, Lerp(sa.X, sb.X, t), Lerp(sa.Y, sb.Y, t), Lerp(sa.Blur, sb.Blur, t), 0, CssColorMath.LegacyMix(sa.Color, sb.Color, t));
                }

                result[i] = new CssFilterFunction(fa.Kind, Lerp(fa.Amount, fb.Amount, t), shadow ?? fa.Shadow ?? fb.Shadow);
            }

            return new CssList<CssFilterFunction>(result);
        }

        private static CssFilterFunction Neutral(CssFilterKind kind) => kind switch
        {
            CssFilterKind.Brightness or CssFilterKind.Contrast or CssFilterKind.Opacity or CssFilterKind.Saturate => new(kind, 1, null),
            CssFilterKind.DropShadow => new(kind, 0, CssShadow.Transparent(false)),
            _ => new(kind, 0, null),
        };
    }

    // ---- keywords ----

    internal sealed class KeywordKind(params string[] words) : CssKind
    {
        public override object? Parse(CssReader r, CssParseContext pc)
        {
            foreach (var w in words)
            {
                if (r.TryIdent(w))
                {
                    return w;
                }
            }

            r.Fail($"Expected one of {string.Join(", ", words)}");
            return null;
        }
    }

    internal sealed class EnumKind<T>(params (string Name, T Value)[] values) : CssKind
        where T : struct, Enum
    {
        public override object? Parse(CssReader r, CssParseContext pc) => TryParse(r, values) ?? Fail(r, values);

        internal static object? TryParse(CssReader r, (string Name, T Value)[] values)
        {
            foreach (var (n, v) in values)
            {
                if (r.TryIdent(n))
                {
                    return v;
                }
            }

            return null;
        }

        private static object? Fail(CssReader r, (string Name, T Value)[] values)
        {
            r.Fail($"Expected one of {string.Join(", ", Array.ConvertAll(values, v => v.Name))}");
            return null;
        }
    }

    internal sealed class EnumListKind<T>(params (string Name, T Value)[] values) : CssKind
        where T : struct, Enum
    {
        public override object? Parse(CssReader r, CssParseContext pc)
        {
            var list = new List<T>();
            do
            {
                if (EnumKind<T>.TryParse(r, values) is not T v)
                {
                    r.Fail($"Expected one of {string.Join(", ", Array.ConvertAll(values, x => x.Name))}");
                    return null;
                }

                list.Add(v);
            }
            while (r.TryComma());

            return new CssList<T>(list.ToArray());
        }
    }

    /// <summary>A keyword value made of keywords from different groups, at most one per group, or a lone default.</summary>
    internal sealed class KeywordSetKind(string single, params string[][] groups) : CssKind
    {
        public override object? Parse(CssReader r, CssParseContext pc)
        {
            if (r.TryIdent(single))
            {
                return single;
            }

            var picked = new string?[groups.Length];
            var any = false;
            while (r.Peek() is { Type: CssTokenType.Ident } c)
            {
                var found = false;
                for (var g = 0; g < groups.Length; g++)
                {
                    foreach (var w in groups[g])
                    {
                        if (c.IsIdent(w))
                        {
                            if (picked[g] is not null)
                            {
                                r.Fail($"'{w}' conflicts with '{picked[g]}'");
                                return null;
                            }

                            picked[g] = w;
                            found = true;
                            break;
                        }
                    }

                    if (found)
                    {
                        break;
                    }
                }

                if (!found)
                {
                    break;
                }

                r.Next();
                any = true;
            }

            if (!any)
            {
                r.Fail("Expected a keyword");
                return null;
            }

            var words = new List<string>();
            foreach (var p in picked)
            {
                if (p is not null)
                {
                    words.Add(p);
                }
            }

            return string.Join(' ', words);
        }
    }

    internal sealed class KeywordListKind(params string[] words) : CssKind
    {
        public override object? Parse(CssReader r, CssParseContext pc)
        {
            var list = new List<string>();
            do
            {
                var ok = false;
                foreach (var w in words)
                {
                    if (r.TryIdent(w))
                    {
                        list.Add(w);
                        ok = true;
                        break;
                    }
                }

                if (!ok)
                {
                    r.Fail($"Expected one of {string.Join(", ", words)}");
                    return null;
                }
            }
            while (r.TryComma());

            return new CssList<string>(list.ToArray());
        }
    }

    internal sealed class IdentListKind : CssKind
    {
        public override object? Parse(CssReader r, CssParseContext pc)
        {
            var list = new List<string>();
            do
            {
                var id = r.TryAnyIdent();
                if (id is null)
                {
                    r.Fail("Expected an identifier");
                    return null;
                }

                list.Add(id.ToLowerInvariant());
            }
            while (r.TryComma());

            return new CssList<string>(list.ToArray());
        }
    }

    // ---- time and timing ----

    internal sealed class TimeListKind : CssKind
    {
        public override object? Parse(CssReader r, CssParseContext pc)
        {
            var list = new List<CssNumeric>();
            do
            {
                var n = CssNumeric.Parse(r, CssNumberFlags.Time);
                if (n is null)
                {
                    return null;
                }

                list.Add(n);
            }
            while (r.TryComma());

            // Times have no context, so they compute at parse time.
            var ms = list.ConvertAll(n => n.Compute(CssNumericContext.Default).Value);
            return new CssList<double>(ms.ToArray());
        }

        // The list holds computed milliseconds.
        public override string Print(object specified) => CssSerializer.Join((CssList<double>)specified, ms => CssSerializer.Number(ms) + "ms");
    }

    internal sealed class TimingListKind : CssKind
    {
        public override object? Parse(CssReader r, CssParseContext pc)
        {
            var list = new List<CssEasing>();
            do
            {
                var e = CssValueParsers.ParseEasing(r);
                if (e is null)
                {
                    return null;
                }

                list.Add(e);
            }
            while (r.TryComma());

            return new CssList<CssEasing>(list.ToArray());
        }
    }

    internal sealed class IterationCountListKind : CssKind
    {
        public override object? Parse(CssReader r, CssParseContext pc)
        {
            var list = new List<double>();
            do
            {
                if (r.TryIdent("infinite"))
                {
                    list.Add(double.PositiveInfinity);
                    continue;
                }

                var n = CssNumeric.Parse(r, CssNumberFlags.Number | CssNumberFlags.PositiveOnly);
                if (n is null)
                {
                    return null;
                }

                list.Add(n.Compute(CssNumericContext.Default).Value);
            }
            while (r.TryComma());

            return new CssList<double>(list.ToArray());
        }

        public override string Print(object specified) =>
            CssSerializer.Join((CssList<double>)specified, n => double.IsPositiveInfinity(n) ? "infinite" : CssSerializer.Number(n));
    }
}
