using System.Collections.Generic;

namespace AvaAdwaita.CssEngine.Syntax;

/// <summary>A cursor over a list of component values that skips whitespace between values.</summary>
internal sealed class CssReader
{
    private readonly IReadOnlyList<CssComponent> _items;
    private int _pos;

    public CssReader(IReadOnlyList<CssComponent> items)
    {
        _items = items;
        SkipWhitespace();
    }

    /// <summary>The first error message, if a parser recorded one.</summary>
    public string? Error { get; private set; }

    /// <summary>Set when a parser met a function it does not know.</summary>
    public string? UnknownFunction { get; private set; }

    public int Position
    {
        get => _pos;
        set => _pos = value;
    }

    public bool AtEnd => _pos >= _items.Count;

    public CssComponent? Peek() => _pos < _items.Count ? _items[_pos] : null;

    /// <summary>The component after the current one, skipping whitespace.</summary>
    public CssComponent? PeekNext()
    {
        var i = _pos + 1;
        while (i < _items.Count && _items[i].IsWhitespace)
        {
            i++;
        }

        return i < _items.Count ? _items[i] : null;
    }

    public CssComponent Next()
    {
        var c = _items[_pos++];
        SkipWhitespace();
        return c;
    }

    /// <summary>True when the reader stands right after whitespace.</summary>
    public bool PrecededByWhitespace => _pos > 0 && _pos <= _items.Count && _items[_pos - 1].IsWhitespace;

    public void SkipWhitespace()
    {
        while (_pos < _items.Count && _items[_pos].IsWhitespace)
        {
            _pos++;
        }
    }

    public bool TryIdent(string name)
    {
        if (Peek() is { } c && c.IsIdent(name))
        {
            Next();
            return true;
        }

        return false;
    }

    public bool PeekIdent(string name) => Peek() is { } c && c.IsIdent(name);

    public string? TryAnyIdent()
    {
        if (Peek() is { Type: CssTokenType.Ident } c)
        {
            Next();
            return c.Token.Text;
        }

        return null;
    }

    public bool TryComma()
    {
        if (Peek() is { Type: CssTokenType.Comma })
        {
            Next();
            return true;
        }

        return false;
    }

    public bool TryDelim(char ch)
    {
        if (Peek() is { } c && c.IsDelim(ch))
        {
            Next();
            return true;
        }

        return false;
    }

    public bool PeekComma => Peek() is { Type: CssTokenType.Comma };

    public bool PeekFunction(string name) => Peek() is { } c && c.IsFunctionNamed(name);

    public bool Fail(string message)
    {
        Error ??= message;
        return false;
    }

    public void ReportUnknownFunction(string name)
    {
        UnknownFunction ??= name;
        Error ??= $"unknown function {name}()";
    }

    /// <summary>Copies an error from a nested reader.</summary>
    public void Absorb(CssReader inner)
    {
        UnknownFunction ??= inner.UnknownFunction;
        Error ??= inner.Error;
    }

    /// <summary>Splits a component list at top-level commas.</summary>
    public static List<List<CssComponent>> SplitCommas(IReadOnlyList<CssComponent> items)
    {
        var parts = new List<List<CssComponent>> { new() };
        foreach (var c in items)
        {
            if (c.Type == CssTokenType.Comma)
            {
                parts.Add(new List<CssComponent>());
            }
            else
            {
                parts[^1].Add(c);
            }
        }

        return parts;
    }
}
