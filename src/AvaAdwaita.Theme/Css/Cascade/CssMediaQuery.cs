using System;
using System.Collections.Generic;
using AvaAdwaita.CssEngine.Syntax;

namespace AvaAdwaita.CssEngine.Cascade;

/// <summary>A parsed <c>@media</c> condition.</summary>
/// <remarks>Port of gtkcssmediaquery.c: <c>not</c>, <c>and</c>, <c>or</c> and discrete features in parentheses.</remarks>
internal abstract class CssMediaQuery
{
    public abstract bool Evaluate(CssMediaState state);

    /// <summary>Combines this query with the one of an enclosing <c>@media</c>.</summary>
    public static CssMediaQuery? And(CssMediaQuery? outer, CssMediaQuery? inner) =>
        outer is null ? inner : inner is null ? outer : new Op(true, [outer, inner]);

    public static CssMediaQuery? Parse(IReadOnlyList<CssComponent> prelude, List<CssParseError> errors)
    {
        var r = new CssReader(prelude);
        var q = ParseCondition(r, errors);
        if (q is null)
        {
            return null;
        }

        if (!r.AtEnd)
        {
            var c = r.Peek()!;
            errors.Add(new CssParseError(CssErrorKind.Syntax, "Junk at end of @media query", c.Token.Line, c.Token.Column));
            return null;
        }

        return q;
    }

    private static CssMediaQuery? ParseCondition(CssReader r, List<CssParseError> errors)
    {
        CssMediaQuery? q;
        if (r.TryIdent("not"))
        {
            var inner = ParseInParens(r, errors);
            q = inner is null ? null : new Not(inner);
        }
        else
        {
            q = ParseInParens(r, errors);
        }

        if (q is null)
        {
            return null;
        }

        while (r.PeekIdent("and") || r.PeekIdent("or"))
        {
            var isAnd = r.TryIdent("and");
            if (!isAnd)
            {
                r.TryIdent("or");
            }

            var next = ParseInParens(r, errors);
            if (next is null)
            {
                return null;
            }

            q = new Op(isAnd, [q, next]);
        }

        return q;
    }

    private static CssMediaQuery? ParseInParens(CssReader r, List<CssParseError> errors)
    {
        var c = r.Peek();
        if (c is null || c.Type != CssTokenType.OpenParen)
        {
            errors.Add(new CssParseError(CssErrorKind.Syntax, "Expected '(' after @media query", c?.Token.Line ?? 0, c?.Token.Column ?? 0));
            return null;
        }

        r.Next();
        var inner = new CssReader(c.Children);
        if (inner.PeekIdent("not") || inner.Peek() is { Type: CssTokenType.OpenParen })
        {
            var cond = ParseCondition(inner, errors);
            return cond is not null && inner.AtEnd ? cond : null;
        }

        var name = inner.TryAnyIdent();
        if (name is null || inner.Peek() is not { Type: CssTokenType.Colon })
        {
            errors.Add(new CssParseError(CssErrorKind.Syntax, "Expected ':' after @media feature name", c.Token.Line, c.Token.Column));
            return null;
        }

        inner.Next();
        var value = inner.TryAnyIdent();
        if (value is null || !inner.AtEnd)
        {
            errors.Add(new CssParseError(CssErrorKind.Syntax, "Expected a media feature value", c.Token.Line, c.Token.Column));
            return null;
        }

        var feature = new Feature(name.ToLowerInvariant(), value.ToLowerInvariant());
        if (!feature.IsKnown)
        {
            errors.Add(new CssParseError(CssErrorKind.Warning, $"Undefined @media feature '{name}'", c.Token.Line, c.Token.Column));
        }

        return feature;
    }

    private sealed class Not(CssMediaQuery inner) : CssMediaQuery
    {
        public override bool Evaluate(CssMediaState state) => !inner.Evaluate(state);

        public override string ToString() => $"not {inner}";
    }

    private sealed class Op(bool isAnd, CssMediaQuery[] parts) : CssMediaQuery
    {
        public override bool Evaluate(CssMediaState state) =>
            isAnd ? Array.TrueForAll(parts, p => p.Evaluate(state)) : Array.Exists(parts, p => p.Evaluate(state));

        public override string ToString() => string.Join(isAnd ? " and " : " or ", (IEnumerable<CssMediaQuery>)parts);
    }

    private sealed class Feature(string name, string value) : CssMediaQuery
    {
        public bool IsKnown => name is "prefers-color-scheme" or "prefers-contrast" or "prefers-reduced-motion";

        public override bool Evaluate(CssMediaState state) => name switch
        {
            "prefers-color-scheme" => value == (state.Dark ? "dark" : "light"),
            "prefers-contrast" => value == state.Contrast switch
            {
                CssContrast.More => "more",
                CssContrast.Less => "less",
                _ => "no-preference",
            },
            "prefers-reduced-motion" => value == (state.ReducedMotion ? "reduce" : "no-preference"),
            _ => false,
        };

        public override string ToString() => $"({name}: {value})";
    }
}
