using System.Collections.Generic;

namespace AvaAdwaita.CssEngine.Syntax;

/// <summary>Groups tokens into component values (CSS Syntax Level 3, §5.4.7–5.4.9).</summary>
/// <remarks>
/// Tokens stream straight from the tokenizer. Whitespace and punctuation share one component each,
/// which keeps a parse of gtk.css from allocating an object per token.
/// </remarks>
internal sealed class CssComponentParser
{
    private static readonly CssComponent s_whitespace = Shared(CssTokenType.Whitespace, " ");
    private static readonly CssComponent s_comma = Shared(CssTokenType.Comma, ",");
    private static readonly CssComponent s_colon = Shared(CssTokenType.Colon, ":");
    private static readonly CssComponent s_semicolon = Shared(CssTokenType.Semicolon, ";");
    private readonly CssTokenizer _tokenizer;
    private readonly List<List<CssComponent>> _pool = new();
    private readonly string _text;
    private readonly bool _deferRuleBlocks;
    private CssToken _current;

    private CssComponentParser(string text, CssTokenizer tokenizer, bool deferRuleBlocks)
    {
        _text = text;
        _tokenizer = tokenizer;
        _deferRuleBlocks = deferRuleBlocks;
        _current = _tokenizer.Next();
    }

    private static CssComponent Shared(CssTokenType type, string text) => new(new CssToken(type, text, 0, false, false, false, 0, 0));

    /// <summary>Tokenizes and parses <paramref name="text"/> into a list of component values.</summary>
    public static List<CssComponent> Parse(string text) => Parse(text, deferRuleBlocks: false);

    /// <summary>
    /// Parses <paramref name="text"/>. With <paramref name="deferRuleBlocks"/>, the '{}' block of every
    /// statement that does not start with an at-keyword (a style rule's declarations) is skipped and
    /// tokenized only when its contents are first read.
    /// </summary>
    public static List<CssComponent> Parse(string text, bool deferRuleBlocks)
    {
        var p = new CssComponentParser(text, new CssTokenizer(text), deferRuleBlocks);
        var list = new List<CssComponent>();
        var atStatement = false;
        var statementStart = true;
        while (p._current.Type != CssTokenType.EndOfFile)
        {
            list.Add(p.ConsumeStatementPart(0, ref statementStart, ref atStatement));
        }

        return list;
    }

    internal static List<CssComponent> ParseRange(string text, int start, int end, int line, int lineStart)
    {
        var p = new CssComponentParser(text, new CssTokenizer(text, start, end, line, lineStart), deferRuleBlocks: false);
        var list = new List<CssComponent>();
        while (p._current.Type != CssTokenType.EndOfFile)
        {
            list.Add(p.Consume(0));
        }

        return list;
    }

    /// <summary>Consumes a component and tracks statement boundaries for block deferral.</summary>
    private CssComponent ConsumeStatementPart(int depth, ref bool statementStart, ref bool atStatement)
    {
        var t = _current;
        if (statementStart && t.Type is not (CssTokenType.Whitespace or CssTokenType.Cdo or CssTokenType.Cdc))
        {
            atStatement = t.Type == CssTokenType.AtKeyword;
            statementStart = false;
        }

        if (t.Type == CssTokenType.Semicolon)
        {
            statementStart = true;
        }

        if (t.Type == CssTokenType.OpenCurly)
        {
            statementStart = true;
            if (_deferRuleBlocks && !atStatement)
            {
                // The tokenizer stands just after '{'.
                var line = _tokenizer.Line;
                var lineStart = _tokenizer.LineStart;
                var start = _tokenizer.Position;
                var close = _tokenizer.SkipBlock();
                _current = _tokenizer.Next();
                return new CssComponent(t, _text, start, close, line, lineStart);
            }

            if (_deferRuleBlocks)
            {
                // An at-rule block (as @media) holds statements: defer the rule blocks inside it too.
                _current = _tokenizer.Next();
                return new CssComponent(t, ConsumeStatements(depth + 1));
            }
        }

        return Consume(depth);
    }

    private CssComponent[] ConsumeStatements(int depth)
    {
        var list = new List<CssComponent>();
        var statementStart = true;
        var atStatement = false;
        while (true)
        {
            var t = _current;
            if (t.Type == CssTokenType.EndOfFile)
            {
                break;
            }

            if (t.Type == CssTokenType.CloseCurly)
            {
                _current = _tokenizer.Next();
                break;
            }

            list.Add(ConsumeStatementPart(depth, ref statementStart, ref atStatement));
        }

        return list.ToArray();
    }

    private CssComponent Consume(int depth)
    {
        var t = _current;
        _current = _tokenizer.Next();
        switch (t.Type)
        {
            case CssTokenType.Whitespace:
                return s_whitespace;
            case CssTokenType.Comma:
                return s_comma;
            case CssTokenType.Colon:
                return s_colon;
            case CssTokenType.Semicolon:
                return s_semicolon;
            case CssTokenType.Function:
            case CssTokenType.OpenParen:
                return new CssComponent(t, ConsumeUntil(CssTokenType.CloseParen, depth + 1));
            case CssTokenType.OpenSquare:
                return new CssComponent(t, ConsumeUntil(CssTokenType.CloseSquare, depth + 1));
            case CssTokenType.OpenCurly:
                return new CssComponent(t, ConsumeUntil(CssTokenType.CloseCurly, depth + 1));
            default:
                return new CssComponent(t);
        }
    }

    private CssComponent[] ConsumeUntil(CssTokenType end, int depth)
    {
        while (_pool.Count <= depth)
        {
            _pool.Add(new List<CssComponent>());
        }

        var list = _pool[depth];
        list.Clear();
        while (true)
        {
            var t = _current;
            if (t.Type == CssTokenType.EndOfFile)
            {
                break;
            }

            if (t.Type == end)
            {
                _current = _tokenizer.Next();
                break;
            }

            list.Add(Consume(depth));
        }

        var a = list.ToArray();
        list.Clear();
        return a;
    }
}
