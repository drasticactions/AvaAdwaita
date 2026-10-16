using System;
using System.Collections.Generic;
using System.Text;

namespace AvaAdwaita.CssEngine.Syntax;

/// <summary>A CSS component value: a preserved token, a function or a simple block.</summary>
internal sealed class CssComponent
{
    private static readonly CssComponent[] s_none = [];

    private CssComponent[]? _children;
    private readonly DeferredBlock? _deferred;

    public CssComponent(CssToken token, CssComponent[]? children = null)
    {
        Token = token;
        _children = children ?? s_none;
    }

    /// <summary>A '{}' block whose contents are tokenized on first use.</summary>
    internal CssComponent(CssToken token, string text, int start, int end, int line, int lineStart)
    {
        Token = token;
        _deferred = new DeferredBlock(text, start, end, line, lineStart);
    }

    /// <summary>The token, or the function token, or the block's opening token.</summary>
    public CssToken Token { get; }

    /// <summary>The function arguments or the block contents.</summary>
    public CssComponent[] Children => _children ??= _deferred!.Parse();

    /// <summary>True when the block's contents have not been tokenized yet.</summary>
    public bool IsDeferred => _children is null;

    private sealed record DeferredBlock(string Text, int Start, int End, int Line, int LineStart)
    {
        public CssComponent[] Parse() => CssComponentParser.ParseRange(Text, Start, End, Line, LineStart).ToArray();
    }

    public CssTokenType Type => Token.Type;

    public bool IsFunction => Token.Type == CssTokenType.Function;

    public bool IsBlock => Token.Type is CssTokenType.OpenParen or CssTokenType.OpenSquare or CssTokenType.OpenCurly;

    public bool IsWhitespace => Token.Type == CssTokenType.Whitespace;

    public bool IsIdent(string name) => Token.IsIdent(name);

    public bool IsFunctionNamed(string name) => Token.IsFunction(name);

    public bool IsDelim(char c) => Token.IsDelim(c);

    /// <summary>True when this component or a nested one is a <c>var()</c> function.</summary>
    public bool ContainsVar()
    {
        if (IsFunctionNamed("var"))
        {
            return true;
        }

        foreach (var c in Children)
        {
            if (c.ContainsVar())
            {
                return true;
            }
        }

        return false;
    }

    public static bool ContainsVar(IReadOnlyList<CssComponent> list)
    {
        foreach (var c in list)
        {
            if (c.ContainsVar())
            {
                return true;
            }
        }

        return false;
    }

    public void AppendCss(StringBuilder sb)
    {
        sb.Append(Token.ToCss());
        if (IsFunction || IsBlock)
        {
            foreach (var c in Children)
            {
                c.AppendCss(sb);
            }

            sb.Append(Token.Type switch
            {
                CssTokenType.OpenSquare => "]",
                CssTokenType.OpenCurly => "}",
                _ => ")",
            });
        }
    }

    public static string ToCss(IReadOnlyList<CssComponent> list)
    {
        var sb = new StringBuilder();
        foreach (var c in list)
        {
            c.AppendCss(sb);
        }

        return sb.ToString().Trim();
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        AppendCss(sb);
        return sb.ToString();
    }

    /// <summary>Structural equality, ignoring source positions.</summary>
    public static bool ListEquals(IReadOnlyList<CssComponent> a, IReadOnlyList<CssComponent> b)
    {
        if (ReferenceEquals(a, b))
        {
            return true;
        }

        if (a.Count != b.Count)
        {
            return false;
        }

        for (var i = 0; i < a.Count; i++)
        {
            var x = a[i];
            var y = b[i];
            if (ReferenceEquals(x, y))
            {
                continue;
            }

            if (x.Token.Type != y.Token.Type || x.Token.Number != y.Token.Number || !string.Equals(x.Token.Text, y.Token.Text, StringComparison.Ordinal) || !ListEquals(x.Children, y.Children))
            {
                return false;
            }
        }

        return true;
    }

    public static int ListHash(IReadOnlyList<CssComponent> list)
    {
        var h = new HashCode();
        foreach (var c in list)
        {
            h.Add(c.Token.Type);
            h.Add(c.Token.Text);
            h.Add(c.Token.Number);
            if (c.Children.Length > 0)
            {
                h.Add(ListHash(c.Children));
            }
        }

        return h.ToHashCode();
    }
}
