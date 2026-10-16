namespace AvaAdwaita.CssEngine.Values;

/// <summary><c>cross-fade()</c>: images with their weights in percent.</summary>
internal sealed record CssCrossFadeImage(CssList<(double Percent, CssImage? Image)> Layers) : CssImage;
