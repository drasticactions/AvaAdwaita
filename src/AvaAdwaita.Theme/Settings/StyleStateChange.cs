using System;

namespace AvaAdwaita.Settings;

/// <summary>What changed in a <see cref="StyleState"/>.</summary>
[Flags]
public enum StyleStateChange
{
    /// <summary>Nothing.</summary>
    None = 0,

    /// <summary>Dark or light.</summary>
    ColorScheme = 1,

    /// <summary>High contrast.</summary>
    Contrast = 2,

    /// <summary>The accent color.</summary>
    Accent = 4,

    /// <summary>The interface, document or monospace font.</summary>
    Fonts = 8,

    /// <summary>The window button layout.</summary>
    ButtonLayout = 16,

    /// <summary>Animations or reduced motion.</summary>
    Motion = 32,

    /// <summary>The icon theme.</summary>
    Icons = 64,

    /// <summary>Whether status shapes show (a switch's on and off marks).</summary>
    StatusShapes = 128,
}
