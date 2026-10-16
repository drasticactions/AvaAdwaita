namespace AvaAdwaita.CssEngine.Values;

/// <summary>A computed <c>linear-gradient()</c>.</summary>
/// <param name="Side">The <c>to</c> side flags, or 0 when <paramref name="Angle"/> is used.</param>
/// <param name="Angle">The angle in degrees.</param>
/// <param name="Repeating">For <c>repeating-linear-gradient()</c>.</param>
/// <param name="Stops">The color stops.</param>
internal sealed record CssLinearGradient(int Side, double Angle, bool Repeating, CssList<CssColorStop> Stops) : CssImage
{
    public const int Top = 1;
    public const int Right = 2;
    public const int Bottom = 4;
    public const int Left = 8;
}
