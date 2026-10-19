using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using AvaAdwaita.Nodes;

namespace AvaAdwaita.Controls.Primitives;

/// <summary>
/// A tab grid's container for a thumbnail (a <c>tabgridchild</c> node, overflow hidden): it scales up from 75% as the
/// thumbnail appears.
/// </summary>
public class TabGridChild : Decorator
{
    private const double MinScale = 0.75;

    /// <summary>Creates a container.</summary>
    public TabGridChild()
    {
        Css.SetNode(this, "tabgridchild");
        ClipToBounds = true;
        Focusable = true;
    }

    /// <summary>How far the thumbnail has appeared, 0 to 1.</summary>
    internal double AppearProgress
    {
        set
        {
            var scale = MinScale + ((1 - MinScale) * value);
            RenderTransform = value < 1 ? new ScaleTransform(scale, scale) : null;
            Opacity = value < 1 ? value : 1;
        }
    }

    /// <inheritdoc/>
    protected override Avalonia.Automation.Peers.AutomationPeer OnCreateAutomationPeer() => new Automation.TabThumbnailAutomationPeer(this);

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        Child?.Measure(availableSize);
        return Child?.DesiredSize ?? default;
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        // At the thumbnail's width, whatever it was last measured for.
        Child?.Measure(new Size(finalSize.Width, double.PositiveInfinity));
        Child?.Arrange(new Rect(finalSize));
        return finalSize;
    }
}
