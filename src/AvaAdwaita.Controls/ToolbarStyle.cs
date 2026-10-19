namespace AvaAdwaita.Controls;

/// <summary>How a <see cref="ToolbarView"/>'s bars look against its content (AdwToolbarStyle).</summary>
public enum ToolbarStyle
{
    /// <summary>The bars share the content's background; scrolled content shows an undershoot under them.</summary>
    Flat,

    /// <summary>The bars have a background of their own and a shadow over the content.</summary>
    Raised,

    /// <summary>As <see cref="Raised"/>, with a border instead of the shadow.</summary>
    RaisedBorder,
}
