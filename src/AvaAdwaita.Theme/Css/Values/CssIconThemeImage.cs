namespace AvaAdwaita.CssEngine.Values;

/// <summary><c>-gtk-icontheme("name")</c>: a named icon, recolored with the node's color and palette.</summary>
internal sealed record CssIconThemeImage(string Name) : CssImage
{
    public override string ToString() => $"-gtk-icontheme(\"{Name}\")";
}
