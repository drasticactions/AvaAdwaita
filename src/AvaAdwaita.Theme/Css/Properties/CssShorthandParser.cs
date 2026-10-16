using AvaAdwaita.CssEngine.Syntax;

namespace AvaAdwaita.CssEngine.Properties;

/// <summary>Parses a shorthand into specified values for its longhands; a null entry means the longhand's initial value.</summary>
internal delegate object?[]? CssShorthandParser(CssReader r, CssShorthand shorthand, CssParseContext pc);
