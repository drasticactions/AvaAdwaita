using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Rendering;
using AvaAdwaita.CssEngine.Computed;
using AvaAdwaita.Nodes;

namespace AvaAdwaita.Presenters;

/// <summary>
/// The drawing and the box model of one CSS node: shadows, backgrounds, border and outline, and the
/// node's margin, border, padding and minimum size in layout. It shows the style of the nearest node at or
/// above it, so a template's root box draws its control's node.
/// </summary>
/// <remarks>
/// The margin is applied inside the box, never through <see cref="Avalonia.Layout.Layoutable.Margin"/>, so an application's
/// margin still adds on top, and the margin is not hit-testable, as in GTK.
/// </remarks>
public class AdwBox : Decorator, ICustomHitTest
{
    private NodeEffects? _effects;
    private ComputedStyle? _style;

    static AdwBox()
    {
        Nodes.Css.StyleProperty.Changed.AddClassHandler<AdwBox>((b, _) => b.OnStyleChanged());
    }

    /// <summary>The style the box shows.</summary>
    protected ComputedStyle? CurrentStyle => _style;

    /// <summary>When false, the box only lays out and does not draw.</summary>
    public bool Draws { get; set; } = true;

    /// <summary>When false, opacity, filters and transforms are left alone.</summary>
    public bool AppliesEffects { get; set; } = true;

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (AppliesEffects)
        {
            _effects = new NodeEffects(this);
        }

        OnStyleChanged();
    }

    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _effects?.Dispose();
        _effects = null;
    }

    private void OnStyleChanged()
    {
        var s = GetValue(Nodes.Css.StyleProperty);
        var old = _style;
        _style = s;
        if (s is null)
        {
            return;
        }

        _effects?.Push(s);
        if (old is null || !ReferenceEquals(old.Size, s.Size) || old.Border.TopWidth != s.Border.TopWidth || old.Border.LeftWidth != s.Border.LeftWidth
            || old.Border.RightWidth != s.Border.RightWidth || old.Border.BottomWidth != s.Border.BottomWidth)
        {
            InvalidateMeasure();
        }

        InvalidateVisual();
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        if (_style is not { } s)
        {
            return base.MeasureOverride(availableSize);
        }

        var extra = Extra(s);
        var child = Child;
        var inner = new Size(Math.Max(0, availableSize.Width - extra.Width), Math.Max(0, availableSize.Height - extra.Height));
        var desired = default(Size);
        if (child is not null)
        {
            child.Measure(inner);
            desired = child.DesiredSize;
        }

        // gtksizerequest.c: max (child, min-size) + margin + border + padding.
        var w = Math.Max(desired.Width, Math.Ceiling(s.Size.MinWidth)) + extra.Width;
        var h = Math.Max(desired.Height, Math.Ceiling(s.Size.MinHeight)) + extra.Height;
        return new Size(w, h);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (_style is not { } s)
        {
            return base.ArrangeOverride(finalSize);
        }

        var g = new CssBoxGeometry(s, finalSize);
        Child?.Arrange(g.ContentBox);
        return finalSize;
    }

    private static Size Extra(ComputedStyle s)
    {
        var m = s.Margin;
        var b = s.BorderWidths;
        var p = s.Padding;
        static double R(double d) => CssBoxGeometry.RoundLength(d);
        return new Size(
            R(m.Left) + R(m.Right) + R(b.Left) + R(b.Right) + R(p.Left) + R(p.Right),
            R(m.Top) + R(m.Bottom) + R(b.Top) + R(b.Bottom) + R(p.Top) + R(p.Bottom));
    }

    /// <inheritdoc/>
    public override void Render(DrawingContext context)
    {
        if (Draws && _style is { } s)
        {
            BoxPainter.Paint(context, s, Bounds.Size, this);
        }
    }

    /// <summary>The border box in local coordinates.</summary>
    public Rect BorderBox => _style is { } s ? new CssBoxGeometry(s, Bounds.Size).BorderBox : new Rect(Bounds.Size);

    /// <inheritdoc/>
    public bool HitTest(Point point) => BorderBox.Contains(point);
}
