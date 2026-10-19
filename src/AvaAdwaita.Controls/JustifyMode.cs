namespace AvaAdwaita.Controls;

/// <summary>How a wrap box fills its lines (AdwJustifyMode).</summary>
public enum JustifyMode
{
    /// <summary>Children keep their natural sizes; the line's alignment places them.</summary>
    None,

    /// <summary>Children grow to fill the line.</summary>
    Fill,

    /// <summary>The space between children grows to fill the line.</summary>
    Spread,
}
