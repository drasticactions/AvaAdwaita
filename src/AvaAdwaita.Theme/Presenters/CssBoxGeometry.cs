using System;
using Avalonia;
using AvaAdwaita.CssEngine.Computed;
using AvaAdwaita.CssEngine.Values;

namespace AvaAdwaita.Presenters;

/// <summary>The CSS boxes of one node in a given size: margin, border, padding and content, with rounded corners.</summary>
/// <remarks>Follows gtkcssboxesimpl.h and gtk_rounded_rect_init_from_css (gskroundedrect.c).</remarks>
internal readonly struct CssBoxGeometry
{
    public CssBoxGeometry(ComputedStyle s, Size size)
    {
        Margin = Round(s.Margin);
        Border = Round(s.BorderWidths);
        Padding = Round(s.Padding);
        BorderBox = new Rect(Margin.Left, Margin.Top, Math.Max(0, size.Width - Margin.Horizontal), Math.Max(0, size.Height - Margin.Vertical));
        PaddingBox = Deflate(BorderBox, Border);
        ContentBox = Deflate(PaddingBox, Padding);
        var w = BorderBox.Width;
        var h = BorderBox.Height;
        var r = s.Radii;
        Outer = Normalize(BorderBox, r.TopLeft.Resolve(w, h), r.TopRight.Resolve(w, h), r.BottomRight.Resolve(w, h), r.BottomLeft.Resolve(w, h));
    }

    public CssBoxSides Margin { get; }

    public CssBoxSides Border { get; }

    public CssBoxSides Padding { get; }

    public Rect BorderBox { get; }

    public Rect PaddingBox { get; }

    public Rect ContentBox { get; }

    /// <summary>The rounded border box.</summary>
    public RoundedRect Outer { get; }

    /// <summary>The rounded padding box: the border box shrunk by the border widths.</summary>
    public RoundedRect PaddingRounded => Shrink(Outer, Border.Top, Border.Right, Border.Bottom, Border.Left);

    /// <summary>The rounded content box.</summary>
    public RoundedRect ContentRounded => Shrink(PaddingRounded, Padding.Top, Padding.Right, Padding.Bottom, Padding.Left);

    public RoundedRect ForArea(CssArea area) => area switch
    {
        CssArea.PaddingBox => PaddingRounded,
        CssArea.ContentBox => ContentRounded,
        _ => Outer,
    };

    // gtksizerequest.c get_number: values below 1 round up, others down.
    internal static double RoundLength(double d) => d < 1 ? Math.Ceiling(d) : Math.Floor(d);

    private static CssBoxSides Round(CssBoxSides s) => new(RoundLength(s.Left), RoundLength(s.Top), RoundLength(s.Right), RoundLength(s.Bottom));

    private static Rect Deflate(Rect r, CssBoxSides s) =>
        new(r.X + s.Left, r.Y + s.Top, Math.Max(0, r.Width - s.Horizontal), Math.Max(0, r.Height - s.Vertical));

    /// <summary>Scales radii down so adjacent corners fit their side, as gsk_rounded_rect_normalize.</summary>
    public static RoundedRect Normalize(Rect rect, (double X, double Y) tl, (double X, double Y) tr, (double X, double Y) br, (double X, double Y) bl)
    {
        var factor = 1.0;
        void Fit(double side, double a, double b)
        {
            if (a + b > side && a + b > 0)
            {
                factor = Math.Min(factor, side / (a + b));
            }
        }

        Fit(rect.Width, tl.X, tr.X);
        Fit(rect.Width, bl.X, br.X);
        Fit(rect.Height, tl.Y, bl.Y);
        Fit(rect.Height, tr.Y, br.Y);
        return new RoundedRect(rect,
            new Vector(tl.X * factor, tl.Y * factor), new Vector(tr.X * factor, tr.Y * factor),
            new Vector(br.X * factor, br.Y * factor), new Vector(bl.X * factor, bl.Y * factor));
    }

    /// <summary>Shrinks a rounded rect by per-side amounts; negative grows it. As gsk_rounded_rect_shrink.</summary>
    public static RoundedRect Shrink(RoundedRect r, double top, double right, double bottom, double left)
    {
        var rect = r.Rect;
        var x = rect.X + left;
        var y = rect.Y + top;
        var w = Math.Max(0, rect.Width - left - right);
        var h = Math.Max(0, rect.Height - top - bottom);
        static Vector Corner(Vector v, double dx, double dy) =>
            v.X <= 0 || v.Y <= 0 ? new Vector(0, 0) : new Vector(Math.Max(0, v.X - dx), Math.Max(0, v.Y - dy));

        return new RoundedRect(new Rect(x, y, w, h),
            Corner(r.RadiiTopLeft, left, top), Corner(r.RadiiTopRight, right, top),
            Corner(r.RadiiBottomRight, right, bottom), Corner(r.RadiiBottomLeft, left, bottom));
    }
}
