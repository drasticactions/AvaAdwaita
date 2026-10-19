using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using AvaAdwaita.Animation;

namespace AvaAdwaita.Controls;

/// <summary>
/// Keeps its child at most <see cref="MaximumSize"/> wide, centered (AdwClamp). Between
/// <see cref="TighteningThreshold"/> and the maximum the child grows more slowly than the clamp, so the margins
/// around it appear gradually. The child has the <c>small</c>, <c>medium</c> or <c>large</c> class for where it is
/// in that range. Sizes are in pixels, as libadwaita's scale-independent pixels with no text scaling.
/// </summary>
public class Clamp : Decorator
{
    /// <summary>libadwaita's ADW_EASE_OUT_TAN_CUBIC: the slope of an ease-out cubic at its start.</summary>
    private const double EaseOutTanCubic = 3;

    /// <summary>Defines <see cref="MaximumSize"/>.</summary>
    public static readonly StyledProperty<double> MaximumSizeProperty =
        AvaloniaProperty.Register<Clamp, double>(nameof(MaximumSize), 600);

    /// <summary>Defines <see cref="TighteningThreshold"/>.</summary>
    public static readonly StyledProperty<double> TighteningThresholdProperty =
        AvaloniaProperty.Register<Clamp, double>(nameof(TighteningThreshold), 400);

    /// <summary>Defines <see cref="Orientation"/>.</summary>
    public static readonly StyledProperty<Orientation> OrientationProperty =
        AvaloniaProperty.Register<Clamp, Orientation>(nameof(Orientation), Orientation.Horizontal);

    static Clamp()
    {
        AffectsMeasure<Clamp>(MaximumSizeProperty, TighteningThresholdProperty, OrientationProperty);
    }

    /// <summary>Creates a clamp. It is a <c>clamp</c> node, as AdwClamp.</summary>
    public Clamp()
    {
        SetValue(Nodes.Css.NodeProperty, "clamp", Avalonia.Data.BindingPriority.Style);
    }

    /// <summary>The largest size the child gets.</summary>
    public double MaximumSize
    {
        get => GetValue(MaximumSizeProperty);
        set => SetValue(MaximumSizeProperty, value);
    }

    /// <summary>The size up to which the child gets all of the clamp's size.</summary>
    public double TighteningThreshold
    {
        get => GetValue(TighteningThresholdProperty);
        set => SetValue(TighteningThresholdProperty, value);
    }

    /// <summary>The direction that is clamped.</summary>
    public Orientation Orientation
    {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    // Avalonia measures no minimum size, so a child's minimum counts as none.
    private (double Lower, double Max, double Upper) Range()
    {
        var lower = Math.Min(TighteningThreshold, MaximumSize);
        var max = Math.Max(lower, MaximumSize);
        return (lower, max, lower + (EaseOutTanCubic * (max - lower)));
    }

    /// <summary>The child's size for the clamp's size (child_size_from_clamp).</summary>
    internal double ChildSize(double size, double natural)
    {
        var (lower, max, upper) = Range();
        if (double.IsInfinity(size))
        {
            return Math.Min(natural, Math.Ceiling(max));
        }

        if (size <= lower)
        {
            return size;
        }

        if (size >= upper)
        {
            return Math.Ceiling(max);
        }

        var progress = (size - lower) / (upper - lower);
        return Math.Floor(lower + ((max - lower) * Easing.Ease(AdwEasing.EaseOutCubic, progress)));
    }

    /// <summary>The clamp's size for the child's (clamp_size_from_child).</summary>
    internal double ClampSize(double childSize)
    {
        var (lower, max, upper) = Range();
        if (childSize <= lower)
        {
            return childSize;
        }

        if (childSize >= max)
        {
            return upper;
        }

        var progress = 1 + Math.Cbrt(((childSize - lower) / (max - lower)) - 1);
        return Math.Ceiling(lower + ((upper - lower) * progress));
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        if (Child is not { } child)
        {
            return default;
        }

        var horizontal = Orientation == Orientation.Horizontal;
        child.Measure(horizontal ? new Size(double.PositiveInfinity, availableSize.Height) : new Size(availableSize.Width, double.PositiveInfinity));
        var natural = horizontal ? child.DesiredSize.Width : child.DesiredSize.Height;
        var along = horizontal ? availableSize.Width : availableSize.Height;
        var size = ChildSize(along, natural);
        child.Measure(horizontal ? new Size(size, availableSize.Height) : new Size(availableSize.Width, size));

        // The clamp's natural size comes from the child's natural size (clamp_size_from_child), not from the size it
        // was measured at.
        var clamped = Math.Min(along, ClampSize(natural));
        return horizontal ? new Size(clamped, child.DesiredSize.Height) : new Size(child.DesiredSize.Width, clamped);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Child is not { } child)
        {
            return finalSize;
        }

        var horizontal = Orientation == Orientation.Horizontal;
        var (lower, max, _) = Range();
        var size = ChildSize(horizontal ? finalSize.Width : finalSize.Height, double.PositiveInfinity);
        child.Classes.Set("large", size >= Math.Ceiling(max));
        child.Classes.Set("small", size <= Math.Ceiling(lower) && size < Math.Ceiling(max));
        child.Classes.Set("medium", size > Math.Ceiling(lower) && size < Math.Ceiling(max));

        // The child is centered across the clamped direction.
        child.Arrange(horizontal
            ? new Rect(Math.Floor((finalSize.Width - size) / 2), 0, size, finalSize.Height)
            : new Rect(0, Math.Floor((finalSize.Height - size) / 2), finalSize.Width, size));
        return finalSize;
    }
}
