using System;

namespace AvaAdwaita.Resources;

/// <summary>
/// A gettext <c>Plural-Forms</c> expression (a C expression of <c>n</c>: arithmetic, comparisons, <c>&amp;&amp;</c>,
/// <c>||</c>, <c>!</c> and <c>?:</c>) as a function from a count to a variant.
/// </summary>
internal sealed class PluralExpression
{
    private readonly string _text;
    private int _pos;

    private PluralExpression(string text) => _text = text;

    /// <summary>The expression as a function, or null when it does not parse.</summary>
    public static Func<long, long>? Parse(string text)
    {
        try
        {
            var parser = new PluralExpression(text);
            var f = parser.Conditional();
            parser.Skip();
            return parser._pos == text.Length ? f : null;
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private void Skip()
    {
        while (_pos < _text.Length && char.IsWhiteSpace(_text[_pos]))
        {
            _pos++;
        }
    }

    private bool Eat(string token)
    {
        Skip();
        if (string.CompareOrdinal(_text, _pos, token, 0, token.Length) == 0)
        {
            _pos += token.Length;
            return true;
        }

        return false;
    }

    private Func<long, long> Conditional()
    {
        var condition = Or();
        if (!Eat("?"))
        {
            return condition;
        }

        var yes = Conditional();
        if (!Eat(":"))
        {
            throw new FormatException();
        }

        var no = Conditional();
        return n => condition(n) != 0 ? yes(n) : no(n);
    }

    private Func<long, long> Or()
    {
        var left = And();
        while (Eat("||"))
        {
            var l = left;
            var r = And();
            left = n => l(n) != 0 || r(n) != 0 ? 1 : 0;
        }

        return left;
    }

    private Func<long, long> And()
    {
        var left = Equality();
        while (Eat("&&"))
        {
            var l = left;
            var r = Equality();
            left = n => l(n) != 0 && r(n) != 0 ? 1 : 0;
        }

        return left;
    }

    private Func<long, long> Equality()
    {
        var left = Relational();
        while (true)
        {
            var l = left;
            if (Eat("=="))
            {
                var r = Relational();
                left = n => l(n) == r(n) ? 1 : 0;
            }
            else if (Eat("!="))
            {
                var r = Relational();
                left = n => l(n) != r(n) ? 1 : 0;
            }
            else
            {
                return left;
            }
        }
    }

    private Func<long, long> Relational()
    {
        var left = Additive();
        while (true)
        {
            var l = left;
            if (Eat("<="))
            {
                var r = Additive();
                left = n => l(n) <= r(n) ? 1 : 0;
            }
            else if (Eat(">="))
            {
                var r = Additive();
                left = n => l(n) >= r(n) ? 1 : 0;
            }
            else if (Eat("<"))
            {
                var r = Additive();
                left = n => l(n) < r(n) ? 1 : 0;
            }
            else if (Eat(">"))
            {
                var r = Additive();
                left = n => l(n) > r(n) ? 1 : 0;
            }
            else
            {
                return left;
            }
        }
    }

    private Func<long, long> Additive()
    {
        var left = Multiplicative();
        while (true)
        {
            var l = left;
            if (Eat("+"))
            {
                var r = Multiplicative();
                left = n => l(n) + r(n);
            }
            else if (Eat("-"))
            {
                var r = Multiplicative();
                left = n => l(n) - r(n);
            }
            else
            {
                return left;
            }
        }
    }

    private Func<long, long> Multiplicative()
    {
        var left = Unary();
        while (true)
        {
            var l = left;
            if (Eat("*"))
            {
                var r = Unary();
                left = n => l(n) * r(n);
            }
            else if (Eat("/"))
            {
                var r = Unary();
                left = n => r(n) == 0 ? 0 : l(n) / r(n);
            }
            else if (Eat("%"))
            {
                var r = Unary();
                left = n => r(n) == 0 ? 0 : l(n) % r(n);
            }
            else
            {
                return left;
            }
        }
    }

    private Func<long, long> Unary()
    {
        if (Eat("!"))
        {
            var operand = Unary();
            return n => operand(n) == 0 ? 1 : 0;
        }

        if (Eat("("))
        {
            var inner = Conditional();
            if (!Eat(")"))
            {
                throw new FormatException();
            }

            return inner;
        }

        if (Eat("n"))
        {
            return n => n;
        }

        Skip();
        var start = _pos;
        while (_pos < _text.Length && char.IsAsciiDigit(_text[_pos]))
        {
            _pos++;
        }

        if (start == _pos)
        {
            throw new FormatException();
        }

        var value = long.Parse(_text.AsSpan(start, _pos - start), System.Globalization.CultureInfo.InvariantCulture);
        return _ => value;
    }
}
