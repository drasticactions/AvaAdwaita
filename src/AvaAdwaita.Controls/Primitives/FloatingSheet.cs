using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media;
using AvaAdwaita.Nodes;
using AvaAdwaita.Presenters;

namespace AvaAdwaita.Controls.Primitives;

/// <summary>
/// A floating dialog's sheet over the dimmed window (AdwFloatingSheet): it grows from 0.8 of its size and fades in on
/// a spring as it opens, and back as it closes. The sheet is centered with a padding that grows with the window.
/// </summary>
public class FloatingSheet : Control
{
    private const double MinScale = 0.8;
    private const double HorzPaddingMinWidth = 720;
    private const double HorzPaddingMinValue = 30;
    private const double HorzPaddingTargetWidth = 1440;
    private const double HorzPaddingTargetValue = 120;
    private const double VertPaddingMinHeight = 720;
    private const double VertPaddingMinValue = 20;
    private const double VertPaddingTargetHeight = 1440;
    private const double VertPaddingTargetValue = 120;

    private readonly AdwBox _dimming = new() { Opacity = 0, IsHitTestVisible = false };
    private readonly AdwBox _sheet = new() { Opacity = 0 };
    private readonly AdwBox _bin = new();
    private readonly SpringAnimation _animation;
    private Action? _closed;
    private double _progress;

    /// <summary>Creates a sheet: a <c>floating-sheet</c> node with <c>dimming</c> and <c>sheet.background</c>.</summary>
    public FloatingSheet()
    {
        SetValue(Css.NodeProperty, "floating-sheet", BindingPriority.Style);
        Css.SetNode(_dimming, "dimming");
        Css.SetNode(_sheet, "sheet");
        Css.SetClasses(_sheet, "background");
        Css.SetNode(_bin, "widget");
        _sheet.Child = _bin;
        VisualChildren.Add(_dimming);
        VisualChildren.Add(_sheet);
        LogicalChildren.Add(_sheet);
        _animation = new SpringAnimation(this, 0.62, 1, 500, SetProgress, OnAnimationDone);
    }

    /// <summary>The dialog's content.</summary>
    public Control? Child
    {
        get => _bin.Child;
        set => _bin.Child = value;
    }

    /// <summary>The sheet's width; NaN takes the child's.</summary>
    public double ContentWidth { get; set; } = double.NaN;

    /// <summary>The sheet's height; NaN takes the child's.</summary>
    public double ContentHeight { get; set; } = double.NaN;

    /// <summary>The sheet node.</summary>
    internal Control Sheet => _sheet;

    /// <summary>How far the sheet shows, 0 to 1.</summary>
    internal double Progress => _progress;

    /// <summary>Opens the sheet.</summary>
    public void Open()
    {
        _closed = null;
        _animation.Animate(1);
    }

    /// <summary>Closes the sheet, then calls <paramref name="closed"/>.</summary>
    public void Close(Action closed)
    {
        _closed = closed;
        _animation.Animate(0, clamp: true);
    }

    // open_animation_cb.
    private void SetProgress(double value)
    {
        _progress = value;
        _dimming.Opacity = Math.Clamp(value, 0, 1);
        _sheet.Opacity = Math.Clamp(value, 0, 1);
        InvalidateArrange();
    }

    private void OnAnimationDone()
    {
        if (_progress < 0.5 && _closed is { } closed)
        {
            _closed = null;
            closed();
        }
    }

    private Size SheetSize(Size available)
    {
        _bin.Measure(new Size(double.IsNaN(ContentWidth) ? available.Width : ContentWidth, double.IsNaN(ContentHeight) ? available.Height : ContentHeight));
        _sheet.Measure(available);
        var extra = _sheet.DesiredSize - _bin.DesiredSize;
        return new Size(
            (double.IsNaN(ContentWidth) ? _bin.DesiredSize.Width : ContentWidth) + extra.Width,
            (double.IsNaN(ContentHeight) ? _bin.DesiredSize.Height : ContentHeight) + extra.Height);
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        _dimming.Measure(availableSize);
        var sheet = SheetSize(availableSize);
        return new Size(Math.Max(_dimming.DesiredSize.Width, sheet.Width), Math.Max(_dimming.DesiredSize.Height, sheet.Height));
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        var (width, height) = (finalSize.Width, finalSize.Height);
        if (width == 0 && height == 0)
        {
            return finalSize;
        }

        _dimming.Arrange(new Rect(finalSize));

        // adw_floating_sheet_size_allocate: the padding grows from 30 and 20 at 720 to 120 at 1440.
        var horz = Math.Floor(Lerp(HorzPaddingMinValue, HorzPaddingTargetValue, Math.Max(0, (width - HorzPaddingMinWidth) / (HorzPaddingTargetWidth - HorzPaddingMinWidth))));
        var vert = Math.Floor(Lerp(VertPaddingMinValue, VertPaddingTargetValue, Math.Max(0, (height - VertPaddingMinHeight) / (VertPaddingTargetHeight - VertPaddingMinHeight))));
        var natural = SheetSize(new Size(Math.Max(0, width - (horz * 2)), double.PositiveInfinity));
        var sheetWidth = Math.Min(natural.Width, width - (horz * 2));
        var naturalHeight = SheetSize(new Size(sheetWidth, double.PositiveInfinity)).Height;
        var sheetHeight = Math.Min(naturalHeight, height - (vert * 2));
        var x = Math.Round((width - sheetWidth) * 0.5);
        var y = Math.Round((height - sheetHeight) * 0.5);

        // Measured last at its final width, and at its final height when that is set or less than it wants: content
        // measured with no height (as a grid's star rows or a scrolled window) would keep that height. Otherwise it is
        // measured with no height, so content that grows later grows the sheet, as it is measured again with that
        // constraint.
        var fixedHeight = !double.IsNaN(ContentHeight) || sheetHeight < naturalHeight;
        _sheet.Measure(new Size(Math.Max(0, sheetWidth), fixedHeight ? Math.Max(0, sheetHeight) : double.PositiveInfinity));
        _sheet.Arrange(new Rect(x, y, Math.Max(0, sheetWidth), Math.Max(0, sheetHeight)));
        OverflowClip.Apply(_sheet, _bin);

        // It grows from the middle of the window as it opens.
        var scale = MinScale + ((1 - MinScale) * _progress);
        _sheet.RenderTransformOrigin = new RelativePoint((width / 2) - x, (height / 2) - y, RelativeUnit.Absolute);
        _sheet.RenderTransform = scale == 1 ? null : new ScaleTransform(scale, scale);
        return finalSize;
    }

    private static double Lerp(double a, double b, double t) => a + ((b - a) * t);
}
