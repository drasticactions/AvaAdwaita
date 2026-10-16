using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using AvaAdwaita.Animation;
using AvaAdwaita.CssEngine.Computed;
using AvaAdwaita.CssEngine.Properties;
using AvaAdwaita.CssEngine.Values;
using AvaAdwaita.Nodes;

namespace AvaAdwaita.Presenters;

/// <summary>
/// Runs a node's CSS transitions and animations. When a new computed style changes an animatable property
/// that <c>transition-property</c> covers, the shown value moves from the current one to the new one over the
/// duration, with the timing function. An <c>animation-name</c> that names <c>@keyframes</c> runs those
/// keyframes over the shown style. Both advance one frame at a time.
/// </summary>
/// <remarks>Follows gtk_css_animated_style_new (gtkcssanimatedstyle.c), gtkcsstransition.c and gtkcssanimation.c.</remarks>
internal sealed class TransitionDriver
{
    private readonly Visual _owner;
    private readonly CssNode? _node;
    private readonly Action<ComputedStyle> _show;
    private readonly List<Running> _running = new();
    private readonly List<KeyframeAnimation> _animations = new();
    private readonly CssStyleInterner _frameInterner = new();
    private ComputedStyle? _target;
    private object?[]? _shown;
    private bool _frameRequested;

    public TransitionDriver(Visual owner, Action<ComputedStyle> show, CssNode? node = null)
    {
        _owner = owner;
        _show = show;
        _node = node;
    }

    /// <summary>True while transitions or animations run.</summary>
    public bool IsRunning => _running.Count > 0 || _animations.Count > 0;

    private sealed record Running(int Id, object From, object To, double Start, double Delay, double Duration, CssEasing Easing);

    private sealed record Keyframe(double Offset, ComputedStyle? Style, HashSet<int> Declared);

    private sealed record KeyframeAnimation(string Name, double Start, double Delay, double Duration, double Iterations, string Direction, string FillMode, CssEasing Easing, List<Keyframe> Frames);

    /// <summary>Shows <paramref name="next"/>, transitioning from what is shown now and running its animations.</summary>
    public void Apply(ComputedStyle? previous, ComputedStyle next)
    {
        _target = next;
        var live = !AdwaitaEnvironment.TransitionsSuppressedFor(_owner) && TopLevel.GetTopLevel(_owner) is not null;
        var now = Now();
        UpdateAnimations(next, live, now);
        if (previous is null || !live)
        {
            _running.Clear();
            _shown = null;
            Tick(now);
            RequestFrameIfRunning();
            return;
        }

        var shown = _shown ?? Snapshot(previous);
        var targetValues = Snapshot(next);
        var t = next.Transition;
        var all = CssPropertyTable.All;
        var keep = new List<Running>();
        for (var id = 0; id < all.Length; id++)
        {
            var prop = all[id];
            if (!prop.Animated)
            {
                continue;
            }

            var index = TransitionIndex(t.Property, prop);
            var current = shown[id];
            var target = targetValues[id];
            var running = _running.Find(r => r.Id == id);
            if (index < 0 || Equals(current, target) || current is null || target is null)
            {
                continue;
            }

            var duration = t.Duration.Count > 0 ? t.Duration.Cycle(index) : 0;
            var delay = t.Delay.Count > 0 ? t.Delay.Cycle(index) : 0;
            if (duration + delay <= 0)
            {
                continue;
            }

            if (running is not null && Equals(running.To, target))
            {
                keep.Add(running);
                continue;
            }

            if (prop.Kind.Interpolate(current, target, 0) is null)
            {
                continue;
            }

            var easing = t.TimingFunction.Count > 0 ? t.TimingFunction.Cycle(index) : CssEasing.Ease;
            keep.Add(new Running(id, current, target, now, delay, duration, easing));
        }

        _running.Clear();
        _running.AddRange(keep);
        _shown = _running.Count > 0 ? shown : null;
        Tick(now);
        RequestFrameIfRunning();
    }

    private static object?[] Snapshot(ComputedStyle s)
    {
        var v = new object?[CssPropertyTable.Count];
        s.CopyTo(v);
        return v;
    }

    private static int TransitionIndex(CssList<string> properties, CssProperty prop)
    {
        for (var i = 0; i < properties.Count; i++)
        {
            var name = properties[i];
            if (name == "all" || name == prop.Name)
            {
                return i;
            }

            if (name == "none")
            {
                continue;
            }

            if (CssPropertyTable.LookupShorthand(name) is { } sh && Array.IndexOf(sh.Longhands, prop.Id) >= 0)
            {
                return i;
            }
        }

        return -1;
    }

    // Keeps the animations the style still names, and starts the ones it newly names.
    private void UpdateAnimations(ComputedStyle next, bool live, double now)
    {
        var a = next.Animation;
        var names = a.Name;
        if (!live || _node is null)
        {
            _animations.Clear();
            return;
        }

        var kept = new List<KeyframeAnimation>();
        for (var i = 0; i < names.Count; i++)
        {
            var name = names[i];
            if (name == "none")
            {
                continue;
            }

            if (_animations.Find(x => x.Name == name) is { } existing)
            {
                kept.Add(existing);
                continue;
            }

            if (StartAnimation(name, i, a, now) is { } started)
            {
                kept.Add(started);
            }
        }

        _animations.Clear();
        _animations.AddRange(kept);
    }

    private KeyframeAnimation? StartAnimation(string name, int index, ComputedStyle.AnimationValues a, double now)
    {
        var node = _node!;
        var engine = node.Engine ?? AdwaitaEnvironment.Engine;
        var resolver = engine.ResolverFor(node.MediaOverride ?? engine.Media);
        if (resolver.Styles.LookupKeyframes(name) is not { } keyframes)
        {
            return null;
        }

        var frames = new List<Keyframe>();
        foreach (var (offset, declarations) in keyframes.Frames.OrderBy(f => f.Offset))
        {
            var style = resolver.ResolveKeyframe(node, node.Parent?.Style, declarations);
            var declared = declarations.Where(d => !d.IsCustom).Select(d => d.PropertyId).ToHashSet();
            frames.Add(new Keyframe(offset, style, declared));
        }

        // A missing first or last keyframe takes the underlying value.
        if (frames.Count == 0 || frames[0].Offset > 0)
        {
            frames.Insert(0, new Keyframe(0, null, []));
        }

        if (frames[^1].Offset < 1)
        {
            frames.Add(new Keyframe(1, null, []));
        }

        return new KeyframeAnimation(
            name,
            now,
            a.Delay.Count > 0 ? a.Delay.Cycle(index) : 0,
            a.Duration.Count > 0 ? a.Duration.Cycle(index) : 0,
            a.IterationCount.Count > 0 ? a.IterationCount.Cycle(index) : 1,
            a.Direction.Count > 0 ? a.Direction.Cycle(index) : "normal",
            a.FillMode.Count > 0 ? a.FillMode.Cycle(index) : "none",
            a.TimingFunction.Count > 0 ? a.TimingFunction.Cycle(index) : CssEasing.Ease,
            frames);
    }

    private static double Now() => Environment.TickCount64;

    private void RequestFrameIfRunning()
    {
        if (!IsRunning || _frameRequested || TopLevel.GetTopLevel(_owner) is not { } top)
        {
            return;
        }

        _frameRequested = true;
        FrameClock.Request(top, _ =>
        {
            _frameRequested = false;
            Tick(Now());
            RequestFrameIfRunning();
        });
    }

    private void Tick(double now)
    {
        if (_target is not { } target)
        {
            return;
        }

        var values = Snapshot(target);
        var all = CssPropertyTable.All;
        for (var i = _running.Count - 1; i >= 0; i--)
        {
            var r = _running[i];
            var elapsed = now - r.Start - r.Delay;
            var progress = r.Duration <= 0 ? 1 : Math.Clamp(elapsed / r.Duration, 0, 1);
            if (elapsed < 0)
            {
                values[r.Id] = r.From;
                continue;
            }

            if (progress >= 1)
            {
                _running.RemoveAt(i);
                continue;
            }

            values[r.Id] = all[r.Id].Kind.Interpolate(r.From, r.To, r.Easing.Transform(progress)) ?? r.To;
        }

        if (_running.Count > 0)
        {
            _shown = (object?[])values.Clone();
        }
        else
        {
            _shown = null;
        }

        var animated = false;
        for (var i = _animations.Count - 1; i >= 0; i--)
        {
            switch (AnimationProgress(_animations[i], now))
            {
                case null:
                    _animations.RemoveAt(i);
                    break;
                case { } p:
                    ApplyKeyframes(_animations[i], p, values);
                    animated = true;
                    break;
            }
        }

        _frameInterner.Clear();
        if (_running.Count == 0 && !animated)
        {
            _show(target);
            return;
        }

        _show(ComputedStyle.Create(values, null, target.Custom, target.CurrentColorDependents, _frameInterner));
    }

    // The iteration progress, 0 to 1 within the keyframes, or null when the animation is over and does not fill.
    private static double? AnimationProgress(KeyframeAnimation a, double now)
    {
        var t = now - a.Start - a.Delay;
        var backwards = a.FillMode is "backwards" or "both";
        var forwards = a.FillMode is "forwards" or "both";
        if (a.Duration <= 0)
        {
            return forwards ? Directed(a, a.Iterations, 1) : null;
        }

        if (t < 0)
        {
            return backwards ? Directed(a, 0, 0) : double.NaN;
        }

        var total = a.Duration * a.Iterations;
        if (t >= total)
        {
            if (!forwards)
            {
                return null;
            }

            var lastIteration = Math.Max(0, Math.Ceiling(a.Iterations) - 1);
            var end = a.Iterations - Math.Floor(a.Iterations);
            return Directed(a, lastIteration, end == 0 ? 1 : end);
        }

        var iteration = Math.Floor(t / a.Duration);
        return Directed(a, iteration, (t - (iteration * a.Duration)) / a.Duration);
    }

    private static double Directed(KeyframeAnimation a, double iteration, double p)
    {
        var odd = ((long)iteration & 1) == 1;
        var reverse = a.Direction switch
        {
            "reverse" => true,
            "alternate" => odd,
            "alternate-reverse" => !odd,
            _ => false,
        };
        return reverse ? 1 - p : p;
    }

    private static void ApplyKeyframes(KeyframeAnimation a, double p, object?[] values)
    {
        if (double.IsNaN(p))
        {
            return;
        }

        var frames = a.Frames;
        var k = 0;
        while (k < frames.Count - 2 && p > frames[k + 1].Offset)
        {
            k++;
        }

        var from = frames[k];
        var to = frames[k + 1];
        var span = to.Offset - from.Offset;
        var local = span <= 0 ? 1 : Math.Clamp((p - from.Offset) / span, 0, 1);
        var eased = a.Easing.Transform(local);
        var all = CssPropertyTable.All;
        foreach (var id in from.Declared.Union(to.Declared))
        {
            var start = from.Declared.Contains(id) ? from.Style!.GetValue(id) : values[id];
            var end = to.Declared.Contains(id) ? to.Style!.GetValue(id) : values[id];
            if (start is null || end is null)
            {
                continue;
            }

            values[id] = all[id].Kind.Interpolate(start, end, eased) ?? (eased < 0.5 ? start : end);
        }
    }
}
