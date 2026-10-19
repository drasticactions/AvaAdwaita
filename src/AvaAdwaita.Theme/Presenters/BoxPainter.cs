using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using AvaAdwaita.CssEngine.Computed;
using AvaAdwaita.CssEngine.Values;
using AvaAdwaita.Markup;

namespace AvaAdwaita.Presenters;

/// <summary>
/// Draws one node's CSS box in GTK's order: outset shadows, background color, background images,
/// inset shadows, border, outline.
/// </summary>
/// <remarks>Follows gtkrenderbackground.c, gtkrenderborder.c and gtkcssshadowvalue.c.</remarks>
internal static class BoxPainter
{
    // The color filters of the node being painted; the helpers below draw every color through them.
    [ThreadStatic]
    private static CssColorMatrix? s_filter;

    private static Color Col(CssColor c) => CssConvert.Color(s_filter is { } m ? m.Apply(c) : c);

    private static IImmutableSolidColorBrush Brush(CssColor c) => CssConvert.Brush(s_filter is { } m ? m.Apply(c) : c);

    public static void Paint(DrawingContext ctx, ComputedStyle s, Size size, Visual? owner = null)
    {
        var outer = s_filter;
        s_filter = s.ColorMatrix;
        try
        {
            PaintFiltered(ctx, s, size, owner);
        }
        finally
        {
            s_filter = outer;
        }
    }

    private static void PaintFiltered(DrawingContext ctx, ComputedStyle s, Size size, Visual? owner)
    {
        var g = new CssBoxGeometry(s, size);
        if (g.BorderBox.Width <= 0 || g.BorderBox.Height <= 0)
        {
            return;
        }

        var rec = DrawRecorder.For(owner);
        PaintOutsetShadows(ctx, s.Background.BoxShadow, g.Outer);
        PaintBackground(ctx, s, g);
        PaintInsetShadows(ctx, s.Background.BoxShadow, g.PaddingRounded);
        PaintBorder(ctx, s, g);
        PaintOutline(ctx, s, g);
        if (rec is not null)
        {
            Record(rec, owner!, s, g);
        }
    }

    private static double[] Radii(RoundedRect r) => DrawRecorder.Radii(r);

    /// <summary>Reports the box's draws in the order and form GTK's render nodes have them.</summary>
    private static void Record(DrawRecorder rec, Visual owner, ComputedStyle s, CssBoxGeometry g)
    {
        var shadows = s.Background.BoxShadow;
        for (var i = 0; i < shadows.Count; i++)
        {
            var sh = shadows[i];
            if (!sh.Inset && !sh.Color.IsTransparent)
            {
                rec.Add(owner, new DrawRecord("shadow", g.Outer.Rect) { Radii = DrawRecorder.Radii(g.Outer), Color = Col(sh.Color), Alpha = sh.Color.ToSrgb().A, Shadow = [sh.X, sh.Y, sh.Spread, sh.Blur] });
            }
        }

        var bg = s.Background;
        if (!bg.Color.IsTransparent)
        {
            var clip = g.ForArea(bg.Clip.Count > 0 ? bg.Clip.Cycle(Math.Max(0, bg.Image.Count - 1)) : CssArea.BorderBox);
            rec.Add(owner, new DrawRecord("color", clip.Rect) { Radii = DrawRecorder.Radii(clip), Color = Col(bg.Color), Alpha = bg.Color.ToSrgb().A });
        }

        for (var i = bg.Image.Count - 1; i >= 0; i--)
        {
            if (bg.Image[i] is CssColorImage ci && !ci.Color.IsTransparent)
            {
                var clip = g.ForArea(bg.Clip.Count > 0 ? bg.Clip.Cycle(i) : CssArea.BorderBox);
                rec.Add(owner, new DrawRecord("color", clip.Rect) { Radii = DrawRecorder.Radii(clip), Color = Col(ci.Color), Alpha = ci.Color.ToSrgb().A });
            }
            else if (bg.Image[i] is CssLinearGradient or CssRadialGradient)
            {
                rec.Add(owner, new DrawRecord("gradient", g.ForArea(bg.Clip.Count > 0 ? bg.Clip.Cycle(i) : CssArea.BorderBox).Rect));
            }
        }

        for (var i = 0; i < shadows.Count; i++)
        {
            var sh = shadows[i];
            if (sh.Inset && !sh.Color.IsTransparent && sh.Spread == 0 && sh.Blur == 0 && Radii(g.PaddingRounded).All(r => r == 0))
            {
                // GTK draws these as color strips along the edges (gtk_css_shadow_value_snapshot_inset).
                var p = g.PaddingRounded.Rect;
                var c = Col(sh.Color);
                var a = sh.Color.ToSrgb().A;
                var top = sh.Y > 0 ? sh.Y : 0;
                if (sh.X > 0)
                {
                    rec.Add(owner, new DrawRecord("color", new Rect(p.X, p.Y + top, sh.X, p.Height - Math.Abs(sh.Y))) { Color = c, Alpha = a });
                }
                else if (sh.X < 0)
                {
                    rec.Add(owner, new DrawRecord("color", new Rect(p.Right + sh.X, p.Y + top, -sh.X, p.Height - Math.Abs(sh.Y))) { Color = c, Alpha = a });
                }

                if (sh.Y > 0)
                {
                    rec.Add(owner, new DrawRecord("color", new Rect(p.X, p.Y, p.Width, sh.Y)) { Color = c, Alpha = a });
                }
                else if (sh.Y < 0)
                {
                    rec.Add(owner, new DrawRecord("color", new Rect(p.X, p.Bottom + sh.Y, p.Width, -sh.Y)) { Color = c, Alpha = a });
                }
            }
            else if (sh.Inset && !sh.Color.IsTransparent)
            {
                rec.Add(owner, new DrawRecord("inset-shadow", g.PaddingRounded.Rect) { Radii = DrawRecorder.Radii(g.PaddingRounded), Color = Col(sh.Color), Alpha = sh.Color.ToSrgb().A, Shadow = [sh.X, sh.Y, sh.Spread, sh.Blur] });
            }
        }

        var b = s.Border;
        var w = g.Border;
        if ((w.Left > 0 || w.Top > 0 || w.Right > 0 || w.Bottom > 0)
            && !(b.TopColor.IsTransparent && b.RightColor.IsTransparent && b.BottomColor.IsTransparent && b.LeftColor.IsTransparent))
        {
            rec.Add(owner, new DrawRecord("border", g.Outer.Rect)
            {
                Radii = DrawRecorder.Radii(g.Outer),
                Widths = [w.Top, w.Right, w.Bottom, w.Left],
                Colors = [Col(b.TopColor), Col(b.RightColor), Col(b.BottomColor), Col(b.LeftColor)],
                Alphas = [b.TopColor.ToSrgb().A, b.RightColor.ToSrgb().A, b.BottomColor.ToSrgb().A, b.LeftColor.ToSrgb().A],
            });
        }

        var o = s.Outline;
        var ow = CssBoxGeometry.RoundLength(o.Width);
        if (ow > 0 && !o.Color.IsTransparent)
        {
            var inner = CssBoxGeometry.Shrink(g.Outer, -o.Offset, -o.Offset, -o.Offset, -o.Offset);
            var outer = CssBoxGeometry.Shrink(inner, -ow, -ow, -ow, -ow);
            var c = Col(o.Color);
            var a = o.Color.ToSrgb().A;
            rec.Add(owner, new DrawRecord("border", outer.Rect) { Radii = DrawRecorder.Radii(outer), Widths = [ow, ow, ow, ow], Colors = [c, c, c, c], Alphas = [a, a, a, a] });
        }
    }

    /// <summary>The Avalonia blur radius whose Skia sigma equals GTK's σ = r / 2.</summary>
    public static double AvaloniaBlur(double gtkBlur) => gtkBlur <= 1 ? gtkBlur / 2 : ((gtkBlur / 2) - 0.5) / 0.288675;

    // GTK draws the shadows in the order they are listed, so the first is at the bottom
    // (gtk_css_shadow_value_snapshot_outset and _inset), the reverse of the CSS specification.
    public static void PaintOutsetShadows(DrawingContext ctx, CssList<CssShadow> shadows, RoundedRect box)
    {
        for (var i = 0; i < shadows.Count; i++)
        {
            var sh = shadows[i];
            if (sh.Inset || sh.Color.IsTransparent)
            {
                continue;
            }

            var shadow = new BoxShadow
            {
                OffsetX = sh.X,
                OffsetY = sh.Y,
                Blur = AvaloniaBlur(sh.Blur),
                Spread = sh.Spread,
                Color = Col(sh.Color),
            };
            ctx.DrawRectangle(null, null, box, new BoxShadows(shadow));
        }
    }

    public static void PaintInsetShadows(DrawingContext ctx, CssList<CssShadow> shadows, RoundedRect padding)
    {
        for (var i = 0; i < shadows.Count; i++)
        {
            var sh = shadows[i];
            if (!sh.Inset || sh.Color.IsTransparent)
            {
                continue;
            }

            if (sh.Blur == 0)
            {
                // A sharp inset shadow is the padding box minus the box moved by the offset and shrunk by the spread.
                var inner = CssBoxGeometry.Shrink(padding, sh.Spread + sh.Y, sh.Spread - sh.X, sh.Spread - sh.Y, sh.Spread + sh.X);
                // The ring already lies inside the padding box; clipping to it as well would square the
                // anti-aliased coverage along the curves.
                var geom = new CombinedGeometry(GeometryCombineMode.Exclude, Rounded(padding), Rounded(inner));
                ctx.DrawGeometry(Brush(sh.Color), null, geom);

                continue;
            }

            var shadow = new BoxShadow
            {
                IsInset = true,
                OffsetX = sh.X,
                OffsetY = sh.Y,
                Blur = AvaloniaBlur(sh.Blur),
                Spread = sh.Spread,
                Color = Col(sh.Color),
            };
            ctx.DrawRectangle(null, null, padding, new BoxShadows(shadow));
        }
    }

    // The background color with every image layer over it, when they are all solid colors over the same area.
    private static CssColor? SolidLayers(ComputedStyle.BackgroundValues bg, CssArea colorClip)
    {
        var (r, g, b, a) = bg.Color.ToSrgb();
        for (var i = bg.Image.Count - 1; i >= 0; i--)
        {
            if (bg.Image[i] is null)
            {
                continue;
            }

            if (bg.Image[i] is not CssColorImage layer || (bg.Clip.Count > 0 ? bg.Clip.Cycle(i) : CssArea.BorderBox) != colorClip)
            {
                return null;
            }

            var (lr, lg, lb, la) = layer.Color.ToSrgb();
            var oa = la + (a * (1 - la));
            if (oa > 0)
            {
                (r, g, b) = (((lr * la) + (r * a * (1 - la))) / oa, ((lg * la) + (g * a * (1 - la))) / oa, ((lb * la) + (b * a * (1 - la))) / oa);
            }

            a = oa;
        }

        return CssColor.FromSrgb(r, g, b, a);
    }

    private static void PaintBackground(DrawingContext ctx, ComputedStyle s, CssBoxGeometry g)
    {
        var bg = s.Background;
        var clips = bg.Clip;
        var images = bg.Image;

        // The color is clipped by the last layer's clip.
        var colorClip = clips.Count > 0 ? clips.Cycle(Math.Max(0, images.Count - 1)) : CssArea.BorderBox;

        // GTK filters what the node drew as a whole, clamping once: solid layers over the color blend first.
        if (s_filter is not null && SolidLayers(bg, colorClip) is { } blended)
        {
            if (!blended.IsTransparent)
            {
                ctx.DrawRectangle(Brush(blended), null, g.ForArea(colorClip));
            }

            return;
        }

        if (!bg.Color.IsTransparent)
        {
            ctx.DrawRectangle(Brush(bg.Color), null, g.ForArea(colorClip));
        }

        for (var i = images.Count - 1; i >= 0; i--)
        {
            if (images[i] is not { } image)
            {
                continue;
            }

            var clip = g.ForArea(clips.Count > 0 ? clips.Cycle(i) : CssArea.BorderBox);
            var origin = bg.Origin.Count > 0 ? bg.Origin.Cycle(i) : CssArea.PaddingBox;
            var area = origin switch
            {
                CssArea.BorderBox => g.BorderBox,
                CssArea.ContentBox => g.ContentBox,
                _ => g.PaddingBox,
            };
            var bsize = bg.Size.Count > 0 ? bg.Size.Cycle(i) : CssBackgroundSize.Auto;
            var pos = bg.Position.Count > 0 ? bg.Position.Cycle(i) : new CssPosition(CssNumber.Pct(0), CssNumber.Pct(0));
            var repeat = bg.Repeat.Count > 0 ? bg.Repeat.Cycle(i) : CssBackgroundRepeat.Repeat;
            PaintImageLayer(ctx, image, clip, area, bsize, pos, repeat, s.Core.Color);
        }
    }

    private static void PaintImageLayer(DrawingContext ctx, CssImage image, RoundedRect clip, Rect area, CssBackgroundSize size, CssPosition pos, CssBackgroundRepeat repeat, CssColor current)
    {
        // Gradients and colors fill the area at its own size unless a size is given; that covers gtk.css's uses.
        var w = size.Width is { } sw ? sw.Resolve(area.Width) : area.Width;
        var h = size.Height is { } shh ? shh.Resolve(area.Height) : size.Width is not null && image is CssColorImage ? area.Height : area.Height;
        if (size.Width is not null && size.Height is null)
        {
            h = area.Height;
        }

        var x = area.X + pos.X.Value + (pos.X.Percent * (area.Width - w) / 100);
        var y = area.Y + pos.Y.Value + (pos.Y.Percent * (area.Height - h) / 100);
        var brush = ImageBrush(image, new Rect(0, 0, w, h));
        if (brush is null)
        {
            return;
        }

        using (ctx.PushGeometryClip(Rounded(clip)))
        {
            var repeatX = repeat.X is CssRepeatStyle.Repeat or CssRepeatStyle.Round or CssRepeatStyle.Space;
            var repeatY = repeat.Y is CssRepeatStyle.Repeat or CssRepeatStyle.Round or CssRepeatStyle.Space;
            if ((!repeatX || w >= area.Width) && (!repeatY || h >= area.Height))
            {
                using (ctx.PushTransform(Matrix.CreateTranslation(x, y)))
                {
                    ctx.DrawRectangle(brush, null, new Rect(0, 0, w, h));
                }

                return;
            }

            if (w <= 0 || h <= 0)
            {
                return;
            }

            var startX = repeatX ? x - (Math.Ceiling((x - area.X) / w) * w) : x;
            var startY = repeatY ? y - (Math.Ceiling((y - area.Y) / h) * h) : y;
            var endX = repeatX ? area.Right : x + w;
            var endY = repeatY ? area.Bottom : y + h;
            for (var ty = startY; ty < endY; ty += h)
            {
                for (var tx = startX; tx < endX; tx += w)
                {
                    using (ctx.PushTransform(Matrix.CreateTranslation(tx, ty)))
                    {
                        ctx.DrawRectangle(brush, null, new Rect(0, 0, w, h));
                    }
                }
            }
        }
    }

    /// <summary>A brush that paints the image over <paramref name="bounds"/>, or null when it cannot be drawn.</summary>
    public static IBrush? ImageBrush(CssImage image, Rect bounds)
    {
        switch (image)
        {
            case CssColorImage c:
                return Brush(c.Color);
            case CssLinearGradient lg:
                return LinearBrush(lg, bounds);
            case CssRadialGradient rg:
                return RadialBrush(rg, bounds);
            case CssCrossFadeImage cf:
                {
                    // Cross-fades of solid colors blend; other layers take the heaviest image.
                    CssColor? mixed = null;
                    double total = 0;
                    foreach (var (pct, img) in cf.Layers)
                    {
                        if (img is CssColorImage ci)
                        {
                            mixed = mixed is null ? ci.Color : CssColorMath.LegacyMix(mixed.Value, ci.Color, pct / Math.Max(1e-6, total + pct));
                            total += pct;
                        }
                    }

                    return mixed is { } m ? Brush(m) : null;
                }

            default:
                return null;
        }
    }

    private static IReadOnlyList<ImmutableGradientStop> Stops(CssList<CssColorStop> stops, double length)
    {
        // Positions in px over the gradient line; missing ones spread evenly, as CSS Images 3.
        var n = stops.Count;
        var offsets = new double?[n];
        for (var i = 0; i < n; i++)
        {
            if (stops[i].Offset is { } o)
            {
                offsets[i] = length > 0 ? o.Resolve(length) / length : o.Percent / 100;
            }
        }

        offsets[0] ??= 0;
        offsets[n - 1] ??= 1;
        double last = offsets[0]!.Value;
        for (var i = 1; i < n; i++)
        {
            if (offsets[i] is { } v)
            {
                offsets[i] = Math.Max(v, last);
                last = offsets[i]!.Value;
            }
        }

        for (var i = 1; i < n - 1; i++)
        {
            if (offsets[i] is null)
            {
                var j = i;
                while (offsets[j] is null)
                {
                    j++;
                }

                var a = offsets[i - 1]!.Value;
                var b = offsets[j]!.Value;
                for (var k = i; k < j; k++)
                {
                    offsets[k] = a + ((b - a) * (k - i + 1) / (j - i + 1));
                }
            }
        }

        var result = new ImmutableGradientStop[n];
        for (var i = 0; i < n; i++)
        {
            result[i] = new ImmutableGradientStop(offsets[i]!.Value, Col(stops[i].Color));
        }

        return result;
    }

    private static IBrush LinearBrush(CssLinearGradient lg, Rect b)
    {
        double angle;
        if (lg.Side != 0)
        {
            // "to" a side or corner: the corner angle depends on the box, as gtkcssimagelinear.c.
            var x = (lg.Side & CssLinearGradient.Left) != 0 ? -1 : (lg.Side & CssLinearGradient.Right) != 0 ? 1 : 0;
            var y = (lg.Side & CssLinearGradient.Top) != 0 ? -1 : (lg.Side & CssLinearGradient.Bottom) != 0 ? 1 : 0;
            if (x != 0 && y != 0)
            {
                angle = (Math.Atan2(y * b.Width, x * b.Height) * 180 / Math.PI) + 90;
            }
            else
            {
                angle = x == 1 ? 90 : x == -1 ? 270 : y == -1 ? 0 : 180;
            }
        }
        else
        {
            angle = lg.Angle;
        }

        var rad = angle * Math.PI / 180;
        var dx = Math.Sin(rad);
        var dy = -Math.Cos(rad);
        var len = Math.Abs(b.Width * dx) + Math.Abs(b.Height * dy);
        var cx = b.X + (b.Width / 2);
        var cy = b.Y + (b.Height / 2);
        var start = new RelativePoint(cx - (dx * len / 2), cy - (dy * len / 2), RelativeUnit.Absolute);
        var end = new RelativePoint(cx + (dx * len / 2), cy + (dy * len / 2), RelativeUnit.Absolute);
        return new ImmutableLinearGradientBrush(Stops(lg.Stops, len), 1, null, null, lg.Repeating ? GradientSpreadMethod.Repeat : GradientSpreadMethod.Pad, start, end);
    }

    private static IBrush RadialBrush(CssRadialGradient rg, Rect b)
    {
        var cx = b.X + rg.Center.X.Resolve(b.Width);
        var cy = b.Y + rg.Center.Y.Resolve(b.Height);
        double rx, ry;
        if (rg.Size == CssRadialSize.Explicit && rg.RadiusX is { } ex)
        {
            rx = ex.Resolve(b.Width);
            ry = rg.RadiusY is { } ey ? ey.Resolve(b.Height) : rx;
        }
        else
        {
            var left = cx - b.X;
            var right = b.Right - cx;
            var top = cy - b.Y;
            var bottom = b.Bottom - cy;
            (rx, ry) = rg.Size switch
            {
                CssRadialSize.ClosestSide => (Math.Min(left, right), Math.Min(top, bottom)),
                CssRadialSize.FarthestSide => (Math.Max(left, right), Math.Max(top, bottom)),
                CssRadialSize.ClosestCorner => (Math.Min(left, right) * Math.Sqrt(2), Math.Min(top, bottom) * Math.Sqrt(2)),
                _ => (Math.Max(left, right) * Math.Sqrt(2), Math.Max(top, bottom) * Math.Sqrt(2)),
            };
            if (rg.Circle)
            {
                rx = ry = rg.Size is CssRadialSize.ClosestSide or CssRadialSize.FarthestSide ? Math.Min(rx, ry) : Math.Max(rx, ry);
            }
        }

        var center = new RelativePoint(cx, cy, RelativeUnit.Absolute);
        return new ImmutableRadialGradientBrush(Stops(rg.Stops, rx), 1, null, null, rg.Repeating ? GradientSpreadMethod.Repeat : GradientSpreadMethod.Pad,
            center, center, new RelativeScalar(rx, RelativeUnit.Absolute), new RelativeScalar(ry, RelativeUnit.Absolute));
    }

    private static Geometry Rounded(RoundedRect r)
    {
        var g = new StreamGeometry();
        using (var c = g.Open())
        {
            AddRounded(c, r);
        }

        return g;
    }

    private static void AddRounded(StreamGeometryContext c, RoundedRect r)
    {
        var rect = r.Rect;
        var tl = r.RadiiTopLeft;
        var tr = r.RadiiTopRight;
        var br = r.RadiiBottomRight;
        var bl = r.RadiiBottomLeft;
        c.BeginFigure(new Point(rect.Left + tl.X, rect.Top), true);
        c.LineTo(new Point(rect.Right - tr.X, rect.Top));
        if (tr.X > 0 && tr.Y > 0)
        {
            c.ArcTo(new Point(rect.Right, rect.Top + tr.Y), new Size(tr.X, tr.Y), 0, false, SweepDirection.Clockwise);
        }

        c.LineTo(new Point(rect.Right, rect.Bottom - br.Y));
        if (br.X > 0 && br.Y > 0)
        {
            c.ArcTo(new Point(rect.Right - br.X, rect.Bottom), new Size(br.X, br.Y), 0, false, SweepDirection.Clockwise);
        }

        c.LineTo(new Point(rect.Left + bl.X, rect.Bottom));
        if (bl.X > 0 && bl.Y > 0)
        {
            c.ArcTo(new Point(rect.Left, rect.Bottom - bl.Y), new Size(bl.X, bl.Y), 0, false, SweepDirection.Clockwise);
        }

        c.LineTo(new Point(rect.Left, rect.Top + tl.Y));
        if (tl.X > 0 && tl.Y > 0)
        {
            c.ArcTo(new Point(rect.Left + tl.X, rect.Top), new Size(tl.X, tl.Y), 0, false, SweepDirection.Clockwise);
        }

        c.EndFigure(true);
    }

    /// <summary>A ring between two rounded rects, filled even-odd.</summary>
    public static Geometry Ring(RoundedRect outer, RoundedRect inner)
    {
        var g = new StreamGeometry();
        using (var c = g.Open())
        {
            c.SetFillRule(FillRule.EvenOdd);
            AddRounded(c, outer);
            AddRounded(c, inner);
        }

        return g;
    }

    private static void PaintBorder(DrawingContext ctx, ComputedStyle s, CssBoxGeometry g)
    {
        var b = s.Border;
        var w = g.Border;
        if (w.Left <= 0 && w.Top <= 0 && w.Right <= 0 && w.Bottom <= 0)
        {
            return;
        }

        var outer = g.Outer;
        var inner = g.PaddingRounded;
        var uniform = b.TopColor == b.RightColor && b.TopColor == b.BottomColor && b.TopColor == b.LeftColor;
        if (uniform)
        {
            if (!b.TopColor.IsTransparent)
            {
                ctx.DrawGeometry(Brush(b.TopColor), null, Ring(outer, inner));
            }

            return;
        }

        // Different colors: four trapezoids split at the corner diagonals, each clipped to the ring.
        var ring = Ring(outer, inner);
        var r = g.BorderBox;
        var ir = g.PaddingBox;
        void Side(CssColor color, double width, Point a, Point bpt, Point c, Point d)
        {
            if (width <= 0 || color.IsTransparent)
            {
                return;
            }

            var poly = new StreamGeometry();
            using (var pc = poly.Open())
            {
                pc.BeginFigure(a, true);
                pc.LineTo(bpt);
                pc.LineTo(c);
                pc.LineTo(d);
                pc.EndFigure(true);
            }

            using (ctx.PushGeometryClip(ring))
            {
                ctx.DrawGeometry(Brush(color), null, poly);
            }
        }

        Side(b.TopColor, w.Top, r.TopLeft, r.TopRight, ir.TopRight, ir.TopLeft);
        Side(b.RightColor, w.Right, r.TopRight, r.BottomRight, ir.BottomRight, ir.TopRight);
        Side(b.BottomColor, w.Bottom, r.BottomRight, r.BottomLeft, ir.BottomLeft, ir.BottomRight);
        Side(b.LeftColor, w.Left, r.BottomLeft, r.TopLeft, ir.TopLeft, ir.BottomLeft);
    }

    private static void PaintOutline(DrawingContext ctx, ComputedStyle s, CssBoxGeometry g)
    {
        var o = s.Outline;
        var width = CssBoxGeometry.RoundLength(o.Width);
        if (width <= 0 || o.Color.IsTransparent)
        {
            return;
        }

        // The outline sits outside the border box by its offset, which Adwaita often makes negative.
        var offset = o.Offset;
        var inner = CssBoxGeometry.Shrink(g.Outer, -offset, -offset, -offset, -offset);
        var outer = CssBoxGeometry.Shrink(inner, -width, -width, -width, -width);
        ctx.DrawGeometry(Brush(o.Color), null, Ring(outer, inner));
    }
}
