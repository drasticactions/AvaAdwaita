using System;
using Avalonia;
using Avalonia.Controls;
using AvaAdwaita.Nodes;
using AvaAdwaita.Presenters;

namespace AvaAdwaita.Controls.Primitives;

/// <summary>
/// A tab bar's scrolled window for a row of tabs (a <c>scrolledwindow</c> node with no scroll bars, the row scrolling
/// itself): as wide as the row with <see cref="PropagatesNaturalWidth"/>, otherwise <see cref="MinContentWidth"/>.
/// </summary>
public class TabScroller : AdwBox
{
    /// <summary>Creates a scrolled window.</summary>
    public TabScroller()
    {
        Css.SetNode(this, "scrolledwindow");
        ClipToBounds = true;
    }

    /// <summary>Whether its natural width is the row's (propagate-natural-width).</summary>
    public bool PropagatesNaturalWidth { get; set; }

    /// <summary>Its width without the row's (min-content-width).</summary>
    public double MinContentWidth { get; set; }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        var size = base.MeasureOverride(availableSize);
        return PropagatesNaturalWidth ? size : size.WithWidth(Math.Min(size.Width, MinContentWidth));
    }
}
