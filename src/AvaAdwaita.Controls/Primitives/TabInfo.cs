using Avalonia.Controls;
using AvaAdwaita.Presenters;

namespace AvaAdwaita.Controls.Primitives;

/// <summary>A tab box's tab (adw-tab-box.c's TabInfo): its page, tab, container and separator, and where it is.</summary>
internal sealed class TabInfo(TabPage page, Tab tab, TabBoxChild container, AdwBox separator)
{
    /// <summary>The page; none once it is closing.</summary>
    public TabPage? Page { get; set; } = page;

    public Tab Tab { get; } = tab;

    public TabBoxChild Container { get; } = container;

    public AdwBox Separator { get; } = separator;

    /// <summary>Where the tab is, and where it is going.</summary>
    public double Pos { get; set; } = -1;

    public double FinalPos { get; set; } = -1;

    /// <summary>How wide the tab is, and will be.</summary>
    public double Width { get; set; } = -1;

    public double FinalWidth { get; set; }

    /// <summary>Its width when the widths froze.</summary>
    public double LastWidth { get; set; }

    /// <summary>How far it has opened, 0 to 1.</summary>
    public double AppearProgress { get; set; }

    public TimedAnimation? AppearAnimation { get; set; }

    /// <summary>Where the tab would be with no tab being reordered.</summary>
    public double UnshiftedPos { get; set; } = -1;

    /// <summary>How far the tab has moved aside for a reordered one, in its widths with the spacing, and how far it is going.</summary>
    public double ReorderOffset { get; set; }

    public double EndReorderOffset { get; set; }

    public TimedAnimation? ReorderAnimation { get; set; }
}
