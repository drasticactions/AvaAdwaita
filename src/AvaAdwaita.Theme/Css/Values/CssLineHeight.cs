namespace AvaAdwaita.CssEngine.Values;

/// <summary>A computed <c>line-height</c>: normal, a factor of the font size, or px.</summary>
internal readonly record struct CssLineHeight(bool IsNormal, double Factor, double Px)
{
    public static CssLineHeight Normal { get; } = new(true, 0, 0);

    /// <summary>The line height in px for a font size, or null for normal.</summary>
    public double? Resolve(double fontSizePx) => IsNormal ? null : Px > 0 ? Px : Factor * fontSizePx;

    public override string ToString() => IsNormal ? "normal" : Px > 0 ? $"{Px}px" : $"{Factor}";
}
