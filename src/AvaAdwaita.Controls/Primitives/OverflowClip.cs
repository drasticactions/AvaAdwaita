using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using AvaAdwaita.Nodes;
using AvaAdwaita.Presenters;

namespace AvaAdwaita.Controls.Primitives;

/// <summary>
/// GTK's <c>overflow: hidden</c> on a node: its children are clipped to its padding box, rounded corners included,
/// while the node's own shadow still draws outside it.
/// </summary>
internal static class OverflowClip
{
    /// <summary>Clips <paramref name="child"/>, arranged inside <paramref name="node"/>, to the node's rounded padding box.</summary>
    public static void Apply(Control node, Control child)
    {
        if (Css.GetStyle(node) is not { } style)
        {
            child.Clip = null;
            return;
        }

        var r = new CssBoxGeometry(style, node.Bounds.Size).PaddingRounded;
        var offset = child.TranslatePoint(default, node) ?? default;
        var g = new StreamGeometry();
        using (var c = g.Open())
        {
            var rect = r.Rect.Translate(-(Vector)offset);
            var (tl, tr, br, bl) = (r.RadiiTopLeft, r.RadiiTopRight, r.RadiiBottomRight, r.RadiiBottomLeft);
            c.BeginFigure(new Point(rect.Left + tl.X, rect.Top), true);
            c.LineTo(new Point(rect.Right - tr.X, rect.Top));
            c.ArcTo(new Point(rect.Right, rect.Top + tr.Y), new Size(tr.X, tr.Y), 0, false, SweepDirection.Clockwise);
            c.LineTo(new Point(rect.Right, rect.Bottom - br.Y));
            c.ArcTo(new Point(rect.Right - br.X, rect.Bottom), new Size(br.X, br.Y), 0, false, SweepDirection.Clockwise);
            c.LineTo(new Point(rect.Left + bl.X, rect.Bottom));
            c.ArcTo(new Point(rect.Left, rect.Bottom - bl.Y), new Size(bl.X, bl.Y), 0, false, SweepDirection.Clockwise);
            c.LineTo(new Point(rect.Left, rect.Top + tl.Y));
            c.ArcTo(new Point(rect.Left + tl.X, rect.Top), new Size(tl.X, tl.Y), 0, false, SweepDirection.Clockwise);
            c.EndFigure(true);
        }

        child.Clip = g;
    }
}
