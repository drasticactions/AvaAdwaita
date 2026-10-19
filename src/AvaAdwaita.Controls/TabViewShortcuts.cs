using System;

namespace AvaAdwaita.Controls;

/// <summary>The keyboard shortcuts a tab view handles (AdwTabViewShortcuts), anywhere in its window.</summary>
[Flags]
public enum TabViewShortcuts
{
    /// <summary>None.</summary>
    None = 0,

    /// <summary>Ctrl+Tab: the next page, from the last to the first.</summary>
    ControlTab = 1 << 0,

    /// <summary>Ctrl+Shift+Tab: the previous page, from the first to the last.</summary>
    ControlShiftTab = 1 << 1,

    /// <summary>Ctrl+Page Up: the previous page.</summary>
    ControlPageUp = 1 << 2,

    /// <summary>Ctrl+Page Down: the next page.</summary>
    ControlPageDown = 1 << 3,

    /// <summary>Ctrl+Home: the first page.</summary>
    ControlHome = 1 << 4,

    /// <summary>Ctrl+End: the last page.</summary>
    ControlEnd = 1 << 5,

    /// <summary>Ctrl+Shift+Page Up: move the page back.</summary>
    ControlShiftPageUp = 1 << 6,

    /// <summary>Ctrl+Shift+Page Down: move the page forward.</summary>
    ControlShiftPageDown = 1 << 7,

    /// <summary>Ctrl+Shift+Home: move the page first.</summary>
    ControlShiftHome = 1 << 8,

    /// <summary>Ctrl+Shift+End: move the page last.</summary>
    ControlShiftEnd = 1 << 9,

    /// <summary>Alt+1 to Alt+9: that page.</summary>
    AltDigits = 1 << 10,

    /// <summary>Alt+0: the tenth page.</summary>
    AltZero = 1 << 11,

    /// <summary>All of them.</summary>
    AllShortcuts = 0xFFF,
}
