using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.VisualTree;

namespace AvaAdwaita.Presenters;

/// <summary>
/// A scale's <c>marks</c> node: a <c>mark</c> › <c>indicator</c> for each of the slider's ticks, centered where the
/// slider's center would be at that value, as GtkScale places its marks. The marks span the trough's length, as
/// GtkScale allocates them, and take their node's margin, border and padding.
/// </summary>
/// <remarks>Follows gtk_range_calc_marks (gtkrange.c) and gtk_scale_allocate_marks (gtkscale.c).</remarks>
public class AdwScaleMarks : Panel
{
    /// <summary>Defines <see cref="IsBefore"/>.</summary>
    public static readonly StyledProperty<bool> IsBeforeProperty =
        AvaloniaProperty.Register<AdwScaleMarks, bool>(nameof(IsBefore));

    private readonly List<double> _values = new();
    private Slider? _slider;

    static AdwScaleMarks()
    {
        AffectsArrange<AdwScaleMarks>(IsBeforeProperty);
    }

    /// <summary>Whether these are the marks before the trough (above or left), else after it.</summary>
    public bool IsBefore
    {
        get => GetValue(IsBeforeProperty);
        set => SetValue(IsBeforeProperty, value);
    }

    /// <summary>The tick values a slider shows on the given side, or none.</summary>
    internal static List<double> Ticks(Slider s, bool before)
    {
        var list = new List<double>();
        var side = before ? TickPlacement.TopLeft : TickPlacement.BottomRight;
        if (s.TickPlacement != side && s.TickPlacement != TickPlacement.Outside)
        {
            return list;
        }

        if (s.Ticks is { Count: > 0 } ticks)
        {
            list.AddRange(ticks);
        }
        else if (s.TickFrequency > 0 && s.Maximum > s.Minimum)
        {
            for (var v = s.Minimum; v <= s.Maximum + (s.TickFrequency * 1e-9); v += s.TickFrequency)
            {
                list.Add(Math.Min(v, s.Maximum));
            }
        }

        return list;
    }

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _slider = TemplatedParent as Slider;
        if (_slider is not null)
        {
            _slider.PropertyChanged += OnSliderPropertyChanged;
        }

        Rebuild();
    }

    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (_slider is not null)
        {
            _slider.PropertyChanged -= OnSliderPropertyChanged;
        }

        _slider = null;
    }

    private void OnSliderPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Slider.TickPlacementProperty || e.Property == Slider.TicksProperty || e.Property == Slider.TickFrequencyProperty
            || e.Property == RangeBase.MinimumProperty || e.Property == RangeBase.MaximumProperty)
        {
            Rebuild();
        }
        else if (e.Property == Slider.IsDirectionReversedProperty || e.Property == Slider.OrientationProperty)
        {
            InvalidateArrange();
        }
    }

    private void Rebuild()
    {
        _values.Clear();
        if (_slider is not null)
        {
            _values.AddRange(Ticks(_slider, IsBefore));
        }

        IsVisible = _values.Count > 0;
        while (Children.Count > _values.Count)
        {
            Children.RemoveAt(Children.Count - 1);
        }

        while (Children.Count < _values.Count)
        {
            var mark = new AdwBox { Child = new AdwBox() };
            Nodes.Css.SetNode(mark, "mark");
            Nodes.Css.SetNode(mark.Child!, "indicator");
            Children.Add(mark);
        }

        InvalidateMeasure();
    }

    private Thickness Extra() => GetValue(Nodes.Css.StyleProperty) is { } s && Nodes.Css.GetCssNode(this) is not null
        ? new CssBoxGeometry(s, default) is var g
            ? new Thickness(g.Margin.Left + g.Border.Left + g.Padding.Left, g.Margin.Top + g.Border.Top + g.Padding.Top, g.Margin.Right + g.Border.Right + g.Padding.Right, g.Margin.Bottom + g.Border.Bottom + g.Padding.Bottom)
            : default
        : default;

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        var horizontal = _slider?.Orientation != Orientation.Vertical;
        double cross = 0;
        foreach (var c in Children)
        {
            c.Measure(Size.Infinity);
            cross = Math.Max(cross, horizontal ? c.DesiredSize.Height : c.DesiredSize.Width);
        }

        var e = Extra();
        return horizontal ? new Size(e.Left + e.Right, cross + e.Top + e.Bottom) : new Size(cross + e.Left + e.Right, e.Top + e.Bottom);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (_slider is not { } s)
        {
            return finalSize;
        }

        var horizontal = s.Orientation != Orientation.Vertical;
        if (FindPart<Thumb>(s, null) is not { } thumb)
        {
            return finalSize;
        }

        // The slider's measured size, not its drawn one: libadwaita rotates the slider when marks show.
        var content = new Rect(finalSize).Deflate(Extra());
        var troughLength = (int)(horizontal ? content.Width : content.Height);
        var slider = (int)(horizontal ? thumb.DesiredSize.Width : thumb.DesiredSize.Height);
        var origin = content.Position;
        var range = s.Maximum - s.Minimum;
        for (var i = 0; i < Children.Count && i < _values.Count; i++)
        {
            var frac = range > 0 ? Math.Clamp((_values[i] - s.Minimum) / range, 0, 1) : 0;
            if (s.IsDirectionReversed == horizontal)
            {
                // A horizontal scale grows to the right, a vertical one upwards.
                frac = 1 - frac;
            }

            var at = (int)((troughLength - slider) * frac) + (slider / 2);
            var c = Children[i];
            var size = (int)(horizontal ? c.DesiredSize.Width : c.DesiredSize.Height);
            c.Arrange(horizontal
                ? new Rect(origin.X + at - (size / 2), origin.Y, size, content.Height)
                : new Rect(origin.X, origin.Y + at - (size / 2), content.Width, size));
        }

        return finalSize;
    }

    private static T? FindPart<T>(Slider s, string? name)
        where T : Control
    {
        foreach (var v in s.GetVisualDescendants())
        {
            if (v is T c && (name is null || c.Name == name) && ReferenceEquals(c.TemplatedParent, s))
            {
                return c;
            }
        }

        return null;
    }
}
