namespace AvaAdwaita.CssEngine.Values;

/// <summary>One computed transform function. Translations are px plus a percentage of the box; angles are degrees.</summary>
/// <param name="Kind">The function.</param>
/// <param name="A">translate x, scale x, rotate or skew angle, or matrix a.</param>
/// <param name="B">translate y, scale y, skew y angle, or matrix b.</param>
/// <param name="C">matrix c.</param>
/// <param name="D">matrix d.</param>
/// <param name="E">matrix e.</param>
/// <param name="F">matrix f.</param>
/// <param name="Tx">translate x.</param>
/// <param name="Ty">translate y.</param>
internal readonly record struct CssTransformFunction(CssTransformKind Kind, double A, double B, double C, double D, double E, double F, CssNumber Tx, CssNumber Ty);
