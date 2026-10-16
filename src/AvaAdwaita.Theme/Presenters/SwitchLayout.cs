using System;
using Avalonia;
using Avalonia.Controls;

namespace AvaAdwaita.Presenters;

/// <summary>
/// Lays out a switch as gtk_switch_measure and gtk_switch_allocate: twice the slider's width, the on
/// image centered in the left half, the off image in the right half, and the knob canvas spanning the travel.
/// The images show only with status shapes on, as GTK binds their opacity to <c>gtk-show-status-shapes</c>.
/// </summary>
public class SwitchLayout : Panel
{
    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        AdwaitaEnvironment.Changed += OnSettingsChanged;
        UpdateShapes();
    }

    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        AdwaitaEnvironment.Changed -= OnSettingsChanged;
    }

    private void OnSettingsChanged(Settings.StyleStateChange change)
    {
        if ((change & Settings.StyleStateChange.StatusShapes) != 0)
        {
            UpdateShapes();
        }
    }

    private void UpdateShapes()
    {
        var opacity = AdwaitaEnvironment.State.ShowStatusShapes ? 1 : 0;
        foreach (var c in Children)
        {
            if (GetIsOnImage(c) is not null)
            {
                c.Opacity = opacity;
            }
        }
    }

    /// <summary>Marks the on and off images.</summary>
    public static readonly AttachedProperty<bool?> IsOnImageProperty =
        AvaloniaProperty.RegisterAttached<SwitchLayout, Control, bool?>("IsOnImage");

    /// <summary>Gets <see cref="IsOnImageProperty"/>: true for the on image, false for the off image, null for the knob canvas.</summary>
    public static bool? GetIsOnImage(Control c) => c.GetValue(IsOnImageProperty);

    /// <summary>Sets <see cref="IsOnImageProperty"/>.</summary>
    public static void SetIsOnImage(Control c, bool? v) => c.SetValue(IsOnImageProperty, v);

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        double slider = 0, sliderH = 0, text = 0, textH = 0;
        foreach (var c in Children)
        {
            c.Measure(Size.Infinity);
            if (GetIsOnImage(c) is null)
            {
                slider = Math.Max(slider, c.DesiredSize.Width);
                sliderH = Math.Max(sliderH, c.DesiredSize.Height);
            }
            else
            {
                text = Math.Max(text, c.DesiredSize.Width);
                textH = Math.Max(textH, c.DesiredSize.Height);
            }
        }

        return new Size(2 * Math.Max(slider, text), Math.Max(sliderH, textH));
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        // Avalonia mirrors a right-to-left layout itself, so the on image is laid out on the left here.
        var half = Math.Floor(finalSize.Width / 2);
        foreach (var c in Children)
        {
            var on = GetIsOnImage(c);
            if (on is null)
            {
                c.Arrange(new Rect(0, 0, finalSize.Width - half, finalSize.Height));
                continue;
            }

            var w = c.DesiredSize.Width;
            var h = c.DesiredSize.Height;
            var x = Math.Floor((half - w) / 2) + (on.Value ? 0 : half);
            c.Arrange(new Rect(x, Math.Floor((finalSize.Height - h) / 2), w, h));
        }

        return finalSize;
    }
}
