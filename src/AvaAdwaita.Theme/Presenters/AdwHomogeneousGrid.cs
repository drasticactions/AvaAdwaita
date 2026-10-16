using System;
using Avalonia;
using Avalonia.Controls;

namespace AvaAdwaita.Presenters;

/// <summary>
/// A grid whose rows are all as tall as the tallest cell and whose columns all as wide as the widest, sharing any
/// extra space evenly, as a GtkGrid with homogeneous rows and columns. The row and column counts come from the
/// definitions; children are placed by <see cref="Grid.RowProperty"/> and <see cref="Grid.ColumnProperty"/>.
/// </summary>
public class AdwHomogeneousGrid : Grid
{
    /// <summary>Defines <see cref="MeasuredColumns"/>.</summary>
    public static readonly StyledProperty<int> MeasuredColumnsProperty =
        AvaloniaProperty.Register<AdwHomogeneousGrid, int>(nameof(MeasuredColumns));

    static AdwHomogeneousGrid()
    {
        AffectsMeasure<AdwHomogeneousGrid>(MeasuredColumnsProperty);
    }

    /// <summary>
    /// The number of columns the grid asks room for, when more than it shows: GtkGrid counts a column whose
    /// children are all hidden (a calendar's week numbers) when it measures, and not when it allocates.
    /// </summary>
    public int MeasuredColumns
    {
        get => GetValue(MeasuredColumnsProperty);
        set => SetValue(MeasuredColumnsProperty, value);
    }

    /// <inheritdoc/>
    protected override Type StyleKeyOverride => typeof(Grid);

    private int Columns => Math.Max(1, ColumnDefinitions.Count);

    private int Rows => Math.Max(1, RowDefinitions.Count);

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size constraint)
    {
        double w = 0, h = 0;
        foreach (var c in Children)
        {
            c.Measure(new Size(constraint.Width / Columns, constraint.Height / Rows));
            w = Math.Max(w, c.DesiredSize.Width);
            h = Math.Max(h, c.DesiredSize.Height);
        }

        // GTK sizes in whole logical pixels at every scale.
        w = Math.Ceiling(w);
        h = Math.Ceiling(h);
        return new Size(w * Math.Max(Columns, MeasuredColumns), h * Rows);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size arrangeSize)
    {
        foreach (var c in Children)
        {
            var col = Math.Min(GetColumn(c), Columns - 1);
            var row = Math.Min(GetRow(c), Rows - 1);
            var x = Line(arrangeSize.Width, Columns, col);
            var y = Line(arrangeSize.Height, Rows, row);
            var right = Line(arrangeSize.Width, Columns, Math.Min(col + GetColumnSpan(c), Columns));
            var bottom = Line(arrangeSize.Height, Rows, Math.Min(row + GetRowSpan(c), Rows));
            c.Arrange(new Rect(x, y, right - x, bottom - y));
        }

        return arrangeSize;
    }

    // gtk_grid_request_allocate: each line gets the whole pixels the size divides into, and the first lines one more
    // each until the rest is used up. This is where line <paramref name="index"/> starts.
    private static double Line(double size, int count, int index)
    {
        var total = (int)Math.Floor(size);
        var each = total / count;
        var rest = total % count;
        return (index * each) + Math.Min(index, rest);
    }
}
