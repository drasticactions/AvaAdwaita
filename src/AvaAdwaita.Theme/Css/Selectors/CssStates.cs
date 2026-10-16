using System;

namespace AvaAdwaita.CssEngine.Selectors;

/// <summary>The state flags a node can carry, as GTK's <c>GtkStateFlags</c>.</summary>
[Flags]
public enum CssStates : uint
{
    /// <summary>No state.</summary>
    None = 0,

    /// <summary><c>:active</c>.</summary>
    Active = 1 << 0,

    /// <summary><c>:hover</c>.</summary>
    Hover = 1 << 1,

    /// <summary><c>:selected</c>.</summary>
    Selected = 1 << 2,

    /// <summary><c>:disabled</c>.</summary>
    Disabled = 1 << 3,

    /// <summary><c>:indeterminate</c>.</summary>
    Indeterminate = 1 << 4,

    /// <summary><c>:focus</c>.</summary>
    Focus = 1 << 5,

    /// <summary><c>:backdrop</c>.</summary>
    Backdrop = 1 << 6,

    /// <summary><c>:dir(ltr)</c>.</summary>
    DirLtr = 1 << 7,

    /// <summary><c>:dir(rtl)</c>.</summary>
    DirRtl = 1 << 8,

    /// <summary><c>:link</c>.</summary>
    Link = 1 << 9,

    /// <summary><c>:visited</c>.</summary>
    Visited = 1 << 10,

    /// <summary><c>:checked</c>.</summary>
    Checked = 1 << 11,

    /// <summary><c>:drop(active)</c>.</summary>
    DropActive = 1 << 12,

    /// <summary><c>:focus-visible</c>.</summary>
    FocusVisible = 1 << 13,

    /// <summary><c>:focus-within</c>.</summary>
    FocusWithin = 1 << 14,
}
