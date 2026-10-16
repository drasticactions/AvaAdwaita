using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using AvaAdwaita.CssEngine.Computed;
using AvaAdwaita.Markup;

namespace AvaAdwaita.Chrome;

/// <summary>
/// A popover's <c>arrow</c> node: the tail between the contents and the anchor, filled with the node's
/// background and stroked with its border, as GtkPopover draws it.
/// </summary>
/// <remarks>Follows gtk_popover_get_gap_coords and create_arrow_render_node (gtkpopover.c).</remarks>
public class PopoverArrow : Control
{
    /// <summary>The tail's height, GtkPopover's TAIL_HEIGHT.</summary>
    public const double TailHeight = 12;

    /// <summary>The tail's width at its base, GtkPopover's TAIL_GAP_WIDTH.</summary>
    public const double TailGapWidth = 24;

    private PopoverSide _side;
    private double _tip;
    private Thickness _shadow;
    private double _radius;
    private Thickness _contentsBorder;

    static PopoverArrow()
    {
        Nodes.Css.StyleProperty.Changed.AddClassHandler<PopoverArrow>((a, _) => a.InvalidateVisual());
    }

    /// <summary>Creates the arrow node.</summary>
    public PopoverArrow()
    {
        Nodes.Css.SetNode(this, "arrow");
        IsHitTestVisible = false;
        ClipToBounds = false;
    }

    /// <summary>Adds <paramref name="amount"/> to the side of <paramref name="t"/> that faces the anchor.</summary>
    internal static Thickness WithTail(Thickness t, PopoverSide side, double amount) => side switch
    {
        PopoverSide.Top => new Thickness(t.Left, t.Top + amount, t.Right, t.Bottom),
        PopoverSide.Bottom => new Thickness(t.Left, t.Top, t.Right, t.Bottom + amount),
        PopoverSide.Left => new Thickness(t.Left + amount, t.Top, t.Right, t.Bottom),
        PopoverSide.Right => new Thickness(t.Left, t.Top, t.Right + amount, t.Bottom),
        _ => t,
    };

    /// <summary>Places the tail: the side facing the anchor, the anchor's center along that side, and the contents' geometry.</summary>
    internal void Update(PopoverSide side, double tip, Thickness shadow, double radius, Thickness contentsBorder)
    {
        if (side == _side && tip == _tip && shadow == _shadow && radius == _radius && contentsBorder == _contentsBorder)
        {
            return;
        }

        _side = side;
        _tip = tip;
        _shadow = shadow;
        _radius = radius;
        _contentsBorder = contentsBorder;
        InvalidateVisual();
    }

    /// <summary>The tail's three points: initial, tip and final.</summary>
    internal (Point Initial, Point Tip, Point Final)? Gap()
    {
        var w = Bounds.Width;
        var h = Bounds.Height;
        var radius = Math.Round(_radius);
        double tip, baseLine;
        switch (_side)
        {
            case PopoverSide.Top:
                tip = _shadow.Top;
                baseLine = tip + TailHeight + _contentsBorder.Top;
                break;
            case PopoverSide.Left:
                tip = _shadow.Left;
                baseLine = tip + TailHeight + _contentsBorder.Top;
                break;
            case PopoverSide.Bottom:
                tip = h - _shadow.Bottom;
                baseLine = tip - _contentsBorder.Bottom - TailHeight;
                break;
            case PopoverSide.Right:
                tip = w - _shadow.Right;
                baseLine = tip - _contentsBorder.Right - TailHeight;
                break;
            default:
                return null;
        }

        // GTK works in whole pixels here. It keeps the base out of the corners measured from the surface's edges,
        // shadow included, so a tip near a corner puts the base over the curve; the base is kept on the contents
        // here, and the tail leans toward the tip instead, which is the same wherever GTK's base fits.
        var pos = Math.Floor(_tip);
        if (_side is PopoverSide.Top or PopoverSide.Bottom)
        {
            double start = _shadow.Left + radius, end = w - _shadow.Right - radius;
            return (
                new Point(Math.Clamp(pos - (TailGapWidth / 2), start, Math.Max(start, end - TailGapWidth)), baseLine),
                new Point(Math.Clamp(pos, 0, w), tip),
                new Point(Math.Clamp(pos + (TailGapWidth / 2), start + TailGapWidth, Math.Max(start + TailGapWidth, end)), baseLine));
        }

        double top = _shadow.Top + radius, bottom = h - _shadow.Bottom - radius;
        return (
            new Point(baseLine, Math.Clamp(pos - (TailGapWidth / 2), top, Math.Max(top, bottom - TailGapWidth))),
            new Point(tip, Math.Clamp(pos, 0, h)),
            new Point(baseLine, Math.Clamp(pos + (TailGapWidth / 2), top + TailGapWidth, Math.Max(top + TailGapWidth, bottom))));
    }

    /// <inheritdoc/>
    public override void Render(DrawingContext context)
    {
        if (GetValue(Nodes.Css.StyleProperty) is not ComputedStyle s || Gap() is not { } gap)
        {
            return;
        }

        var path = new StreamGeometry();
        using (var c = path.Open())
        {
            c.BeginFigure(gap.Initial, true);
            c.LineTo(gap.Tip);
            c.LineTo(gap.Final);
            c.EndFigure(true);
        }

        if (!s.Background.Color.IsTransparent)
        {
            context.DrawGeometry(CssConvert.Brush(s.Drawn(s.Background.Color)), null, path);
        }

        // The tip's border: two lines, clipped to the tail, GTK strokes them border-bottom + 1 wide.
        var width = s.Border.BottomWidth;
        if (width > 0 && !s.Border.LeftColor.IsTransparent)
        {
            var lines = new StreamGeometry();
            using (var c = lines.Open())
            {
                c.BeginFigure(gap.Initial, false);
                c.LineTo(gap.Tip);
                c.LineTo(gap.Final);
                c.EndFigure(false);
            }

            using (context.PushGeometryClip(path))
            {
                context.DrawGeometry(null, new Pen(CssConvert.Brush(s.Drawn(s.Border.LeftColor)), width + 1), lines);
            }
        }
    }
}
