using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Avalonia.Controls;

namespace AvaAdwaita.Animation;

/// <summary>
/// Animation frames for the theme's animations, at most one per <see cref="MinInterval"/> for each window.
/// </summary>
/// <remarks>
/// <see cref="TopLevel.RequestAnimationFrame"/> calls back once the last frame's commit is processed. Avalonia's
/// Wayland backend processes a commit as soon as it arrives, not on the compositor's frame callback, so an
/// animation that asks for the next frame from each callback runs as fast as the CPU allows. Like GTK's frame
/// clock, this hands out frames no faster than a display shows them; a frame that comes sooner commits nothing,
/// so Avalonia's animation timer paces the next one.
/// </remarks>
internal static class FrameClock
{
    /// <summary>The shortest time between frames: a 144 Hz display's.</summary>
    public static readonly TimeSpan MinInterval = TimeSpan.FromSeconds(1.0 / 144);

    private static readonly ConditionalWeakTable<TopLevel, Pacer> s_pacers = new();

    /// <summary>Calls <paramref name="callback"/> on the next frame of <paramref name="top"/>.</summary>
    public static void Request(TopLevel top, Action<TimeSpan> callback) =>
        s_pacers.GetValue(top, t => new Pacer(t)).Request(callback);

    private sealed class Pacer(TopLevel top)
    {
        private readonly List<Action<TimeSpan>> _pending = new();
        private readonly List<Action<TimeSpan>> _running = new();
        private long _last;
        private bool _scheduled;

        public void Request(Action<TimeSpan> callback)
        {
            _pending.Add(callback);
            Schedule();
        }

        private void Schedule()
        {
            if (_scheduled)
            {
                return;
            }

            _scheduled = true;
            top.RequestAnimationFrame(OnFrame);
        }

        private void OnFrame(TimeSpan time)
        {
            if (DateTime.UtcNow.Ticks - _last < MinInterval.Ticks)
            {
                // Too soon after the last frame: ask for the next one without drawing. A frame that commits nothing is
                // paced by Avalonia's own animation timer.
                top.RequestAnimationFrame(OnFrame);
                return;
            }

            _scheduled = false;
            _last = DateTime.UtcNow.Ticks;
            _running.AddRange(_pending);
            _pending.Clear();
            foreach (var callback in _running)
            {
                callback(time);
            }

            _running.Clear();
        }
    }
}
