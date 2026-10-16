namespace AvaAdwaita.CssEngine.Values;

/// <summary>A <c>url()</c> image, already resolved against its style sheet.</summary>
internal sealed record CssUrlImage(string Url) : CssImage
{
    public override string ToString() => $"url(\"{Url}\")";
}
