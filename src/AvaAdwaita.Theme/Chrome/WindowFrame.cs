using Avalonia;
using Avalonia.Media;
using AvaAdwaita.Presenters;

namespace AvaAdwaita.Chrome;

/// <summary>
/// The window's own box: its background, rounded corners, shadow and inner outline. The content is
/// clipped to the rounded border box, which Avalonia does not do for a window by itself.
/// </summary>
public class WindowFrame : AdwBox
{
    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        var size = base.ArrangeOverride(finalSize);
        if (Child is { } child && CurrentStyle is { } s)
        {
            var g = new CssBoxGeometry(s, finalSize);
            var r = g.PaddingRounded;
            var local = new RoundedRect(r.Rect.Translate(-child.Bounds.Position), r.RadiiTopLeft, r.RadiiTopRight, r.RadiiBottomRight, r.RadiiBottomLeft);
            child.Clip = local.IsRounded ? new RectangleGeometry(local.Rect, r.RadiiTopLeft.X, r.RadiiTopLeft.Y) : null;
        }

        return size;
    }
}
