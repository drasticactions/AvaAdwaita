namespace AvaAdwaita.CssEngine.Values;

/// <summary>Four computed numbers, top, right, bottom and left, for <c>border-image-slice</c> and <c>border-image-width</c>.</summary>
internal readonly record struct CssBoxValues(CssNumber Top, CssNumber Right, CssNumber Bottom, CssNumber Left, bool Fill);
