using Avalonia.Media;

namespace AvaAdwaita.Icons;

/// <summary>One shape of an icon: its geometry, fill and stroke, with their roles and original colors.</summary>
/// <param name="Geometry">The geometry in view-box coordinates.</param>
/// <param name="FillRole">The fill's role.</param>
/// <param name="Fill">The original fill color, for non-symbolic icons.</param>
/// <param name="FillOpacity">The fill opacity, including group opacity.</param>
/// <param name="StrokeRole">The stroke's role.</param>
/// <param name="Stroke">The original stroke color.</param>
/// <param name="StrokeOpacity">The stroke opacity.</param>
/// <param name="StrokeWidth">The stroke width.</param>
/// <param name="LineCap">The line cap.</param>
/// <param name="LineJoin">The line join.</param>
public sealed record IconShape(Geometry Geometry, IconRole FillRole, Color? Fill, double FillOpacity, IconRole StrokeRole, Color? Stroke, double StrokeOpacity,
    double StrokeWidth, PenLineCap LineCap, PenLineJoin LineJoin);
