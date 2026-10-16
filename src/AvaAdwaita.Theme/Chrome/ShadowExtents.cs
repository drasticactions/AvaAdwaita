using System;
using Avalonia;
using AvaAdwaita.CssEngine.Values;

namespace AvaAdwaita.Chrome;

/// <summary>How far outset shadows reach outside the box.</summary>
/// <remarks>Port of gtk_css_shadow_value_get_extents (gtkcssshadowvalue.c) and gsk_cairo_blur_compute_pixels.</remarks>
internal static class ShadowExtents
{
    private static readonly double s_gaussianScale = 3.0 * Math.Sqrt(2 * Math.PI) / 4;

    public static double BlurPixels(double radius) => Math.Floor((radius * s_gaussianScale * 1.5) + 0.5);

    public static Thickness Of(CssList<CssShadow> shadows)
    {
        double top = 0, right = 0, bottom = 0, left = 0;
        foreach (var s in shadows)
        {
            if (s.Inset)
            {
                continue;
            }

            var clip = BlurPixels(s.Blur / 2.0);
            top = Math.Max(top, Math.Ceiling(clip + s.Spread - s.Y));
            right = Math.Max(right, Math.Ceiling(clip + s.Spread + s.X));
            bottom = Math.Max(bottom, Math.Ceiling(clip + s.Spread + s.Y));
            left = Math.Max(left, Math.Ceiling(clip + s.Spread - s.X));
        }

        return new Thickness(left, top, right, bottom);
    }
}
