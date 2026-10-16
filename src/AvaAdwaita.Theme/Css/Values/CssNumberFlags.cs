using System;

namespace AvaAdwaita.CssEngine.Values;

/// <summary>What a numeric value may be, as GTK's <c>GtkCssNumberParseFlags</c>.</summary>
[Flags]
internal enum CssNumberFlags : byte
{
    None = 0,
    Number = 1,
    Percent = 2,
    Length = 4,
    Angle = 8,
    Time = 16,
    PositiveOnly = 32,
}
