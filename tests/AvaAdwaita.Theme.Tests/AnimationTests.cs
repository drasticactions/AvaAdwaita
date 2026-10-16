using AvaAdwaita.Animation;
using Xunit;

namespace AvaAdwaita.Theme.Tests;

public class AnimationTests
{
    [Theory]
    [InlineData(AdwEasing.Linear, 0.25, 0.25)]
    [InlineData(AdwEasing.EaseOutCubic, 0.5, 0.875)]
    [InlineData(AdwEasing.EaseInOutCubic, 0.25, 0.0625)]
    [InlineData(AdwEasing.EaseInOutCubic, 0.75, 0.9375)]
    [InlineData(AdwEasing.EaseOutBounce, 1.0, 1.0)]
    [InlineData(AdwEasing.EaseInExpo, 0.0, 0.0)]
    public void Easing_Matches_Libadwaita(AdwEasing easing, double t, double expected) =>
        Assert.Equal(expected, Easing.Ease(easing, t), 6);

    [Fact]
    public void Every_Curve_Starts_At_0_And_Ends_At_1()
    {
        foreach (var e in System.Enum.GetValues<AdwEasing>())
        {
            Assert.Equal(0, Easing.Ease(e, 0), 3);
            Assert.Equal(1, Easing.Ease(e, 1), 3);
        }
    }

    [Fact]
    public void Spring_Settles_At_The_End_Value()
    {
        var (d, m, k) = SpringSolver.Params(1, 1, 100);
        var s = new SpringSolver(0, 1, d, m, k);
        var duration = s.EstimatedDuration();
        Assert.InRange(duration, 100, 2000);
        Assert.InRange(s.Value(duration), 0.99, 1.0);
        var under = new SpringSolver(0, 1, SpringSolver.Params(0.5, 1, 100).Damping, 1, 100, clamp: true);
        Assert.InRange(under.EstimatedDuration(), 50, 400);
    }
}
