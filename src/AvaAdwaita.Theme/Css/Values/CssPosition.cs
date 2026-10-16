namespace AvaAdwaita.CssEngine.Values;

/// <summary>A computed position or pair of lengths: x and y, each px plus a percentage.</summary>
internal readonly record struct CssPosition(CssNumber X, CssNumber Y)
{
    public override string ToString() => $"{X} {Y}";
}
