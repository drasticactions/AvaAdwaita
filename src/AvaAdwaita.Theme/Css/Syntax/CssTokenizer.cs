using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace AvaAdwaita.CssEngine.Syntax;

/// <summary>A CSS Syntax Level 3 tokenizer.</summary>
/// <remarks>Comments are dropped. Line and column are 1-based.</remarks>
internal sealed class CssTokenizer
{
    private static readonly string[] s_delims = BuildDelims();
    private readonly string _s;
    private readonly Dictionary<string, string> _pool = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string>.AlternateLookup<ReadOnlySpan<char>> _poolLookup;
    private readonly int _end;
    private int _pos;
    private int _line = 1;
    private int _lineStart;

    /// <summary>Creates a tokenizer that hands out tokens one by one with <see cref="Next"/>.</summary>
    public CssTokenizer(string text)
        : this(text, 0, text.Length, 1, 0)
    {
    }

    /// <summary>Creates a tokenizer over part of a text, keeping the text's line numbers.</summary>
    public CssTokenizer(string text, int start, int end, int line, int lineStart)
    {
        _s = text;
        _pos = start;
        _end = end;
        _line = line;
        _lineStart = lineStart;
        _poolLookup = _pool.GetAlternateLookup<ReadOnlySpan<char>>();
    }

    /// <summary>The offset of the next character.</summary>
    public int Position => _pos;

    public int Line => _line;

    public int LineStart => _lineStart;

    /// <summary>
    /// Skips to just after the '}' that closes a block whose '{' was the last token, without making
    /// tokens. Strings, comments and escapes are respected. Returns the offset of the closing '}'.
    /// </summary>
    public int SkipBlock()
    {
        var depth = 1;
        while (_pos < _end)
        {
            var c = _s[_pos];
            switch (c)
            {
                case '\n':
                    _line++;
                    _lineStart = _pos + 1;
                    _pos++;
                    break;
                case '\\':
                    _pos += 2;
                    break;
                case '"':
                case '\'':
                    _pos++;
                    while (_pos < _end && _s[_pos] != c && _s[_pos] != '\n')
                    {
                        _pos += _s[_pos] == '\\' ? 2 : 1;
                    }

                    _pos++;
                    break;
                case '/' when _pos + 1 < _end && _s[_pos + 1] == '*':
                    _pos += 2;
                    while (_pos + 1 < _end && !(_s[_pos] == '*' && _s[_pos + 1] == '/'))
                    {
                        if (_s[_pos] == '\n')
                        {
                            _line++;
                            _lineStart = _pos + 1;
                        }

                        _pos++;
                    }

                    _pos += 2;
                    break;
                case '{':
                    depth++;
                    _pos++;
                    break;
                case '}':
                    depth--;
                    _pos++;
                    if (depth == 0)
                    {
                        return _pos - 1;
                    }

                    break;
                default:
                    _pos++;
                    break;
            }
        }

        _pos = Math.Min(_pos, _end);
        return _pos;
    }

    private static string[] BuildDelims()
    {
        var a = new string[128];
        for (var i = 0; i < a.Length; i++)
        {
            a[i] = ((char)i).ToString();
        }

        return a;
    }

    private static string Delim(char c) => c < 128 ? s_delims[c] : c.ToString();

    private string Intern(ReadOnlySpan<char> span)
    {
        if (_poolLookup.TryGetValue(span, out var s))
        {
            return s;
        }

        s = span.ToString();
        _pool[s] = s;
        return s;
    }

    /// <summary>Tokenizes the whole text. The list ends with an end-of-file token.</summary>
    public static List<CssToken> Tokenize(string text)
    {
        var t = new CssTokenizer(text);
        var list = new List<CssToken>(text.Length / 3);
        while (true)
        {
            var tok = t.Next();
            list.Add(tok);
            if (tok.Type == CssTokenType.EndOfFile)
            {
                return list;
            }
        }
    }

    private char Peek(int offset = 0)
    {
        var i = _pos + offset;
        return i < _end ? _s[i] : '\0';
    }

    private bool AtEnd(int offset = 0) => _pos + offset >= _end;

    private void Advance(int n = 1)
    {
        for (var i = 0; i < n && _pos < _end; i++)
        {
            if (_s[_pos] == '\n')
            {
                _line++;
                _lineStart = _pos + 1;
            }

            _pos++;
        }
    }

    private CssToken Make(CssTokenType type, string text, int line, int col, double number = 0, bool isInteger = false, bool hasSign = false, bool isId = false) =>
        new(type, text, number, isInteger, hasSign, isId, line, col);

    /// <summary>The next token; an end-of-file token at the end.</summary>
    public CssToken Next()
    {
        while (Peek() == '/' && Peek(1) == '*')
        {
            Advance(2);
            while (!AtEnd() && !(Peek() == '*' && Peek(1) == '/'))
            {
                Advance();
            }

            Advance(2);
        }

        var line = _line;
        var col = _pos - _lineStart + 1;
        if (AtEnd())
        {
            return CssToken.Eof(line, col);
        }

        var c = Peek();
        if (IsWhitespace(c))
        {
            while (_pos < _end && IsWhitespace(_s[_pos]))
            {
                if (_s[_pos] == '\n')
                {
                    _line++;
                    _lineStart = _pos + 1;
                }

                _pos++;
            }

            return Make(CssTokenType.Whitespace, " ", line, col);
        }

        switch (c)
        {
            case '"':
            case '\'':
                return ConsumeString(c, line, col);
            case '#':
                if (IsNameChar(Peek(1)) || IsValidEscape(Peek(1), Peek(2)))
                {
                    Advance();
                    var isId = StartsIdentifier(Peek(), Peek(1), Peek(2));
                    return Make(CssTokenType.Hash, ConsumeName(), line, col, isId: isId);
                }

                Advance();
                return Make(CssTokenType.Delim, "#", line, col);
            case '(':
                Advance();
                return Make(CssTokenType.OpenParen, "(", line, col);
            case ')':
                Advance();
                return Make(CssTokenType.CloseParen, ")", line, col);
            case '[':
                Advance();
                return Make(CssTokenType.OpenSquare, "[", line, col);
            case ']':
                Advance();
                return Make(CssTokenType.CloseSquare, "]", line, col);
            case '{':
                Advance();
                return Make(CssTokenType.OpenCurly, "{", line, col);
            case '}':
                Advance();
                return Make(CssTokenType.CloseCurly, "}", line, col);
            case ',':
                Advance();
                return Make(CssTokenType.Comma, ",", line, col);
            case ':':
                Advance();
                return Make(CssTokenType.Colon, ":", line, col);
            case ';':
                Advance();
                return Make(CssTokenType.Semicolon, ";", line, col);
            case '+':
            case '.':
                if (StartsNumber(c, Peek(1), Peek(2)))
                {
                    return ConsumeNumeric(line, col);
                }

                Advance();
                return Make(CssTokenType.Delim, Delim(c), line, col);
            case '-':
                if (StartsNumber(c, Peek(1), Peek(2)))
                {
                    return ConsumeNumeric(line, col);
                }

                if (Peek(1) == '-' && Peek(2) == '>')
                {
                    Advance(3);
                    return Make(CssTokenType.Cdc, "-->", line, col);
                }

                if (StartsIdentifier(c, Peek(1), Peek(2)))
                {
                    return ConsumeIdentLike(line, col);
                }

                Advance();
                return Make(CssTokenType.Delim, "-", line, col);
            case '<':
                if (Peek(1) == '!' && Peek(2) == '-' && Peek(3) == '-')
                {
                    Advance(4);
                    return Make(CssTokenType.Cdo, "<!--", line, col);
                }

                Advance();
                return Make(CssTokenType.Delim, "<", line, col);
            case '@':
                if (StartsIdentifier(Peek(1), Peek(2), Peek(3)))
                {
                    Advance();
                    return Make(CssTokenType.AtKeyword, ConsumeName(), line, col);
                }

                Advance();
                return Make(CssTokenType.Delim, "@", line, col);
            case '\\':
                if (IsValidEscape(c, Peek(1)))
                {
                    return ConsumeIdentLike(line, col);
                }

                Advance();
                return Make(CssTokenType.Delim, "\\", line, col);
        }

        if (char.IsAsciiDigit(c))
        {
            return ConsumeNumeric(line, col);
        }

        if (IsNameStart(c))
        {
            return ConsumeIdentLike(line, col);
        }

        Advance();
        return Make(CssTokenType.Delim, Delim(c), line, col);
    }

    private static bool IsWhitespace(char c) => c is ' ' or '\t' or '\n' or '\r' or '\f';

    private static bool IsNameStart(char c) => char.IsAsciiLetter(c) || c == '_' || c >= 0x80;

    private static bool IsNameChar(char c) => IsNameStart(c) || char.IsAsciiDigit(c) || c == '-';

    private static bool IsValidEscape(char a, char b) => a == '\\' && b != '\n' && b != '\0';

    private static bool StartsIdentifier(char a, char b, char c)
    {
        if (a == '-')
        {
            return IsNameStart(b) || b == '-' || IsValidEscape(b, c);
        }

        return IsNameStart(a) || IsValidEscape(a, b);
    }

    private static bool StartsNumber(char a, char b, char c)
    {
        if (a is '+' or '-')
        {
            return char.IsAsciiDigit(b) || (b == '.' && char.IsAsciiDigit(c));
        }

        if (a == '.')
        {
            return char.IsAsciiDigit(b);
        }

        return char.IsAsciiDigit(a);
    }

    private string ConsumeName()
    {
        // Fast path: no escapes, so the name is a slice of the text.
        var start = _pos;
        while (_pos < _end && IsNameChar(_s[_pos]))
        {
            _pos++;
        }

        if (_pos >= _end || _s[_pos] != '\\')
        {
            return Intern(_s.AsSpan(start, _pos - start));
        }

        _pos = start;
        var sb = new StringBuilder();
        while (!AtEnd())
        {
            var c = Peek();
            if (IsNameChar(c))
            {
                sb.Append(c);
                Advance();
            }
            else if (IsValidEscape(c, Peek(1)))
            {
                Advance();
                sb.Append(ConsumeEscape());
            }
            else
            {
                break;
            }
        }

        return sb.ToString();
    }

    private string ConsumeEscape()
    {
        if (AtEnd())
        {
            return "�";
        }

        var c = Peek();
        if (char.IsAsciiHexDigit(c))
        {
            var start = _pos;
            var n = 0;
            while (n < 6 && char.IsAsciiHexDigit(Peek()))
            {
                Advance();
                n++;
            }

            var code = int.Parse(_s.AsSpan(start, _pos - start), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            if (IsWhitespace(Peek()))
            {
                Advance();
            }

            if (code == 0 || code > 0x10FFFF || (code >= 0xD800 && code <= 0xDFFF))
            {
                return "�";
            }

            return char.ConvertFromUtf32(code);
        }

        Advance();
        return c.ToString();
    }

    private CssToken ConsumeString(char quote, int line, int col)
    {
        Advance();
        var sb = new StringBuilder();
        while (true)
        {
            if (AtEnd())
            {
                return Make(CssTokenType.String, sb.ToString(), line, col);
            }

            var c = Peek();
            if (c == quote)
            {
                Advance();
                return Make(CssTokenType.String, sb.ToString(), line, col);
            }

            if (c == '\n')
            {
                return Make(CssTokenType.BadString, sb.ToString(), line, col);
            }

            if (c == '\\')
            {
                if (AtEnd(1))
                {
                    Advance();
                    continue;
                }

                if (Peek(1) == '\n')
                {
                    Advance(2);
                    continue;
                }

                Advance();
                sb.Append(ConsumeEscape());
                continue;
            }

            sb.Append(c);
            Advance();
        }
    }

    private CssToken ConsumeNumeric(int line, int col)
    {
        var start = _pos;
        var isInteger = true;
        var hasSign = Peek() is '+' or '-';
        if (hasSign)
        {
            Advance();
        }

        while (_pos < _end && char.IsAsciiDigit(_s[_pos]))
        {
            _pos++;
        }

        if (Peek() == '.' && char.IsAsciiDigit(Peek(1)))
        {
            isInteger = false;
            _pos++;
            while (_pos < _end && char.IsAsciiDigit(_s[_pos]))
            {
                _pos++;
            }
        }

        if ((Peek() is 'e' or 'E') && (char.IsAsciiDigit(Peek(1)) || ((Peek(1) is '+' or '-') && char.IsAsciiDigit(Peek(2)))))
        {
            isInteger = false;
            Advance(2);
            while (char.IsAsciiDigit(Peek()))
            {
                Advance();
            }
        }

        var value = double.Parse(_s.AsSpan(start, _pos - start), NumberStyles.Float, CultureInfo.InvariantCulture);
        if (StartsIdentifier(Peek(), Peek(1), Peek(2)))
        {
            return Make(CssTokenType.Dimension, ConsumeName(), line, col, value, isInteger, hasSign);
        }

        if (Peek() == '%')
        {
            _pos++;
            return Make(CssTokenType.Percentage, "%", line, col, value, isInteger, hasSign);
        }

        return Make(CssTokenType.Number, string.Empty, line, col, value, isInteger, hasSign);
    }

    private CssToken ConsumeIdentLike(int line, int col)
    {
        var name = ConsumeName();
        if (Peek() == '(')
        {
            Advance();
            if (string.Equals(name, "url", System.StringComparison.OrdinalIgnoreCase))
            {
                var save = _pos;
                var saveLine = _line;
                var saveStart = _lineStart;
                while (IsWhitespace(Peek()))
                {
                    Advance();
                }

                if (Peek() is '"' or '\'')
                {
                    _pos = save;
                    _line = saveLine;
                    _lineStart = saveStart;
                    return Make(CssTokenType.Function, name, line, col);
                }

                return ConsumeUrl(line, col);
            }

            return Make(CssTokenType.Function, name, line, col);
        }

        return Make(CssTokenType.Ident, name, line, col);
    }

    private CssToken ConsumeUrl(int line, int col)
    {
        var sb = new StringBuilder();
        while (true)
        {
            if (AtEnd())
            {
                return Make(CssTokenType.Url, sb.ToString(), line, col);
            }

            var c = Peek();
            if (c == ')')
            {
                Advance();
                return Make(CssTokenType.Url, sb.ToString(), line, col);
            }

            if (IsWhitespace(c))
            {
                while (IsWhitespace(Peek()))
                {
                    Advance();
                }

                if (Peek() == ')' || AtEnd())
                {
                    Advance();
                    return Make(CssTokenType.Url, sb.ToString(), line, col);
                }

                return ConsumeBadUrl(line, col);
            }

            if (c is '"' or '\'' or '(' || c < ' ')
            {
                return ConsumeBadUrl(line, col);
            }

            if (c == '\\')
            {
                if (IsValidEscape(c, Peek(1)))
                {
                    Advance();
                    sb.Append(ConsumeEscape());
                    continue;
                }

                return ConsumeBadUrl(line, col);
            }

            sb.Append(c);
            Advance();
        }
    }

    private CssToken ConsumeBadUrl(int line, int col)
    {
        while (!AtEnd())
        {
            if (Peek() == ')')
            {
                Advance();
                break;
            }

            if (IsValidEscape(Peek(), Peek(1)))
            {
                Advance();
                ConsumeEscape();
                continue;
            }

            Advance();
        }

        return Make(CssTokenType.BadUrl, string.Empty, line, col);
    }
}
