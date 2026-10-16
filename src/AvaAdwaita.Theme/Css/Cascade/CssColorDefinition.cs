using AvaAdwaita.CssEngine.Values;

namespace AvaAdwaita.CssEngine.Cascade;

/// <summary>A <c>@define-color</c>.</summary>
internal sealed record CssColorDefinition(string Name, CssColorExpr Value, CssMediaQuery? Media);
