using System;

namespace AvaAdwaita.Controls.Primitives;

/// <summary>The modifiers of a GTK accelerator (GdkModifierType).</summary>
[Flags]
internal enum GdkModifiers
{
    None = 0,
    Shift = 1,
    Control = 2,
    Alt = 4,
    Super = 8,
    Hyper = 16,
    Meta = 32,
}
