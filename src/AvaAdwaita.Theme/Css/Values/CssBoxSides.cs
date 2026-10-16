namespace AvaAdwaita.CssEngine.Values;

/// <summary>Four computed lengths in px: left, top, right, bottom.</summary>
internal readonly record struct CssBoxSides(double Left, double Top, double Right, double Bottom)
{
    public double Horizontal => Left + Right;

    public double Vertical => Top + Bottom;
}
