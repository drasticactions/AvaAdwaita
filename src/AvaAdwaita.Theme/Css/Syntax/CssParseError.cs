namespace AvaAdwaita.CssEngine.Syntax;

/// <summary>One problem found while parsing a style sheet.</summary>
/// <param name="Kind">The classification.</param>
/// <param name="Message">What went wrong.</param>
/// <param name="Line">The 1-based line.</param>
/// <param name="Column">The 1-based column.</param>
public sealed record CssParseError(CssErrorKind Kind, string Message, int Line, int Column)
{
    /// <inheritdoc/>
    public override string ToString() => $"{Line}:{Column}: {Kind}: {Message}";
}
