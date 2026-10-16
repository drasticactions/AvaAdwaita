using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace AvaAdwaita.Animation;

/// <summary>
/// A page transition driven by libadwaita's spring. <see cref="AdwSlideMode.Cover"/> is AdwNavigationView's: the
/// new page slides in over the old one, which stays where it is, and slides back out when popped.
/// <see cref="AdwSlideMode.Slide"/> is AdwCarousel's: both pages move. Without animations it switches at once.
/// </summary>
/// <remarks>Follows adw_navigation_view_size_allocate and adw_navigation_view_snapshot (adw-navigation-view.c).</remarks>
public class AdwSpringSlide : IPageTransition
{
    /// <summary>The spring's damping ratio: 1 is critically damped.</summary>
    public double DampingRatio { get; set; } = 1;

    /// <summary>The spring's mass.</summary>
    public double Mass { get; set; } = 1;

    /// <summary>The spring's stiffness.</summary>
    public double Stiffness { get; set; } = 1000;

    /// <summary>Whether the new page covers the old one or both move.</summary>
    public AdwSlideMode Mode { get; set; } = AdwSlideMode.Cover;

    /// <summary>The axis the pages move along.</summary>
    public PageSlide.SlideAxis Orientation { get; set; } = PageSlide.SlideAxis.Horizontal;

    /// <inheritdoc/>
    public async Task Start(Visual? from, Visual? to, bool forward, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        if (to is not null)
        {
            to.IsVisible = true;
        }

        var parent = (to ?? from)?.GetVisualParent();
        var top = parent is null ? null : TopLevel.GetTopLevel(parent);
        var horizontal = Orientation == PageSlide.SlideAxis.Horizontal;
        var distance = parent is null ? 0 : horizontal ? parent.Bounds.Width : parent.Bounds.Height;
        if (top is not null && distance > 0 && AdwaitaEnvironment.State.AnimationsEnabled && !AdwaitaEnvironment.TransitionsSuppressed)
        {
            var (damping, mass, stiffness) = SpringSolver.Params(DampingRatio, Mass, Stiffness);
            var spring = new SpringSolver(0, 1, damping, mass, stiffness);
            var duration = spring.EstimatedDuration();
            var start = -1L;
            var done = new TaskCompletionSource();
            using var registration = cancellationToken.Register(() => done.TrySetResult());

            void Frame(TimeSpan _)
            {
                if (done.Task.IsCompleted)
                {
                    return;
                }

                var now = Environment.TickCount64;
                start = start < 0 ? now : start;
                var t = now - start;
                Place(from, to, forward, Math.Clamp(spring.Value(Math.Min(t, duration)), 0, 1.5), distance, horizontal);
                if (t >= duration)
                {
                    done.TrySetResult();
                }
                else
                {
                    FrameClock.Request(top, Frame);
                }
            }

            FrameClock.Request(top, Frame);
            await done.Task;
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        if (from is not null)
        {
            from.IsVisible = false;
            Reset(from);
        }

        if (to is not null)
        {
            Reset(to);
        }
    }

    private static void Reset(Visual v)
    {
        v.RenderTransform = null;
        v.Clip = null;
    }

    // progress runs 0 → 1 as the transition goes from the old page to the new one.
    private void Place(Visual? from, Visual? to, bool forward, double progress, double distance, bool horizontal)
    {
        if (Mode == AdwSlideMode.Slide)
        {
            var sign = forward ? 1 : -1;
            Translate(from, -sign * progress * distance, horizontal);
            Translate(to, sign * (1 - progress) * distance, horizontal);
            return;
        }

        // A push slides the new page in over the old; a pop slides the old page off the new.
        var (moving, still, offset) = forward
            ? (to, from, Math.Round((1 - progress) * distance))
            : (from, to, Math.Round(progress * distance));
        Translate(moving, offset, horizontal);
        if (still is not null)
        {
            Translate(still, 0, horizontal);
            var size = still.Bounds.Size;
            still.Clip = new RectangleGeometry(horizontal ? new Rect(0, 0, offset, size.Height) : new Rect(0, 0, size.Width, offset));
        }
    }

    private static void Translate(Visual? v, double by, bool horizontal)
    {
        if (v is null)
        {
            return;
        }

        v.RenderTransform = horizontal ? new TranslateTransform(by, 0) : new TranslateTransform(0, by);
    }
}
