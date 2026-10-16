using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using AvaAdwaita.Animation;

namespace AvaAdwaita.Presenters;

/// <summary>
/// Lays out a progress bar's <c>progress</c> node inside its trough, as GtkProgressBar allocates it. An empty
/// bar's progress has no size. An indeterminate bar shows GTK's activity block, a fifth of the trough, moving
/// back and forth as an application that pulses every 100 ms by the default step of 0.1 makes it.
/// </summary>
/// <remarks>Follows gtk_progress_bar_allocate_trough and tick_cb (gtkprogressbar.c).</remarks>
public class AdwProgressTrack : Panel
{
    /// <summary>The block's speed in trough lengths per second.</summary>
    private const double Speed = 1.0;

    /// <summary>GtkProgressBar's activity_blocks.</summary>
    private const int ActivityBlocks = 5;

    private double _pos;
    private bool _forward = true;
    private long _last;
    private bool _frameRequested;

    private ProgressBar? Bar => TemplatedParent as ProgressBar;

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (Bar is { } bar)
        {
            bar.PropertyChanged += OnBarPropertyChanged;
        }

        Tick();
    }

    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (Bar is { } bar)
        {
            bar.PropertyChanged -= OnBarPropertyChanged;
        }
    }

    private void OnBarPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == ProgressBar.IsIndeterminateProperty || e.Property == Avalonia.Controls.Primitives.RangeBase.ValueProperty || e.Property == ProgressBar.OrientationProperty)
        {
            _last = 0;
            InvalidateArrange();
            Tick();
        }
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Children.Count == 0 || Children[0] is not Control indicator || Bar is not { } bar)
        {
            return base.ArrangeOverride(finalSize);
        }

        var horizontal = bar.Orientation == Orientation.Horizontal;
        var length = horizontal ? finalSize.Width : finalSize.Height;
        var cross = horizontal ? finalSize.Height : finalSize.Width;
        if (bar.IsIndeterminate)
        {
            var min = horizontal ? indicator.MinWidth : indicator.MinHeight;
            var block = min + ((length - min) / ActivityBlocks);
            var at = _pos * (length - block);
            if (horizontal)
            {
                indicator.Width = block;
                indicator.Arrange(new Rect(at, 0, block, cross));
            }
            else
            {
                indicator.Height = block;
                indicator.Arrange(new Rect(0, at, cross, block));
            }

            Nodes.Css.SetClasses(indicator, "pulse" + (_pos <= 0 ? " left" : string.Empty) + (_pos >= 1 ? " right" : string.Empty));
            return finalSize;
        }

        // Avalonia sets the indicator's length from the value.
        var size = horizontal ? indicator.Width : indicator.Height;
        if (double.IsNaN(size) || size <= 0)
        {
            indicator.Arrange(horizontal ? new Rect(0, Math.Floor(cross / 2), 0, 0) : new Rect(Math.Floor(cross / 2), length, 0, 0));
            return finalSize;
        }

        indicator.Arrange(horizontal ? new Rect(0, 0, size, cross) : new Rect(0, length - size, cross, size));
        return finalSize;
    }

    private void Tick()
    {
        if (_frameRequested || Bar is not { IsIndeterminate: true } || TopLevel.GetTopLevel(this) is not { } top || AdwaitaEnvironment.TransitionsSuppressed && !AdwaitaEnvironment.State.AnimationsEnabled)
        {
            return;
        }

        _frameRequested = true;
        FrameClock.Request(top, _ =>
        {
            _frameRequested = false;
            var now = Environment.TickCount64;
            if (_last != 0)
            {
                var step = Speed * (now - _last) / 1000.0;
                _pos += _forward ? step : -step;
                if (_pos >= 1)
                {
                    _pos = 1;
                    _forward = false;
                }
                else if (_pos <= 0)
                {
                    _pos = 0;
                    _forward = true;
                }

                InvalidateArrange();
            }

            _last = now;
            Tick();
        });
    }
}
