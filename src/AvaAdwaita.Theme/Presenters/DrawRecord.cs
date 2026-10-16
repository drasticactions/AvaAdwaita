using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;

namespace AvaAdwaita.Presenters;

/// <summary>
/// One draw, normalized the way the parity oracle reports GTK's render nodes: <c>color</c>, <c>border</c>,
/// <c>shadow</c>, <c>inset-shadow</c>, <c>text</c>, <c>icon</c> or <c>gradient</c>, in the recording root's
/// coordinates, with colors multiplied by the opacity around them.
/// </summary>
internal sealed record DrawRecord(string Kind, Rect Rect)
{
    public double[]? Radii { get; init; }

    public Color? Color { get; init; }

    /// <summary>The color's alpha before it was rounded to 8 bits, so opacity multiplies as GTK's floats do.</summary>
    public float? Alpha { get; init; }

    /// <summary>The unrounded alphas of <see cref="Colors"/>.</summary>
    public float[]? Alphas { get; init; }

    public double[]? Widths { get; init; }

    public IReadOnlyList<Color>? Colors { get; init; }

    /// <summary>dx, dy, spread, blur.</summary>
    public double[]? Shadow { get; init; }

    public string? Family { get; init; }

    public int Weight { get; init; }

    public double Size { get; init; }
}
