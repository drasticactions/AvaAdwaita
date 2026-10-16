namespace AvaAdwaita.Nodes;

/// <summary>Where a node's <c>:first-child</c> and <c>:last-child</c> come from.</summary>
public enum CssPositionSource
{
    /// <summary>The node's siblings.</summary>
    Auto,

    /// <summary>The item index in the items control, which virtualization keeps out of visual order.</summary>
    Item,
}
