using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using AvaAdwaita.Nodes;
using AvaAdwaita.Presenters;

namespace AvaAdwaita.Controls.Primitives;

/// <summary>
/// A label that fades out where it is cut off instead of ellipsizing (AdwFadingLabel): a <c>widget</c> node holding the
/// label at its natural width, placed by <see cref="Align"/>, with 18 px fades at the clipped ends.
/// </summary>
public class FadingLabel : Control
{
    private const double FadeWidth = 18;

    /// <summary>Creates a fading label.</summary>
    public FadingLabel()
    {
        Css.SetNode(this, "widget");
        Css.SetNode(Label, "label");
        Label.TextWrapping = TextWrapping.NoWrap;
        ClipToBounds = true;
        VisualChildren.Add(Label);
    }

    /// <summary>The label.</summary>
    public AdwLabel Label { get; } = new();

    /// <summary>Where the label sits when it fits, from 0 (the start) to 1 (the end); the end it fades at is the other.</summary>
    public double Align { get; set; }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        // adw_fading_label_measure: the label's size; its minimum width is none.
        Label.Measure(new Size(double.PositiveInfinity, availableSize.Height));
        return new Size(Math.Min(Label.DesiredSize.Width, availableSize.Width), Label.DesiredSize.Height);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        var width = Label.DesiredSize.Width;
        var offset = (finalSize.Width - width) * Align;
        Label.Arrange(new Rect(offset, 0, width, finalSize.Height));

        // adw_fading_label_snapshot: a clipped label fades out over its last 18 px, at whichever ends it is cut.
        var clipped = width - finalSize.Width;
        if (clipped <= 0 || finalSize.Width <= 0)
        {
            OpacityMask = null;
        }
        else
        {
            var fade = Math.Min(1, FadeWidth / finalSize.Width);
            var brush = new LinearGradientBrush { StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative), EndPoint = new RelativePoint(1, 0, RelativeUnit.Relative) };
            brush.GradientStops.Add(new GradientStop(Align > 0 ? Colors.Transparent : Colors.Black, 0));
            brush.GradientStops.Add(new GradientStop(Colors.Black, Align > 0 ? fade : 0));
            brush.GradientStops.Add(new GradientStop(Colors.Black, Align < 1 ? 1 - fade : 1));
            brush.GradientStops.Add(new GradientStop(Align < 1 ? Colors.Transparent : Colors.Black, 1));
            OpacityMask = brush;
        }

        return finalSize;
    }
}
