using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using AvaAdwaita.Presenters;

namespace AvaAdwaita.Controls.Primitives;

/// <summary>A tab's parts laid out as adw_tab_size_allocate does: the indicator and close button at the ends, the icon and title centered between them.</summary>
internal sealed class TabLayout : Panel
{
    private const double BaseWidth = 118;
    private const double BaseWidthPinned = 26;
    private const double TitleFadeWidth = 18;

    private readonly Tab _tab;
    private readonly AdwIcon _icon;
    private readonly AdwSpinner _spinner;
    private readonly FadingLabel _title;
    private readonly Button _indicator;
    private readonly Button _close;
    private readonly AdwBox _attention;

    public TabLayout(Tab tab, AdwIcon icon, AdwSpinner spinner, FadingLabel title, Button indicator, Button close, AdwBox attention)
    {
        (_tab, _icon, _spinner, _title, _indicator, _close, _attention) = (tab, icon, spinner, title, indicator, close, attention);
        Children.AddRange([icon, spinner, title, indicator, close, attention]);
    }

    private static double NaturalWidth(Control c) => c.IsVisible ? c.DesiredSize.Width : 0;

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        // adw_tab_measure: a fixed natural width, as tall as the tallest part.
        var height = 0.0;
        foreach (var c in Children)
        {
            c.Measure(new Size(double.PositiveInfinity, availableSize.Height));
            height = Math.Max(height, c.DesiredSize.Height);
        }

        return new Size(_tab.IsPinned ? BaseWidthPinned : BaseWidth, height);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        var (width, height) = (finalSize.Width, finalSize.Height);
        Control icon = _icon.IsVisible ? _icon : _spinner;
        var iconWidth = NaturalWidth(icon);
        var titleWidth = NaturalWidth(_title);
        var indicatorWidth = NaturalWidth(_indicator);
        var closeWidth = NaturalWidth(_close);
        double start = 0, end = 0;
        void Place(Control c, double x, double w) => c.Arrange(new Rect(x, 0, Math.Max(0, w), height));

        if (_indicator.IsVisible)
        {
            if (_tab.IsPinned)
            {
                Place(_indicator, Math.Floor((width - indicatorWidth) / 2), indicatorWidth);
            }
            else if (_tab.IsInverted)
            {
                Place(_indicator, width - indicatorWidth, indicatorWidth);
                end = indicatorWidth;
            }
            else
            {
                Place(_indicator, 0, indicatorWidth);
                start = indicatorWidth;
            }
        }

        if (_close.IsVisible)
        {
            if (_tab.IsInverted)
            {
                Place(_close, 0, closeWidth);
                start = closeWidth;
            }
            else
            {
                Place(_close, width - closeWidth, closeWidth);
                if (_tab.IsTitleInverted)
                {
                    end = closeWidth;
                }
            }
        }

        var centerWidth = Math.Min(width - start - end, iconWidth + titleWidth);
        var centerX = Math.Clamp(Math.Floor((width - centerWidth) / 2), start, Math.Max(start, width - centerWidth - end));
        _tab.CloseOverlap = !_tab.IsInverted && !_tab.IsTitleInverted && _title.IsVisible && _close.IsVisible
            && centerX + centerWidth > width - closeWidth;

        var attentionWidth = Math.Max(NaturalWidth(_attention), _tab.AttentionWidth(centerWidth));
        Place(_attention, Math.Floor((width - attentionWidth) / 2), attentionWidth);

        if (icon.IsVisible)
        {
            Place(icon, centerX, iconWidth);
            centerX += iconWidth;
            centerWidth -= iconWidth;
        }

        if (_title.IsVisible)
        {
            Place(_title, centerX, centerWidth);
        }

        UpdateTitleFade(width);
        return finalSize;
    }

    // adw_tab_snapshot: where the close button covers the title, the title fades out under it as the button shows.
    private void UpdateTitleFade(double width)
    {
        if (!_tab.CloseOverlap || _close.Opacity <= 0)
        {
            return;
        }

        var offset = _close.Bounds.Width + _title.Margin.Right;
        var titleX = _title.Bounds.X;
        var fadeEnd = width - offset - titleX;
        var fadeStart = fadeEnd - TitleFadeWidth;
        if (_title.Bounds.Width <= 0)
        {
            return;
        }

        var brush = new LinearGradientBrush { StartPoint = new RelativePoint(0, 0, RelativeUnit.Absolute), EndPoint = new RelativePoint(_title.Bounds.Width, 0, RelativeUnit.Absolute) };
        var transparent = Color.FromArgb((byte)Math.Round(255 * (1 - _close.Opacity)), 0, 0, 0);
        brush.GradientStops.Add(new GradientStop(Colors.Black, 0));
        brush.GradientStops.Add(new GradientStop(Colors.Black, Math.Clamp(fadeStart / _title.Bounds.Width, 0, 1)));
        brush.GradientStops.Add(new GradientStop(transparent, Math.Clamp(fadeEnd / _title.Bounds.Width, 0, 1)));
        brush.GradientStops.Add(new GradientStop(transparent, 1));
        _title.OpacityMask = brush;
    }
}
