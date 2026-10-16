using System.Globalization;

namespace AvaAdwaita.CssEngine.Values;

/// <summary>A computed shadow, as <c>box-shadow</c>, <c>text-shadow</c>, <c>-gtk-icon-shadow</c> or <c>drop-shadow()</c>.</summary>
internal sealed record CssShadow(bool Inset, double X, double Y, double Blur, double Spread, CssColor Color)
{
    public static CssShadow Transparent(bool inset) => new(inset, 0, 0, 0, 0, CssColor.Transparent);

    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"{(Inset ? "inset " : string.Empty)}{X}px {Y}px {Blur}px {Spread}px {Color}");
}
