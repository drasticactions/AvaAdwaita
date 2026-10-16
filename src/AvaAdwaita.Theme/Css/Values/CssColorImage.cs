namespace AvaAdwaita.CssEngine.Values;

/// <summary>A solid color image, as <c>image(color)</c> computes.</summary>
internal sealed record CssColorImage(CssColor Color) : CssImage
{
    public override string ToString() => $"image({Color})";
}
