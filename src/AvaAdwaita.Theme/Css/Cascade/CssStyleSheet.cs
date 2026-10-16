using System;
using System.Collections.Generic;
using AvaAdwaita.CssEngine.Properties;
using AvaAdwaita.CssEngine.Selectors;
using AvaAdwaita.CssEngine.Syntax;
using AvaAdwaita.CssEngine.Values;

namespace AvaAdwaita.CssEngine.Cascade;

/// <summary>A parsed style sheet: style rules, named colors and keyframes, with the problems found while parsing.</summary>
/// <remarks>Follows the rule parsing of gtkcssprovider.c.</remarks>
public sealed class CssStyleSheet
{
    private readonly List<CssParseError> _errors = new();
    private readonly Func<string, string?>? _import;
    private readonly CssParseContext _pc;
    private readonly bool _lazy;
    private int _order;
    private int _importDepth;

    private CssStyleSheet(CssOrigin origin, string name, string? baseUri, Func<string, string?>? import, bool lazy)
    {
        _lazy = lazy;
        Origin = origin;
        Name = name;
        BaseUri = baseUri;
        _import = import;
        _pc = new CssParseContext(ResolveUrl);
    }

    /// <summary>The origin.</summary>
    public CssOrigin Origin { get; }

    /// <summary>A name for messages.</summary>
    public string Name { get; }

    /// <summary>The base that relative <c>url()</c> values resolve against.</summary>
    public string? BaseUri { get; }

    /// <summary>The problems found while parsing.</summary>
    public IReadOnlyList<CssParseError> Errors => _errors;

    /// <summary>True when parsing found nothing but warnings.</summary>
    public bool IsClean => _errors.TrueForAll(e => e.Kind == CssErrorKind.Warning);

    /// <summary>The number of style rules.</summary>
    public int RuleCount => Rules.Count;

    internal List<CssRule> Rules { get; } = new();

    internal List<CssColorDefinition> Colors { get; } = new();

    internal List<CssKeyframes> Keyframes { get; } = new();

    /// <summary>Parses a style sheet.</summary>
    /// <param name="text">The CSS.</param>
    /// <param name="origin">Where the sheet comes from.</param>
    /// <param name="name">A name for messages.</param>
    /// <param name="baseUri">The base for relative URLs, ending in '/'.</param>
    /// <param name="import">Loads the text of an <c>@import</c>ed URL, or null.</param>
    /// <param name="lazy">
    /// Parse declaration values on first use. Only for CSS already known to parse cleanly: value
    /// errors are then not reported.
    /// </param>
    public static CssStyleSheet Parse(string text, CssOrigin origin = CssOrigin.Application, string? name = null, string? baseUri = null, Func<string, string?>? import = null, bool lazy = false)
    {
        var sheet = new CssStyleSheet(origin, name ?? "style sheet", baseUri, import, lazy);
        sheet.ParseInto(text, null);
        return sheet;
    }

    private string ResolveUrl(string url)
    {
        if (BaseUri is null || url.Contains(':', StringComparison.Ordinal) || url.StartsWith('/'))
        {
            return url;
        }

        return BaseUri.EndsWith('/') ? BaseUri + url : BaseUri + "/" + url;
    }

    private void ParseInto(string text, CssMediaQuery? media)
    {
        var items = CssComponentParser.Parse(text, deferRuleBlocks: _lazy);
        ParseRules(items, media, topLevel: true);
    }

    private void Error(CssErrorKind kind, string message, CssComponent? at) =>
        _errors.Add(new CssParseError(kind, message, at?.Token.Line ?? 0, at?.Token.Column ?? 0));

    private void ParseRules(IReadOnlyList<CssComponent> items, CssMediaQuery? media, bool topLevel)
    {
        var i = 0;
        while (i < items.Count)
        {
            var c = items[i];
            if (c.IsWhitespace || c.Type is CssTokenType.Cdo or CssTokenType.Cdc || c.Type == CssTokenType.Semicolon)
            {
                i++;
                continue;
            }

            var prelude = new List<CssComponent>();
            CssComponent? block = null;
            var start = c;
            if (c.Type == CssTokenType.AtKeyword)
            {
                i++;
                while (i < items.Count)
                {
                    var x = items[i++];
                    if (x.Type == CssTokenType.Semicolon)
                    {
                        break;
                    }

                    if (x.Type == CssTokenType.OpenCurly)
                    {
                        block = x;
                        break;
                    }

                    prelude.Add(x);
                }

                ParseAtRule(start, prelude, block, media);
                continue;
            }

            while (i < items.Count)
            {
                var x = items[i++];
                if (x.Type == CssTokenType.OpenCurly)
                {
                    block = x;
                    break;
                }

                prelude.Add(x);
            }

            if (block is null)
            {
                Error(CssErrorKind.Syntax, "Expected a '{' after the selector", start);
                break;
            }

            ParseStyleRule(start, prelude, block, media);
        }
    }

    private void ParseAtRule(CssComponent at, List<CssComponent> prelude, CssComponent? block, CssMediaQuery? media)
    {
        var name = at.Token.Text.ToLowerInvariant();
        switch (name)
        {
            case "define-color":
                {
                    var r = new CssReader(prelude);
                    var colorName = r.TryAnyIdent();
                    if (colorName is null)
                    {
                        Error(CssErrorKind.Syntax, "Expected name for @define-color", at);
                        return;
                    }

                    var value = CssColorParser.Parse(r);
                    if (value is null || !r.AtEnd)
                    {
                        Error(r.UnknownFunction is not null ? CssErrorKind.UnknownFunction : CssErrorKind.BadValue, $"@define-color {colorName}: {r.Error ?? "junk at end of value"}", at);
                        return;
                    }

                    if (block is not null)
                    {
                        Error(CssErrorKind.Syntax, "Junk at end of @define-color", block);
                    }

                    Colors.Add(new CssColorDefinition(colorName, value, media));
                    return;
                }

            case "media":
                {
                    if (block is null)
                    {
                        Error(CssErrorKind.Syntax, "Expected a '{' after @media", at);
                        return;
                    }

                    var q = CssMediaQuery.Parse(prelude, _errors);
                    if (q is null)
                    {
                        return;
                    }

                    ParseRules(block.Children, CssMediaQuery.And(media, q), topLevel: false);
                    return;
                }

            case "keyframes":
                {
                    var r = new CssReader(prelude);
                    var kname = r.TryAnyIdent();
                    if (kname is null || block is null)
                    {
                        Error(CssErrorKind.Syntax, "Expected a name and a block for @keyframes", at);
                        return;
                    }

                    ParseKeyframes(kname, block, media);
                    return;
                }

            case "import":
                {
                    var r = new CssReader(prelude);
                    string? url = null;
                    if (r.Peek() is { } u)
                    {
                        if (u.Type is CssTokenType.Url or CssTokenType.String)
                        {
                            url = u.Token.Text;
                        }
                        else if (u.IsFunctionNamed("url") && Array.Find(u.Children, x => x.Type == CssTokenType.String) is { } s)
                        {
                            url = s.Token.Text;
                        }
                    }

                    if (url is null)
                    {
                        Error(CssErrorKind.Syntax, "Expected a URL for @import", at);
                        return;
                    }

                    var resolved = ResolveUrl(url);
                    var text = _importDepth < 8 ? _import?.Invoke(resolved) : null;
                    if (text is null)
                    {
                        Error(CssErrorKind.BadValue, $"Could not load @import {resolved}", at);
                        return;
                    }

                    _importDepth++;
                    ParseInto(text, media);
                    _importDepth--;
                    return;
                }

            default:
                Error(CssErrorKind.UnknownAtRule, $"Unknown @ rule @{at.Token.Text}", at);
                return;
        }
    }

    private void ParseKeyframes(string name, CssComponent block, CssMediaQuery? media)
    {
        var frames = new List<(double, CssDeclaration[])>();
        var items = block.Children;
        var i = 0;
        while (i < items.Length)
        {
            if (items[i].IsWhitespace)
            {
                i++;
                continue;
            }

            var prelude = new List<CssComponent>();
            CssComponent? body = null;
            while (i < items.Length)
            {
                var x = items[i++];
                if (x.Type == CssTokenType.OpenCurly)
                {
                    body = x;
                    break;
                }

                prelude.Add(x);
            }

            if (body is null)
            {
                Error(CssErrorKind.Syntax, "Expected a '{' in @keyframes", block);
                return;
            }

            var offsets = new List<double>();
            foreach (var part in CssReader.SplitCommas(prelude))
            {
                var r = new CssReader(part);
                if (r.TryIdent("from"))
                {
                    offsets.Add(0);
                }
                else if (r.TryIdent("to"))
                {
                    offsets.Add(1);
                }
                else if (r.Peek() is { Type: CssTokenType.Percentage } p)
                {
                    r.Next();
                    offsets.Add(Math.Clamp(p.Token.Number / 100, 0, 1));
                }
                else
                {
                    Error(CssErrorKind.Syntax, "Expected a keyframe selector", block);
                    return;
                }
            }

            var decls = ParseDeclarations(body.Children);
            foreach (var o in offsets)
            {
                frames.Add((o, decls));
            }
        }

        frames.Sort((a, b) => a.Item1.CompareTo(b.Item1));
        Keyframes.Add(new CssKeyframes(name, frames, media));
    }

    private void ParseStyleRule(CssComponent start, List<CssComponent> prelude, CssComponent block, CssMediaQuery? media)
    {
        var selectors = CssSelectorParser.ParseList(prelude, out var error);
        if (selectors is null)
        {
            _errors.Add(error!);
            return;
        }

        if (block.IsDeferred)
        {
            Rules.Add(new CssRule
            {
                Selectors = selectors,
                ParseDeclarations = () => ParseDeclarations(block.Children),
                Media = media,
                Order = _order++,
                Line = start.Token.Line,
            });
            return;
        }

        Rules.Add(new CssRule
        {
            Selectors = selectors,
            Declarations = ParseDeclarations(block.Children),
            Media = media,
            Order = _order++,
            Line = start.Token.Line,
        });
    }

    private CssDeclaration[] ParseDeclarations(IReadOnlyList<CssComponent> items)
    {
        var result = new List<CssDeclaration>();
        var part = new List<CssComponent>();
        foreach (var c in items)
        {
            if (c.Type == CssTokenType.Semicolon)
            {
                ParseDeclaration(part, result);
                part.Clear();
            }
            else
            {
                part.Add(c);
            }
        }

        ParseDeclaration(part, result);
        return result.ToArray();
    }

    private static CssComponent[] Trim(List<CssComponent> items, int start)
    {
        var s = start;
        var e = items.Count;
        while (s < e && items[s].IsWhitespace)
        {
            s++;
        }

        while (e > s && items[e - 1].IsWhitespace)
        {
            e--;
        }

        return items.GetRange(s, e - s).ToArray();
    }

    private void ParseDeclaration(List<CssComponent> part, List<CssDeclaration> result)
    {
        var i = 0;
        while (i < part.Count && part[i].IsWhitespace)
        {
            i++;
        }

        if (i >= part.Count)
        {
            return;
        }

        var nameTok = part[i];
        if (nameTok.Type != CssTokenType.Ident)
        {
            Error(CssErrorKind.Syntax, "Expected a property name", nameTok);
            return;
        }

        i++;
        while (i < part.Count && part[i].IsWhitespace)
        {
            i++;
        }

        if (i >= part.Count || part[i].Type != CssTokenType.Colon)
        {
            Error(CssErrorKind.Syntax, $"Expected ':' after {nameTok.Token.Text}", nameTok);
            return;
        }

        var value = Trim(part, i + 1);
        value = StripImportant(value, nameTok);
        var name = nameTok.Token.Text;
        var line = nameTok.Token.Line;

        if (name.StartsWith("--", StringComparison.Ordinal))
        {
            result.Add(new CssDeclaration { PropertyId = -1, CustomName = name, Raw = value, Line = line });
            return;
        }

        var longhand = CssPropertyTable.Lookup(name);
        var shorthand = longhand is null ? CssPropertyTable.LookupShorthand(name) : null;
        if (longhand is null && shorthand is null)
        {
            Error(CssErrorKind.UnknownProperty, $"No property named \"{name}\"", nameTok);
            return;
        }

        if (value.Length == 0)
        {
            Error(CssErrorKind.BadValue, $"Empty value for {name}", nameTok);
            return;
        }

        if (WideKeyword(value) is { } wide)
        {
            if (longhand is not null)
            {
                result.Add(new CssDeclaration { PropertyId = longhand.Id, Value = wide, Line = line });
            }
            else
            {
                foreach (var id in shorthand!.Longhands)
                {
                    result.Add(new CssDeclaration { PropertyId = id, Value = wide, Line = line });
                }
            }

            return;
        }

        var hasVar = CssComponent.ContainsVar(value);
        if (hasVar || _lazy)
        {
            if (longhand is not null)
            {
                result.Add(new CssDeclaration { PropertyId = longhand.Id, Raw = value, HasVar = hasVar, Context = _pc, Line = line });
            }
            else
            {
                var pending = new CssPendingShorthand(shorthand!, value, hasVar, _pc);
                for (var k = 0; k < shorthand!.Longhands.Length; k++)
                {
                    result.Add(new CssDeclaration { PropertyId = shorthand.Longhands[k], Raw = value, HasVar = hasVar, Pending = pending, PendingIndex = k, Line = line });
                }
            }

            return;
        }

        var r = new CssReader(value);
        if (longhand is not null)
        {
            var v = longhand.Kind.Parse(r, _pc);
            if (v is null || !r.AtEnd)
            {
                ValueError(r, name, nameTok);
                return;
            }

            result.Add(new CssDeclaration { PropertyId = longhand.Id, Value = v, Line = line });
            return;
        }

        var values = shorthand!.Parser(r, shorthand, _pc);
        if (values is null || !r.AtEnd)
        {
            ValueError(r, name, nameTok);
            return;
        }

        for (var k = 0; k < shorthand.Longhands.Length; k++)
        {
            var id = shorthand.Longhands[k];
            result.Add(new CssDeclaration { PropertyId = id, Value = values[k] ?? CssPropertyTable.All[id].Initial, Line = line });
        }
    }

    private void ValueError(CssReader r, string name, CssComponent at)
    {
        if (r.UnknownFunction is { } fn)
        {
            Error(CssErrorKind.UnknownFunction, $"{name}: unknown function {fn}()", at);
        }
        else
        {
            Error(CssErrorKind.BadValue, $"{name}: {r.Error ?? "junk at end of value"}", at);
        }
    }

    private CssComponent[] StripImportant(CssComponent[] value, CssComponent at)
    {
        var n = value.Length;
        if (n >= 2 && value[n - 1].IsIdent("important"))
        {
            var k = n - 2;
            while (k >= 0 && value[k].IsWhitespace)
            {
                k--;
            }

            if (k >= 0 && value[k].IsDelim('!'))
            {
                Error(CssErrorKind.Warning, "!important is ignored", at);
                var e = k;
                while (e > 0 && value[e - 1].IsWhitespace)
                {
                    e--;
                }

                return value[..e];
            }
        }

        return value;
    }

    internal static CssWideKeyword? WideKeyword(IReadOnlyList<CssComponent> value)
    {
        if (value.Count != 1 || value[0].Type != CssTokenType.Ident)
        {
            return null;
        }

        var t = value[0];
        if (t.IsIdent("initial"))
        {
            return CssWideKeyword.Initial;
        }

        if (t.IsIdent("inherit"))
        {
            return CssWideKeyword.Inherit;
        }

        if (t.IsIdent("unset"))
        {
            return CssWideKeyword.Unset;
        }

        return null;
    }

    /// <inheritdoc/>
    public override string ToString() => $"{Name} ({Origin}, {Rules.Count} rules, {_errors.Count} problems)";
}
