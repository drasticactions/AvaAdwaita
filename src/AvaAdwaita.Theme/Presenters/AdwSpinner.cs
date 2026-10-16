using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using AvaAdwaita.Animation;
using AvaAdwaita.Markup;

namespace AvaAdwaita.Presenters;

/// <summary>
/// libadwaita's loading spinner, an <c>image.spinner</c> node: a faint circle and an arc that grows, shrinks and
/// turns once every 1.2 s, in the node's color. It sizes to the space it gets, from 16 to 64 pixels.
/// </summary>
/// <remarks>Port of adw-spinner.c and adw_spinner_paintable_snapshot_with_weight (adw-spinner-paintable.c).</remarks>
public class AdwSpinner : Control
{
    private const double MinSize = 16;
    private const double MaxRadius = 32;
    private const double SpinDurationMs = 1200;
    private const double StartAngle = Math.PI * 0.35;
    private const double CircleOpacity = 0.15;
    private const double MinArcLength = Math.PI * 0.015;
    private const double MaxArcLength = Math.PI * 0.9;
    private const double IdleDistance = Math.PI * 0.9;
    private const double OverlapDistance = Math.PI * 0.7;
    private const double ExtendDistance = Math.PI * 1.1;
    private const double ContractDistance = Math.PI * 1.35;
    private const int Cycles = 53;

    private long _start;
    private bool _frameRequested;

    /// <summary>A fixed time into the animation in milliseconds, for comparing a still frame with GTK's.</summary>
    internal double? FixedTime { get; set; }

    /// <summary>Creates a spinner.</summary>
    public AdwSpinner()
    {
        SetValue(Nodes.Css.NodeProperty, "image", Avalonia.Data.BindingPriority.Style);
        SetValue(Nodes.Css.ClassesProperty, "spinner", Avalonia.Data.BindingPriority.Style);
    }

    static AdwSpinner()
    {
        Nodes.Css.StyleProperty.Changed.AddClassHandler<AdwSpinner>((s, _) => s.InvalidateVisual());
    }

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _start = Environment.TickCount64;
        RequestFrame();
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize) => new(MinSize, MinSize);

    private void RequestFrame()
    {
        if (_frameRequested || TopLevel.GetTopLevel(this) is not { } top)
        {
            return;
        }

        _frameRequested = true;
        FrameClock.Request(top, _ =>
        {
            _frameRequested = false;
            if (IsEffectivelyVisible)
            {
                InvalidateVisual();
            }

            if (TopLevel.GetTopLevel(this) is not null)
            {
                RequestFrame();
            }
        });
    }

    private static double ArcStart(double angle)
    {
        const double L = IdleDistance + ExtendDistance + ContractDistance - OverlapDistance;
        angle %= L;
        var t = angle > ExtendDistance ? 1 : Easing.Ease(AdwEasing.EaseInOutSine, angle / ExtendDistance);
        return MinArcLength + ((MaxArcLength - MinArcLength) * t) - (angle * MaxArcLength / L);
    }

    private static double ArcEnd(double angle)
    {
        const double L = IdleDistance + ExtendDistance + ContractDistance - OverlapDistance;
        angle %= L;
        double t;
        if (angle < ExtendDistance - OverlapDistance)
        {
            t = 0;
        }
        else if (angle > L - IdleDistance)
        {
            t = 1;
        }
        else
        {
            t = Easing.Ease(AdwEasing.EaseInOutSine, (angle - ExtendDistance + OverlapDistance) / ContractDistance);
        }

        return ((MaxArcLength - MinArcLength) * t) - (angle * MaxArcLength / L);
    }

    /// <summary>The arc's start and end angles, in radians clockwise from the x axis, at a time into the animation.</summary>
    internal static (double Start, double End) Arc(double ms)
    {
        var elapsed = ms % (SpinDurationMs * Cycles);
        var progress = elapsed / SpinDurationMs * Math.PI * 2;
        return (Normalize(progress + ArcStart(progress) + StartAngle), Normalize(progress + ArcEnd(progress) + StartAngle));
    }

    /// <summary>The length of the whole animation, after which it repeats, in milliseconds.</summary>
    internal const double Period = SpinDurationMs * Cycles;

    private static double Normalize(double angle)
    {
        angle %= Math.PI * 2;
        return angle < 0 ? angle + (Math.PI * 2) : angle;
    }

    // gtksvg.c's weight interpolation: 400 is the plain width, 1 a quarter of it, 1000 one and a half.
    private static double LineWidth(double diameter, double weight)
    {
        var stroke = diameter / 8.0;
        if (weight < 400)
        {
            return stroke + (((0.25 * stroke) - stroke) * ((400 - weight) / 399));
        }

        return stroke + (((1.5 * stroke) - stroke) * ((Math.Min(weight, 1000) - 400) / 600));
    }

    /// <inheritdoc/>
    public override void Render(DrawingContext context)
    {
        if (GetValue(Nodes.Css.StyleProperty) is not { } s)
        {
            return;
        }

        var radius = Math.Min(Math.Floor(Math.Min(Bounds.Width, Bounds.Height) / 2), MaxRadius);
        var lineWidth = LineWidth(radius * 2, s.Icon.Weight);
        if (radius < lineWidth / 2)
        {
            return;
        }

        var color = CssConvert.Color(s.Drawn(s.Core.Color));
        var center = new Point(Math.Round(Bounds.Width / 2), Math.Round(Bounds.Height / 2));
        var r = radius - (lineWidth / 2);

        var circle = new Pen(new SolidColorBrush(color, CircleOpacity), lineWidth);
        context.DrawEllipse(null, circle, center, r, r);

        // libadwaita's spinner turns even when animations are off (it does not follow enable-animations).
        var (start, end) = Arc(FixedTime ?? (Environment.TickCount64 - _start));

        // The arc runs along the circle, clockwise as GTK builds it, from the end angle to the start angle.
        var sweep = Normalize(start - end);
        var geometry = new StreamGeometry();
        using (var g = geometry.Open())
        {
            g.BeginFigure(new Point(center.X + (r * Math.Cos(end)), center.Y + (r * Math.Sin(end))), false);
            g.ArcTo(new Point(center.X + (r * Math.Cos(start)), center.Y + (r * Math.Sin(start))), new Size(r, r), 0, sweep > Math.PI, SweepDirection.Clockwise);
            g.EndFigure(false);
        }

        context.DrawGeometry(null, new Pen(new SolidColorBrush(color), lineWidth, lineCap: PenLineCap.Round), geometry);

        // GTK's spinner paintable is one icon draw over its square, in the node's color.
        DrawRecorder.For(this)?.Add(this, new DrawRecord("icon", new Rect(center.X - radius, center.Y - radius, radius * 2, radius * 2)) { Color = color });
    }
}
