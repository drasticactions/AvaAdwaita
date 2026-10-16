using System;
using System.Collections.Generic;
using AvaAdwaita.CssEngine.Syntax;
using AvaAdwaita.CssEngine.Values;

namespace AvaAdwaita.CssEngine.Properties;

/// <content>The background and image kinds.</content>
internal static partial class CssKinds
{
    /// <summary>A specified list of layers, printed comma-separated.</summary>
    internal interface ISpecifiedList
    {
        object[] Items { get; }
    }

    /// <summary>A specified list whose items compute one by one.</summary>
    internal sealed class ListSpec<T>(object[] items) : ICssSpecified, ISpecifiedList
    {
        public object[] Items => items;

        public bool UsesCurrentColor => Array.Exists(items, i => i is ICssSpecified { UsesCurrentColor: true });

        public object Compute(CssComputeContext ctx)
        {
            var a = new T[items.Length];
            for (var i = 0; i < a.Length; i++)
            {
                a[i] = (T)(items[i] is ICssSpecified s ? s.Compute(ctx) : items[i]);
            }

            return new CssList<T>(a);
        }
    }

    internal static readonly object s_noImage = new();

    /// <summary>Parses <c>none</c> or an image. Returns <see cref="s_noImage"/> for none.</summary>
    internal static object? ParseImageOrNone(CssReader r, CssParseContext pc)
    {
        if (r.TryIdent("none"))
        {
            return s_noImage;
        }

        if (!CssValueParsers.CanParseImage(r))
        {
            if (r.Peek() is { IsFunction: true } f)
            {
                r.ReportUnknownFunction(f.Token.Text);
            }
            else
            {
                r.Fail("Expected an image");
            }

            return null;
        }

        return CssValueParsers.ParseImage(r, pc);
    }

    internal sealed class NullableImageSpec(object item) : ICssSpecified
    {
        /// <summary>An image's specified value, or <see cref="s_noImage"/>.</summary>
        public object Item => item;

        public bool UsesCurrentColor => item is ICssSpecified { UsesCurrentColor: true };

        public object Compute(CssComputeContext ctx) =>
            item is CssValueParsers.ImageSpec s ? (object?)s.ComputeImage(ctx) ?? NoImage.Instance : NoImage.Instance;
    }

    /// <summary>The boxed computed value of <c>none</c> for single-image properties.</summary>
    internal sealed class NoImage
    {
        public static readonly NoImage Instance = new();
    }

    internal sealed class BackgroundImageKind : CssKind
    {
        public override object? Parse(CssReader r, CssParseContext pc)
        {
            var list = new List<object>();
            do
            {
                var i = ParseImageOrNone(r, pc);
                if (i is null)
                {
                    return null;
                }

                list.Add(i);
            }
            while (r.TryComma());

            return new BackgroundImageSpec(list.ToArray());
        }

        public override object? Interpolate(object from, object to, double t)
        {
            // Solid color layers interpolate their colors; anything else switches at the end.
            var a = (CssList<CssImage?>)from;
            var b = (CssList<CssImage?>)to;
            if (a.Count != b.Count)
            {
                return null;
            }

            var result = new CssImage?[a.Count];
            for (var i = 0; i < a.Count; i++)
            {
                var ca = a[i] as CssColorImage;
                var cb = b[i] as CssColorImage;
                if (ca is null && a[i] is null && cb is not null)
                {
                    ca = new CssColorImage(CssColorMath.ScaleAlpha(cb.Color, 0));
                }

                if (cb is null && b[i] is null && ca is not null)
                {
                    cb = new CssColorImage(CssColorMath.ScaleAlpha(ca.Color, 0));
                }

                if (ca is null || cb is null)
                {
                    if (InterpolateGradient(a[i], b[i], t) is not { } g)
                    {
                        return null;
                    }

                    result[i] = g;
                    continue;
                }

                result[i] = new CssColorImage(CssColorMath.LegacyMix(ca.Color, cb.Color, t));
            }

            return new CssList<CssImage?>(result);
        }
    }

    /// <summary>
    /// Two gradients of the same shape and stop count interpolate stop by stop, as GTK's image transitions
    /// (gtk_css_image_linear_transition, gtk_css_image_radial_transition); others cannot.
    /// </summary>
    private static CssImage? InterpolateGradient(CssImage? from, CssImage? to, double t)
    {
        switch (from, to)
        {
            case (CssLinearGradient a, CssLinearGradient b) when a.Repeating == b.Repeating && a.Side == b.Side && a.Stops.Count == b.Stops.Count:
                return InterpolateStops(a.Stops, b.Stops, t) is { } ls ? a with { Angle = a.Angle + ((b.Angle - a.Angle) * t), Stops = ls } : null;
            case (CssRadialGradient a, CssRadialGradient b) when a.Repeating == b.Repeating && a.Circle == b.Circle && a.Size == b.Size
                && a.RadiusX == b.RadiusX && a.RadiusY == b.RadiusY && a.Center == b.Center && a.Stops.Count == b.Stops.Count:
                return InterpolateStops(a.Stops, b.Stops, t) is { } rs ? a with { Stops = rs } : null;
            default:
                return null;
        }
    }

    private static CssList<CssColorStop>? InterpolateStops(CssList<CssColorStop> a, CssList<CssColorStop> b, double t)
    {
        var stops = new CssColorStop[a.Count];
        for (var i = 0; i < stops.Length; i++)
        {
            CssNumber? offset;
            if (a[i].Offset is null && b[i].Offset is null)
            {
                offset = null;
            }
            else if (a[i].Offset is { } oa && b[i].Offset is { } ob && oa.Class == ob.Class)
            {
                offset = new CssNumber(oa.Value + ((ob.Value - oa.Value) * t), oa.Percent + ((ob.Percent - oa.Percent) * t), oa.Class);
            }
            else
            {
                return null;
            }

            stops[i] = new CssColorStop(CssColorMath.LegacyMix(a[i].Color, b[i].Color, t), offset);
        }

        return new CssList<CssColorStop>(stops);
    }

    internal sealed class BackgroundImageSpec(object[] items) : ICssSpecified
    {
        public object[] Items => items;

        public bool UsesCurrentColor => Array.Exists(items, i => i is ICssSpecified { UsesCurrentColor: true });

        public object Compute(CssComputeContext ctx)
        {
            var a = new CssImage?[items.Length];
            for (var i = 0; i < a.Length; i++)
            {
                a[i] = items[i] is CssValueParsers.ImageSpec s ? s.ComputeImage(ctx) : null;
            }

            return new CssList<CssImage?>(a);
        }
    }

    internal sealed class ImageKind : CssKind
    {
        public override object? Parse(CssReader r, CssParseContext pc)
        {
            var i = ParseImageOrNone(r, pc);
            return i is null ? null : new NullableImageSpec(i);
        }

        public override object Compute(object specified, CssComputeContext ctx)
        {
            var v = ((ICssSpecified)specified).Compute(ctx);
            return v is NoImage ? null! : v;
        }
    }

    internal sealed class BackgroundSizeKind : CssKind
    {
        public override object? Parse(CssReader r, CssParseContext pc)
        {
            var list = new List<object>();
            do
            {
                var s = TryParseOne(r);
                if (s is null)
                {
                    r.Fail("Expected a background size");
                    return null;
                }

                list.Add(s);
            }
            while (r.TryComma());

            return new ListSpec<CssBackgroundSize>(list.ToArray());
        }

        public static object? TryParseOne(CssReader r)
        {
            if (r.TryIdent("cover"))
            {
                return new CssBackgroundSize(true, false, null, null);
            }

            if (r.TryIdent("contain"))
            {
                return new CssBackgroundSize(false, true, null, null);
            }

            CssNumeric? w = null;
            if (!r.TryIdent("auto"))
            {
                if (!CssNumeric.CanParse(r))
                {
                    return null;
                }

                w = CssNumeric.Parse(r, CssValueParsers.LengthPercent | CssNumberFlags.PositiveOnly);
                if (w is null)
                {
                    return null;
                }
            }

            CssNumeric? h = null;
            if (!r.TryIdent("auto") && CssNumeric.CanParse(r))
            {
                h = CssNumeric.Parse(r, CssValueParsers.LengthPercent | CssNumberFlags.PositiveOnly);
                if (h is null)
                {
                    return null;
                }
            }

            return new SizeSpec(w, h);
        }

        public override object? Interpolate(object from, object to, double t)
        {
            var a = (CssList<CssBackgroundSize>)from;
            var b = (CssList<CssBackgroundSize>)to;
            if (a.Count != b.Count)
            {
                return null;
            }

            var result = new CssBackgroundSize[a.Count];
            for (var i = 0; i < a.Count; i++)
            {
                var sa = a[i];
                var sb = b[i];
                if (sa.Cover != sb.Cover || sa.Contain != sb.Contain || sa.Width.HasValue != sb.Width.HasValue || sa.Height.HasValue != sb.Height.HasValue)
                {
                    return null;
                }

                result[i] = new CssBackgroundSize(sa.Cover, sa.Contain,
                    sa.Width is { } wa ? CssNumber.Lerp(wa, sb.Width!.Value, t) : null,
                    sa.Height is { } ha ? CssNumber.Lerp(ha, sb.Height!.Value, t) : null);
            }

            return new CssList<CssBackgroundSize>(result);
        }

        internal sealed class SizeSpec(CssNumeric? w, CssNumeric? h) : ICssSpecified
        {
            /// <summary>The width, or null for auto.</summary>
            public CssNumeric? Width => w;

            /// <summary>The height, or null for auto.</summary>
            public CssNumeric? Height => h;

            public bool UsesCurrentColor => false;

            public object Compute(CssComputeContext ctx) => new CssBackgroundSize(false, false, w?.Compute(ctx.Numeric), h?.Compute(ctx.Numeric));
        }
    }

    internal sealed class BackgroundPositionKind : CssKind
    {
        public override object? Parse(CssReader r, CssParseContext pc)
        {
            var list = new List<object>();
            do
            {
                var p = CssValueParsers.TryParsePosition(r);
                if (p is null)
                {
                    r.Fail("Unrecognized position value");
                    return null;
                }

                list.Add(p);
            }
            while (r.TryComma());

            return new ListSpec<CssPosition>(list.ToArray());
        }

        public override object? Interpolate(object from, object to, double t)
        {
            var a = (CssList<CssPosition>)from;
            var b = (CssList<CssPosition>)to;
            if (a.Count != b.Count)
            {
                return null;
            }

            var result = new CssPosition[a.Count];
            for (var i = 0; i < a.Count; i++)
            {
                result[i] = new CssPosition(CssNumber.Lerp(a[i].X, b[i].X, t), CssNumber.Lerp(a[i].Y, b[i].Y, t));
            }

            return new CssList<CssPosition>(result);
        }
    }

    private static CssRepeatStyle? TryRepeatStyle(CssReader r, bool border)
    {
        if (r.TryIdent("repeat"))
        {
            return CssRepeatStyle.Repeat;
        }

        if (r.TryIdent("space"))
        {
            return CssRepeatStyle.Space;
        }

        if (r.TryIdent("round"))
        {
            return CssRepeatStyle.Round;
        }

        if (!border && r.TryIdent("no-repeat"))
        {
            return CssRepeatStyle.NoRepeat;
        }

        if (border && r.TryIdent("stretch"))
        {
            return CssRepeatStyle.Stretch;
        }

        return null;
    }

    /// <summary>Tries one <c>background-repeat</c> layer.</summary>
    internal static CssBackgroundRepeat? TryParseRepeat(CssReader r)
    {
        if (r.TryIdent("repeat-x"))
        {
            return new CssBackgroundRepeat(CssRepeatStyle.Repeat, CssRepeatStyle.NoRepeat);
        }

        if (r.TryIdent("repeat-y"))
        {
            return new CssBackgroundRepeat(CssRepeatStyle.NoRepeat, CssRepeatStyle.Repeat);
        }

        if (TryRepeatStyle(r, false) is not { } x)
        {
            return null;
        }

        var y = TryRepeatStyle(r, false) ?? x;
        return new CssBackgroundRepeat(x, y);
    }

    internal sealed class BackgroundRepeatKind : CssKind
    {
        public override object? Parse(CssReader r, CssParseContext pc)
        {
            var list = new List<CssBackgroundRepeat>();
            do
            {
                if (TryParseRepeat(r) is not { } v)
                {
                    r.Fail("Expected a background repeat");
                    return null;
                }

                list.Add(v);
            }
            while (r.TryComma());

            return new CssList<CssBackgroundRepeat>(list.ToArray());
        }
    }

    internal sealed class BorderImageRepeatKind : CssKind
    {
        public override object? Parse(CssReader r, CssParseContext pc) => TryParse(r) ?? Fail(r);

        internal static object? TryParse(CssReader r)
        {
            if (TryRepeatStyle(r, true) is not { } x)
            {
                return null;
            }

            var y = TryRepeatStyle(r, true) ?? x;
            return new CssBackgroundRepeat(x, y);
        }

        private static object? Fail(CssReader r)
        {
            r.Fail("Expected a border image repeat");
            return null;
        }
    }

    internal sealed class BorderImageBoxKind(bool slice) : CssKind
    {
        public override object? Parse(CssReader r, CssParseContext pc)
        {
            var fill = false;
            var values = new List<CssNumeric?>();
            while (!r.AtEnd && values.Count < 4)
            {
                if (slice && !fill && r.TryIdent("fill"))
                {
                    fill = true;
                    continue;
                }

                if (!slice && r.TryIdent("auto"))
                {
                    values.Add(null);
                    continue;
                }

                if (!CssNumeric.CanParse(r))
                {
                    break;
                }

                var flags = slice
                    ? CssNumberFlags.Number | CssNumberFlags.Percent | CssNumberFlags.PositiveOnly
                    : CssNumberFlags.Number | CssNumberFlags.Percent | CssNumberFlags.Length | CssNumberFlags.PositiveOnly;
                var n = CssNumeric.Parse(r, flags);
                if (n is null)
                {
                    return null;
                }

                values.Add(n);
            }

            if (slice && !fill && r.TryIdent("fill"))
            {
                fill = true;
            }

            if (values.Count == 0)
            {
                r.Fail("Expected a number");
                return null;
            }

            while (values.Count < 4)
            {
                values.Add(values[(values.Count - 1) >> 1]);
            }

            return new BoxSpec(values.ToArray(), fill);
        }

        internal sealed class BoxSpec(CssNumeric?[] values, bool fill) : ICssSpecified
        {
            /// <summary>Top, right, bottom and left; null is auto.</summary>
            public CssNumeric?[] Values => values;

            public bool Fill => fill;

            public bool UsesCurrentColor => false;

            public object Compute(CssComputeContext ctx)
            {
                // auto is stored as NaN.
                CssNumber C(CssNumeric? n) => n?.Compute(ctx.Numeric) ?? CssNumber.Plain(double.NaN);
                return new CssBoxValues(C(values[0]), C(values[1]), C(values[2]), C(values[3]), fill);
            }
        }
    }
}
