using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace AvaAdwaita.Controls.Primitives;

/// <summary>
/// GtkOverlay's layout: the first child fills it and sizes it; the others go over it at their natural sizes, placed by
/// their alignment, and size it too (measure-overlay).
/// </summary>
public class OverlayPanel : Panel
{
    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        var size = default(Size);
        foreach (var c in Children)
        {
            c.Measure(availableSize);
            size = new Size(Math.Max(size.Width, c.DesiredSize.Width), Math.Max(size.Height, c.DesiredSize.Height));
        }

        return size;
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        for (var i = 0; i < Children.Count; i++)
        {
            var c = Children[i];
            if (i == 0)
            {
                c.Arrange(new Rect(finalSize));
                continue;
            }

            // gtk_overlay_compute_child_allocation: a filling child takes the whole axis.
            var w = c.HorizontalAlignment == HorizontalAlignment.Stretch ? finalSize.Width : Math.Min(c.DesiredSize.Width, finalSize.Width);
            var h = c.VerticalAlignment == VerticalAlignment.Stretch ? finalSize.Height : Math.Min(c.DesiredSize.Height, finalSize.Height);
            // gtk_overlay_get_child_position: each half rounded down on its own.
            var x = c.HorizontalAlignment switch
            {
                HorizontalAlignment.Right => finalSize.Width - w,
                HorizontalAlignment.Center => Math.Floor(finalSize.Width / 2) - Math.Floor(w / 2),
                _ => 0,
            };
            var y = c.VerticalAlignment switch
            {
                VerticalAlignment.Bottom => finalSize.Height - h,
                VerticalAlignment.Center => Math.Floor(finalSize.Height / 2) - Math.Floor(h / 2),
                _ => 0,
            };
            c.Arrange(new Rect(x, y, w, h));
        }

        return finalSize;
    }
}
