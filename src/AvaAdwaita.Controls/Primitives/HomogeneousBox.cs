using System.Linq;
using Avalonia;
using Avalonia.Controls;
using AvaAdwaita.Nodes;

namespace AvaAdwaita.Controls.Primitives;

/// <summary>
/// A horizontal homogeneous GtkBoxLayout: each visible child as wide as the widest, spaced by the nearest node's
/// <c>border-spacing</c>.
/// </summary>
public class HomogeneousBox : Panel
{
    static HomogeneousBox()
    {
        Css.StyleProperty.Changed.AddClassHandler<HomogeneousBox>((b, _) => b.InvalidateMeasure());
    }

    private double Spacing => Css.GetStyle(this)?.Size.BorderSpacing.X.Value ?? 0;

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        var visible = Children.Where(c => c.IsVisible).ToList();
        double width = 0, height = 0;
        foreach (var c in visible)
        {
            c.Measure(Size.Infinity);
            width = System.Math.Max(width, c.DesiredSize.Width);
            height = System.Math.Max(height, c.DesiredSize.Height);
        }

        return new Size((width * visible.Count) + (Spacing * System.Math.Max(0, visible.Count - 1)), height);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        var visible = Children.Where(c => c.IsVisible).ToList();
        if (visible.Count == 0)
        {
            return finalSize;
        }

        var width = (finalSize.Width - (Spacing * (visible.Count - 1))) / visible.Count;
        var x = 0.0;
        foreach (var c in visible)
        {
            c.Arrange(new Rect(x, 0, width, finalSize.Height));
            x += width + Spacing;
        }

        return finalSize;
    }
}
