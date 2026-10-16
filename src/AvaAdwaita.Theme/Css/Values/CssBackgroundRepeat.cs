namespace AvaAdwaita.CssEngine.Values;

/// <summary>A computed <c>background-repeat</c> layer.</summary>
internal readonly record struct CssBackgroundRepeat(CssRepeatStyle X, CssRepeatStyle Y)
{
    public static CssBackgroundRepeat Repeat { get; } = new(CssRepeatStyle.Repeat, CssRepeatStyle.Repeat);
}
