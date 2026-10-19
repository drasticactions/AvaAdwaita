using System;
using Avalonia;
using Avalonia.Controls;
using AvaAdwaita.Nodes;
using AvaAdwaita.Presenters;

namespace AvaAdwaita.Controls.Primitives;

/// <summary>
/// A bottom sheet's page (adw-bottom-sheet.c's sheet_page): the sheet's child, with the drag handle over its top middle.
/// </summary>
public class SheetPage : Control
{
    private readonly AdwBox _handle = new() { IsHitTestVisible = false };
    private Control? _child;

    /// <summary>Creates a page: a <c>widget</c> node holding <c>drag-handle</c>.</summary>
    public SheetPage()
    {
        Css.SetNode(this, "widget");
        Css.SetNode(_handle, "drag-handle");
        VisualChildren.Add(_handle);
    }

    /// <summary>The sheet's child.</summary>
    public Control? Child
    {
        get => _child;
        set
        {
            if (_child is not null)
            {
                VisualChildren.Remove(_child);
                LogicalChildren.Remove(_child);
            }

            // Under the handle, as adw_bottom_sheet_set_sheet inserts it before.
            _child = value;
            if (value is not null)
            {
                VisualChildren.Insert(0, value);
                LogicalChildren.Add(value);
            }

            InvalidateMeasure();
        }
    }

    /// <summary>Shows the drag handle.</summary>
    public bool ShowsHandle
    {
        get => _handle.IsVisible;
        set => _handle.IsVisible = value;
    }

    /// <summary>The narrowest natural width, as adw_bottom_sheet_set_min_natural_width; NaN for none.</summary>
    internal double MinNaturalWidth { get; set; } = double.NaN;

    /// <summary>The child's width, as a dialog's content width; NaN takes the child's.</summary>
    internal double ContentWidth { get; set; } = double.NaN;

    /// <summary>The child's height, as a dialog's content height; NaN takes the child's.</summary>
    internal double ContentHeight { get; set; } = double.NaN;

    /// <summary>
    /// measure_sheet: the larger of the child and the handle, at least <see cref="MinNaturalWidth"/> wide. Measured with
    /// no width, it is the natural width.
    /// </summary>
    internal Size Natural(double forWidth)
    {
        var child = default(Size);
        if (_child is { IsVisible: true } c)
        {
            c.Measure(new Size(double.IsNaN(ContentWidth) ? forWidth : ContentWidth, double.IsNaN(ContentHeight) ? double.PositiveInfinity : ContentHeight));
            child = new Size(double.IsNaN(ContentWidth) ? c.DesiredSize.Width : ContentWidth, double.IsNaN(ContentHeight) ? c.DesiredSize.Height : ContentHeight);
        }

        if (!double.IsNaN(MinNaturalWidth))
        {
            child = child.WithWidth(Math.Max(child.Width, MinNaturalWidth));
        }

        var handle = default(Size);
        if (_handle.IsVisible)
        {
            _handle.Measure(Size.Infinity);
            handle = _handle.DesiredSize;
        }

        return new Size(Math.Max(child.Width, handle.Width), Math.Max(child.Height, handle.Height));
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        var natural = Natural(double.PositiveInfinity);
        return double.IsInfinity(availableSize.Width) ? natural : Natural(availableSize.Width).WithWidth(Math.Min(availableSize.Width, natural.Width));
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        // allocate_sheet: the handle at its natural size, centered at the top; the child fills the page.
        if (_handle.IsVisible)
        {
            var w = Math.Min(_handle.DesiredSize.Width, finalSize.Width);
            var h = Math.Min(_handle.DesiredSize.Height, finalSize.Height);
            _handle.Arrange(new Rect(Math.Round((finalSize.Width - w) / 2), 0, w, h));
        }

        // The child lays out for the width the sheet has, not the width it was last measured for, and for the height
        // when that is set or less than it wants; otherwise with no height, so that it can grow the sheet later.
        if (_child is { } child)
        {
            child.Measure(finalSize.WithHeight(double.PositiveInfinity));
            if (!double.IsNaN(ContentHeight) || child.DesiredSize.Height > finalSize.Height)
            {
                child.Measure(finalSize);
            }

            child.Arrange(new Rect(finalSize));
        }
        return finalSize;
    }
}
