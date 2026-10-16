namespace AvaAdwaita.CssEngine.Values;

/// <summary>The color spaces of GTK's <c>GtkCssColorSpace</c>, in its order.</summary>
internal enum CssColorSpace : byte
{
    Srgb,
    SrgbLinear,
    Hsl,
    Hwb,
    Oklab,
    Oklch,
    DisplayP3,
    Xyz,
    Rec2020,
}
