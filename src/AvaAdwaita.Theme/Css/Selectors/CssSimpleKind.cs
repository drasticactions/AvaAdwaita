namespace AvaAdwaita.CssEngine.Selectors;

/// <summary>The kinds of simple selector GTK knows.</summary>
internal enum CssSimpleKind : byte
{
    Any,
    Name,
    Class,
    Id,
    State,
    PositionForward,
    PositionBackward,
    PositionOnly,
    Root,
}
