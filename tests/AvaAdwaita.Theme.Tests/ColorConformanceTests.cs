using System;
using AvaAdwaita.CssEngine.Syntax;
using AvaAdwaita.CssEngine.Values;
using Xunit;

namespace AvaAdwaita.Theme.Tests;

/// <summary>Cases ported from GTK 4.24's testsuite/css/color.c, with its tolerance.</summary>
public class ColorConformanceTests
{
    private const float Epsilon = 0.005f;

    private sealed class NoNames : ICssColorScope
    {
        public CssColorExpr? LookupColor(string name) => null;
    }

    private static CssColor Parse(string s)
    {
        var r = new CssReader(CssComponentParser.Parse(s));
        var e = CssColorParser.Parse(r) ?? throw new FormatException(r.Error);
        Assert.True(r.AtEnd);
        return e.Resolve(new NoNames(), CssColor.Black) ?? throw new FormatException(s);
    }

    private static void AssertNear(CssColor expected, CssColor actual)
    {
        var ok = expected.Space == actual.Space && expected.Missing == actual.Missing
            && MathF.Abs(expected.C0 - actual.C0) <= Epsilon && MathF.Abs(expected.C1 - actual.C1) <= Epsilon
            && MathF.Abs(expected.C2 - actual.C2) <= Epsilon && MathF.Abs(expected.Alpha - actual.Alpha) <= Epsilon;
        Assert.True(ok, $"expected {expected.Space}({expected.C0} {expected.C1} {expected.C2} / {expected.Alpha}) m{expected.Missing}, got {actual.Space}({actual.C0} {actual.C1} {actual.C2} / {actual.Alpha}) m{actual.Missing}");
    }

    [Theory]
    [InlineData("rgb(255,0,0)", (int)CssColorSpace.SrgbLinear, "color(srgb-linear 1 0 0)")]
    [InlineData("color(srgb 0.5 none 1 / 0.7)", (int)CssColorSpace.SrgbLinear, "color(srgb-linear 0.214041 0 1 / 0.7)")]
    [InlineData("rgb(100,100,100)", (int)CssColorSpace.Hsl, "hsla(0deg 0 39.215687 / 1)")]
    [InlineData("oklch(40.101% 0.12332 21.555)", (int)CssColorSpace.Srgb, "rgb(49.06% 13.87% 15.9%)")]
    [InlineData("oklch(59.686% 0.15619 49.7694)", (int)CssColorSpace.Srgb, "rgb(77.61% 36.34% 2.45%)")]
    [InlineData("oklch(0.65125 0.13138 104.097)", (int)CssColorSpace.Srgb, "rgb(61.65% 57.51% 9.28%)")]
    [InlineData("oklch(0.66016 0.15546 134.231)", (int)CssColorSpace.Srgb, "rgb(40.73% 65.12% 22.35%)")]
    [InlineData("oklch(72.322% 0.12403 247.996)", (int)CssColorSpace.Srgb, "rgb(38.29% 67.27% 93.85%)")]
    [InlineData("oklch(42.1% 48.25% 328.4)", (int)CssColorSpace.Srgb, "color(srgb 0.501808 0.00257216 0.501403)")]
    [InlineData("oklch(0.392 0.4 none)", (int)CssColorSpace.Oklch, "oklch(0.392 0.4 0)")]
    [InlineData("color(display-p3 1 1 1)", (int)CssColorSpace.DisplayP3, "color(display-p3 1 1 1)")]
    [InlineData("color(rec2020 1 1 1)", (int)CssColorSpace.Rec2020, "color(rec2020 1 1 1)")]
    [InlineData("color(xyz 0.5 0.7 99%)", (int)CssColorSpace.DisplayP3, "color(display-p3 0.48 0.93 0.96)")]
    [InlineData("hsl(250 100 20)", (int)CssColorSpace.Rec2020, "color(rec2020 0.042 0.008 0.3226)")]
    public void Conversion(string input, int dest, string expected)
    {
        var result = CssColorMath.Convert(Parse(input), (CssColorSpace)dest);
        AssertNear(Parse(expected), result);
    }

    [Theory]
    [InlineData("oklch(78.3% 0.108 326.5)", "oklch(39.2% 0.4 none)", (int)CssColorSpace.Oklch, (int)CssHueInterpolation.Shorter, "oklch(58.75% 0.254 326.5)")]
    [InlineData("oklch(0.783 0.108 326.5 / 0.5)", "oklch(0.392 0.4 0 / none)", (int)CssColorSpace.Oklch, (int)CssHueInterpolation.Shorter, "oklch(0.5875 0.254 343.25 / 0.5)")]
    [InlineData("rgb(24% 12% 98% / 0.4)", "rgb(62% 26% 64% / 0.6)", (int)CssColorSpace.Srgb, (int)CssHueInterpolation.Shorter, "rgb(46.8% 20.4% 77.6% / 0.5)")]
    [InlineData("oklch(0.6 0.24 30)", "oklch(0.8 0.15 90)", (int)CssColorSpace.Oklch, (int)CssHueInterpolation.Shorter, "oklch(0.7 0.195 60)")]
    [InlineData("oklch(0.6 0.24 30)", "oklch(0.8 0.15 90)", (int)CssColorSpace.Oklch, (int)CssHueInterpolation.Longer, "oklch(0.7 0.195 240)")]
    public void Interpolation(string a, string b, int space, int interp, string expected)
    {
        var result = CssColorMath.Interpolate(Parse(a), Parse(b), 0.5f, (CssColorSpace)space, (CssHueInterpolation)interp);
        AssertNear(Parse(expected), result);
    }

    [Theory]
    [InlineData("color-mix(in srgb, white, black)", "rgb(128,128,128)")]
    [InlineData("color-mix(in srgb, currentColor 10%, transparent)", "rgba(0,0,0,0.1)")]
    [InlineData("color-mix(in srgb, red 30%, blue 30%)", "rgba(128,0,128,0.6)")]
    [InlineData("color-mix(in oklab, #3584e4 50%, white)", "rgb(157,195,245)")]
    [InlineData("oklab(from #3584e4 min(l, 0.5) a b)", "rgb(4,97,190)")]
    [InlineData("oklab(from #3584e4 max(l, 0.85) a b)", "rgb(129,208,255)")]
    [InlineData("rgb(from red r g b / 0)", "rgba(255,0,0,0)")]
    [InlineData("hsl(from #808080 h calc(s * 1.1) calc(l * 1.1))", "rgb(141,141,141)")]
    [InlineData("shade(#808080, 1.3)", "rgb(166,166,166)")]
    [InlineData("alpha(red, 0.5)", "rgba(255,0,0,0.5)")]
    [InlineData("mix(red, blue, 0.5)", "rgb(128,0,128)")]
    [InlineData("#abc", "rgb(170,187,204)")]
    [InlineData("RGB(0 0 6 / 80%)", "rgba(0,0,6,0.8)")]
    [InlineData("rgba(10, 20, 30, 0.5)", "rgba(10,20,30,0.5)")]
    public void Functions(string input, string expected) => Assert.Equal(expected, Parse(input).ToString());

    [Theory]
    [InlineData("rgba(1, 2, 3)")]
    [InlineData("rgb(1, 2%, 3)")]
    [InlineData("color-mix(in srgb, red 0%, blue 0%)")]
    [InlineData("color-mix(in srgb, red 150%, blue)")]
    [InlineData("notacolor")]
    public void Invalid(string input)
    {
        var r = new CssReader(CssComponentParser.Parse(input));
        var e = CssColorParser.Parse(r);
        Assert.True(e is null || !r.AtEnd);
    }
}
