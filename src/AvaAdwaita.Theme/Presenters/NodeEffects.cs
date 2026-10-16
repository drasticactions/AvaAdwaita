using System;
using Avalonia;
using Avalonia.Data;
using Avalonia.Media;
using AvaAdwaita.CssEngine.Computed;
using AvaAdwaita.CssEngine.Values;
using AvaAdwaita.Nodes;

namespace AvaAdwaita.Presenters;

/// <summary>
/// Applies a node's <c>opacity</c>, <c>filter</c> and <c>transform</c> to a visual and its children, at style
/// priority, as GTK applies them around the widget. Of the filters, <c>opacity()</c> fades the group, and the
/// first <c>blur()</c> or <c>drop-shadow()</c> becomes the visual's effect. The color filters are a matrix on the
/// computed style (<see cref="CssEngine.Computed.ComputedStyle.ColorMatrix"/>), which every presenter draws its
/// colors through, as Avalonia has no color matrix effect.
/// </summary>
/// <remarks>
/// GTK fades a widget as one group: a slider over its trough stays opaque within the switch. Avalonia's
/// <see cref="Visual.Opacity"/> multiplies each draw instead, so the opacity is an opacity mask, which Avalonia
/// composites through a layer.
/// </remarks>
internal sealed class NodeEffects : IDisposable
{
    private readonly StyleObservable<IBrush?> _opacity = new();
    private readonly StyleObservable<ITransform?> _transform = new();
    private readonly StyleObservable<IEffect?> _effect = new();
    private readonly IDisposable[] _bindings;

    public NodeEffects(Visual v)
    {
        _bindings =
        [
            v.Bind(Visual.OpacityMaskProperty, _opacity, BindingPriority.Style),
            v.Bind(Visual.RenderTransformProperty, _transform, BindingPriority.Style),
            v.Bind(Visual.EffectProperty, _effect, BindingPriority.Style),
        ];
    }

    /// <summary>Applies the style; <paramref name="fallback"/> is the effect to use when the filter gives none.</summary>
    public void Push(ComputedStyle s, IEffect? fallback = null)
    {
        var opacity = s.Other.Opacity;
        IEffect? effect = null;
        foreach (var f in s.Other.Filter)
        {
            switch (f.Kind)
            {
                case CssFilterKind.Opacity:
                    opacity *= Math.Clamp(f.Amount, 0, 1);
                    break;

                // gtk_css_filter_value_push_snapshot: blur(x) blurs with a standard deviation of x.
                case CssFilterKind.Blur when effect is null && f.Amount > 0:
                    effect = new ImmutableBlurEffect(RadiusForSigma(f.Amount));
                    break;
                case CssFilterKind.DropShadow when effect is null && f.Shadow is { } sh:
                    effect = DropShadow(sh);
                    break;
            }
        }

        _opacity.Push(opacity >= 1 ? null : new Avalonia.Media.Immutable.ImmutableSolidColorBrush(Colors.White, opacity));
        _transform.Push(Transform(s.Other.Transform));
        _effect.Push(effect ?? fallback);
    }

    /// <summary>Sets only the effect, for a visual that draws a node's text but is no node itself.</summary>
    public void PushEffect(IEffect? effect) => _effect.Push(effect);

    /// <summary>A shadow as GTK's shadow node draws it: its blur is twice the standard deviation.</summary>
    internal static IEffect DropShadow(CssShadow sh) =>
        new ImmutableDropShadowEffect(sh.X, sh.Y, RadiusForSigma(sh.Blur / 2), Markup.CssConvert.Color(sh.Color), 1);

    // Avalonia's Skia backend blurs a radius r with a standard deviation of 0.288675 r + 0.5.
    private static double RadiusForSigma(double sigma) => sigma <= 0 ? 0 : sigma <= 0.5 ? 0.01 : (sigma - 0.5) / 0.288675;

    /// <summary>The transform as a matrix around the box center, or null for none.</summary>
    internal static ITransform? Transform(CssList<CssTransformFunction> list)
    {
        if (list.Count == 0)
        {
            return null;
        }

        // Translations with percentages resolve against the element at render time; gtk.css uses px only.
        var m = Matrix.Identity;
        foreach (var f in list)
        {
            var step = f.Kind switch
            {
                CssTransformKind.Translate => Matrix.CreateTranslation(f.Tx.Value, f.Ty.Value),
                CssTransformKind.Scale => Matrix.CreateScale(f.A, f.B),
                CssTransformKind.Rotate => Matrix.CreateRotation(f.A * Math.PI / 180),
                CssTransformKind.Skew => Matrix.CreateSkew(f.A * Math.PI / 180, f.B * Math.PI / 180),
                CssTransformKind.Matrix => new Matrix(f.A, f.B, f.C, f.D, f.E, f.F),
                _ => Matrix.Identity,
            };
            m = step * m;
        }

        return m.IsIdentity ? null : new MatrixTransform(m);
    }

    public void Dispose()
    {
        foreach (var b in _bindings)
        {
            b.Dispose();
        }
    }
}
