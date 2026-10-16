namespace AvaAdwaita.CssEngine.Values;

/// <summary>What computing a numeric value needs: the em and rem sizes, the dpi, and for relative colors the origin's channels.</summary>
internal readonly record struct CssNumericContext(double EmPx, double RemPx, double Dpi, float[]? Channels = null)
{
    public static CssNumericContext Default { get; } = new(13.333333333333334, 13.333333333333334, 96);
}
