using System.Collections.Generic;
using AvaAdwaita.CssEngine.Syntax;
using AvaAdwaita.CssEngine.Values;

namespace AvaAdwaita.CssEngine.Properties;

/// <summary>The shorthand parsers.</summary>
/// <remarks>Port of gtkcssshorthandpropertyimpl.c. A null result entry means the longhand's initial value.</remarks>
internal static class CssShorthandParsers
{
    /// <summary>One to four values for top, right, bottom and left, as parse_four_numbers.</summary>
    public static object?[]? Box(CssReader r, CssShorthand s, CssParseContext pc)
    {
        var values = new object?[4];
        var kind = s.Longhand(0).Kind;
        int i;
        for (i = 0; i < 4 && !r.AtEnd; i++)
        {
            values[i] = kind.Parse(r, pc);
            if (values[i] is null)
            {
                return null;
            }
        }

        if (i == 0)
        {
            r.Fail("Expected a value");
            return null;
        }

        for (; i < 4; i++)
        {
            values[i] = values[(i - 1) >> 1];
        }

        return values;
    }

    public static object?[]? BorderRadius(CssReader r, CssShorthand s, CssParseContext pc)
    {
        const CssNumberFlags Flags = CssNumberFlags.PositiveOnly | CssNumberFlags.Percent | CssNumberFlags.Length;
        var x = new CssNumeric[4];
        var y = new CssNumeric[4];
        int i;
        for (i = 0; i < 4 && CssNumeric.CanParse(r); i++)
        {
            var n = CssNumeric.Parse(r, Flags);
            if (n is null)
            {
                return null;
            }

            x[i] = n;
        }

        if (i == 0)
        {
            r.Fail("Expected a number");
            return null;
        }

        for (; i < 4; i++)
        {
            x[i] = x[(i - 1) >> 1];
        }

        if (r.TryDelim('/'))
        {
            for (i = 0; i < 4 && CssNumeric.CanParse(r); i++)
            {
                var n = CssNumeric.Parse(r, Flags);
                if (n is null)
                {
                    return null;
                }

                y[i] = n;
            }

            if (i == 0)
            {
                r.Fail("Expected a number");
                return null;
            }

            for (; i < 4; i++)
            {
                y[i] = y[(i - 1) >> 1];
            }
        }
        else
        {
            y = x;
        }

        var values = new object?[4];
        for (i = 0; i < 4; i++)
        {
            values[i] = new CssKinds.CornerSpec(x[i], y[i]);
        }

        return values;
    }

    /// <summary>width, style and color in any order, as parse_border_side.</summary>
    public static object?[]? BorderSide(CssReader r, CssShorthand s, CssParseContext pc)
    {
        var values = new object?[3];
        do
        {
            if (values[0] is null && CssNumeric.CanParse(r))
            {
                values[0] = CssNumeric.Parse(r, CssValueParsers.PositiveLength);
                if (values[0] is null)
                {
                    return null;
                }
            }
            else if (values[1] is null && TryBorderStyle(r) is { } style)
            {
                values[1] = style;
            }
            else if (values[2] is null)
            {
                values[2] = CssColorParser.Parse(r);
                if (values[2] is null)
                {
                    return null;
                }
            }
            else
            {
                break;
            }
        }
        while (!r.AtEnd);

        return values;
    }

    private static object? TryBorderStyle(CssReader r)
    {
        var save = r.Position;
        if (r.Peek() is not { Type: CssTokenType.Ident })
        {
            return null;
        }

        var v = CssKinds.BorderStyle.Parse(r, CssParseContext.Default);
        if (v is null)
        {
            r.Position = save;
        }

        return v;
    }

    /// <summary>As parse_border: the border-image longhands are reset, not set.</summary>
    public static object?[]? Border(CssReader r, CssShorthand s, CssParseContext pc)
    {
        var values = new object?[16];
        do
        {
            if (values[0] is null && CssNumeric.CanParse(r))
            {
                var n = CssNumeric.Parse(r, CssValueParsers.PositiveLength);
                if (n is null)
                {
                    return null;
                }

                values[0] = values[1] = values[2] = values[3] = n;
            }
            else if (values[4] is null && TryBorderStyle(r) is { } style)
            {
                values[4] = values[5] = values[6] = values[7] = style;
            }
            else if (values[8] is null)
            {
                var c = CssColorParser.Parse(r);
                if (c is null)
                {
                    return null;
                }

                values[8] = values[9] = values[10] = values[11] = c;
            }
            else
            {
                break;
            }
        }
        while (!r.AtEnd);

        return values;
    }

    public static object?[]? BorderImage(CssReader r, CssShorthand s, CssParseContext pc)
    {
        var values = new object?[4];
        do
        {
            if (values[0] is null && (r.PeekIdent("none") || CssValueParsers.CanParseImage(r)))
            {
                values[0] = CssKinds.Image.Parse(r, pc);
                if (values[0] is null)
                {
                    return null;
                }
            }
            else if (values[3] is null && CssKinds.BorderImageRepeatKind.TryParse(r) is { } rep)
            {
                values[3] = rep;
            }
            else if (values[1] is null && CssNumeric.CanParse(r))
            {
                values[1] = CssKinds.BorderImageSlice.Parse(r, pc);
                if (values[1] is null)
                {
                    return null;
                }

                if (r.TryDelim('/'))
                {
                    values[2] = CssKinds.BorderImageWidth.Parse(r, pc);
                    if (values[2] is null)
                    {
                        return null;
                    }
                }
            }
            else
            {
                break;
            }
        }
        while (!r.AtEnd);

        return values;
    }

    /// <summary>As parse_background: layers of image, position / size, repeat, origin, clip, and a color.</summary>
    public static object?[]? Background(CssReader r, CssShorthand s, CssParseContext pc)
    {
        var images = new List<object>();
        var positions = new List<object>();
        var sizes = new List<object>();
        var repeats = new List<CssBackgroundRepeat>();
        var clips = new List<CssArea>();
        var origins = new List<CssArea>();
        CssColorExpr? color = null;
        do
        {
            object? image = null, position = null, size = null;
            CssBackgroundRepeat? repeat = null;
            CssArea? clip = null, origin = null;
            do
            {
                if (image is null && (r.PeekIdent("none") || CssValueParsers.CanParseImage(r)))
                {
                    image = CssKinds.ParseImageOrNone(r, pc);
                    if (image is null)
                    {
                        return null;
                    }
                }
                else if (position is null && CssValueParsers.TryParsePosition(r) is { } p)
                {
                    position = p;
                    if (r.TryDelim('/'))
                    {
                        size = CssKinds.BackgroundSizeKind.TryParseOne(r);
                        if (size is null)
                        {
                            r.Fail("Expected a background size");
                            return null;
                        }
                    }
                }
                else if (repeat is null && CssKinds.TryParseRepeat(r) is { } rep)
                {
                    repeat = rep;
                }
                else if ((clip is null || origin is null) && TryArea(r) is { } area)
                {
                    if (origin is null)
                    {
                        origin = area;
                    }
                    else
                    {
                        clip = area;
                    }
                }
                else if (color is null)
                {
                    color = CssColorParser.Parse(r);
                    if (color is null)
                    {
                        return null;
                    }
                }
                else
                {
                    break;
                }
            }
            while (!r.AtEnd && !r.PeekComma);

            if (origin is not null && clip is null)
            {
                clip = origin;
            }

            images.Add(image ?? CssKinds.s_noImage);
            positions.Add(position ?? new CssPosition(CssNumber.Pct(0), CssNumber.Pct(0)));
            sizes.Add(size ?? CssBackgroundSize.Auto);
            repeats.Add(repeat ?? CssBackgroundRepeat.Repeat);
            clips.Add(clip ?? CssArea.BorderBox);
            origins.Add(origin ?? CssArea.PaddingBox);
        }
        while (r.TryComma());

        return
        [
            new CssKinds.BackgroundImageSpec(images.ToArray()),
            new CssKinds.ListSpec<CssPosition>(positions.ToArray()),
            new CssKinds.ListSpec<CssBackgroundSize>(sizes.ToArray()),
            new CssList<CssBackgroundRepeat>(repeats.ToArray()),
            new CssList<CssArea>(clips.ToArray()),
            new CssList<CssArea>(origins.ToArray()),
            color,
        ];
    }

    private static CssArea? TryArea(CssReader r)
    {
        if (r.TryIdent("border-box"))
        {
            return CssArea.BorderBox;
        }

        if (r.TryIdent("padding-box"))
        {
            return CssArea.PaddingBox;
        }

        if (r.TryIdent("content-box"))
        {
            return CssArea.ContentBox;
        }

        return null;
    }

    private static double? TryTime(CssReader r)
    {
        var c = r.Peek();
        if (c is null || !CssNumeric.CanParse(r))
        {
            return null;
        }

        var save = r.Position;
        var n = CssNumeric.Parse(r, CssNumberFlags.Time);
        if (n is null)
        {
            r.Position = save;
            return null;
        }

        return n.Compute(CssNumericContext.Default).Value;
    }

    /// <summary>As parse_transition: property, duration, delay and timing function per layer.</summary>
    public static object?[]? Transition(CssReader r, CssShorthand s, CssParseContext pc)
    {
        var props = new List<string>();
        var durations = new List<double>();
        var delays = new List<double>();
        var timings = new List<CssEasing>();
        do
        {
            string? prop = null;
            double? duration = null, delay = null;
            CssEasing? timing = null;
            while (!r.AtEnd && !r.PeekComma)
            {
                if (timing is null && CssValueParsers.CanParseEasing(r))
                {
                    timing = CssValueParsers.ParseEasing(r);
                    if (timing is null)
                    {
                        return null;
                    }
                }
                else if (duration is null && TryTime(r) is { } d)
                {
                    duration = d;
                }
                else if (delay is null && TryTime(r) is { } dl)
                {
                    delay = dl;
                }
                else if (prop is null && r.TryAnyIdent() is { } id)
                {
                    prop = id.ToLowerInvariant();
                }
                else
                {
                    r.Fail("Junk in transition");
                    return null;
                }
            }

            props.Add(prop ?? "all");
            durations.Add(duration ?? 0);
            delays.Add(delay ?? 0);
            timings.Add(timing ?? CssEasing.Ease);
        }
        while (r.TryComma());

        return
        [
            new CssList<string>(props.ToArray()),
            new CssList<double>(durations.ToArray()),
            new CssList<double>(delays.ToArray()),
            new CssList<CssEasing>(timings.ToArray()),
        ];
    }

    private static readonly string[] s_directions = ["normal", "reverse", "alternate", "alternate-reverse"];
    private static readonly string[] s_fillModes = ["none", "forwards", "backwards", "both"];

    public static object?[]? Animation(CssReader r, CssShorthand s, CssParseContext pc)
    {
        var names = new List<string>();
        var counts = new List<double>();
        var durations = new List<double>();
        var delays = new List<double>();
        var timings = new List<CssEasing>();
        var directions = new List<string>();
        var fills = new List<string>();
        do
        {
            string? name = null, direction = null, fill = null;
            double? count = null, duration = null, delay = null;
            CssEasing? timing = null;
            while (!r.AtEnd && !r.PeekComma)
            {
                if (timing is null && CssValueParsers.CanParseEasing(r))
                {
                    timing = CssValueParsers.ParseEasing(r);
                    if (timing is null)
                    {
                        return null;
                    }
                }
                else if (count is null && r.TryIdent("infinite"))
                {
                    count = double.PositiveInfinity;
                }
                else if (count is null && r.Peek() is { Type: CssTokenType.Number } n)
                {
                    r.Next();
                    count = n.Token.Number;
                }
                else if (duration is null && TryTime(r) is { } d)
                {
                    duration = d;
                }
                else if (delay is null && TryTime(r) is { } dl)
                {
                    delay = dl;
                }
                else if (direction is null && TryWord(r, s_directions) is { } dir)
                {
                    direction = dir;
                }
                else if (fill is null && TryWord(r, s_fillModes) is { } f)
                {
                    fill = f;
                }
                else if (name is null && r.TryAnyIdent() is { } id)
                {
                    name = id;
                }
                else
                {
                    r.Fail("Junk in animation");
                    return null;
                }
            }

            names.Add(name ?? "none");
            counts.Add(count ?? 1);
            durations.Add(duration ?? 0);
            delays.Add(delay ?? 0);
            timings.Add(timing ?? CssEasing.Ease);
            directions.Add(direction ?? "normal");
            fills.Add(fill ?? "none");
        }
        while (r.TryComma());

        return
        [
            new CssList<string>(names.ToArray()),
            new CssList<double>(counts.ToArray()),
            new CssList<double>(durations.ToArray()),
            new CssList<double>(delays.ToArray()),
            new CssList<CssEasing>(timings.ToArray()),
            new CssList<string>(directions.ToArray()),
            new CssList<string>(fills.ToArray()),
        ];
    }

    private static string? TryWord(CssReader r, string[] words)
    {
        foreach (var w in words)
        {
            if (r.TryIdent(w))
            {
                return w;
            }
        }

        return null;
    }

    public static object?[]? TextDecoration(CssReader r, CssShorthand s, CssParseContext pc)
    {
        var values = new object?[3];
        var lines = new List<string>();
        while (!r.AtEnd)
        {
            if (TryWord(r, ["underline", "overline", "line-through"]) is { } line)
            {
                if (lines.Contains(line))
                {
                    r.Fail("Invalid combination of text-decoration-line values");
                    return null;
                }

                lines.Add(line);
            }
            else if (lines.Count == 0 && values[0] is null && r.TryIdent("none"))
            {
                values[0] = "none";
            }
            else if (values[1] is null && TryWord(r, ["solid", "double", "wavy"]) is { } style)
            {
                values[1] = style;
            }
            else if (values[2] is null)
            {
                values[2] = CssColorParser.Parse(r);
                if (values[2] is null)
                {
                    return null;
                }
            }
            else
            {
                break;
            }
        }

        if (lines.Count > 0)
        {
            string[] order = ["underline", "overline", "line-through"];
            values[0] = string.Join(' ', System.Array.FindAll(order, lines.Contains));
        }

        return values;
    }

    public static object?[]? FontVariant(CssReader r, CssShorthand s, CssParseContext pc)
    {
        var values = new object?[6];
        if (r.TryIdent("normal"))
        {
            return values;
        }

        if (r.TryIdent("none"))
        {
            values[0] = "none";
            return values;
        }

        // Each keyword belongs to exactly one longhand; collect them per longhand and let the longhand parse them.
        var parts = new List<CssComponent>[6];
        for (var i = 0; i < 6; i++)
        {
            parts[i] = new List<CssComponent>();
        }

        while (r.Peek() is { Type: CssTokenType.Ident } c)
        {
            var placed = false;
            for (var i = 0; i < 6 && !placed; i++)
            {
                var probe = new CssReader([c]);
                if (s.Longhand(i).Kind.Parse(probe, pc) is not null && probe.AtEnd && !c.IsIdent("normal"))
                {
                    parts[i].Add(c);
                    parts[i].Add(new CssComponent(new CssToken(CssTokenType.Whitespace, " ", 0, false, false, false, 0, 0)));
                    placed = true;
                }
            }

            if (!placed)
            {
                r.Fail($"Unknown font-variant value {c.Token.Text}");
                return null;
            }

            r.Next();
        }

        for (var i = 0; i < 6; i++)
        {
            if (parts[i].Count == 0)
            {
                continue;
            }

            var pr = new CssReader(parts[i]);
            values[i] = s.Longhand(i).Kind.Parse(pr, pc);
            if (values[i] is null || !pr.AtEnd)
            {
                r.Absorb(pr);
                r.Fail("Invalid combination of font-variant values");
                return null;
            }
        }

        return values;
    }

    /// <summary>As parse_font: [style || small-caps || weight || width]? size family.</summary>
    public static object?[]? Font(CssReader r, CssShorthand s, CssParseContext pc)
    {
        // Longhands: family, style, variant-caps, weight, width, size.
        var values = new object?[6];
        bool parsedOne;
        do
        {
            parsedOne = false;
            if (values[1] is null && TryWord(r, ["normal", "oblique", "italic"]) is { } style)
            {
                values[1] = style;
                parsedOne = true;
            }

            if (values[2] is null && TryWord(r, ["small-caps"]) is { } caps)
            {
                values[2] = caps;
                parsedOne = true;
            }

            if (values[3] is null)
            {
                if (TryWord(r, ["bold", "bolder", "lighter"]) is { } w)
                {
                    values[3] = CssKinds.FontWeight.Parse(new CssReader(CssComponentParser.Parse(w)), pc);
                    parsedOne = true;
                }
                else if (r.Peek() is { Type: CssTokenType.Number } n)
                {
                    if (n.Token.Number < 1 || n.Token.Number > 1000)
                    {
                        r.Fail("Font weight values must be between 1 and 1000");
                        return null;
                    }

                    r.Next();
                    values[3] = n.Token.Number;
                    parsedOne = true;
                }
            }

            if (values[4] is null && TryWord(r, ["ultra-condensed", "extra-condensed", "condensed", "semi-condensed", "semi-expanded", "expanded", "extra-expanded", "ultra-expanded"]) is { } width)
            {
                values[4] = width;
                parsedOne = true;
            }
        }
        while (parsedOne && !r.AtEnd);

        values[5] = CssKinds.FontSize.Parse(r, pc);
        if (values[5] is null)
        {
            return null;
        }

        values[0] = CssKinds.FontFamily.Parse(r, pc);
        return values[0] is null ? null : values;
    }

    public static object?[]? FontStretch(CssReader r, CssShorthand s, CssParseContext pc)
    {
        if (r.Peek() is { Type: CssTokenType.Percentage } p)
        {
            r.Next();
            var pct = p.Token.Number;
            (double Max, string Name)[] map =
            [
                (56.25, "ultra-condensed"), (68.75, "extra-condensed"), (81.25, "condensed"), (93.75, "semi-condensed"),
                (106.25, "normal"), (118.75, "semi-expanded"), (137.5, "expanded"), (175, "extra-expanded"), (double.MaxValue, "ultra-expanded"),
            ];
            foreach (var (max, name) in map)
            {
                if (pct < max)
                {
                    return [name];
                }
            }
        }

        var v = CssKinds.FontWidth.Parse(r, pc);
        return v is null ? null : [v];
    }

    public static object?[]? All(CssReader r, CssShorthand s, CssParseContext pc)
    {
        r.Fail("The 'all' property can only be set to 'initial', 'inherit' or 'unset'");
        return null;
    }
}
