using System;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;

namespace AvaAdwaita.Presenters;

/// <summary>
/// A slider track laid out as GtkRange lays out its trough: the slider at its own size, centered across the
/// trough and placed along it in whole pixels, and the highlight from the start to the slider's center.
/// </summary>
/// <remarks>Follows gtk_range_compute_slider_position and gtk_range_allocate_trough (gtkrange.c).</remarks>
public class AdwRangeTrack : Track
{
    /// <inheritdoc/>
    protected override Type StyleKeyOverride => typeof(Track);

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        // The trough is as large as the slider's margin box or the highlight, whichever is larger.
        var size = base.MeasureOverride(availableSize);
        if (Thumb is { } t)
        {
            size = Orientation == Orientation.Horizontal ? size.WithHeight(t.DesiredSize.Height) : size.WithWidth(t.DesiredSize.Width);
        }

        return size;
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        base.ArrangeOverride(finalSize);
        if (Thumb is not { } thumb)
        {
            return finalSize;
        }

        var range = Maximum - Minimum;
        var fraction = range > 0 ? Math.Clamp((Value - Minimum) / range, 0, 1) : 0;
        var sw = thumb.DesiredSize.Width;
        var sh = thumb.DesiredSize.Height;
        var w = finalSize.Width;
        var h = finalSize.Height;
        if (Orientation == Orientation.Horizontal)
        {
            var x = Math.Clamp((double)(int)((w - sw) * fraction), 0, w);
            var reversed = IsDirectionReversed;
            if (reversed)
            {
                x = w - x - sw;
            }

            thumb.Arrange(new Rect(x, Math.Floor((h - sh) / 2), sw, sh));
            var hw = Math.Max(0, (int)(fraction * w));
            DecreaseButton?.Arrange(new Rect(reversed ? w - hw : 0, 0, hw, h));
            IncreaseButton?.Arrange(reversed ? new Rect(0, 0, w - hw, h) : new Rect(hw, 0, w - hw, h));
        }
        else
        {
            var y = Math.Clamp((double)(int)((h - sh) * fraction), 0, h);
            var reversed = !IsDirectionReversed;
            if (reversed)
            {
                y = h - y - sh;
            }

            thumb.Arrange(new Rect(Math.Floor((w - sw) / 2), y, sw, sh));
            var hh = Math.Max(0, (int)(fraction * h));
            DecreaseButton?.Arrange(new Rect(0, reversed ? h - hh : 0, w, hh));
            IncreaseButton?.Arrange(reversed ? new Rect(0, 0, w, h - hh) : new Rect(0, hh, w, h - hh));
        }

        return finalSize;
    }
}
