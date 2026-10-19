using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;

namespace AvaAdwaita.Controls.Primitives;

/// <summary>
/// GtkCenterLayout: a start, center and end child (by <see cref="Grid.ColumnProperty"/> 0, 1 and 2), the center in the
/// middle unless a side pushes it. Its natural width is the center's and twice the wider side's.
/// </summary>
public class CenterGrid : Panel
{
    /// <summary>Defines <see cref="IsStrict"/>.</summary>
    public static readonly StyledProperty<bool> IsStrictProperty =
        AvaloniaProperty.Register<CenterGrid, bool>(nameof(IsStrict));

    /// <summary>Defines <see cref="ExpandsCenter"/>.</summary>
    public static readonly StyledProperty<bool> ExpandsCenterProperty =
        AvaloniaProperty.Register<CenterGrid, bool>(nameof(ExpandsCenter));

    static CenterGrid()
    {
        AffectsMeasure<CenterGrid>(IsStrictProperty, ExpandsCenterProperty);
    }

    /// <summary>Makes the sides equally wide, as AdwHeaderBar's strict centering puts them in a size group.</summary>
    public bool IsStrict
    {
        get => GetValue(IsStrictProperty);
        set => SetValue(IsStrictProperty, value);
    }

    /// <summary>Lets the center take the space the sides leave, as a hexpand center widget does.</summary>
    public bool ExpandsCenter
    {
        get => GetValue(ExpandsCenterProperty);
        set => SetValue(ExpandsCenterProperty, value);
    }

    private Control? Child(int column) => Children.FirstOrDefault(c => Grid.GetColumn(c) == column);

    private double Natural(Control? c) => c is { IsVisible: true } ? c.DesiredSize.Width : 0;

    // The sides' natural widths, the larger of the two for both when strict.
    private (double Start, double End) Sides()
    {
        var (start, end) = (Natural(Child(0)), Natural(Child(2)));
        return IsStrict ? (Math.Max(start, end), Math.Max(start, end)) : (start, end);
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        var height = 0.0;
        foreach (var c in Children)
        {
            c.Measure(new Size(double.PositiveInfinity, availableSize.Height));
            height = Math.Max(height, c.DesiredSize.Height);
        }

        // gtk_center_layout_measure_orientation: the center and twice the wider side.
        var (start, end) = Sides();
        return new Size(Natural(Child(1)) + (2 * Math.Max(start, end)), height);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        // gtk_center_layout_distribute, the sides keeping their natural widths and the center shrinking first.
        var size = finalSize.Width;
        var (startNat, endNat) = Sides();
        var center = Child(1);
        var centerSize = center is { IsVisible: true } ? Math.Clamp(size - (startNat + endNat), 0, Natural(center)) : 0;
        var startSize = Math.Clamp(size - (centerSize + endNat), 0, startNat);
        var endSize = Math.Clamp(size - (centerSize + startNat), 0, endNat);
        var centerPos = Math.Floor(size / 2) - Math.Floor(centerSize / 2);
        if (center is { IsVisible: true })
        {
            if (startSize > 0 && startSize > centerPos)
            {
                centerPos = startSize;
            }
            else if (endSize > 0 && size - endSize < centerPos + centerSize)
            {
                centerPos = size - centerSize - endSize;
            }
            else if (ExpandsCenter)
            {
                centerSize = size - (2 * Math.Max(startSize, endSize));
                centerPos = Math.Floor(size / 2) - Math.Floor(centerSize / 2);
            }
        }

        // gtk_center_layout_allocate; Avalonia mirrors it right to left.
        Child(0)?.Arrange(new Rect(0, 0, startSize, finalSize.Height));
        center?.Arrange(new Rect(centerPos, 0, centerSize, finalSize.Height));
        Child(2)?.Arrange(new Rect(size - endSize, 0, endSize, finalSize.Height));
        return finalSize;
    }
}
