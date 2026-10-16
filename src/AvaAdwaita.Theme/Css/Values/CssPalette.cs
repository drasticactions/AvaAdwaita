namespace AvaAdwaita.CssEngine.Values;

/// <summary>A computed <c>-gtk-icon-palette</c>: named colors for symbolic icons.</summary>
internal sealed record CssPalette(CssList<(string Name, CssColor Color)> Colors)
{
    public CssColor? Get(string name)
    {
        foreach (var (n, c) in Colors)
        {
            if (n == name)
            {
                return c;
            }
        }

        return null;
    }

    public override string ToString() => Colors.Count == 0 ? "default" : string.Join(", ", Colors);
}
