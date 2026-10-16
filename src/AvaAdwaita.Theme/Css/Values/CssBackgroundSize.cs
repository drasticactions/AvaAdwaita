namespace AvaAdwaita.CssEngine.Values;

/// <summary>A computed <c>background-size</c> layer. Null lengths are <c>auto</c>.</summary>
internal readonly record struct CssBackgroundSize(bool Cover, bool Contain, CssNumber? Width, CssNumber? Height)
{
    public static CssBackgroundSize Auto { get; } = new(false, false, null, null);

    public override string ToString() => Cover ? "cover" : Contain ? "contain" : $"{Width?.ToString() ?? "auto"} {Height?.ToString() ?? "auto"}";
}
