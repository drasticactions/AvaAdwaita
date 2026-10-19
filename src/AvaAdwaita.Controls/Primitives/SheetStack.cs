using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using AvaAdwaita.Nodes;

namespace AvaAdwaita.Controls.Primitives;

/// <summary>
/// A bottom sheet's stack (a homogeneous GtkStack): the page or the bottom bar's button, sized for the larger of the
/// two whichever shows.
/// </summary>
public class SheetStack : Control
{
    /// <summary>Creates the stack: a <c>stack</c> node holding <paramref name="page"/> and <paramref name="bar"/>.</summary>
    public SheetStack(SheetPage page, Button bar)
    {
        Page = page;
        Bar = bar;
        Css.SetNode(this, "stack");
        VisualChildren.Add(page);
        VisualChildren.Add(bar);
        LogicalChildren.Add(page);
        LogicalChildren.Add(bar);
    }

    /// <summary>The page.</summary>
    public SheetPage Page { get; }

    /// <summary>The bottom bar's button.</summary>
    public Button Bar { get; }

    /// <summary>The bar's natural size for a width (or none); the bar's content when the button is hidden.</summary>
    internal Size BarNatural(double forWidth)
    {
        Control? bar = Bar.IsVisible ? Bar : Bar.Content as Control;
        if (bar is null)
        {
            return default;
        }

        bar.Measure(new Size(forWidth, double.PositiveInfinity));
        return bar.DesiredSize;
    }

    /// <summary>The stack's natural size for a width (or none): the larger of the page and the bar.</summary>
    internal Size Natural(double forWidth)
    {
        var page = Page.Natural(forWidth);
        var bar = Bar.Content is null ? default : BarNatural(forWidth);
        return new Size(Math.Max(page.Width, bar.Width), Math.Max(page.Height, bar.Height));
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        if (Page.IsVisible)
        {
            Page.Measure(availableSize);
        }

        if (Bar.IsVisible)
        {
            Bar.Measure(availableSize);
        }

        return Natural(availableSize.Width);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        Page.Arrange(new Rect(finalSize));

        // The bar's button is valign start: its own height, at the top.
        Bar.Arrange(new Rect(0, 0, finalSize.Width, Math.Min(finalSize.Height, Bar.DesiredSize.Height)));
        return finalSize;
    }
}
