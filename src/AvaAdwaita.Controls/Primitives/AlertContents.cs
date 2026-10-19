using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.VisualTree;
using AvaAdwaita.Nodes;
using AvaAdwaita.Presenters;

namespace AvaAdwaita.Controls.Primitives;

/// <summary>
/// An alert dialog's contents (adw-alert-dialog.c's measure_child): at least 300 wide, and at most 372, or as wide as
/// side-by-side responses need, up to 600, when the dialog prefers a wide layout or is short.
/// </summary>
public class AlertContents : Decorator
{
    private const double DialogMaxWidth = 372;
    private const double DialogMaxWideWidth = 600;
    private const double DialogPreferredWidth = 300;
    private const double DialogMinWidth = 300;
    private const double ResponseHorzPadding = 48;
    private const double ResponseHorzPaddingShort = 36;

    /// <summary>Creates the contents: a <c>widget</c> node.</summary>
    public AlertContents()
    {
        Css.SetNode(this, "widget");
    }

    /// <summary>The heading, whose natural width the dialog's follows.</summary>
    public AlertHeading? Heading { get; set; }

    /// <summary>The responses.</summary>
    public AlertResponses? Responses { get; set; }

    /// <summary>Lets side-by-side responses widen the dialog up to 600.</summary>
    public bool PreferWideLayout { get; set; }

    /// <summary>The dialog is short: smaller heading and responses always side by side.</summary>
    public bool IsShort { get; set; }

    /// <summary>
    /// Measures again for a new natural width. Avalonia measures an invalid control with its last constraint, the
    /// sheet's height-for-width pass, so the sheet itself has to measure again too.
    /// </summary>
    public void Relayout()
    {
        for (Visual? v = this; v is not null and not Dialog; v = v.GetVisualParent())
        {
            (v as Layoutable)?.InvalidateMeasure();
        }
    }

    private double MaxNaturalWidth(double minSize)
    {
        if (Responses is not { } responses)
        {
            return DialogMaxWidth;
        }

        var padding = IsShort ? ResponseHorzPaddingShort : ResponseHorzPadding;
        var wide = responses.NaturalWidth(false) + padding;
        var narrow = Math.Max(responses.NaturalWidth(true) + padding, DialogPreferredWidth);
        var heading = 0.0;
        if (Heading is { IsVisible: true } h)
        {
            h.Measure(Size.Infinity);
            heading = h.DesiredSize.Width;
        }

        heading = Math.Clamp(heading, narrow, Math.Max(narrow, wide));
        double max;
        if (responses.Children.Count > 1 && wide > minSize)
        {
            if (PreferWideLayout || IsShort)
            {
                max = wide > DialogMaxWideWidth ? (IsShort ? wide : DialogMaxWidth) : Math.Min(wide, DialogMaxWideWidth);
            }
            else
            {
                // Tall enough for stacked responses, so narrower instead.
                max = Math.Min(DialogMaxWidth, heading);
            }
        }
        else
        {
            max = DialogMaxWidth;
        }

        return Math.Max(minSize, max);
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        if (Child is not { } child)
        {
            return default;
        }

        child.Measure(Size.Infinity);
        var natural = Math.Clamp(child.DesiredSize.Width, DialogMinWidth, MaxNaturalWidth(DialogMinWidth));
        var width = Math.Max(DialogMinWidth, Math.Min(natural, availableSize.Width));
        child.Measure(new Size(width, availableSize.Height));
        return new Size(width, child.DesiredSize.Height);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        Child?.Arrange(new Rect(finalSize));
        return finalSize;
    }
}
