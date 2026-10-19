using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace AvaAdwaita.Controls.Primitives;

/// <summary>
/// A vertical drag of an element between its closed (0) and open (1) positions, as AdwSwipeTracker does it for a
/// pointer: it takes the drag once it has moved <see cref="DragThresholdDistance"/> from where it started in the swipe
/// area, follows it with overshoot past the ends it allows, and ends at the position its speed and place point to.
/// Presses that do not become drags, as a button's click, are left alone.
/// </summary>
internal sealed class SwipeTracker
{
    // adw-swipe-tracker.c.
    private const double EventHistoryThresholdMs = 150;
    private const double VelocityThresholdTouch = 0.3;
    private const double DecelerationTouch = 0.998;
    private const double VelocityCurveThreshold = 2;
    private const double DecelerationParabolaMultiplier = 0.35;
    private const double DragThresholdDistance = 16;
    private const double Epsilon = 0.005;
    private const double OvershootDistanceMultiplier = 0.1;

    private static readonly double[] s_points = [0, 1];

    private readonly InputElement _owner;
    private readonly List<(double Delta, ulong Time)> _history = [];
    private State _state;
    private Point _start;
    private double _prevOffset;
    private double _initialProgress;
    private double _progress;
    private IPointer? _pointer;

    /// <summary>Tracks drags on <paramref name="owner"/>, seen before its children handle them.</summary>
    public SwipeTracker(InputElement owner)
    {
        _owner = owner;
        owner.AddHandler(InputElement.PointerPressedEvent, OnPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
        owner.AddHandler(InputElement.PointerMovedEvent, OnMoved, RoutingStrategies.Tunnel, handledEventsToo: true);
        owner.AddHandler(InputElement.PointerReleasedEvent, OnReleased, RoutingStrategies.Tunnel, handledEventsToo: true);
        owner.AddHandler(InputElement.PointerCaptureLostEvent, OnCaptureLost, RoutingStrategies.Direct | RoutingStrategies.Bubble, handledEventsToo: true);
    }

    private enum State
    {
        None,
        Pressed,
        Pending,
        Scrolling,
        Rejected,
    }

    /// <summary>Whether drags are tracked at all.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>Whether a mouse drags, not only touch and pens.</summary>
    public bool AllowMouseDrag { get; set; }

    /// <summary>Whether a drag may go a little past 0, resisting.</summary>
    public bool LowerOvershoot { get; set; }

    /// <summary>Whether a drag may go a little past 1, resisting.</summary>
    public bool UpperOvershoot { get; set; }

    /// <summary>The length that moves the progress from 0 to 1.</summary>
    public Func<double> Distance { get; set; } = () => 1;

    /// <summary>The progress where a drag starts.</summary>
    public Func<double> Progress { get; set; } = () => 0;

    /// <summary>Where a press can start a drag, in the owner's coordinates.</summary>
    public Func<Rect> SwipeArea { get; set; } = () => default;

    /// <summary>Asked when a drag starts: whether it may go on (the swipeable's prepare and its checks).</summary>
    public Func<bool> Prepare { get; set; } = () => true;

    /// <summary>Raised when the drag is taken.</summary>
    public Action? Begin { get; set; }

    /// <summary>Raised with the progress as the drag moves.</summary>
    public Action<double>? Update { get; set; }

    /// <summary>Raised with the velocity, in progress per millisecond, and the progress to end at.</summary>
    public Action<double, double>? End { get; set; }

    /// <summary>Whether a drag is being followed.</summary>
    public bool IsSwiping => _state == State.Scrolling;

    private void OnPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_state != State.None || !IsEnabled || !e.GetCurrentPoint(_owner).Properties.IsLeftButtonPressed
            || (e.Pointer.Type == PointerType.Mouse && !AllowMouseDrag))
        {
            return;
        }

        _state = State.Pressed;
        _start = e.GetPosition(_owner);
        _prevOffset = 0;
        _history.Clear();
        _pointer = e.Pointer;
    }

    // drag_update_cb.
    private void OnMoved(object? sender, PointerEventArgs e)
    {
        if (_state is State.None or State.Rejected || e.Pointer != _pointer)
        {
            return;
        }

        var position = e.GetPosition(_owner);
        var (offsetX, offsetY) = (position.X - _start.X, position.Y - _start.Y);

        // Dragging up opens.
        var offset = -offsetY;
        var delta = offset - _prevOffset;
        _prevOffset = offset;
        Append(delta, e.Timestamp);

        if (_state == State.Pressed)
        {
            if (!Prepare())
            {
                _state = State.Rejected;
                return;
            }

            _initialProgress = Progress();
            _progress = _initialProgress;
            _state = State.Pending;
        }

        if (_state == State.Pending)
        {
            if (Math.Sqrt((offsetX * offsetX) + (offsetY * offsetY)) < DragThresholdDistance)
            {
                return;
            }

            var area = SwipeArea();
            if (!area.Contains(_start) && !area.Contains(position))
            {
                _state = State.Rejected;
                return;
            }

            var overshootingLower = offset < 0 && _progress <= 0;
            var overshootingUpper = offset > 0 && _progress >= 1;
            if ((!LowerOvershoot && overshootingLower) || (!UpperOvershoot && overshootingUpper))
            {
                _state = State.Rejected;
                return;
            }

            // gesture_begin: the drag is taken from what the press was on, as a button.
            _state = State.Scrolling;
            _prevOffset = offset;
            e.Pointer.Capture(_owner);
            Begin?.Invoke();
        }

        if (_state == State.Scrolling)
        {
            e.Handled = true;
            Move(delta / Distance());
        }
    }

    private void OnReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.Pointer != _pointer)
        {
            return;
        }

        var swiping = _state == State.Scrolling;
        if (swiping)
        {
            e.Handled = true;
            Trim(e.Timestamp);
            Finish(cancelled: false);
            e.Pointer.Capture(null);
        }

        Reset();
    }

    private void OnCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (_state == State.Scrolling && e.Pointer == _pointer)
        {
            Finish(cancelled: true);
            Reset();
        }
    }

    private void Reset()
    {
        _state = State.None;
        _pointer = null;
        _history.Clear();
    }

    private void Append(double delta, ulong time)
    {
        Trim(time);
        _history.Add((delta, time));
    }

    private void Trim(ulong time)
    {
        var threshold = time > EventHistoryThresholdMs ? time - (ulong)EventHistoryThresholdMs : 0;
        _history.RemoveAll(r => r.Time < threshold);
    }

    // adjust_for_overshoot: the further past the end, the less it moves.
    private double AdjustForOvershoot(double amount)
    {
        var d = Distance() * OvershootDistanceMultiplier;
        return (1 - (1 / (1 + (amount * d)))) / d;
    }

    // gesture_update.
    private void Move(double delta)
    {
        _progress += delta;
        var progress = _progress;
        if (progress < 0)
        {
            progress = LowerOvershoot ? -AdjustForOvershoot(-progress) : _progress = 0;
        }

        if (progress > 1)
        {
            progress = UpperOvershoot ? 1 + AdjustForOvershoot(progress - 1) : _progress = 1;
        }

        Update?.Invoke(progress);
    }

    // calculate_velocity.
    private double Velocity()
    {
        if (_history.Count < 2 || _history[0].Time == _history[^1].Time)
        {
            return 0;
        }

        var total = 0.0;
        for (var i = 1; i < _history.Count; i++)
        {
            total += _history[i].Delta;
        }

        var velocity = total / (_history[^1].Time - _history[0].Time);
        if (_progress <= 0)
        {
            if (LowerOvershoot && _progress < 0)
            {
                velocity *= AdjustForOvershoot(-_progress) / -_progress;
            }
            else if (velocity < 0)
            {
                velocity = 0;
            }
        }

        if (_progress >= 1)
        {
            if (UpperOvershoot && _progress > 1)
            {
                velocity *= AdjustForOvershoot(_progress - 1) / (_progress - 1);
            }
            else if (velocity > 0)
            {
                velocity = 0;
            }
        }

        return velocity;
    }

    // get_end_progress, for touch and pointer drags.
    private double EndProgress(double velocity, bool cancelled)
    {
        if (cancelled)
        {
            return Math.Round(_progress);
        }

        if (Math.Abs(velocity) < VelocityThresholdTouch)
        {
            return Math.Clamp(s_points[Closest(_progress)], 0, 1);
        }

        var slope = DecelerationTouch / (1 - DecelerationTouch) / 1000;
        double pos;
        if (Math.Abs(velocity) > VelocityCurveThreshold)
        {
            var c = slope / 2 / DecelerationParabolaMultiplier;
            var x = Math.Abs(velocity) - VelocityCurveThreshold + c;
            pos = (DecelerationParabolaMultiplier * x * x) - (DecelerationParabolaMultiplier * c * c) + (slope * VelocityCurveThreshold);
        }
        else
        {
            pos = Math.Abs(velocity) * slope;
        }

        pos = Math.Clamp((pos * Math.Sign(velocity)) + _progress, 0, 1);

        // find_point_for_projection: past the start point, the next one in the swipe's direction.
        var initial = Closest(_initialProgress);
        var prev = pos >= 1 - Epsilon ? 1 : 0;
        var next = pos <= Epsilon ? 0 : 1;
        if ((velocity > 0 ? prev : next) == initial)
        {
            return s_points[velocity > 0 ? next : prev];
        }

        return s_points[Closest(pos)];
    }

    private static int Closest(double pos) => Math.Abs(s_points[1] - pos) < Math.Abs(s_points[0] - pos) ? 1 : 0;

    private void Finish(bool cancelled)
    {
        var velocity = cancelled ? 0 : Velocity();
        End?.Invoke(velocity, EndProgress(velocity, cancelled));
    }
}
