using System;
using AvaAdwaita.CssEngine.Computed;

namespace AvaAdwaita.Nodes;

/// <summary>The old and new computed style of a node.</summary>
public sealed class CssStyleChangedEventArgs(ComputedStyle? oldStyle, ComputedStyle newStyle) : EventArgs
{
    /// <summary>The previous style, or null for the first resolution.</summary>
    public ComputedStyle? OldStyle { get; } = oldStyle;

    /// <summary>The new style.</summary>
    public ComputedStyle NewStyle { get; } = newStyle;
}
