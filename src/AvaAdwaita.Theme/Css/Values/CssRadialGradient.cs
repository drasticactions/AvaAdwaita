namespace AvaAdwaita.CssEngine.Values;

/// <summary>A computed <c>radial-gradient()</c>.</summary>
internal sealed record CssRadialGradient(bool Circle, CssRadialSize Size, CssNumber? RadiusX, CssNumber? RadiusY, CssPosition Center, bool Repeating, CssList<CssColorStop> Stops) : CssImage;
