using System;
using System.Collections.Generic;
using AvaAdwaita.CssEngine.Syntax;
using AvaAdwaita.CssEngine.Values;

namespace AvaAdwaita.CssEngine.Properties;

/// <summary>Parsers for the composite values several properties and shorthands share.</summary>
internal static class CssValueParsers
{
    public const CssNumberFlags Length = CssNumberFlags.Length;
    public const CssNumberFlags PositiveLength = CssNumberFlags.Length | CssNumberFlags.PositiveOnly;
    public const CssNumberFlags LengthPercent = CssNumberFlags.Length | CssNumberFlags.Percent;

    // ---- shadows (gtkcssshadowvalue.c) ----

    /// <summary>Parses <c>none</c> or a comma list of shadows.</summary>
    public static ShadowListSpec? ParseShadows(CssReader r, bool box)
    {
        if (r.TryIdent("none"))
        {
            return new ShadowListSpec([]);
        }

        var list = new List<ShadowSpec>();
        do
        {
            var s = ParseShadow(r, box);
            if (s is null)
            {
                return null;
            }

            list.Add(s);
        }
        while (r.TryComma());

        return new ShadowListSpec(list.ToArray());
    }

    public static ShadowSpec? ParseShadow(CssReader r, bool box)
    {
        var inset = false;
        CssColorExpr? color = null;
        CssNumeric?[] lengths = new CssNumeric?[4];
        var n = 0;
        while (!r.AtEnd && !r.PeekComma)
        {
            if (box && !inset && r.TryIdent("inset"))
            {
                inset = true;
                continue;
            }

            if (n == 0 && CssNumeric.CanParse(r))
            {
                while (n < (box ? 4 : 3) && CssNumeric.CanParse(r))
                {
                    var flags = n == 2 ? PositiveLength : Length;
                    lengths[n] = CssNumeric.Parse(r, flags);
                    if (lengths[n] is null)
                    {
                        return null;
                    }

                    n++;
                }

                if (n < 2)
                {
                    r.Fail("Expected shadow value to contain a length");
                    return null;
                }

                continue;
            }

            if (color is null && CssColorParser.CanParse(r))
            {
                color = CssColorParser.Parse(r);
                if (color is null)
                {
                    return null;
                }

                continue;
            }

            break;
        }

        if (n < 2)
        {
            r.Fail("Expected shadow value to contain a length");
            return null;
        }

        return new ShadowSpec(inset, lengths[0]!, lengths[1]!, lengths[2], lengths[3], color ?? CssColorExpr.Current);
    }

    public sealed class ShadowSpec(bool inset, CssNumeric x, CssNumeric y, CssNumeric? blur, CssNumeric? spread, CssColorExpr color)
    {
        public bool Inset => inset;

        public CssNumeric X => x;

        public CssNumeric Y => y;

        public CssNumeric? Blur => blur;

        public CssNumeric? Spread => spread;

        public CssColorExpr Color => color;

        public bool UsesCurrentColor => color.UsesCurrentColor;

        public CssShadow Compute(CssComputeContext ctx)
        {
            var n = ctx.Numeric;
            var c = color.Resolve(ctx.Colors, ctx.Color) ?? CssColor.Transparent;
            return new CssShadow(inset, x.Compute(n).Value, y.Compute(n).Value, blur?.Compute(n).Value ?? 0, spread?.Compute(n).Value ?? 0, c);
        }
    }

    public sealed class ShadowListSpec(ShadowSpec[] items) : ICssSpecified
    {
        public ShadowSpec[] Items => items;

        public bool UsesCurrentColor => Array.Exists(items, i => i.UsesCurrentColor);

        public object Compute(CssComputeContext ctx)
        {
            if (items.Length == 0)
            {
                return CssList<CssShadow>.Empty;
            }

            var a = new CssShadow[items.Length];
            for (var i = 0; i < a.Length; i++)
            {
                a[i] = items[i].Compute(ctx);
            }

            return new CssList<CssShadow>(a);
        }
    }

    // ---- positions (gtkcsspositionvalue.c) ----

    private static readonly (string Name, double Percent, bool Horizontal, bool Swap)[] s_positionNames =
    [
        ("left", 0, true, false),
        ("right", 100, true, false),
        ("center", 50, true, true),
        ("top", 0, false, false),
        ("bottom", 100, false, false),
    ];

    /// <summary>Tries a position of one or two values, or returns null and leaves the reader where it was.</summary>
    /// <remarks>Port of position_value_parse (gtkcsspositionvalue.c), which has no three- or four-value syntax.</remarks>
    public static PositionSpec? TryParsePosition(CssReader r)
    {
        var start = r.Position;
        CssNumeric? x = null, y = null;
        var swap = false;
        int i;
        for (i = 0; i < s_positionNames.Length; i++)
        {
            if (r.TryIdent(s_positionNames[i].Name))
            {
                var pct = CssNumeric.Dimension(s_positionNames[i].Percent, CssUnit.Percent);
                if (s_positionNames[i].Horizontal)
                {
                    x = pct;
                }
                else
                {
                    y = pct;
                }

                swap = s_positionNames[i].Swap;
                break;
            }
        }

        if (i == s_positionNames.Length)
        {
            if (!CssNumeric.CanParse(r))
            {
                r.Position = start;
                return null;
            }

            x = CssNumeric.Parse(r, LengthPercent);
            if (x is null)
            {
                r.Position = start;
                return null;
            }
        }

        for (i = 0; i < s_positionNames.Length; i++)
        {
            var n = s_positionNames[i];
            if (!swap && !n.Swap)
            {
                if (n.Horizontal && x is not null)
                {
                    continue;
                }

                if (!n.Horizontal && y is not null)
                {
                    continue;
                }
            }

            if (r.TryIdent(n.Name))
            {
                var pct = CssNumeric.Dimension(n.Percent, CssUnit.Percent);
                if (x is not null)
                {
                    if (n.Horizontal && !n.Swap)
                    {
                        y = x;
                        x = pct;
                    }
                    else
                    {
                        y = pct;
                    }
                }
                else
                {
                    x = pct;
                }

                break;
            }
        }

        if (i == s_positionNames.Length)
        {
            if (CssNumeric.CanParse(r))
            {
                if (y is not null)
                {
                    r.Fail("Invalid combination of values");
                    r.Position = start;
                    return null;
                }

                y = CssNumeric.Parse(r, LengthPercent);
                if (y is null)
                {
                    r.Position = start;
                    return null;
                }
            }
            else if (y is not null)
            {
                x = CssNumeric.Dimension(50, CssUnit.Percent);
            }
            else
            {
                y = CssNumeric.Dimension(50, CssUnit.Percent);
            }
        }

        return new PositionSpec(x!, y!);
    }

    public sealed class PositionSpec(CssNumeric x, CssNumeric y) : ICssSpecified
    {
        public CssNumeric X => x;

        public CssNumeric Y => y;

        public bool UsesCurrentColor => false;

        public object Compute(CssComputeContext ctx) => ComputePosition(ctx);

        public CssPosition ComputePosition(CssComputeContext ctx) => new(x.Compute(ctx.Numeric), y.Compute(ctx.Numeric));
    }

    // ---- images (gtkcssimage*.c) ----

    public static bool CanParseImage(CssReader r)
    {
        var c = r.Peek();
        if (c is null)
        {
            return false;
        }

        if (c.Type == CssTokenType.Url)
        {
            return true;
        }

        if (!c.IsFunction)
        {
            return false;
        }

        return c.Token.Text.ToLowerInvariant() is "linear-gradient" or "repeating-linear-gradient" or "radial-gradient"
            or "repeating-radial-gradient" or "image" or "cross-fade" or "url" or "-gtk-icontheme" or "-gtk-recolor" or "-gtk-scaled";
    }

    /// <summary>Parses one image. <c>none</c> is handled by the caller.</summary>
    public static ImageSpec? ParseImage(CssReader r, CssParseContext pc)
    {
        var c = r.Peek();
        if (c is null)
        {
            r.Fail("Expected an image");
            return null;
        }

        if (c.Type == CssTokenType.Url)
        {
            r.Next();
            return new ImageSpec.Fixed(new CssUrlImage(pc.ResolveUrl(c.Token.Text)));
        }

        if (!c.IsFunction)
        {
            r.Fail("Expected an image");
            return null;
        }

        var name = c.Token.Text.ToLowerInvariant();
        var inner = new CssReader(c.Children);
        ImageSpec? result;
        switch (name)
        {
            case "url":
                {
                    var s = inner.Peek();
                    result = s is { Type: CssTokenType.String } ? new ImageSpec.Fixed(new CssUrlImage(pc.ResolveUrl(s.Token.Text))) : null;
                    if (result is not null)
                    {
                        inner.Next();
                    }

                    break;
                }

            case "-gtk-icontheme":
                {
                    var s = inner.Peek();
                    result = s is { Type: CssTokenType.String } ? new ImageSpec.Fixed(new CssIconThemeImage(s.Token.Text)) : null;
                    if (result is not null)
                    {
                        inner.Next();
                    }

                    break;
                }

            case "-gtk-recolor":
                result = ParseRecolor(c.Children, pc, inner);
                inner = new CssReader([]);
                break;
            case "linear-gradient":
            case "repeating-linear-gradient":
                result = ParseLinearGradient(inner, name.StartsWith("repeating", StringComparison.Ordinal));
                break;
            case "radial-gradient":
            case "repeating-radial-gradient":
                result = ParseRadialGradient(inner, name.StartsWith("repeating", StringComparison.Ordinal));
                break;
            case "image":
                result = ParseImageFunction(c.Children, pc, inner);
                inner = new CssReader([]);
                break;
            case "cross-fade":
                result = ParseCrossFade(c.Children, pc, inner);
                inner = new CssReader([]);
                break;
            default:
                r.ReportUnknownFunction(c.Token.Text);
                return null;
        }

        if (result is null || !inner.AtEnd)
        {
            r.Absorb(inner);
            r.Fail($"invalid {name}()");
            return null;
        }

        r.Next();
        return result;
    }

    private static ImageSpec? ParseRecolor(CssComponent[] args, CssParseContext pc, CssReader err)
    {
        var parts = CssReader.SplitCommas(args);
        var ur = new CssReader(parts[0]);
        var u = ur.Peek();
        string? url = null;
        if (u is { Type: CssTokenType.Url })
        {
            url = u.Token.Text;
        }
        else if (u is not null && u.IsFunctionNamed("url") && u.Children.Length > 0)
        {
            var s = Array.Find(u.Children, x => x.Type == CssTokenType.String);
            url = s?.Token.Text;
        }

        if (url is null)
        {
            err.Fail("Expected a url in -gtk-recolor()");
            return null;
        }

        ur.Next();
        if (!ur.AtEnd)
        {
            return null;
        }

        PaletteSpec? palette = null;
        if (parts.Count > 1)
        {
            var rest = new List<CssComponent>();
            for (var i = 1; i < parts.Count; i++)
            {
                if (i > 1)
                {
                    rest.Add(new CssComponent(new CssToken(CssTokenType.Comma, ",", 0, false, false, false, 0, 0)));
                }

                rest.AddRange(parts[i]);
            }

            var pr = new CssReader(rest);
            palette = ParsePalette(pr);
            if (palette is null || !pr.AtEnd)
            {
                err.Absorb(pr);
                return null;
            }
        }

        return new ImageSpec.Recolor(pc.ResolveUrl(url), palette);
    }

    private static ImageSpec? ParseImageFunction(CssComponent[] args, CssParseContext pc, CssReader err)
    {
        var parts = CssReader.SplitCommas(args);
        var images = new List<ImageSpec>();
        CssColorExpr? color = null;
        for (var i = 0; i < parts.Count; i++)
        {
            var pr = new CssReader(parts[i]);
            if (pr.Peek() is { Type: CssTokenType.String } s)
            {
                pr.Next();
                images.Add(new ImageSpec.Fixed(new CssUrlImage(pc.ResolveUrl(s.Token.Text))));
            }
            else if (CanParseImage(pr))
            {
                var img = ParseImage(pr, pc);
                if (img is null)
                {
                    err.Absorb(pr);
                    return null;
                }

                images.Add(img);
            }
            else if (i == parts.Count - 1)
            {
                color = CssColorParser.Parse(pr);
                if (color is null)
                {
                    err.Absorb(pr);
                    return null;
                }
            }
            else
            {
                err.Fail("Expected an image in image()");
                return null;
            }

            if (!pr.AtEnd)
            {
                err.Fail("junk in image()");
                return null;
            }
        }

        return new ImageSpec.Fallback(images.ToArray(), color);
    }

    private static ImageSpec? ParseCrossFade(CssComponent[] args, CssParseContext pc, CssReader err)
    {
        var parts = CssReader.SplitCommas(args);
        var layers = new List<(double? Percent, ImageSpec? Image, CssColorExpr? Color)>();
        foreach (var part in parts)
        {
            var pr = new CssReader(part);
            double? pct = null;
            ImageSpec? img = null;
            CssColorExpr? col = null;
            while (!pr.AtEnd)
            {
                if (pct is null && pr.Peek() is { Type: CssTokenType.Percentage } p)
                {
                    pr.Next();
                    pct = p.Token.Number;
                }
                else if (img is null && col is null && CanParseImage(pr))
                {
                    img = ParseImage(pr, pc);
                    if (img is null)
                    {
                        err.Absorb(pr);
                        return null;
                    }
                }
                else if (img is null && col is null && CssColorParser.CanParse(pr))
                {
                    col = CssColorParser.Parse(pr);
                    if (col is null)
                    {
                        err.Absorb(pr);
                        return null;
                    }
                }
                else
                {
                    err.Fail("junk in cross-fade()");
                    return null;
                }
            }

            layers.Add((pct, img, col));
        }

        return new ImageSpec.CrossFade(layers.ToArray());
    }

    private static readonly string[] s_sides = ["top", "right", "bottom", "left"];

    private static ImageSpec? ParseLinearGradient(CssReader r, bool repeating)
    {
        var side = 0;
        CssNumeric? angle = null;
        if (r.TryIdent("to"))
        {
            for (var i = 0; i < 2 && r.Peek() is { Type: CssTokenType.Ident } id; i++)
            {
                var idx = Array.FindIndex(s_sides, s => id.IsIdent(s));
                if (idx < 0)
                {
                    break;
                }

                r.Next();
                side |= 1 << idx;
            }

            if (side == 0 || (side & 5) == 5 || (side & 10) == 10)
            {
                r.Fail("Expected side that gradient should go to");
                return null;
            }

            if (!r.TryComma())
            {
                r.Fail("Expected a comma");
                return null;
            }
        }
        else if (CssNumeric.CanParse(r))
        {
            angle = CssNumeric.Parse(r, CssNumberFlags.Angle);
            if (angle is null || !r.TryComma())
            {
                r.Fail("Expected a comma after the angle");
                return null;
            }
        }
        else
        {
            side = CssLinearGradient.Bottom;
        }

        var stops = ParseStops(r);
        return stops is null ? null : new ImageSpec.Linear(side, angle, repeating, stops);
    }

    private static ImageSpec? ParseRadialGradient(CssReader r, bool repeating)
    {
        var circle = false;
        var sawShape = false;
        var size = CssRadialSize.FarthestCorner;
        CssNumeric? rx = null, ry = null;
        PositionSpec? center = null;
        var sawPrelude = false;
        while (!r.AtEnd && !r.PeekComma)
        {
            if (!sawShape && r.TryIdent("circle"))
            {
                circle = true;
                sawShape = true;
            }
            else if (!sawShape && r.TryIdent("ellipse"))
            {
                sawShape = true;
            }
            else if (r.TryIdent("closest-side"))
            {
                size = CssRadialSize.ClosestSide;
            }
            else if (r.TryIdent("closest-corner"))
            {
                size = CssRadialSize.ClosestCorner;
            }
            else if (r.TryIdent("farthest-side"))
            {
                size = CssRadialSize.FarthestSide;
            }
            else if (r.TryIdent("farthest-corner"))
            {
                size = CssRadialSize.FarthestCorner;
            }
            else if (r.TryIdent("at"))
            {
                center = TryParsePosition(r);
                if (center is null)
                {
                    r.Fail("Expected a position after 'at'");
                    return null;
                }
            }
            else if (rx is null && CssNumeric.CanParse(r))
            {
                rx = CssNumeric.Parse(r, LengthPercent | CssNumberFlags.PositiveOnly);
                if (rx is null)
                {
                    return null;
                }

                if (CssNumeric.CanParse(r))
                {
                    ry = CssNumeric.Parse(r, LengthPercent | CssNumberFlags.PositiveOnly);
                    if (ry is null)
                    {
                        return null;
                    }
                }

                size = CssRadialSize.Explicit;
            }
            else
            {
                break;
            }

            sawPrelude = true;
        }

        if (sawPrelude && !r.TryComma())
        {
            // No prelude means the first item was a color stop.
            r.Fail("Expected a comma");
            return null;
        }

        if (rx is not null && ry is null && !circle && sawShape)
        {
            r.Fail("ellipse needs two radii");
            return null;
        }

        if (rx is not null && ry is null)
        {
            circle = true;
        }

        var stops = ParseStops(r);
        return stops is null ? null : new ImageSpec.Radial(circle, size, rx, ry, center, repeating, stops);
    }

    private static (CssColorExpr Color, CssNumeric? Offset)[]? ParseStops(CssReader r)
    {
        var list = new List<(CssColorExpr, CssNumeric?)>();
        do
        {
            CssNumeric? off = null;
            CssColorExpr? col;
            if (CssNumeric.CanParse(r))
            {
                // A lone position is a transition hint; GTK does not support hints.
                r.Fail("Color stops need a color");
                return null;
            }

            col = CssColorParser.Parse(r);
            if (col is null)
            {
                return null;
            }

            if (CssNumeric.CanParse(r))
            {
                off = CssNumeric.Parse(r, LengthPercent);
                if (off is null)
                {
                    return null;
                }
            }

            list.Add((col, off));
            if (CssNumeric.CanParse(r))
            {
                // A second position repeats the color, as in CSS Images 4.
                var off2 = CssNumeric.Parse(r, LengthPercent);
                if (off2 is null)
                {
                    return null;
                }

                list.Add((col, off2));
            }
        }
        while (r.TryComma());

        if (list.Count < 2)
        {
            r.Fail("Gradients need at least two color stops");
            return null;
        }

        return list.ToArray();
    }

    public abstract class ImageSpec : ICssSpecified
    {
        public abstract bool UsesCurrentColor { get; }

        object ICssSpecified.Compute(CssComputeContext ctx) => ComputeImage(ctx)!;

        public abstract CssImage? ComputeImage(CssComputeContext ctx);

        protected static CssList<CssColorStop> ComputeStops((CssColorExpr Color, CssNumeric? Offset)[] stops, CssComputeContext ctx)
        {
            var a = new CssColorStop[stops.Length];
            for (var i = 0; i < a.Length; i++)
            {
                var c = stops[i].Color.Resolve(ctx.Colors, ctx.Color) ?? CssColor.Transparent;
                a[i] = new CssColorStop(c, stops[i].Offset?.Compute(ctx.Numeric));
            }

            return new CssList<CssColorStop>(a);
        }

        public sealed class Fixed(CssImage image) : ImageSpec
        {
            public CssImage Image => image;

            public override bool UsesCurrentColor => image is CssIconThemeImage;

            public override CssImage? ComputeImage(CssComputeContext ctx) => image;
        }

        public sealed class Recolor(string url, PaletteSpec? palette) : ImageSpec
        {
            public string Url => url;

            public PaletteSpec? Palette => palette;

            public override bool UsesCurrentColor => true;

            public override CssImage? ComputeImage(CssComputeContext ctx) => new CssRecolorImage(url, palette?.ComputePalette(ctx));
        }

        public sealed class Linear(int side, CssNumeric? angle, bool repeating, (CssColorExpr, CssNumeric?)[] stops) : ImageSpec
        {
            public int Side => side;

            public CssNumeric? Angle => angle;

            public bool Repeating => repeating;

            public (CssColorExpr Color, CssNumeric? Offset)[] Stops => stops;

            public override bool UsesCurrentColor => Array.Exists(stops, s => s.Item1.UsesCurrentColor);

            public override CssImage? ComputeImage(CssComputeContext ctx) =>
                new CssLinearGradient(side, angle?.Compute(ctx.Numeric).Value ?? 0, repeating, ComputeStops(stops, ctx));
        }

        public sealed class Radial(bool circle, CssRadialSize size, CssNumeric? rx, CssNumeric? ry, PositionSpec? center, bool repeating, (CssColorExpr, CssNumeric?)[] stops) : ImageSpec
        {
            public bool Circle => circle;

            public CssRadialSize Size => size;

            public CssNumeric? RadiusX => rx;

            public CssNumeric? RadiusY => ry;

            public PositionSpec? Center => center;

            public bool Repeating => repeating;

            public (CssColorExpr Color, CssNumeric? Offset)[] Stops => stops;

            public override bool UsesCurrentColor => Array.Exists(stops, s => s.Item1.UsesCurrentColor);

            public override CssImage? ComputeImage(CssComputeContext ctx) =>
                new CssRadialGradient(circle, size, rx?.Compute(ctx.Numeric), ry?.Compute(ctx.Numeric),
                    center?.ComputePosition(ctx) ?? new CssPosition(CssNumber.Pct(50), CssNumber.Pct(50)), repeating, ComputeStops(stops, ctx));
        }

        public sealed class Fallback(ImageSpec[] images, CssColorExpr? color) : ImageSpec
        {
            public ImageSpec[] Images => images;

            public CssColorExpr? Color => color;

            public override bool UsesCurrentColor => color?.UsesCurrentColor == true || Array.Exists(images, i => i.UsesCurrentColor);

            public override CssImage? ComputeImage(CssComputeContext ctx)
            {
                // GTK's image() picks the first image that loads. Gradients and icons always load.
                foreach (var i in images)
                {
                    if (i.ComputeImage(ctx) is { } img)
                    {
                        return img;
                    }
                }

                return new CssColorImage(color?.Resolve(ctx.Colors, ctx.Color) ?? CssColor.Transparent);
            }
        }

        public sealed class CrossFade((double? Percent, ImageSpec? Image, CssColorExpr? Color)[] layers) : ImageSpec
        {
            public (double? Percent, ImageSpec? Image, CssColorExpr? Color)[] Layers => layers;

            public override bool UsesCurrentColor => Array.Exists(layers, l => l.Color?.UsesCurrentColor == true || l.Image?.UsesCurrentColor == true);

            public override CssImage? ComputeImage(CssComputeContext ctx)
            {
                // Missing percentages share what is left of 100%, as in gtkcssimagecrossfade.c.
                double given = 0;
                var missing = 0;
                foreach (var l in layers)
                {
                    if (l.Percent is { } p)
                    {
                        given += p;
                    }
                    else
                    {
                        missing++;
                    }
                }

                var share = missing > 0 ? Math.Max(0, 100 - given) / missing : 0;
                var a = new (double, CssImage?)[layers.Length];
                for (var i = 0; i < a.Length; i++)
                {
                    var l = layers[i];
                    var img = l.Image?.ComputeImage(ctx) ?? (l.Color is { } c ? new CssColorImage(c.Resolve(ctx.Colors, ctx.Color) ?? CssColor.Transparent) : null);
                    a[i] = (l.Percent ?? share, img);
                }

                return new CssCrossFadeImage(new CssList<(double, CssImage?)>(a));
            }
        }
    }

    // ---- palettes ----

    public static PaletteSpec? ParsePalette(CssReader r)
    {
        if (r.TryIdent("default"))
        {
            return PaletteSpec.Default;
        }

        var list = new List<(string, CssColorExpr)>();
        do
        {
            var name = r.TryAnyIdent();
            if (name is null)
            {
                r.Fail("Expected a color name");
                return null;
            }

            var c = CssColorParser.Parse(r);
            if (c is null)
            {
                return null;
            }

            list.Add((name, c));
        }
        while (r.TryComma());

        list.Sort((a, b) => string.CompareOrdinal(a.Item1, b.Item1));
        return new PaletteSpec(list.ToArray());
    }

    public sealed class PaletteSpec((string Name, CssColorExpr Color)[] colors) : ICssSpecified
    {
        public static PaletteSpec Default { get; } = new(
        [
            ("accent", new CssColorExpr.Named("accent_color")),
            ("error", new CssColorExpr.Named("error_color")),
            ("success", new CssColorExpr.Named("success_color")),
            ("warning", new CssColorExpr.Named("warning_color")),
        ]);

        public (string Name, CssColorExpr Color)[] Colors => colors;

        public bool UsesCurrentColor => true;

        public object Compute(CssComputeContext ctx) => ComputePalette(ctx);

        public CssPalette ComputePalette(CssComputeContext ctx)
        {
            var a = new (string, CssColor)[colors.Length];
            for (var i = 0; i < a.Length; i++)
            {
                // An unknown name falls back to the node's color, as GTK's palette fallback.
                a[i] = (colors[i].Name, colors[i].Color.Resolve(ctx.Colors, ctx.Color) ?? ctx.Color);
            }

            return new CssPalette(new CssList<(string, CssColor)>(a));
        }
    }

    // ---- easing (gtkcsseasevalue.c) ----

    public static bool CanParseEasing(CssReader r)
    {
        var c = r.Peek();
        if (c is null)
        {
            return false;
        }

        if (c.Type == CssTokenType.Ident)
        {
            return c.Token.Text.ToLowerInvariant() is "linear" or "ease" or "ease-in" or "ease-out" or "ease-in-out" or "step-start" or "step-end";
        }

        return c.IsFunctionNamed("cubic-bezier") || c.IsFunctionNamed("steps");
    }

    public static CssEasing? ParseEasing(CssReader r)
    {
        var c = r.Peek();
        if (c is null)
        {
            return null;
        }

        if (c.Type == CssTokenType.Ident)
        {
            CssEasing? e = c.Token.Text.ToLowerInvariant() switch
            {
                "linear" => CssEasing.Linear,
                "ease" => CssEasing.Ease,
                "ease-in" => CssEasing.Bezier(0.42, 0, 1, 1),
                "ease-out" => CssEasing.Bezier(0, 0, 0.58, 1),
                "ease-in-out" => CssEasing.Bezier(0.42, 0, 0.58, 1),
                "step-start" => new CssEasing(true, 0, 0, 0, 0, 1, true),
                "step-end" => new CssEasing(true, 0, 0, 0, 0, 1, false),
                _ => null,
            };
            if (e is not null)
            {
                r.Next();
            }
            else
            {
                r.Fail("Unknown timing function");
            }

            return e;
        }

        if (c.IsFunctionNamed("cubic-bezier"))
        {
            var parts = CssReader.SplitCommas(c.Children);
            if (parts.Count != 4)
            {
                r.Fail("cubic-bezier() needs 4 numbers");
                return null;
            }

            var v = new double[4];
            for (var i = 0; i < 4; i++)
            {
                var pr = new CssReader(parts[i]);
                var n = CssNumeric.Parse(pr, CssNumberFlags.Number);
                if (n is null || !pr.AtEnd)
                {
                    r.Fail("cubic-bezier() needs 4 numbers");
                    return null;
                }

                v[i] = n.Compute(CssNumericContext.Default).Value;
            }

            if (v[0] < 0 || v[0] > 1 || v[2] < 0 || v[2] > 1)
            {
                r.Fail("cubic-bezier() x values must be between 0 and 1");
                return null;
            }

            r.Next();
            return CssEasing.Bezier(v[0], v[1], v[2], v[3]);
        }

        if (c.IsFunctionNamed("steps"))
        {
            var parts = CssReader.SplitCommas(c.Children);
            var pr = new CssReader(parts[0]);
            if (pr.Peek() is not { Type: CssTokenType.Number, Token.IsInteger: true } n || n.Token.Number < 1)
            {
                r.Fail("steps() needs a positive integer");
                return null;
            }

            var start = false;
            if (parts.Count == 2)
            {
                var sr = new CssReader(parts[1]);
                if (sr.TryIdent("start") || sr.TryIdent("jump-start"))
                {
                    start = true;
                }
                else if (!(sr.TryIdent("end") || sr.TryIdent("jump-end")))
                {
                    r.Fail("steps() takes start or end");
                    return null;
                }
            }

            r.Next();
            return new CssEasing(true, 0, 0, 0, 0, (int)n.Token.Number, start);
        }

        r.Fail("Expected a timing function");
        return null;
    }

    // ---- transforms (gtkcsstransformvalue.c) ----

    public static TransformSpec? ParseTransform(CssReader r)
    {
        if (r.TryIdent("none"))
        {
            return new TransformSpec([]);
        }

        var list = new List<(CssTransformKind Kind, CssNumeric[] Args, int Axis)>();
        while (!r.AtEnd)
        {
            var c = r.Peek()!;
            if (!c.IsFunction)
            {
                r.Fail("Expected a transform function");
                return null;
            }

            var name = c.Token.Text.ToLowerInvariant();
            var parts = CssReader.SplitCommas(c.Children);
            CssNumeric[]? args;
            switch (name)
            {
                case "matrix":
                    args = Numbers(parts, 6, 6, CssNumberFlags.Number);
                    list.Add((CssTransformKind.Matrix, args!, 0));
                    break;
                case "translate":
                    args = Numbers(parts, 1, 2, LengthPercent);
                    list.Add((CssTransformKind.Translate, args!, 0));
                    break;
                case "translatex":
                    args = Numbers(parts, 1, 1, LengthPercent);
                    list.Add((CssTransformKind.Translate, args!, 1));
                    break;
                case "translatey":
                    args = Numbers(parts, 1, 1, LengthPercent);
                    list.Add((CssTransformKind.Translate, args!, 2));
                    break;
                case "scale":
                    args = Numbers(parts, 1, 2, CssNumberFlags.Number | CssNumberFlags.Percent);
                    list.Add((CssTransformKind.Scale, args!, 0));
                    break;
                case "scalex":
                    args = Numbers(parts, 1, 1, CssNumberFlags.Number | CssNumberFlags.Percent);
                    list.Add((CssTransformKind.Scale, args!, 1));
                    break;
                case "scaley":
                    args = Numbers(parts, 1, 1, CssNumberFlags.Number | CssNumberFlags.Percent);
                    list.Add((CssTransformKind.Scale, args!, 2));
                    break;
                case "rotate":
                case "rotatez":
                    args = Numbers(parts, 1, 1, CssNumberFlags.Angle);
                    list.Add((CssTransformKind.Rotate, args!, 0));
                    break;
                case "skew":
                    args = Numbers(parts, 1, 2, CssNumberFlags.Angle);
                    list.Add((CssTransformKind.Skew, args!, 0));
                    break;
                case "skewx":
                    args = Numbers(parts, 1, 1, CssNumberFlags.Angle);
                    list.Add((CssTransformKind.Skew, args!, 1));
                    break;
                case "skewy":
                    args = Numbers(parts, 1, 1, CssNumberFlags.Angle);
                    list.Add((CssTransformKind.Skew, args!, 2));
                    break;
                case "perspective":
                    args = Numbers(parts, 1, 1, PositiveLength);
                    list.Add((CssTransformKind.Perspective, args!, 0));
                    break;
                default:
                    r.ReportUnknownFunction(c.Token.Text);
                    return null;
            }

            if (args is null)
            {
                r.Fail($"invalid {name}()");
                return null;
            }

            r.Next();
        }

        if (list.Count == 0)
        {
            r.Fail("Expected a transform");
            return null;
        }

        return new TransformSpec(list.ToArray());
    }

    private static CssNumeric[]? Numbers(List<List<CssComponent>> parts, int min, int max, CssNumberFlags flags)
    {
        if (parts.Count < min || parts.Count > max)
        {
            return null;
        }

        var a = new CssNumeric[parts.Count];
        for (var i = 0; i < a.Length; i++)
        {
            var pr = new CssReader(parts[i]);
            var n = CssNumeric.Parse(pr, flags);
            if (n is null || !pr.AtEnd)
            {
                return null;
            }

            a[i] = n;
        }

        return a;
    }

    public sealed class TransformSpec((CssTransformKind Kind, CssNumeric[] Args, int Axis)[] functions) : ICssSpecified
    {
        /// <summary>The functions; an axis of 1 or 2 is the X or Y form, as <c>translateX()</c>.</summary>
        public (CssTransformKind Kind, CssNumeric[] Args, int Axis)[] Functions => functions;

        public bool UsesCurrentColor => false;

        public object Compute(CssComputeContext ctx)
        {
            if (functions.Length == 0)
            {
                return CssList<CssTransformFunction>.Empty;
            }

            var n = ctx.Numeric;
            var a = new CssTransformFunction[functions.Length];
            var zero = CssNumber.Px(0);
            for (var i = 0; i < a.Length; i++)
            {
                var (kind, args, axis) = functions[i];
                double V(int k) => args[k].Compute(n) is var x && x.Class == CssDimensionClass.Percent ? x.Percent / 100 : x.Value;
                a[i] = kind switch
                {
                    CssTransformKind.Matrix => new(kind, V(0), V(1), V(2), V(3), V(4), V(5), zero, zero),
                    CssTransformKind.Translate => axis switch
                    {
                        1 => new(kind, 0, 0, 0, 0, 0, 0, args[0].Compute(n), zero),
                        2 => new(kind, 0, 0, 0, 0, 0, 0, zero, args[0].Compute(n)),
                        _ => new(kind, 0, 0, 0, 0, 0, 0, args[0].Compute(n), args.Length > 1 ? args[1].Compute(n) : zero),
                    },
                    CssTransformKind.Scale => axis switch
                    {
                        1 => new(kind, V(0), 1, 0, 0, 0, 0, zero, zero),
                        2 => new(kind, 1, V(0), 0, 0, 0, 0, zero, zero),
                        _ => new(kind, V(0), args.Length > 1 ? V(1) : V(0), 0, 0, 0, 0, zero, zero),
                    },
                    CssTransformKind.Skew => axis switch
                    {
                        1 => new(kind, V(0), 0, 0, 0, 0, 0, zero, zero),
                        2 => new(kind, 0, V(0), 0, 0, 0, 0, zero, zero),
                        _ => new(kind, V(0), args.Length > 1 ? V(1) : 0, 0, 0, 0, 0, zero, zero),
                    },
                    _ => new(kind, V(0), 0, 0, 0, 0, 0, zero, zero),
                };
            }

            return new CssList<CssTransformFunction>(a);
        }
    }

    // ---- filters (gtkcssfiltervalue.c) ----

    public static FilterSpec? ParseFilter(CssReader r)
    {
        if (r.TryIdent("none"))
        {
            return new FilterSpec([]);
        }

        var list = new List<(CssFilterKind, CssNumeric?, ShadowSpec?)>();
        while (!r.AtEnd)
        {
            var c = r.Peek()!;
            if (!c.IsFunction)
            {
                r.Fail("Expected a filter function");
                return null;
            }

            var name = c.Token.Text.ToLowerInvariant();
            CssFilterKind kind;
            CssNumberFlags flags = CssNumberFlags.Number | CssNumberFlags.Percent | CssNumberFlags.PositiveOnly;
            switch (name)
            {
                case "blur":
                    kind = CssFilterKind.Blur;
                    flags = PositiveLength;
                    break;
                case "brightness":
                    kind = CssFilterKind.Brightness;
                    break;
                case "contrast":
                    kind = CssFilterKind.Contrast;
                    break;
                case "grayscale":
                    kind = CssFilterKind.Grayscale;
                    break;
                case "hue-rotate":
                    kind = CssFilterKind.HueRotate;
                    flags = CssNumberFlags.Angle;
                    break;
                case "invert":
                    kind = CssFilterKind.Invert;
                    break;
                case "opacity":
                    kind = CssFilterKind.Opacity;
                    break;
                case "saturate":
                    kind = CssFilterKind.Saturate;
                    break;
                case "sepia":
                    kind = CssFilterKind.Sepia;
                    break;
                case "drop-shadow":
                    {
                        var inner = new CssReader(c.Children);
                        var s = ParseShadow(inner, box: false);
                        if (s is null || !inner.AtEnd)
                        {
                            r.Absorb(inner);
                            r.Fail("invalid drop-shadow()");
                            return null;
                        }

                        list.Add((CssFilterKind.DropShadow, null, s));
                        r.Next();
                        continue;
                    }

                default:
                    r.ReportUnknownFunction(c.Token.Text);
                    return null;
            }

            var ar = new CssReader(c.Children);
            var amount = CssNumeric.Parse(ar, flags);
            if (amount is null || !ar.AtEnd)
            {
                r.Absorb(ar);
                r.Fail($"invalid {name}()");
                return null;
            }

            list.Add((kind, amount, null));
            r.Next();
        }

        if (list.Count == 0)
        {
            r.Fail("Expected a filter");
            return null;
        }

        return new FilterSpec(list.ToArray());
    }

    public sealed class FilterSpec((CssFilterKind Kind, CssNumeric? Amount, ShadowSpec? Shadow)[] functions) : ICssSpecified
    {
        public (CssFilterKind Kind, CssNumeric? Amount, ShadowSpec? Shadow)[] Functions => functions;

        public bool UsesCurrentColor => Array.Exists(functions, f => f.Shadow?.UsesCurrentColor == true);

        public object Compute(CssComputeContext ctx)
        {
            if (functions.Length == 0)
            {
                return CssList<CssFilterFunction>.Empty;
            }

            var a = new CssFilterFunction[functions.Length];
            for (var i = 0; i < a.Length; i++)
            {
                var (kind, amount, shadow) = functions[i];
                if (shadow is not null)
                {
                    a[i] = new CssFilterFunction(kind, 0, shadow.Compute(ctx));
                    continue;
                }

                var v = amount!.Compute(ctx.Numeric);
                var x = v.Class == CssDimensionClass.Percent ? v.Percent / 100 : v.Value;
                a[i] = new CssFilterFunction(kind, x, null);
            }

            return new CssList<CssFilterFunction>(a);
        }
    }
}
