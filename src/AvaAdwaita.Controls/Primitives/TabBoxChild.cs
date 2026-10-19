using System;
using Avalonia;
using Avalonia.Controls;
using AvaAdwaita.Nodes;

namespace AvaAdwaita.Controls.Primitives;

/// <summary>
/// A tab box's container for a tab (a <c>tabboxchild</c> node, overflow hidden): while the tab opens or closes it is
/// narrower than the tab, which stays at its final width, centered.
/// </summary>
public class TabBoxChild : Decorator
{
    /// <summary>Creates a container.</summary>
    public TabBoxChild()
    {
        Css.SetNode(this, "tabboxchild");
        ClipToBounds = true;
        Focusable = true;
    }

    /// <summary>The tab's width once it has opened.</summary>
    internal double FinalWidth { get; set; }

    /// <inheritdoc/>
    protected override Avalonia.Automation.Peers.AutomationPeer OnCreateAutomationPeer() => new Automation.TabAutomationPeer(this);

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        // measure_tab: the tab's size, with no minimum width.
        Child?.Measure(new Size(double.PositiveInfinity, availableSize.Height));
        return Child?.DesiredSize ?? default;
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        // allocate_tab.
        var diff = Math.Max(0, FinalWidth - finalSize.Width);
        Child?.Arrange(new Rect(-Math.Floor(diff / 2), 0, finalSize.Width + diff, finalSize.Height));
        return finalSize;
    }
}
