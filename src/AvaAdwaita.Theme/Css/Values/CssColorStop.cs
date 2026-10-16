namespace AvaAdwaita.CssEngine.Values;

/// <summary>A computed gradient color stop. A null offset is placed by the gradient.</summary>
internal readonly record struct CssColorStop(CssColor Color, CssNumber? Offset);
