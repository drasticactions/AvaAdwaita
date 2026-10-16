using System;
using System.Globalization;

namespace AvaAdwaita.CssEngine.Syntax;

/// <summary>One CSS token.</summary>
/// <remarks>
/// <see cref="Text"/> holds the ident, function or at-keyword name, the string or url value,
/// the hash name, the delimiter character, or the unit of a dimension.
/// </remarks>
internal readonly record struct CssToken(CssTokenType Type, string Text, double Number, bool IsInteger, bool HasSign, bool IsIdHash, int Line, int Column)
{
    public static CssToken Eof(int line, int column) => new(CssTokenType.EndOfFile, string.Empty, 0, false, false, false, line, column);

    public bool Is(CssTokenType type) => Type == type;

    public bool IsIdent(string name) => Type == CssTokenType.Ident && string.Equals(Text, name, StringComparison.OrdinalIgnoreCase);

    public bool IsFunction(string name) => Type == CssTokenType.Function && string.Equals(Text, name, StringComparison.OrdinalIgnoreCase);

    public bool IsDelim(char c) => Type == CssTokenType.Delim && Text.Length == 1 && Text[0] == c;

    public bool IsNumeric => Type is CssTokenType.Number or CssTokenType.Percentage or CssTokenType.Dimension;

    /// <summary>Writes the token back as CSS text.</summary>
    public string ToCss() => Type switch
    {
        CssTokenType.Whitespace => " ",
        CssTokenType.Ident => Text,
        CssTokenType.Function => Text + "(",
        CssTokenType.AtKeyword => "@" + Text,
        CssTokenType.Hash => "#" + Text,
        CssTokenType.String => "\"" + Text.Replace("\"", "\\\"", StringComparison.Ordinal) + "\"",
        CssTokenType.Url => "url(" + Text + ")",
        CssTokenType.Delim => Text,
        CssTokenType.Number => FormatNumber(Number),
        CssTokenType.Percentage => FormatNumber(Number) + "%",
        CssTokenType.Dimension => FormatNumber(Number) + Text,
        CssTokenType.Colon => ":",
        CssTokenType.Semicolon => ";",
        CssTokenType.Comma => ",",
        CssTokenType.OpenSquare => "[",
        CssTokenType.CloseSquare => "]",
        CssTokenType.OpenParen => "(",
        CssTokenType.CloseParen => ")",
        CssTokenType.OpenCurly => "{",
        CssTokenType.CloseCurly => "}",
        _ => string.Empty,
    };

    private static string FormatNumber(double v) => v.ToString("R", CultureInfo.InvariantCulture);
}
