using Avalonia.Media;

namespace AvaAdwaita.Gallery.Pages;

/// <summary>A color custom property and the brush it resolves to.</summary>
public sealed record Swatch(string Name, IBrush? Brush);
