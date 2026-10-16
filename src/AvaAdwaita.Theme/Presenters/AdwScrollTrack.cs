using System;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;

namespace AvaAdwaita.Presenters;

/// <summary>
/// A scroll bar's track that places its slider as GtkRange does: the slider's length is the trough's times the
/// page's share of the content, in whole pixels and at least the slider's CSS size, and its offset is truncated
/// to a pixel too. Avalonia's own arithmetic still drives dragging, with the same minimum length.
/// </summary>
/// <remarks>Follows gtk_range_compute_slider_position (gtkrange.c).</remarks>
public class AdwScrollTrack : Track
{
    /// <inheritdoc/>
    protected override Type StyleKeyOverride => typeof(Track);

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size arrangeSize)
    {
        var vertical = Orientation == Orientation.Vertical;
        if (Thumb is not { } thumb)
        {
            return base.ArrangeOverride(arrangeSize);
        }

        // Avalonia's track uses the thumb's minimum length only when it is set.
        var min = vertical ? thumb.DesiredSize.Height : thumb.DesiredSize.Width;
        thumb.SetCurrentValue(vertical ? MinHeightProperty : MinWidthProperty, min);
        var size = base.ArrangeOverride(arrangeSize);

        var trough = (int)(vertical ? arrangeSize.Height : arrangeSize.Width);
        var range = Maximum - Minimum;
        var extent = range + ViewportSize;
        var length = extent != 0 ? (int)(trough * (ViewportSize / extent)) : (int)min;
        length = Math.Min(Math.Max(length, (int)Math.Ceiling(min)), trough);
        var offset = range != 0 ? (int)((trough - length) * ((Value - Minimum) / range)) : 0;
        offset = Math.Clamp(offset, 0, trough);
        if (vertical != IsDirectionReversed)
        {
            offset = trough - offset - length;
        }

        // The page areas fill the trough on either side of the slider.
        var first = vertical != IsDirectionReversed ? IncreaseButton : DecreaseButton;
        var last = vertical != IsDirectionReversed ? DecreaseButton : IncreaseButton;
        if (vertical)
        {
            thumb.Arrange(new Rect(0, offset, arrangeSize.Width, length));
            first?.Arrange(new Rect(0, 0, arrangeSize.Width, offset));
            last?.Arrange(new Rect(0, offset + length, arrangeSize.Width, Math.Max(0, arrangeSize.Height - offset - length)));
        }
        else
        {
            thumb.Arrange(new Rect(offset, 0, length, arrangeSize.Height));
            first?.Arrange(new Rect(0, 0, offset, arrangeSize.Height));
            last?.Arrange(new Rect(offset + length, 0, Math.Max(0, arrangeSize.Width - offset - length), arrangeSize.Height));
        }

        return size;
    }
}
