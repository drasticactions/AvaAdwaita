using System;

namespace AvaAdwaita.Animation;

/// <summary>A damped spring from one value to another, as <c>AdwSpringAnimation</c>.</summary>
/// <remarks>Port of oscillate and calculate_duration (adw-spring-animation.c).</remarks>
public sealed class SpringSolver
{
    private const double Delta = 0.001;
    private const int MaxIterations = 20000;

    /// <summary>Creates a spring.</summary>
    /// <param name="from">The start value.</param>
    /// <param name="to">The end value.</param>
    /// <param name="damping">The damping.</param>
    /// <param name="mass">The mass.</param>
    /// <param name="stiffness">The stiffness.</param>
    /// <param name="initialVelocity">The initial velocity.</param>
    /// <param name="epsilon">How close counts as arrived.</param>
    /// <param name="clamp">Stop at the first crossing of the end value.</param>
    public SpringSolver(double from, double to, double damping, double mass, double stiffness, double initialVelocity = 0, double epsilon = 0.001, bool clamp = false)
    {
        From = from;
        To = to;
        Damping = damping;
        Mass = mass;
        Stiffness = stiffness;
        InitialVelocity = initialVelocity;
        Epsilon = epsilon;
        Clamp = clamp;
    }

    /// <summary>Parameters from a damping ratio, as adw_spring_params_new.</summary>
    public static (double Damping, double Mass, double Stiffness) Params(double dampingRatio, double mass, double stiffness)
    {
        var critical = 2 * Math.Sqrt(mass * stiffness);
        return (dampingRatio * critical, mass, stiffness);
    }

    /// <summary>The start value.</summary>
    public double From { get; }

    /// <summary>The end value.</summary>
    public double To { get; }

    /// <summary>The damping.</summary>
    public double Damping { get; }

    /// <summary>The mass.</summary>
    public double Mass { get; }

    /// <summary>The stiffness.</summary>
    public double Stiffness { get; }

    /// <summary>The initial velocity.</summary>
    public double InitialVelocity { get; }

    /// <summary>How close to the end value counts as arrived.</summary>
    public double Epsilon { get; }

    /// <summary>Stop at the first crossing of the end value.</summary>
    public bool Clamp { get; }

    /// <summary>The value at <paramref name="ms"/> milliseconds.</summary>
    public double Value(double ms) => Oscillate(ms, out _);

    /// <summary>The velocity at <paramref name="ms"/> milliseconds.</summary>
    public double Velocity(double ms)
    {
        Oscillate(ms, out var v);
        return v;
    }

    private double Oscillate(double time, out double velocity)
    {
        var b = Damping;
        var m = Mass;
        var k = Stiffness;
        var v0 = InitialVelocity;
        var t = time / 1000.0;
        var beta = b / (2 * m);
        var omega0 = Math.Sqrt(k / m);
        var x0 = From - To;
        var envelope = Math.Exp(-beta * t);
        if (Math.Abs(beta - omega0) <= float.Epsilon)
        {
            velocity = envelope * ((-beta * t * v0) - (beta * beta * t * x0) + v0);
            return To + (envelope * (x0 + (((beta * x0) + v0) * t)));
        }

        if (beta < omega0)
        {
            var omega1 = Math.Sqrt((omega0 * omega0) - (beta * beta));
            velocity = envelope * ((v0 * Math.Cos(omega1 * t)) - (((x0 * omega1) + (((beta * beta * x0) + (beta * v0)) / omega1)) * Math.Sin(omega1 * t)));
            return To + (envelope * ((x0 * Math.Cos(omega1 * t)) + (((beta * x0) + v0) / omega1 * Math.Sin(omega1 * t))));
        }

        var omega2 = Math.Sqrt((beta * beta) - (omega0 * omega0));
        velocity = envelope * ((v0 * Math.Cosh(omega2 * t)) + (((omega2 * x0) - (((beta * beta * x0) + (beta * v0)) / omega2)) * Math.Sinh(omega2 * t)));
        return To + (envelope * ((x0 * Math.Cosh(omega2 * t)) + (((beta * x0) + v0) / omega2 * Math.Sinh(omega2 * t))));
    }

    /// <summary>The estimated duration in milliseconds, or <see cref="double.PositiveInfinity"/> without damping.</summary>
    public double EstimatedDuration()
    {
        var beta = Damping / (2 * Mass);
        if (Math.Abs(beta) <= double.Epsilon || beta < 0)
        {
            return double.PositiveInfinity;
        }

        if (Clamp)
        {
            return Math.Abs(To - From) <= double.Epsilon ? 0 : FirstZero();
        }

        var omega0 = Math.Sqrt(Stiffness / Mass);
        var x0 = -Math.Log(Epsilon) / beta;
        if (Math.Abs(beta - omega0) <= float.Epsilon || beta < omega0)
        {
            return Math.Floor(x0 * 1000);
        }

        var y0 = Value(x0 * 1000);
        var m = (Value((x0 + Delta) * 1000) - y0) / Delta;
        var x1 = (To - y0 + (m * x0)) / m;
        var y1 = Value(x1 * 1000);
        var i = 0;
        while (Math.Abs(To - y1) > Epsilon)
        {
            if (i++ > 1000)
            {
                return 0;
            }

            x0 = x1;
            y0 = y1;
            m = (Value((x0 + Delta) * 1000) - y0) / Delta;
            x1 = (To - y0 + (m * x0)) / m;
            y1 = Value(x1 * 1000);
        }

        return Math.Floor(x1 * 1000);
    }

    private double FirstZero()
    {
        var i = 1;
        var y = Value(i);
        while ((To - From > double.Epsilon && To - y > Epsilon) || (From - To > double.Epsilon && y - To > Epsilon))
        {
            if (i > MaxIterations)
            {
                return 0;
            }

            y = Value(++i);
        }

        return i;
    }
}
