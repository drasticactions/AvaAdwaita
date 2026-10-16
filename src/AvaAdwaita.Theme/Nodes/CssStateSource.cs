namespace AvaAdwaita.Nodes;

/// <summary>Where a node's states come from.</summary>
public enum CssStateSource
{
    /// <summary>The element itself for templated controls; the templated parent for template parts.</summary>
    Auto,

    /// <summary>The element itself.</summary>
    Self,

    /// <summary>The element's templated parent.</summary>
    TemplatedParent,

    /// <summary>No states except those inherited from the window (backdrop, direction).</summary>
    None,
}
