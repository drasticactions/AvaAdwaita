using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace AvaAdwaita.Presenters;

/// <summary>
/// Sizes a date or time picker column to its widest item, as a GtkSpinButton's text is as wide as its values.
/// Avalonia's picker panel takes all the width it is offered and has no natural width of its own.
/// </summary>
public class AdwPickerColumn : Decorator
{
    private const double Offered = 10000;

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        if (Child is not { } child)
        {
            return default;
        }

        var height = double.IsInfinity(availableSize.Height) ? Offered : availableSize.Height;
        // The panel fills its items in on its first measure, after measuring them; measure again to size them.
        child.Measure(new Size(Offered, height));
        foreach (var v in child.GetVisualDescendants())
        {
            if (v is ListBoxItem i)
            {
                i.InvalidateMeasure();
                i.Measure(new Size(Offered, height));
            }
        }

        double width = 0;
        foreach (var v in child.GetVisualDescendants())
        {
            if (v is ListBoxItem item)
            {
                width = Math.Max(width, item.DesiredSize.Width);
            }
        }

        width = Math.Min(Math.Ceiling(width), availableSize.Width);
        child.Measure(new Size(width, height));
        return new Size(width, child.DesiredSize.Height);
    }
}
