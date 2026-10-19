using System;
using Avalonia;
using Avalonia.Controls;
using AvaAdwaita.Animation;

namespace AvaAdwaita.Controls.Primitives;

/// <summary>
/// A value that moves to a target on a damped spring, a frame at a time, as AdwSpringAnimation. It jumps when the
/// desktop turns animations off or the owner's window suppresses transitions.
/// </summary>
internal sealed class SpringAnimation(Visual owner, double dampingRatio, double mass, double stiffness, Action<double> apply, Action? done = null)
{
    private int _generation;
    private bool _running;

    /// <summary>The value shown now.</summary>
    public double Value { get; private set; }

    /// <summary>Whether the spring is moving.</summary>
    public bool IsRunning => _running;

    /// <summary>Moves from the value shown now to <paramref name="to"/>; clamped, it stops at the target instead of overshooting.</summary>
    public void Animate(double to, bool clamp = false, double velocity = 0)
    {
        _generation++;
        var top = TopLevel.GetTopLevel(owner);
        if (top is null || !AdwaitaEnvironment.State.AnimationsEnabled || AdwaitaEnvironment.TransitionsSuppressedFor(owner) || Value == to)
        {
            Jump(to);
            return;
        }

        var (damping, m, k) = SpringSolver.Params(dampingRatio, mass, stiffness);
        var spring = new SpringSolver(Value, to, damping, m, k, initialVelocity: velocity, clamp: clamp);
        var duration = spring.EstimatedDuration();
        var generation = _generation;
        var start = -1L;
        _running = true;
        void Frame(TimeSpan _)
        {
            if (generation != _generation)
            {
                return;
            }

            var now = Environment.TickCount64;
            start = start < 0 ? now : start;
            var t = now - start;
            if (t >= duration)
            {
                Finish(to);
                return;
            }

            Show(spring.Value(t));
            FrameClock.Request(top, Frame);
        }

        FrameClock.Request(top, Frame);
    }

    /// <summary>Stops where it is, without finishing, and shows <paramref name="value"/>, as a swipe moves it.</summary>
    public void Set(double value)
    {
        _generation++;
        _running = false;
        Show(value);
    }

    /// <summary>Shows <paramref name="to"/> at once, stopping a running animation.</summary>
    public void Jump(double to)
    {
        _generation++;
        Finish(to);
    }

    // A skipped animation is done too, as adw_animation_skip emits done.
    private void Finish(double to)
    {
        _running = false;
        Show(to);
        done?.Invoke();
    }

    private void Show(double value)
    {
        Value = value;
        apply(value);
    }
}
