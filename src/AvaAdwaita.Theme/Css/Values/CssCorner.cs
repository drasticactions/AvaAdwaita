namespace AvaAdwaita.CssEngine.Values;

/// <summary>A computed corner radius: horizontal and vertical, each px plus a percentage of the box.</summary>
internal readonly record struct CssCorner(CssNumber X, CssNumber Y)
{
    public static CssCorner Zero { get; } = new(CssNumber.Px(0), CssNumber.Px(0));

    public (double X, double Y) Resolve(double width, double height) => (X.Resolve(width), Y.Resolve(height));

    public override string ToString() => X == Y ? X.ToString() : $"{X} {Y}";
}
