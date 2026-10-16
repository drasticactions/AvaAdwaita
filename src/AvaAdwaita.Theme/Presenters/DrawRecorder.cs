using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Media;
using Avalonia.VisualTree;
using AvaAdwaita.CssEngine.Values;
using AvaAdwaita.Markup;

namespace AvaAdwaita.Presenters;

/// <summary>
/// Collects what <see cref="AdwBox"/>, <see cref="AdwLabel"/> and <see cref="AdwIcon"/> draw below a root
/// while it is <see cref="Current"/>, for comparison with GTK's render nodes.
/// </summary>
internal sealed class DrawRecorder : IDisposable
{
    [ThreadStatic]
    private static DrawRecorder? t_current;

    private DrawRecorder(Visual root)
    {
        Root = root;
    }

    public static DrawRecorder? Current => t_current;

    public Visual Root { get; }

    public List<DrawRecord> Draws { get; } = new();

    public static DrawRecorder Start(Visual root)
    {
        var r = new DrawRecorder(root);
        t_current = r;
        return r;
    }

    public void Dispose()
    {
        if (ReferenceEquals(t_current, this))
        {
            t_current = null;
        }
    }

    /// <summary>The recorder when <paramref name="owner"/> is the root or below it.</summary>
    public static DrawRecorder? For(Visual? owner)
    {
        if (t_current is not { } r || owner is null)
        {
            return null;
        }

        for (Visual? v = owner; v is not null; v = v.GetVisualParent())
        {
            if (ReferenceEquals(v, r.Root))
            {
                return r;
            }
        }

        return null;
    }

    public void Add(Visual owner, DrawRecord draw)
    {
        var transform = ToRoot(owner, Root);
        var opacity = 1f;
        for (Visual? v = owner; v is not null; v = v.GetVisualParent())
        {
            opacity *= (float)v.Opacity;
            if (v.OpacityMask is ISolidColorBrush mask)
            {
                opacity *= (float)(mask.Opacity * mask.Color.A / 255);
            }

            if (ReferenceEquals(v, Root))
            {
                break;
            }
        }

        Draws.Add(draw with
        {
            Rect = draw.Rect.TransformToAABB(transform),
            Color = draw.Color is { } c ? Fade(c, draw.Alpha, opacity) : null,
            Colors = draw.Colors is { } cs ? cs.Select((x, i) => Fade(x, draw.Alphas?[i], opacity)).ToArray() : null,
        });
    }

    public static Color ToColor(CssColor c) => CssConvert.Color(c);

    /// <summary>
    /// The transform from <paramref name="v"/> to <paramref name="root"/> as the root appears on screen: a
    /// right-to-left root mirrors its own content, which its own coordinates do not show.
    /// </summary>
    public static Matrix ToRoot(Visual v, Visual root)
    {
        if (root.GetVisualParent() is { } parent && v.TransformToVisual(parent) is { } toParent)
        {
            return toParent * Matrix.CreateTranslation(-root.Bounds.X, -root.Bounds.Y);
        }

        return v.TransformToVisual(root) ?? Matrix.Identity;
    }

    public static double[] Radii(RoundedRect r) => [r.RadiiTopLeft.X, r.RadiiTopRight.X, r.RadiiBottomRight.X, r.RadiiBottomLeft.X];

    // GTK keeps colors as floats and rounds once, half away from zero, when it draws.
    private static Color Fade(Color c, float? alpha, float opacity)
    {
        var a = (double)(alpha ?? (c.A / 255f)) * opacity;
        return Avalonia.Media.Color.FromArgb((byte)Math.Round(Math.Clamp(a, 0, 1) * 255, MidpointRounding.AwayFromZero), c.R, c.G, c.B);
    }
}
