using Avalonia.Media.Imaging;

namespace AvaAdwaita.Icons;

/// <summary>A found icon: vector shapes, recolored when symbolic, or a bitmap.</summary>
/// <param name="Name">The icon name that was found, which may be a fallback.</param>
/// <param name="Vector">The vector icon, or null.</param>
/// <param name="Bitmap">The bitmap icon, or null.</param>
/// <param name="IsSymbolic">True when the icon is recolored with the node's colors.</param>
/// <param name="Source">Where it came from, for diagnostics.</param>
public sealed record IconData(string Name, SymbolicSvg? Vector, Bitmap? Bitmap, bool IsSymbolic, string Source);
