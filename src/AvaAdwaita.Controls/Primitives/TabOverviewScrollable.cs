using System;
using Avalonia;
using Avalonia.Controls;
using AvaAdwaita.Nodes;

namespace AvaAdwaita.Controls.Primitives;

/// <summary>
/// A tab overview's scrolled content (AdwTabOverviewScrollable, a <c>widget</c> node): the pinned grid over the other,
/// which fills what is left above the new tab button.
/// </summary>
public class TabOverviewScrollable : Control
{
    /// <summary>Creates the content with its two grids.</summary>
    public TabOverviewScrollable(TabGrid pinnedGrid, TabGrid grid)
    {
        PinnedGrid = pinnedGrid;
        Grid = grid;
        Css.SetNode(this, "widget");
        VisualChildren.Add(pinnedGrid);
        VisualChildren.Add(grid);
        LogicalChildren.Add(pinnedGrid);
        LogicalChildren.Add(grid);
    }

    /// <summary>The grid of pinned pages.</summary>
    public TabGrid PinnedGrid { get; }

    /// <summary>The grid of the other pages.</summary>
    public TabGrid Grid { get; }

    /// <summary>The new tab button over the bottom, which the grid stays clear of.</summary>
    internal Control? NewButton { get; set; }

    private double NewButtonHeight => NewButton is { IsVisible: true } b ? b.DesiredSize.Height : 0;

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        NewButton?.Measure(new Size(availableSize.Width, double.PositiveInfinity));
        var constraint = new Size(availableSize.Width, double.PositiveInfinity);
        PinnedGrid.Measure(constraint);
        Grid.Measure(constraint);
        return new Size(Math.Max(PinnedGrid.DesiredSize.Width, Grid.DesiredSize.Width), PinnedGrid.DesiredSize.Height + Grid.DesiredSize.Height + NewButtonHeight);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        // adw_tab_overview_scrollable_size_allocate.
        var pinned = PinnedGrid.DesiredSize.Height;
        var grid = Math.Max(Grid.DesiredSize.Height, finalSize.Height - NewButtonHeight - pinned);
        PinnedGrid.Arrange(new Rect(0, 0, finalSize.Width, pinned));
        Grid.Arrange(new Rect(0, pinned, finalSize.Width, grid));
        return finalSize;
    }
}
