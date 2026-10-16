namespace AvaAdwaita.CssEngine.Values;

/// <summary><c>-gtk-recolor(url(…), palette)</c>: a symbolic SVG recolored with the node's color and palette.</summary>
internal sealed record CssRecolorImage(string Url, CssPalette? Palette) : CssImage
{
    public override string ToString() => $"-gtk-recolor(url(\"{Url}\"))";
}
