namespace AvaAdwaita.CssEngine.Syntax;

/// <summary>The token kinds of CSS Syntax Level 3.</summary>
internal enum CssTokenType : byte
{
    EndOfFile,
    Whitespace,
    Ident,
    Function,
    AtKeyword,
    Hash,
    String,
    BadString,
    Url,
    BadUrl,
    Delim,
    Number,
    Percentage,
    Dimension,
    Colon,
    Semicolon,
    Comma,
    OpenSquare,
    CloseSquare,
    OpenParen,
    CloseParen,
    OpenCurly,
    CloseCurly,
    Cdo,
    Cdc,
}
