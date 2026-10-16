namespace AvaAdwaita.Chrome;

/// <summary>The side of a popover that faces its anchor, where the arrow is drawn.</summary>
public enum PopoverSide
{
    /// <summary>No arrow.</summary>
    None,

    /// <summary>The popover is below its anchor; the arrow points up.</summary>
    Top,

    /// <summary>The popover is above its anchor; the arrow points down.</summary>
    Bottom,

    /// <summary>The popover is right of its anchor; the arrow points left.</summary>
    Left,

    /// <summary>The popover is left of its anchor; the arrow points right.</summary>
    Right,
}
