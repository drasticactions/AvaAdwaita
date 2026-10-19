using System;
using Avalonia;
using Avalonia.Controls;
using AvaAdwaita.Animation;

namespace AvaAdwaita.Controls.Primitives;

/// <summary>
/// A value that moves to a target over a duration with an easing curve, a frame at a time, as AdwTimedAnimation. It
/// jumps when the desktop turns animations off or the owner's window suppresses transitions.
/// </summary>
internal sealed class TimedAnimation(Visual owner, double durationMs, AdwEasing easing, Action<double> apply, Action? done = null)
{
    private double _from;
    private double _to;
    private long _start = -1;
    private int _generation;

    /// <summary>The value shown now.</summary>
    public double Value { get; private set; }

    /// <summary>How long the animation takes, in milliseconds.</summary>
    public double Duration { get; set; } = durationMs;

    /// <summary>Whether the value is moving.</summary>
    public bool IsRunning { get; private set; }

    /// <summary>Moves from the value shown now to <paramref name="to"/>.</summary>
    public void Animate(double to)
    {
        _generation++;
        if (to == Value && !IsRunning)
        {
            done?.Invoke();
            return;
        }

        var top = TopLevel.GetTopLevel(owner);
        if (top is null || Duration <= 0 || !AdwaitaEnvironment.State.AnimationsEnabled || AdwaitaEnvironment.TransitionsSuppressedFor(owner))
        {
            Jump(to);
            return;
        }

        _from = Value;
        _to = to;
        _start = -1;
        IsRunning = true;
        var generation = _generation;
        void Frame(TimeSpan _)
        {
            if (generation != _generation)
            {
                return;
            }

            var now = Environment.TickCount64;
            _start = _start < 0 ? now : _start;
            var t = Math.Min(1, (now - _start) / Duration);
            Show(_from + ((_to - _from) * Easing.Ease(easing, t)));
            if (t < 1)
            {
                FrameClock.Request(top, Frame);
            }
            else
            {
                _start = -1;
                IsRunning = false;
                done?.Invoke();
            }
        }

        FrameClock.Request(top, Frame);
    }

    /// <summary>Shows <paramref name="to"/> at once, stopping a running animation; a skipped animation is done too.</summary>
    public void Jump(double to)
    {
        _generation++;
        _start = -1;
        IsRunning = false;
        Show(to);
        done?.Invoke();
    }

    /// <summary>Finishes a running animation at once, as adw_animation_skip.</summary>
    public void Skip()
    {
        if (IsRunning)
        {
            Jump(_to);
        }
    }

    /// <summary>Shows <paramref name="value"/> at once to start from, without finishing.</summary>
    public void Reset(double value)
    {
        _generation++;
        _start = -1;
        IsRunning = false;
        Show(value);
    }

    /// <summary>Stops where the value is, without finishing.</summary>
    public void Pause()
    {
        _generation++;
        _start = -1;
        IsRunning = false;
    }

    private void Show(double value)
    {
        Value = value;
        apply(value);
    }
}
