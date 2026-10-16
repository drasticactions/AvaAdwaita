using System.Collections.Generic;
using AvaAdwaita.CssEngine.Syntax;

namespace AvaAdwaita.CssEngine.Selectors;

/// <summary>Parses selector lists.</summary>
/// <remarks>Port of _gtk_css_selector_parse (gtkcssselector.c): <c>:not()</c> takes one simple selector.</remarks>
internal static class CssSelectorParser
{
    /// <summary>Parses a comma-separated selector list, or returns null and sets <paramref name="error"/>.</summary>
    public static List<CssSelector>? ParseList(IReadOnlyList<CssComponent> prelude, out CssParseError? error)
    {
        error = null;
        var list = new List<CssSelector>();
        foreach (var part in CssReader.SplitCommas(prelude))
        {
            var sel = Parse(part, out error);
            if (sel is null)
            {
                return null;
            }

            list.Add(sel);
        }

        return list;
    }

    private static CssParseError Err(CssErrorKind kind, string msg, CssComponent? at) =>
        new(kind, msg, at?.Token.Line ?? 0, at?.Token.Column ?? 0);

    private static CssSelector? Parse(List<CssComponent> items, out CssParseError? error)
    {
        error = null;
        var compounds = new List<CssCompound>();
        var combinators = new List<CssCombinator>();
        var i = 0;
        Skip(items, ref i);
        if (i >= items.Count)
        {
            error = Err(CssErrorKind.Syntax, "Expected a valid selector", items.Count > 0 ? items[0] : null);
            return null;
        }

        while (true)
        {
            var compound = ParseCompound(items, ref i, out error);
            if (compound is null)
            {
                return null;
            }

            compounds.Add(compound);

            var sawSpace = Skip(items, ref i);
            if (i >= items.Count)
            {
                break;
            }

            var c = items[i];
            CssCombinator comb;
            if (c.IsDelim('>'))
            {
                comb = CssCombinator.Child;
                i++;
            }
            else if (c.IsDelim('+'))
            {
                comb = CssCombinator.Adjacent;
                i++;
            }
            else if (c.IsDelim('~'))
            {
                comb = CssCombinator.Sibling;
                i++;
            }
            else if (sawSpace)
            {
                comb = CssCombinator.Descendant;
            }
            else
            {
                error = Err(CssErrorKind.Syntax, "Expected a valid selector", c);
                return null;
            }

            combinators.Add(comb);
            Skip(items, ref i);
            if (i >= items.Count)
            {
                error = Err(CssErrorKind.Syntax, "Expected a selector after a combinator", c);
                return null;
            }
        }

        return new CssSelector(compounds.ToArray(), combinators.ToArray());
    }

    private static bool Skip(List<CssComponent> items, ref int i)
    {
        var any = false;
        while (i < items.Count && items[i].IsWhitespace)
        {
            i++;
            any = true;
        }

        return any;
    }

    private static CssCompound? ParseCompound(List<CssComponent> items, ref int i, out CssParseError? error)
    {
        error = null;
        var parts = new List<CssSimpleSelector>();
        var first = true;
        while (i < items.Count)
        {
            var c = items[i];
            if (first && c.IsDelim('*'))
            {
                parts.Add(new CssSimpleSelector(CssSimpleKind.Any, false, null, 0, 0, 0));
                i++;
            }
            else if (first && c.Type == CssTokenType.Ident)
            {
                parts.Add(new CssSimpleSelector(CssSimpleKind.Name, false, c.Token.Text, 0, 0, 0));
                i++;
            }
            else if (c.Type == CssTokenType.Hash && c.Token.IsIdHash)
            {
                parts.Add(new CssSimpleSelector(CssSimpleKind.Id, false, c.Token.Text, 0, 0, 0));
                i++;
            }
            else if (c.IsDelim('.'))
            {
                if (i + 1 >= items.Count || items[i + 1].Type != CssTokenType.Ident)
                {
                    error = Err(CssErrorKind.Syntax, "No class name after '.' in selector", c);
                    return null;
                }

                parts.Add(new CssSimpleSelector(CssSimpleKind.Class, false, items[i + 1].Token.Text, 0, 0, 0));
                i += 2;
            }
            else if (c.Type == CssTokenType.Colon)
            {
                i++;
                if (i >= items.Count)
                {
                    error = Err(CssErrorKind.Syntax, "Expected a pseudo-class", c);
                    return null;
                }

                var p = ParsePseudo(items[i], negate: false, parts, out error);
                if (!p)
                {
                    return null;
                }

                i++;
            }
            else
            {
                break;
            }

            first = false;
        }

        if (parts.Count == 0)
        {
            error = Err(CssErrorKind.Syntax, "Expected a valid selector", i < items.Count ? items[i] : null);
            return null;
        }

        return new CssCompound(parts.ToArray());
    }

    private static readonly (string Name, CssSimpleKind Kind, int A, int B)[] s_positions =
    [
        ("first-child", CssSimpleKind.PositionForward, 0, 1),
        ("last-child", CssSimpleKind.PositionBackward, 0, 1),
        ("only-child", CssSimpleKind.PositionOnly, 0, 0),
    ];

    private static bool ParsePseudo(CssComponent c, bool negate, List<CssSimpleSelector> parts, out CssParseError? error)
    {
        error = null;
        if (c.Type == CssTokenType.Ident)
        {
            var name = c.Token.Text;
            if (CssStateNames.FromName(name) is { } st)
            {
                parts.Add(new CssSimpleSelector(CssSimpleKind.State, negate, null, st, 0, 0));
                return true;
            }

            foreach (var (n, kind, a, b) in s_positions)
            {
                if (string.Equals(n, name, System.StringComparison.OrdinalIgnoreCase))
                {
                    parts.Add(new CssSimpleSelector(kind, negate, null, 0, a, b));
                    return true;
                }
            }

            if (string.Equals(name, "root", System.StringComparison.OrdinalIgnoreCase))
            {
                parts.Add(new CssSimpleSelector(CssSimpleKind.Root, negate, null, 0, 0, 0));
                return true;
            }

            error = Err(CssErrorKind.UnknownPseudoClass, $"Unknown name of pseudo-class: {name}", c);
            return false;
        }

        if (c.IsFunction)
        {
            var fn = c.Token.Text.ToLowerInvariant();
            var args = new CssReader(c.Children);
            switch (fn)
            {
                case "nth-child":
                case "nth-last-child":
                    if (!ParseAnPlusB(c.Children, out var a, out var b))
                    {
                        error = Err(CssErrorKind.Syntax, $"Invalid argument to :{fn}()", c);
                        return false;
                    }

                    parts.Add(new CssSimpleSelector(fn == "nth-child" ? CssSimpleKind.PositionForward : CssSimpleKind.PositionBackward, negate, null, 0, a, b));
                    return true;
                case "dir":
                    if (args.TryIdent("ltr") && args.AtEnd)
                    {
                        parts.Add(new CssSimpleSelector(CssSimpleKind.State, negate, null, CssStates.DirLtr, 0, 0));
                        return true;
                    }

                    if (args.TryIdent("rtl") && args.AtEnd)
                    {
                        parts.Add(new CssSimpleSelector(CssSimpleKind.State, negate, null, CssStates.DirRtl, 0, 0));
                        return true;
                    }

                    error = Err(CssErrorKind.BadValue, "Expected \"ltr\" or \"rtl\"", c);
                    return false;
                case "drop":
                    if (args.TryIdent("active") && args.AtEnd)
                    {
                        parts.Add(new CssSimpleSelector(CssSimpleKind.State, negate, null, CssStates.DropActive, 0, 0));
                        return true;
                    }

                    error = Err(CssErrorKind.BadValue, "Expected \"active\"", c);
                    return false;
                case "not":
                    if (negate)
                    {
                        error = Err(CssErrorKind.Syntax, "Nesting of :not() not allowed", c);
                        return false;
                    }

                    return ParseNot(c, parts, out error);
                default:
                    error = Err(CssErrorKind.UnknownPseudoClass, $"Unknown pseudoclass: {fn}()", c);
                    return false;
            }
        }

        error = Err(CssErrorKind.UnknownPseudoClass, "Unknown pseudoclass", c);
        return false;
    }

    private static bool ParseNot(CssComponent fn, List<CssSimpleSelector> parts, out CssParseError? error)
    {
        error = null;
        var items = new List<CssComponent>();
        foreach (var x in fn.Children)
        {
            if (!x.IsWhitespace)
            {
                items.Add(x);
            }
        }

        if (items.Count == 1 && items[0].IsDelim('*'))
        {
            parts.Add(new CssSimpleSelector(CssSimpleKind.Any, true, null, 0, 0, 0));
            return true;
        }

        if (items.Count == 1 && items[0].Type == CssTokenType.Ident)
        {
            parts.Add(new CssSimpleSelector(CssSimpleKind.Name, true, items[0].Token.Text, 0, 0, 0));
            return true;
        }

        if (items.Count == 1 && items[0].Type == CssTokenType.Hash)
        {
            parts.Add(new CssSimpleSelector(CssSimpleKind.Id, true, items[0].Token.Text, 0, 0, 0));
            return true;
        }

        if (items.Count == 2 && items[0].IsDelim('.') && items[1].Type == CssTokenType.Ident)
        {
            parts.Add(new CssSimpleSelector(CssSimpleKind.Class, true, items[1].Token.Text, 0, 0, 0));
            return true;
        }

        if (items.Count == 2 && items[0].Type == CssTokenType.Colon)
        {
            return ParsePseudo(items[1], negate: true, parts, out error);
        }

        error = Err(CssErrorKind.Syntax, "Invalid contents of :not() selector", fn);
        return false;
    }

    /// <summary>Parses <c>an+b</c>, <c>odd</c>, <c>even</c> or an integer.</summary>
    private static bool ParseAnPlusB(CssComponent[] args, out int a, out int b)
    {
        var text = CssComponent.ToCss(args).Replace(" ", string.Empty, System.StringComparison.Ordinal).ToLowerInvariant();
        a = 0;
        b = 0;
        if (text == "odd")
        {
            a = 2;
            b = 1;
            return true;
        }

        if (text == "even")
        {
            a = 2;
            b = 0;
            return true;
        }

        var n = text.IndexOf('n');
        if (n < 0)
        {
            return int.TryParse(text, System.Globalization.CultureInfo.InvariantCulture, out b);
        }

        var aPart = text[..n];
        a = aPart switch
        {
            "" or "+" => 1,
            "-" => -1,
            _ => int.TryParse(aPart, System.Globalization.CultureInfo.InvariantCulture, out var av) ? av : int.MinValue,
        };
        if (a == int.MinValue)
        {
            return false;
        }

        var bPart = text[(n + 1)..];
        if (bPart.Length == 0)
        {
            return true;
        }

        return int.TryParse(bPart, System.Globalization.NumberStyles.AllowLeadingSign, System.Globalization.CultureInfo.InvariantCulture, out b);
    }
}
