namespace AvaAdwaita.CssEngine.Values;

/// <summary>One computed filter function. <paramref name="Amount"/> is a factor, px for blur, or degrees for hue-rotate.</summary>
internal sealed record CssFilterFunction(CssFilterKind Kind, double Amount, CssShadow? Shadow);
