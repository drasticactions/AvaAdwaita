using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using AvaAdwaita.Nodes;
using AvaAdwaita.Presenters;

namespace AvaAdwaita.Controls.Primitives;

/// <summary>
/// A child with a dot or a number at its top end corner (AdwIndicatorBin), as a view switcher's icon: the
/// <c>indicator</c> node, and the <c>mask</c> node that cuts the child away around it.
/// </summary>
public class IndicatorBin : Control
{
    /// <summary>Defines <see cref="Child"/>.</summary>
    public static readonly StyledProperty<Control?> ChildProperty =
        Decorator.ChildProperty.AddOwner<IndicatorBin>();

    /// <summary>Defines <see cref="BadgeNumber"/>.</summary>
    public static readonly StyledProperty<int> BadgeNumberProperty =
        AvaloniaProperty.Register<IndicatorBin, int>(nameof(BadgeNumber));

    /// <summary>Defines <see cref="NeedsAttention"/>.</summary>
    public static readonly StyledProperty<bool> NeedsAttentionProperty =
        AvaloniaProperty.Register<IndicatorBin, bool>(nameof(NeedsAttention));

    private readonly AdwBox _mask = new() { Draws = false, IsHitTestVisible = false };
    private readonly AdwBox _indicator = new() { IsHitTestVisible = false };
    private readonly AdwLabel _badge = new();

    static IndicatorBin()
    {
        AffectsMeasure<IndicatorBin>(ChildProperty, BadgeNumberProperty);
        CssNodeProviders.Register<IndicatorBin>(
            b => [.. b.BadgeNumber > 0 ? ["badge"] : Array.Empty<string>(), .. b.NeedsAttention ? ["needs-attention"] : Array.Empty<string>()],
            BadgeNumberProperty,
            NeedsAttentionProperty);
    }

    /// <summary>Creates an indicator bin: an <c>indicatorbin</c> node holding <c>mask</c> and <c>indicator</c>.</summary>
    public IndicatorBin()
    {
        SetValue(Css.NodeProperty, "indicatorbin", Avalonia.Data.BindingPriority.Style);
        Css.SetNode(_mask, "mask");
        Css.SetNode(_indicator, "indicator");
        Css.SetNode(_badge, "label");
        Css.SetClasses(_badge, "numeric");
        _indicator.Child = _badge;
        VisualChildren.Add(_mask);
        VisualChildren.Add(_indicator);
        Update();
    }

    /// <summary>The child.</summary>
    [Avalonia.Metadata.Content]
    public Control? Child
    {
        get => GetValue(ChildProperty);
        set => SetValue(ChildProperty, value);
    }

    /// <summary>The number shown; 0 shows a dot while <see cref="NeedsAttention"/>, else nothing.</summary>
    public int BadgeNumber
    {
        get => GetValue(BadgeNumberProperty);
        set => SetValue(BadgeNumberProperty, value);
    }

    /// <summary>Shows a dot, or colors the number.</summary>
    public bool NeedsAttention
    {
        get => GetValue(NeedsAttentionProperty);
        set => SetValue(NeedsAttentionProperty, value);
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ChildProperty)
        {
            if (change.OldValue is Control old)
            {
                VisualChildren.Remove(old);
                LogicalChildren.Remove(old);
            }

            if (change.NewValue is Control child)
            {
                // After the mask and the indicator, as AdwIndicatorBin adds its child.
                VisualChildren.Add(child);
                LogicalChildren.Add(child);
            }
        }
        else if (change.Property == BadgeNumberProperty || change.Property == NeedsAttentionProperty)
        {
            Update();
        }
    }

    private bool Shows => BadgeNumber > 0 || NeedsAttention;

    // adw_indicator_bin_snapshot draws the indicator only with a badge or the attention mark.
    private void Update()
    {
        _badge.Text = BadgeNumber > 0 ? BadgeNumber.ToString(CultureInfo.CurrentCulture) : null;
        _badge.IsVisible = BadgeNumber > 0;
        _indicator.Opacity = Shows ? 1 : 0;
        InvalidateArrange();
    }

    // The bin's margin, border and padding: GTK lays its children out in its content box.
    private Size Extra()
    {
        if (Css.GetStyle(this) is not { } s)
        {
            return default;
        }

        var g = new CssBoxGeometry(s, default);
        return new Size(g.Margin.Left + g.Margin.Right + g.Border.Left + g.Border.Right + g.Padding.Left + g.Padding.Right,
            g.Margin.Top + g.Margin.Bottom + g.Border.Top + g.Border.Bottom + g.Padding.Top + g.Padding.Bottom);
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        _mask.Measure(Size.Infinity);
        _indicator.Measure(Size.Infinity);
        if (Child is not { } child)
        {
            return default;
        }

        var extra = Extra();
        child.Measure(new Size(Math.Max(0, availableSize.Width - extra.Width), Math.Max(0, availableSize.Height - extra.Height)));
        return new Size(child.DesiredSize.Width + extra.Width, child.DesiredSize.Height + extra.Height);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        var content = Css.GetStyle(this) is { } style ? new CssBoxGeometry(style, finalSize).ContentBox : new Rect(finalSize);
        var (width, height) = (content.Width, content.Height);
        Child?.Arrange(content);
        var size = new Size(Math.Max(_mask.DesiredSize.Width, _indicator.DesiredSize.Width), Math.Max(_mask.DesiredSize.Height, _indicator.DesiredSize.Height));

        // Avalonia mirrors a right-to-left bin itself.
        var x = size.Width > width * 2 ? (width - size.Width) / 2 : width - size.Width + (size.Height / 2);
        var y = -size.Height / 2;
        var rect = new Rect(content.X + x, content.Y + y, size.Width, size.Height);
        _mask.Arrange(rect);
        _indicator.Arrange(rect);

        // The child is cut away under the mask's rounded box.
        if (Child is { } c)
        {
            c.Clip = Shows && Css.GetStyle(_mask) is { } maskStyle
                ? new CombinedGeometry(GeometryCombineMode.Exclude, new RectangleGeometry(new Rect(-size.Width, -size.Height, width + (size.Width * 2), height + (size.Height * 2))),
                    Rounded(new CssBoxGeometry(maskStyle, size).BorderBox.Translate(new Vector(x, y)), Math.Min(size.Width, size.Height) / 2))
                : null;
        }

        return finalSize;
    }

    private static Geometry Rounded(Rect r, double radius) => new RectangleGeometry(r, radius, radius);
}
