using Avalonia;
using Avalonia.Controls;

namespace AvaAdwaita.Presenters;

/// <summary>
/// The switch's knob travel. <see cref="SwitchLayout"/> makes it half the switch wide, so
/// <c>ToggleSwitch</c>'s <c>Canvas.Left</c> of 0 and of the canvas width put the slider, which is as wide
/// as the canvas, in the left and right halves, as gtk_switch_allocate.
/// </summary>
public class SwitchKnobCanvas : Canvas
{
    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        Size knob = default;
        foreach (var c in Children)
        {
            c.Measure(Size.Infinity);
            knob = new Size(System.Math.Max(knob.Width, c.DesiredSize.Width), System.Math.Max(knob.Height, c.DesiredSize.Height));
        }

        return knob;
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        foreach (var c in Children)
        {
            var left = GetLeft(c);
            c.Arrange(new Rect(double.IsNaN(left) ? 0 : left, 0, finalSize.Width, finalSize.Height));
        }

        return finalSize;
    }
}
