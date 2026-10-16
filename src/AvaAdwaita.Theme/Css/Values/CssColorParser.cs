using System;
using System.Collections.Generic;
using AvaAdwaita.CssEngine.Syntax;

namespace AvaAdwaita.CssEngine.Values;

/// <summary>Parses colors.</summary>
/// <remarks>Port of gtk_css_color_value_parse (gtkcsscolorvalue.c) and gdk_rgba_parser_parse (gdkrgba.c).</remarks>
internal static class CssColorParser
{
    private static readonly string[] s_functions =
        ["rgb", "rgba", "hsl", "hsla", "hwb", "oklab", "oklch", "color", "color-mix", "lighter", "darker", "shade", "alpha", "mix"];

    /// <summary>True when the next component could start a color.</summary>
    public static bool CanParse(CssReader r)
    {
        var c = r.Peek();
        if (c is null)
        {
            return false;
        }

        return c.Type switch
        {
            CssTokenType.Ident => true,
            CssTokenType.AtKeyword => true,
            CssTokenType.Hash => true,
            CssTokenType.Function => Array.Exists(s_functions, f => c.IsFunctionNamed(f)),
            _ => false,
        };
    }

    public static CssColorExpr? Parse(CssReader r)
    {
        var c = r.Peek();
        if (c is null)
        {
            r.Fail("expected a color");
            return null;
        }

        switch (c.Type)
        {
            case CssTokenType.Ident:
                {
                    var name = c.Token.Text;
                    if (name.Equals("currentcolor", StringComparison.OrdinalIgnoreCase))
                    {
                        r.Next();
                        return CssColorExpr.Current;
                    }

                    if (name.Equals("transparent", StringComparison.OrdinalIgnoreCase))
                    {
                        r.Next();
                        return CssColorExpr.TransparentLiteral;
                    }

                    if (CssNamedColors.TryGet(name, out var named))
                    {
                        r.Next();
                        return new CssColorExpr.Literal(named);
                    }

                    r.Fail($"\"{name}\" is not a valid color name");
                    return null;
                }

            case CssTokenType.AtKeyword:
                r.Next();
                return new CssColorExpr.Named(c.Token.Text);
            case CssTokenType.Hash:
                {
                    var hex = ParseHex(c.Token.Text);
                    if (hex is null)
                    {
                        r.Fail("Hash code is not a valid hex color");
                        return null;
                    }

                    r.Next();
                    return new CssColorExpr.Literal(hex.Value);
                }

            case CssTokenType.Function:
                {
                    var name = c.Token.Text.ToLowerInvariant();
                    CssColorExpr? result;
                    var inner = new CssReader(c.Children);
                    switch (name)
                    {
                        case "rgb":
                        case "rgba":
                            result = ParseFunction(inner, CssColorSpace.Srgb, legacyRgb: true, allowLegacy: true, allowAlpha: name == "rgba", requireAlpha: name == "rgba");
                            break;
                        case "hsl":
                        case "hsla":
                            result = ParseFunction(inner, CssColorSpace.Hsl, legacyRgb: false, allowLegacy: true, allowAlpha: true, requireAlpha: false);
                            break;
                        case "hwb":
                            result = ParseFunction(inner, CssColorSpace.Hwb, false, false, true, false);
                            break;
                        case "oklab":
                            result = ParseFunction(inner, CssColorSpace.Oklab, false, false, true, false);
                            break;
                        case "oklch":
                            result = ParseFunction(inner, CssColorSpace.Oklch, false, false, true, false);
                            break;
                        case "color":
                            result = ParseColorFunction(inner);
                            break;
                        case "color-mix":
                            result = ParseColorMix(c.Children, r);
                            break;
                        case "lighter":
                        case "darker":
                            {
                                var a = Parse(inner);
                                result = a is not null && inner.AtEnd ? new CssColorExpr.Legacy("shade", a, null, name == "lighter" ? 1.3 : 0.7) : null;
                                break;
                            }

                        case "shade":
                        case "alpha":
                            {
                                var parts = CssReader.SplitCommas(c.Children);
                                result = null;
                                if (parts.Count == 2)
                                {
                                    var ra = new CssReader(parts[0]);
                                    var a = Parse(ra);
                                    var f = SingleNumber(parts[1]);
                                    if (a is not null && ra.AtEnd && f is not null)
                                    {
                                        result = new CssColorExpr.Legacy(name, a, null, f.Value);
                                    }

                                    inner.Absorb(ra);
                                }

                                break;
                            }

                        case "mix":
                            {
                                var parts = CssReader.SplitCommas(c.Children);
                                result = null;
                                if (parts.Count == 3)
                                {
                                    var ra = new CssReader(parts[0]);
                                    var rb = new CssReader(parts[1]);
                                    var a = Parse(ra);
                                    var b = Parse(rb);
                                    var f = SingleNumber(parts[2]);
                                    if (a is not null && b is not null && ra.AtEnd && rb.AtEnd && f is not null)
                                    {
                                        result = new CssColorExpr.Legacy("mix", a, b, f.Value);
                                    }
                                }

                                break;
                            }

                        default:
                            r.ReportUnknownFunction(c.Token.Text);
                            return null;
                    }

                    if (result is null)
                    {
                        r.Absorb(inner);
                        r.Fail($"invalid {name}()");
                        return null;
                    }

                    r.Next();
                    return result;
                }

            default:
                r.Fail("expected a color");
                return null;
        }
    }

    private static double? SingleNumber(List<CssComponent> part)
    {
        var r = new CssReader(part);
        if (r.Peek() is { Type: CssTokenType.Number } n)
        {
            r.Next();
            if (r.AtEnd)
            {
                return n.Token.Number;
            }
        }

        return null;
    }

    public static CssColor? ParseHex(string s)
    {
        static int H(char c) => c switch
        {
            >= '0' and <= '9' => c - '0',
            >= 'a' and <= 'f' => c - 'a' + 10,
            >= 'A' and <= 'F' => c - 'A' + 10,
            _ => -1,
        };

        Span<char> chars = stackalloc char[8];
        switch (s.Length)
        {
            case 3:
                chars[0] = chars[1] = s[0];
                chars[2] = chars[3] = s[1];
                chars[4] = chars[5] = s[2];
                chars[6] = chars[7] = 'F';
                break;
            case 4:
                chars[0] = chars[1] = s[0];
                chars[2] = chars[3] = s[1];
                chars[4] = chars[5] = s[2];
                chars[6] = chars[7] = s[3];
                break;
            case 6:
                s.AsSpan().CopyTo(chars);
                chars[6] = chars[7] = 'F';
                break;
            case 8:
                s.AsSpan().CopyTo(chars);
                break;
            default:
                return null;
        }

        Span<byte> v = stackalloc byte[4];
        for (var i = 0; i < 4; i++)
        {
            var hi = H(chars[i * 2]);
            var lo = H(chars[(i * 2) + 1]);
            if (hi < 0 || lo < 0)
            {
                return null;
            }

            v[i] = (byte)((hi << 4) | lo);
        }

        return CssColor.FromRgb8(v[0], v[1], v[2], v[3]);
    }

    private static CssNumberFlags ChannelFlags(CssColorSpace space, int i, bool legacy)
    {
        if (i == 3)
        {
            return legacy ? CssNumberFlags.Number : CssNumberFlags.Number | CssNumberFlags.Percent;
        }

        return space switch
        {
            CssColorSpace.Hsl when i == 0 => CssNumberFlags.Number | CssNumberFlags.Angle,
            CssColorSpace.Hsl => legacy ? CssNumberFlags.Percent : CssNumberFlags.Percent | CssNumberFlags.Number,
            CssColorSpace.Hwb when i == 0 => CssNumberFlags.Number | CssNumberFlags.Angle,
            CssColorSpace.Oklch when i == 2 => CssNumberFlags.Number | CssNumberFlags.Angle,
            _ => CssNumberFlags.Number | CssNumberFlags.Percent,
        };
    }

    private static CssColorExpr? ParseFunction(CssReader r, CssColorSpace space, bool legacyRgb, bool allowLegacy, bool allowAlpha, bool requireAlpha)
    {
        CssColorExpr? origin = null;
        string[]? channelNames = null;
        if (r.TryIdent("from"))
        {
            origin = Parse(r);
            if (origin is null)
            {
                return null;
            }

            channelNames = CssColorMath.ChannelNames(space);
        }

        var channels = new CssNumeric?[4];
        var legacy = false;
        var alphaGiven = false;
        bool? percentages = null;
        for (var i = 0; i < 4; i++)
        {
            if (i == 3)
            {
                if (r.AtEnd)
                {
                    if (legacy && requireAlpha)
                    {
                        return null;
                    }

                    break;
                }

                if (legacy)
                {
                    if (!allowAlpha || !r.TryComma())
                    {
                        return null;
                    }
                }
                else if (!r.TryDelim('/'))
                {
                    return null;
                }
            }
            else if (i > 0)
            {
                if (legacy && !r.TryComma())
                {
                    return null;
                }
            }

            if (!legacy && r.TryIdent("none"))
            {
                channels[i] = null;
                if (i == 3)
                {
                    alphaGiven = true;
                }
            }
            else
            {
                var isPercent = r.Peek() is { Type: CssTokenType.Percentage };
                var n = CssNumeric.Parse(r, ChannelFlags(space, i, legacy), channelNames);
                if (n is null)
                {
                    return null;
                }

                if (space == CssColorSpace.Srgb && i < 3 && legacy)
                {
                    percentages ??= isPercent;
                    if (percentages != isPercent)
                    {
                        r.Fail("Legacy color syntax doesn't allow mixing numbers and percentages");
                        return null;
                    }
                }
                else if (space == CssColorSpace.Srgb && i == 0)
                {
                    percentages = isPercent;
                }

                channels[i] = n;
                if (i == 3)
                {
                    alphaGiven = true;
                }
            }

            if (i == 0 && origin is null && allowLegacy && r.PeekComma)
            {
                legacy = true;
            }
        }

        if (!r.AtEnd)
        {
            return null;
        }

        if (legacy && requireAlpha && !alphaGiven)
        {
            return null;
        }

        return new CssColorExpr.Function(space, legacyRgb, origin, channels, alphaGiven);
    }

    private static CssColorExpr? ParseColorFunction(CssReader r)
    {
        CssColorExpr? origin = null;
        if (r.TryIdent("from"))
        {
            origin = Parse(r);
            if (origin is null)
            {
                return null;
            }
        }

        var spaceName = r.TryAnyIdent();
        CssColorSpace? space = spaceName?.ToLowerInvariant() switch
        {
            "srgb" => CssColorSpace.Srgb,
            "srgb-linear" => CssColorSpace.SrgbLinear,
            "display-p3" => CssColorSpace.DisplayP3,
            "xyz" => CssColorSpace.Xyz,
            "rec2020" => CssColorSpace.Rec2020,
            _ => null,
        };
        if (space is null)
        {
            r.Fail($"Invalid color space in color(): {spaceName}");
            return null;
        }

        var names = origin is null ? null : CssColorMath.ChannelNames(space.Value);
        var channels = new CssNumeric?[4];
        var alphaGiven = false;
        for (var i = 0; i < 3; i++)
        {
            if (r.TryIdent("none"))
            {
                continue;
            }

            channels[i] = CssNumeric.Parse(r, CssNumberFlags.Number | CssNumberFlags.Percent, names);
            if (channels[i] is null)
            {
                return null;
            }
        }

        if (r.TryDelim('/'))
        {
            alphaGiven = true;
            if (!r.TryIdent("none"))
            {
                channels[3] = CssNumeric.Parse(r, CssNumberFlags.Number | CssNumberFlags.Percent, names);
                if (channels[3] is null)
                {
                    return null;
                }
            }
        }

        return r.AtEnd ? new CssColorExpr.Function(space.Value, false, origin, channels, alphaGiven) : null;
    }

    private static CssColorExpr? ParseColorMix(CssComponent[] args, CssReader outer)
    {
        var parts = CssReader.SplitCommas(args);
        if (parts.Count != 3)
        {
            outer.Fail("color-mix() needs 3 arguments");
            return null;
        }

        var m = new CssReader(parts[0]);
        if (!m.TryIdent("in"))
        {
            outer.Fail("Expected 'in'");
            return null;
        }

        var spaceName = m.TryAnyIdent();
        if (!TryParseInterpolationSpace(spaceName, out var space))
        {
            outer.Fail($"Invalid color space: {spaceName}");
            return null;
        }

        var hue = CssHueInterpolation.Shorter;
        if (CssColorMath.IsPolar(space) && !m.AtEnd)
        {
            var h = m.TryAnyIdent();
            hue = h?.ToLowerInvariant() switch
            {
                "shorter" => CssHueInterpolation.Shorter,
                "longer" => CssHueInterpolation.Longer,
                "increasing" => CssHueInterpolation.Increasing,
                "decreasing" => CssHueInterpolation.Decreasing,
                _ => (CssHueInterpolation)255,
            };
            if ((byte)hue == 255 || !m.TryIdent("hue"))
            {
                outer.Fail("bad hue interpolation");
                return null;
            }
        }

        if (!m.AtEnd)
        {
            outer.Fail("unexpected data in color-mix()");
            return null;
        }

        var a = ParseMixComponent(parts[1], outer, out var p1);
        var b = ParseMixComponent(parts[2], outer, out var p2);
        if (a is null || b is null)
        {
            return null;
        }

        if (p1 > -0.5f && p2 > -0.5f && MathF.Abs(p1 + p2) < 0.001f)
        {
            outer.Fail("color-mix() percentages add up to 0");
            return null;
        }

        return new CssColorExpr.Mix(space, hue, a, p1, b, p2);
    }

    public static bool TryParseInterpolationSpace(string? name, out CssColorSpace space)
    {
        CssColorSpace? s = name?.ToLowerInvariant() switch
        {
            "srgb" => CssColorSpace.Srgb,
            "srgb-linear" => CssColorSpace.SrgbLinear,
            "hsl" => CssColorSpace.Hsl,
            "hwb" => CssColorSpace.Hwb,
            "oklab" => CssColorSpace.Oklab,
            "oklch" => CssColorSpace.Oklch,
            "display-p3" => CssColorSpace.DisplayP3,
            "xyz" => CssColorSpace.Xyz,
            "rec2020" => CssColorSpace.Rec2020,
            _ => null,
        };
        space = s ?? CssColorSpace.Srgb;
        return s is not null;
    }

    private static CssColorExpr? ParseMixComponent(List<CssComponent> part, CssReader outer, out float percentage)
    {
        percentage = -1;
        var r = new CssReader(part);
        CssColorExpr? color = null;
        if (!CssNumeric.CanParse(r))
        {
            color = Parse(r);
            if (color is null)
            {
                outer.Absorb(r);
                return null;
            }
        }

        if (CssNumeric.CanParse(r))
        {
            var n = CssNumeric.Parse(r, CssNumberFlags.Percent | CssNumberFlags.PositiveOnly);
            if (n is null)
            {
                outer.Absorb(r);
                return null;
            }

            percentage = (float)n.Compute(CssNumericContext.Default).Percent;
            if (percentage > 100)
            {
                outer.Fail("Values above 100% are not allowed");
                return null;
            }
        }

        if (color is null)
        {
            color = Parse(r);
            if (color is null)
            {
                outer.Absorb(r);
                return null;
            }
        }

        if (!r.AtEnd)
        {
            outer.Fail("unexpected data in color-mix()");
            return null;
        }

        return color;
    }
}
