using System;

namespace AvaAdwaita.Animation;

/// <summary>libadwaita's easing curves.</summary>
/// <remarks>Port of adw_easing_ease (adw-easing.c).</remarks>
public static class Easing
{
    private const double Back = 1.70158;

    /// <summary>The eased value for <paramref name="t"/> in 0–1.</summary>
    public static double Ease(AdwEasing easing, double t) => easing switch
    {
        AdwEasing.Linear => t,
        AdwEasing.EaseInQuad => t * t,
        AdwEasing.EaseOutQuad => -1.0 * t * (t - 2),
        AdwEasing.EaseInOutQuad => InOut(t, p => 0.5 * p * p, p => -0.5 * ((p * (p - 2)) - 1), shift: 1),
        AdwEasing.EaseInCubic => t * t * t,
        AdwEasing.EaseOutCubic => Cube(t - 1) + 1,
        AdwEasing.EaseInOutCubic => InOut(t, p => 0.5 * p * p * p, p => 0.5 * ((p * p * p) + 2), shift: 2),
        AdwEasing.EaseInQuart => t * t * t * t,
        AdwEasing.EaseOutQuart => -1.0 * (Math.Pow(t - 1, 4) - 1),
        AdwEasing.EaseInOutQuart => InOut(t, p => 0.5 * p * p * p * p, p => -0.5 * ((p * p * p * p) - 2), shift: 2),
        AdwEasing.EaseInQuint => Math.Pow(t, 5),
        AdwEasing.EaseOutQuint => Math.Pow(t - 1, 5) + 1,
        AdwEasing.EaseInOutQuint => InOut(t, p => 0.5 * Math.Pow(p, 5), p => 0.5 * (Math.Pow(p, 5) + 2), shift: 2),
        AdwEasing.EaseInSine => (-1.0 * Math.Cos(t * Math.PI / 2)) + 1.0,
        AdwEasing.EaseOutSine => Math.Sin(t * Math.PI / 2),
        AdwEasing.EaseInOutSine => -0.5 * (Math.Cos(Math.PI * t) - 1),
        AdwEasing.EaseInExpo => Near(t, 0) ? 0.0 : Math.Pow(2, 10 * (t - 1)),
        AdwEasing.EaseOutExpo => Near(t, 1) ? 1.0 : -Math.Pow(2, -10 * t) + 1,
        AdwEasing.EaseInOutExpo => InOutExpo(t),
        AdwEasing.EaseInCirc => -1.0 * (Math.Sqrt(1 - (t * t)) - 1),
        AdwEasing.EaseOutCirc => Math.Sqrt(1 - ((t - 1) * (t - 1))),
        AdwEasing.EaseInOutCirc => InOut(t, p => -0.5 * (Math.Sqrt(1 - (p * p)) - 1), p => 0.5 * (Math.Sqrt(1 - (p * p)) + 1), shift: 2),
        AdwEasing.EaseInElastic => InElastic(t),
        AdwEasing.EaseOutElastic => OutElastic(t),
        AdwEasing.EaseInOutElastic => InOutElastic(t),
        AdwEasing.EaseInBack => t * t * (((Back + 1) * t) - Back),
        AdwEasing.EaseOutBack => (Sq(t - 1) * (((Back + 1) * (t - 1)) + Back)) + 1,
        AdwEasing.EaseInOutBack => InOutBack(t),
        AdwEasing.EaseInBounce => 1.0 - OutBounce(1 - t),
        AdwEasing.EaseOutBounce => OutBounce(t),
        AdwEasing.EaseInOutBounce => t < 0.5 ? (1.0 - OutBounce(1 - (t * 2))) * 0.5 : (OutBounce((t * 2) - 1) * 0.5) + 0.5,
        AdwEasing.Ease => CubicBezier(t, 0.25, 0.1, 0.25, 1.0),
        AdwEasing.EaseIn => CubicBezier(t, 0.42, 0.0, 1.0, 1.0),
        AdwEasing.EaseOut => CubicBezier(t, 0.0, 0.0, 0.58, 1.0),
        AdwEasing.EaseInOut => CubicBezier(t, 0.42, 0.0, 0.58, 1.0),
        _ => t,
    };

    private static bool Near(double a, double b) => Math.Abs(a - b) <= double.Epsilon;

    private static double Sq(double x) => x * x;

    private static double Cube(double x) => x * x * x;

    private static double InOut(double t, Func<double, double> first, Func<double, double> second, int shift)
    {
        var p = t / 0.5;
        return p < 1 ? first(p) : second(p - shift);
    }

    private static double InOutExpo(double t)
    {
        if (Near(t, 0))
        {
            return 0;
        }

        if (Near(t, 1))
        {
            return 1;
        }

        var p = t / 0.5;
        return p < 1 ? 0.5 * Math.Pow(2, 10 * (p - 1)) : 0.5 * (-Math.Pow(2, -10 * (p - 1)) + 2);
    }

    private static double InElastic(double t)
    {
        const double p = 0.3;
        const double s = p / 4;
        if (Near(t, 1))
        {
            return 1.0;
        }

        var q = t - 1;
        return -(Math.Pow(2, 10 * q) * Math.Sin((q - s) * (2 * Math.PI) / p));
    }

    private static double OutElastic(double t)
    {
        const double p = 0.3;
        const double s = p / 4;
        if (Near(t, 1))
        {
            return 1.0;
        }

        return (Math.Pow(2, -10 * t) * Math.Sin((t - s) * (2 * Math.PI) / p)) + 1.0;
    }

    private static double InOutElastic(double t)
    {
        const double p = 0.3 * 1.5;
        const double s = p / 4;
        var q = t / 0.5;
        if (Near(q, 2))
        {
            return 1.0;
        }

        q -= 1;
        return q < 0
            ? -0.5 * (Math.Pow(2, 10 * q) * Math.Sin((q - s) * (2 * Math.PI) / p))
            : (Math.Pow(2, -10 * q) * Math.Sin((q - s) * (2 * Math.PI) / p) * 0.5) + 1.0;
    }

    private static double InOutBack(double t)
    {
        var p = t / 0.5;
        const double s = Back * 1.525;
        if (p < 1)
        {
            return 0.5 * (p * p * (((s + 1) * p) - s));
        }

        p -= 2;
        return 0.5 * ((p * p * (((s + 1) * p) + s)) + 2);
    }

    private static double OutBounce(double p)
    {
        if (p < 1 / 2.75)
        {
            return 7.5625 * p * p;
        }

        if (p < 2 / 2.75)
        {
            p -= 1.5 / 2.75;
            return (7.5625 * p * p) + .75;
        }

        if (p < 2.5 / 2.75)
        {
            p -= 2.25 / 2.75;
            return (7.5625 * p * p) + .9375;
        }

        p -= 2.625 / 2.75;
        return (7.5625 * p * p) + .984375;
    }

    private static double XForT(double t, double x1, double x2)
    {
        var omt = 1.0 - t;
        return (3.0 * omt * omt * t * x1) + (3.0 * omt * t * t * x2) + (t * t * t);
    }

    /// <summary>A cubic Bézier curve, as libadwaita's ease_cubic_bezier.</summary>
    public static double CubicBezier(double p, double x1, double y1, double x2, double y2)
    {
        if (Near(p, 0))
        {
            return 0;
        }

        if (Near(p, 1))
        {
            return 1;
        }

        double min = 0, max = 1;
        for (var i = 0; i < 30; i++)
        {
            var guess = (min + max) / 2.0;
            if (p < XForT(guess, x1, x2))
            {
                max = guess;
            }
            else
            {
                min = guess;
            }
        }

        return XForT((min + max) / 2.0, y1, y2);
    }
}
