using System;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;

namespace AvaAdwaita.Presenters;

/// <summary>
/// Shows a scroll viewer's overlay scroll bars as GtkScrolledWindow shows its indicators: hidden at rest, faded in
/// by pointer motion or scrolling, and faded out 2 s after they appeared or the view last scrolled, unless the
/// pointer is over one. Classic scroll bars (<see cref="ScrollViewer.AllowAutoHide"/> off) always show.
/// </summary>
/// <remarks>Follows indicator_start_fade and maybe_hide_indicator (gtkscrolledwindow.c).</remarks>
public static class OverlayIndicators
{
    /// <summary>Turns the behavior on for a scroll viewer whose template has the two scroll bar parts.</summary>
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<ScrollViewer, bool>("IsEnabled", typeof(OverlayIndicators));

    private static readonly AttachedProperty<Indicators?> IndicatorsProperty =
        AvaloniaProperty.RegisterAttached<ScrollViewer, Indicators?>("Indicators", typeof(OverlayIndicators));

    private static readonly TimeSpan FadeOutDelay = TimeSpan.FromMilliseconds(2000);
    private static readonly TimeSpan FadeDuration = TimeSpan.FromMilliseconds(1000);
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMilliseconds(500);

    static OverlayIndicators()
    {
        IsEnabledProperty.Changed.AddClassHandler<ScrollViewer>((sv, e) =>
        {
            sv.GetValue(IndicatorsProperty)?.Dispose();
            sv.SetValue(IndicatorsProperty, e.NewValue is true ? new Indicators(sv) : null);
        });
    }

    /// <summary>Gets <see cref="IsEnabledProperty"/>.</summary>
    public static bool GetIsEnabled(ScrollViewer sv) => sv.GetValue(IsEnabledProperty);

    /// <summary>Sets <see cref="IsEnabledProperty"/>.</summary>
    public static void SetIsEnabled(ScrollViewer sv, bool value) => sv.SetValue(IsEnabledProperty, value);

    private sealed class Indicators : IDisposable
    {
        private readonly ScrollViewer _sv;
        private readonly DispatcherTimer _timer;
        private ScrollBar? _h;
        private ScrollBar? _v;
        private bool _shown;
        private long _lastShown;

        public Indicators(ScrollViewer sv)
        {
            _sv = sv;
            _timer = new DispatcherTimer { Interval = CheckInterval };
            _timer.Tick += (_, _) => MaybeHide();
            sv.TemplateApplied += OnTemplateApplied;
            sv.PointerMoved += OnPointerMoved;
            sv.ScrollChanged += OnScrollChanged;
            sv.PropertyChanged += OnPropertyChanged;
            Find();
        }

        public void Dispose()
        {
            _timer.Stop();
            _sv.TemplateApplied -= OnTemplateApplied;
            _sv.PointerMoved -= OnPointerMoved;
            _sv.ScrollChanged -= OnScrollChanged;
            _sv.PropertyChanged -= OnPropertyChanged;
        }

        private void OnTemplateApplied(object? sender, TemplateAppliedEventArgs e) => Find();

        private void OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == ScrollViewer.AllowAutoHideProperty)
            {
                Apply(immediate: true);
            }
        }

        private void Find()
        {
            _h = _sv.FindDescendantScrollBar("PART_HorizontalScrollBar");
            _v = _sv.FindDescendantScrollBar("PART_VerticalScrollBar");
            Apply(immediate: true);
        }

        // Pointer motion shows the indicators; it does not keep them up (indicator_start_fade returns early).
        private void OnPointerMoved(object? sender, Avalonia.Input.PointerEventArgs e)
        {
            if (!_shown)
            {
                Show();
            }
        }

        private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
        {
            if (e.OffsetDelta != default)
            {
                Show();
            }
        }

        private void Show()
        {
            _lastShown = Environment.TickCount64;
            if (!_shown)
            {
                _shown = true;
                Apply(immediate: false);
            }

            _timer.Start();
        }

        private void MaybeHide()
        {
            var over = _h?.IsPointerOver == true || _v?.IsPointerOver == true;
            if (Environment.TickCount64 - _lastShown >= FadeOutDelay.TotalMilliseconds && !over)
            {
                _shown = false;
                _timer.Stop();
                Apply(immediate: false);
            }
        }

        private void Apply(bool immediate)
        {
            var overlay = _sv.AllowAutoHide;
            var opacity = !overlay || _shown ? 1.0 : 0.0;
            var animate = !immediate && overlay && AdwaitaEnvironment.State.AnimationsEnabled && !AdwaitaEnvironment.TransitionsSuppressedFor(_sv);
            foreach (var bar in new[] { _h, _v })
            {
                if (bar is null)
                {
                    continue;
                }

                bar.Transitions = animate
                    ? [new DoubleTransition { Property = Visual.OpacityProperty, Duration = FadeDuration, Easing = new CubicEaseOut() }]
                    : null;
                bar.Opacity = opacity;
            }
        }
    }

    private static ScrollBar? FindDescendantScrollBar(this ScrollViewer sv, string name)
    {
        foreach (var v in Avalonia.VisualTree.VisualExtensions.GetVisualDescendants(sv))
        {
            if (v is ScrollBar b && b.Name == name && ReferenceEquals(b.TemplatedParent, sv))
            {
                return b;
            }
        }

        return null;
    }
}
