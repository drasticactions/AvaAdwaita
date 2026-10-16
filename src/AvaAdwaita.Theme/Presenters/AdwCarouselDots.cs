using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using AvaAdwaita.Markup;

namespace AvaAdwaita.Presenters;

/// <summary>
/// AdwCarouselIndicatorDots' dots, in the nearest node's color: radius 3 at 30 % opacity, growing to radius 4 at
/// 90 % at the current position, 15 px apart, with a 6 px margin. Clicking a dot sets <see cref="Position"/>.
/// </summary>
/// <remarks>Follows adw_carousel_indicator_dots_measure and snapshot_dots (adw-carousel-indicator-dots.c).</remarks>
public class AdwCarouselDots : Control
{
    /// <summary>Defines <see cref="Count"/>.</summary>
    public static readonly StyledProperty<int> CountProperty =
        AvaloniaProperty.Register<AdwCarouselDots, int>(nameof(Count));

    /// <summary>Defines <see cref="Position"/>.</summary>
    public static readonly StyledProperty<int> PositionProperty =
        AvaloniaProperty.Register<AdwCarouselDots, int>(nameof(Position), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    /// <summary>Defines <see cref="Orientation"/>.</summary>
    public static readonly StyledProperty<Orientation> OrientationProperty =
        AvaloniaProperty.Register<AdwCarouselDots, Orientation>(nameof(Orientation));

    private const double Radius = 3;
    private const double SelectedRadius = 4;
    private const double DotOpacity = 0.3;
    private const double SelectedOpacity = 0.9;
    private const double Spacing = 7;
    private const double Margin_ = 6;
    private const double DotSize = (2 * SelectedRadius) + Spacing;

    static AdwCarouselDots()
    {
        AffectsMeasure<AdwCarouselDots>(CountProperty, OrientationProperty);
        AffectsRender<AdwCarouselDots>(PositionProperty, Nodes.Css.StyleProperty);
    }

    /// <summary>The number of pages.</summary>
    public int Count
    {
        get => GetValue(CountProperty);
        set => SetValue(CountProperty, value);
    }

    /// <summary>The current page.</summary>
    public int Position
    {
        get => GetValue(PositionProperty);
        set => SetValue(PositionProperty, value);
    }

    /// <summary>Whether the dots run across or down.</summary>
    public Orientation Orientation
    {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        var along = Math.Ceiling(DotSize * Math.Max(0, Count)) + (2 * Margin_);
        var across = (2 * SelectedRadius) + (2 * Margin_);
        return Orientation == Orientation.Horizontal ? new Size(along, across) : new Size(across, along);
    }

    // The center of each dot along the widget, as snapshot_dots places them.
    private double Start(double length)
    {
        var indicator = (DotSize * Count) - Spacing;
        var full = Math.Round(indicator / DotSize) * DotSize;
        var whole = (int)length;
        if ((whole - (int)full) % 2 == 0)
        {
            whole--;
        }

        return (whole - indicator) / 2.0;
    }

    /// <inheritdoc/>
    public override void Render(DrawingContext context)
    {
        if (Count < 2 || GetValue(Nodes.Css.StyleProperty) is not { } s)
        {
            return;
        }

        var horizontal = Orientation == Orientation.Horizontal;
        var length = horizontal ? Bounds.Width : Bounds.Height;
        var thickness = (int)(horizontal ? Bounds.Height : Bounds.Width);
        var color = CssConvert.Color(s.Drawn(s.Core.Color));
        var along = Start(length);
        var across = thickness / 2;
        var rec = DrawRecorder.For(this);
        for (var i = 0; i < Count; i++)
        {
            along += DotSize / 2;
            var current = i == Position;
            var r = current ? SelectedRadius : Radius;
            var opacity = current ? SelectedOpacity : DotOpacity;
            var (cx, cy) = horizontal ? (along, (double)across) : (across, along);
            var rect = new Rect(cx - r, cy - r, 2 * r, 2 * r);
            var c = Color.FromArgb((byte)Math.Round(color.A * opacity), color.R, color.G, color.B);
            context.DrawRectangle(new SolidColorBrush(c), null, new RoundedRect(rect, r));
            rec?.Add(this, new DrawRecord("color", rect) { Color = c, Alpha = (float)(s.Core.Color.Alpha * opacity), Radii = [r, r, r, r] });
            along += DotSize / 2;
        }
    }

    /// <inheritdoc/>
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (Count < 1)
        {
            return;
        }

        var p = e.GetPosition(this);
        var horizontal = Orientation == Orientation.Horizontal;
        var at = (horizontal ? p.X : p.Y) - Start(horizontal ? Bounds.Width : Bounds.Height);
        Position = Math.Clamp((int)Math.Floor(at / DotSize), 0, Count - 1);
        e.Handled = true;
    }
}
