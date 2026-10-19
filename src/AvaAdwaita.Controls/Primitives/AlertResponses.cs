using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using AvaAdwaita.Nodes;

namespace AvaAdwaita.Controls.Primitives;

/// <summary>
/// An alert dialog's response buttons (adw-alert-dialog.c's response_area): side by side in equal shares while they
/// fit, else stacked from the bottom, the first response lowest, with the <c>compact</c> class.
/// </summary>
public class AlertResponses : Panel
{
    /// <summary>libadwaita's BUTTON_SPACING.</summary>
    internal const double ButtonSpacing = 12;

    /// <summary>Creates the area: a <c>widget.response-area</c> node.</summary>
    public AlertResponses()
    {
        Css.SetNode(this, "widget");
        Css.SetClasses(this, "response-area");
    }

    /// <summary>Uses the side-by-side layout always, as a short dialog does.</summary>
    public bool IsShort { get; set; }

    private Thickness Inset => Css.GetStyle(this) is { } s ? new Presenters.CssBoxGeometry(s, default) is var g
        ? new Thickness(g.Margin.Left + g.Border.Left + g.Padding.Left, g.Margin.Top + g.Border.Top + g.Padding.Top, g.Margin.Right + g.Border.Right + g.Padding.Right, g.Margin.Bottom + g.Border.Bottom + g.Padding.Bottom)
        : default : default;

    /// <summary>measure_responses_do for the horizontal natural width: compact is the widest button, else all side by side.</summary>
    internal double NaturalWidth(bool compact)
    {
        var buttons = Children.Where(c => c.IsVisible).ToList();
        foreach (var b in buttons)
        {
            b.Measure(Size.Infinity);
        }

        if (buttons.Count == 0)
        {
            return 0;
        }

        return compact
            ? buttons.Max(b => b.DesiredSize.Width)
            : (buttons.Max(b => b.DesiredSize.Width) * buttons.Count) + (ButtonSpacing * (buttons.Count - 1));
    }

    private bool Vertical(double width) => !IsShort && NaturalWidth(false) > width;

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        var inset = Inset;
        var buttons = Children.Where(c => c.IsVisible).ToList();
        foreach (var b in buttons)
        {
            b.Measure(Size.Infinity);
        }

        var inner = availableSize.Width - inset.Left - inset.Right;
        var vertical = !double.IsInfinity(inner) && Vertical(inner);
        var height = buttons.Count == 0 ? 0 : vertical
            ? buttons.Sum(b => b.DesiredSize.Height) + (ButtonSpacing * (buttons.Count - 1))
            : buttons.Max(b => b.DesiredSize.Height);
        var width = double.IsInfinity(inner) ? NaturalWidth(false) : Math.Min(inner, NaturalWidth(false));
        return new Size(width + inset.Left + inset.Right, height + inset.Top + inset.Bottom);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        var inset = Inset;
        var area = new Rect(finalSize).Deflate(inset);
        var buttons = Children.Where(c => c.IsVisible).ToList();
        var vertical = Vertical(area.Width);
        Css.SetClasses(this, vertical ? "response-area compact" : "response-area");
        if (vertical)
        {
            // allocate_responses: stacked from the bottom.
            var pos = area.Bottom;
            foreach (var b in buttons)
            {
                pos -= b.DesiredSize.Height;
                b.Arrange(new Rect(area.X, pos, area.Width, b.DesiredSize.Height));
                pos -= ButtonSpacing;
            }
        }
        else if (buttons.Count > 0)
        {
            // Equal shares, rounded up, the last taking what is left; Avalonia mirrors right to left.
            var total = area.Width - (ButtonSpacing * Math.Max(0, buttons.Count - 1));
            var buttonWidth = Math.Ceiling(total / buttons.Count);
            var x = area.X;
            foreach (var b in buttons)
            {
                buttonWidth = Math.Min(buttonWidth, total);
                total -= buttonWidth;
                b.Arrange(new Rect(x, area.Y, buttonWidth, area.Height));
                x += buttonWidth + ButtonSpacing;
            }
        }

        return finalSize;
    }
}
