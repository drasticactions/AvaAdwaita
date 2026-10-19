using System;
using Avalonia;
using Avalonia.Controls;
using AvaAdwaita.Nodes;
using AvaAdwaita.Presenters;

namespace AvaAdwaita.Controls.Primitives;

/// <summary>
/// The dimming, shadow, border and outline a sliding pane casts on what is under it (AdwShadowHelper): four nodes the
/// host adds first and draws last.
/// </summary>
internal sealed class ShadowHelper
{
    private readonly AdwBox _dimming = Node("dimming");
    private readonly AdwBox _shadow = Node("shadow");
    private readonly AdwBox _border = Node("border");
    private readonly AdwBox _outline = Node("outline");

    /// <summary>The nodes, in GTK's order; the host makes them its first children.</summary>
    public Control[] Nodes => [_dimming, _shadow, _border, _outline];

    private static AdwBox Node(string name)
    {
        var box = new AdwBox { IsVisible = false, IsHitTestVisible = false, ZIndex = 10 };
        Css.SetNode(box, name);
        return box;
    }

    /// <summary>Hides the nodes, as when the host shows no sliding pane.</summary>
    public void Hide()
    {
        foreach (var n in Nodes)
        {
            n.IsVisible = false;
        }
    }

    /// <summary>
    /// Places the nodes for a pane at (<paramref name="x"/>, <paramref name="y"/>) over a host of
    /// <paramref name="width"/> by <paramref name="height"/>, <paramref name="progress"/> of the way to fully covered
    /// (adw_shadow_helper_size_allocate).
    /// </summary>
    public void Arrange(double width, double height, double x, double y, double progress, ShadowDirection direction)
    {
        var cls = direction.ToString().ToLowerInvariant();
        foreach (var n in Nodes)
        {
            Css.SetClasses(n, cls);
            n.IsVisible = progress < 1;
            n.Measure(Size.Infinity);
        }

        _dimming.Arrange(new Rect(x, y, width, height));
        var horizontal = direction is ShadowDirection.Left or ShadowDirection.Right;
        double Extent(Control c) => horizontal ? c.DesiredSize.Width : c.DesiredSize.Height;
        var (shadow, border, outline) = (Extent(_shadow), Extent(_border), Extent(_outline));
        var remaining = (1 - progress) * (horizontal ? width : height);
        _dimming.Opacity = 1 - progress;
        _shadow.Opacity = remaining < shadow && shadow > 0 ? remaining / shadow : 1;

        Rect Across(double at, double size) => horizontal
            ? new Rect(at, y, size, Math.Max(height, size))
            : new Rect(x, at, Math.Max(width, size), size);

        switch (direction)
        {
            case ShadowDirection.Left:
                _shadow.Arrange(Across(x, shadow));
                _border.Arrange(Across(x, border));
                _outline.Arrange(Across(x - outline, outline));
                break;
            case ShadowDirection.Right:
                _shadow.Arrange(Across(x + width - shadow, shadow));
                _border.Arrange(Across(x + width - border, border));
                _outline.Arrange(Across(x + width, outline));
                break;
            case ShadowDirection.Up:
                _shadow.Arrange(Across(y, shadow));
                _border.Arrange(Across(y, border));
                _outline.Arrange(Across(y - outline, outline));
                break;
            default:
                _shadow.Arrange(Across(y + height - shadow, shadow));
                _border.Arrange(Across(y + height - border, border));
                _outline.Arrange(Across(y + height, outline));
                break;
        }
    }
}
