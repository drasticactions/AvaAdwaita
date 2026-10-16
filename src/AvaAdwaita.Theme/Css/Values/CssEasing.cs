using System;
using System.Globalization;

namespace AvaAdwaita.CssEngine.Values;

/// <summary>A computed timing function: a cubic Bézier, or steps.</summary>
/// <remarks>Port of gtkcsseasevalue.c.</remarks>
internal sealed record CssEasing(bool IsSteps, double X1, double Y1, double X2, double Y2, int Steps, bool JumpStart)
{
    public static CssEasing Ease { get; } = Bezier(0.25, 0.1, 0.25, 1.0);

    public static CssEasing Linear { get; } = Bezier(0, 0, 1, 1);

    public static CssEasing Bezier(double x1, double y1, double x2, double y2) => new(false, x1, y1, x2, y2, 0, false);

    /// <summary>The eased progress for <paramref name="progress"/> in 0–1.</summary>
    /// <remarks>Port of _gtk_css_ease_value_transform (gtkcsseasevalue.c).</remarks>
    public double Transform(double progress)
    {
        if (progress <= 0)
        {
            return 0;
        }

        if (progress >= 1)
        {
            return 1;
        }

        if (IsSteps)
        {
            progress *= Steps;
            progress = Math.Floor(progress) + (JumpStart ? 0 : 1);
            return progress / Steps;
        }

        const double Epsilon = 0.00001;
        double tmin = 0.0, tmax = 1.0, t = progress;
        while (tmin < tmax)
        {
            var sample = ((((((1.0 + (3 * X1)) - (3 * X2)) * t) + (-6 * X1) + (3 * X2)) * t) + (3 * X1)) * t;
            if (Math.Abs(sample - progress) < Epsilon)
            {
                break;
            }

            if (progress > sample)
            {
                tmin = t;
            }
            else
            {
                tmax = t;
            }

            t = (tmax + tmin) * .5;
        }

        return ((((((1.0 + (3 * Y1)) - (3 * Y2)) * t) + (-6 * Y1) + (3 * Y2)) * t) + (3 * Y1)) * t;
    }

    public override string ToString() => IsSteps
        ? string.Create(CultureInfo.InvariantCulture, $"steps({Steps}, {(JumpStart ? "start" : "end")})")
        : string.Create(CultureInfo.InvariantCulture, $"cubic-bezier({X1}, {Y1}, {X2}, {Y2})");
}
