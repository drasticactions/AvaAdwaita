using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;
using AvaAdwaita.CssEngine.Selectors;

namespace AvaAdwaita.Nodes;

/// <summary>
/// Places nodes in the CSS node tree. A node's parent is the nearest visual ancestor with a node;
/// a popup's content is parented to its window's node, as no rule in gtk.css depends on what lies between.
/// </summary>
internal static class NodeTree
{
    static NodeTree()
    {
        WindowBase.IsActiveProperty.Changed.AddClassHandler<WindowBase>((w, _) => RefreshWindow(w));
        ThemeVariantScope.RequestedThemeVariantProperty.Changed.AddClassHandler<ThemeVariantScope>((s, _) => RefreshDescendants(s));
    }

    public static void Insert(NodeBinder binder)
    {
        var node = binder.Node;
        var parent = FindParent(binder.Owner);
        if (parent is null)
        {
            node.Parent?.RemoveChild(node);
            AdwaitaEnvironment.Engine.AddRoot(node);
        }
        else
        {
            AdwaitaEnvironment.Engine.RemoveRoot(node);
            parent.Node.InsertChild(IndexAmong(parent, binder), node);
        }

        if (binder.Owner is Visual v)
        {
            AdoptDescendants(binder, v);
        }
    }

    public static void Remove(NodeBinder binder)
    {
        var node = binder.Node;
        var children = new List<CssNode>(node.Children);
        if (node.Parent is { } p)
        {
            p.RemoveChild(node);
        }
        else
        {
            AdwaitaEnvironment.Engine.RemoveRoot(node);
        }

        // Children of a node that went away while they stay attached find a new parent.
        foreach (var c in children)
        {
            node.RemoveChild(c);
            if (c.Owner is NodeBinder cb && cb.Owner is Visual cv && cv.IsAttachedToVisualTree())
            {
                Insert(cb);
            }
        }
    }

    /// <summary>The binder whose node is the parent of <paramref name="owner"/>'s node.</summary>
    public static NodeBinder? FindParent(StyledElement owner)
    {
        if (owner is not Visual start)
        {
            return null;
        }

        var v = start.GetVisualParent();
        Visual? last = start;
        while (v is not null)
        {
            if (v.GetValue(Css.BinderProperty) is { } b)
            {
                return b;
            }

            // A tooltip is a window of its own (GtkTooltipWindow): its node is a root, not a child of the widget.
            if (v is PopupRoot or OverlayPopupHost && (v as StyledElement)?.Parent is Popup { Child: ToolTip })
            {
                return null;
            }

            // GTK parents a popover to the widget it pops up from (dropdown > popover, menubutton > popover),
            // and stylesheet rules such as dropdown popover.menu depend on it.
            if (v is PopupRoot or OverlayPopupHost && PopupOwner(v) is { } popupOwner)
            {
                last = v;
                v = popupOwner;
                continue;
            }

            if (v is PopupRoot pr)
            {
                return pr.ParentTopLevel.GetValue(Css.BinderProperty);
            }

            last = v;
            v = v.GetVisualParent();
        }

        // Window decorations live beside the window under its host: use the window's node.
        if (last is not null && !ReferenceEquals(last, start))
        {
            foreach (var c in last.GetVisualChildren())
            {
                if (c is TopLevel tl && !ReferenceEquals(tl, start) && tl.GetValue(Css.BinderProperty) is { } wb)
                {
                    return wb;
                }
            }
        }

        return null;
    }

    // The element a popup belongs to: the Popup itself when it is in a control's template, or what it was opened
    // from (a flyout's or a context menu's target).
    private static Visual? PopupOwner(Visual host) => (host as StyledElement)?.Parent is Popup popup
        ? popup.IsAttachedToVisualTree() ? popup : popup.PlacementTarget
        : null;

    private static int IndexAmong(NodeBinder parent, NodeBinder child)
    {
        var siblings = parent.Node.Children;
        if (child.Owner is not Visual cv || parent.Owner is not Visual pv)
        {
            return siblings.Count;
        }

        var path = PathFrom(pv, cv);
        if (path is null)
        {
            return siblings.Count;
        }

        for (var i = 0; i < siblings.Count; i++)
        {
            if (siblings[i].Owner is NodeBinder sb && sb.Owner is Visual sv && !ReferenceEquals(sb, child))
            {
                var other = PathFrom(pv, sv);
                if (other is not null && Compare(path, other) < 0)
                {
                    return i;
                }
            }
        }

        return siblings.Count;
    }

    private static List<int>? PathFrom(Visual ancestor, Visual node)
    {
        var path = new List<int>();
        var n = node;
        while (!ReferenceEquals(n, ancestor))
        {
            var p = n.GetVisualParent();
            if (p is null)
            {
                return null;
            }

            var index = 0;
            foreach (var c in p.GetVisualChildren())
            {
                if (ReferenceEquals(c, n))
                {
                    break;
                }

                index++;
            }

            path.Add(index);
            n = p;
        }

        path.Reverse();
        return path;
    }

    private static int Compare(List<int> a, List<int> b)
    {
        for (var i = 0; i < a.Count && i < b.Count; i++)
        {
            if (a[i] != b[i])
            {
                return a[i].CompareTo(b[i]);
            }
        }

        return a.Count.CompareTo(b.Count);
    }

    private static void AdoptDescendants(NodeBinder binder, Visual root)
    {
        foreach (var c in root.GetVisualChildren())
        {
            if (c.GetValue(Css.BinderProperty) is { } cb)
            {
                if (!ReferenceEquals(cb.Node.Parent, binder.Node))
                {
                    Insert(cb);
                }

                continue;
            }

            AdoptDescendants(binder, c);
        }
    }

    /// <summary>States added to every window's nodes, for tests that need <c>:backdrop</c> without an inactive window.</summary>
    public static CssStates ExtraWindowStates { get; set; }

    /// <summary>
    /// The dark state a ThemeVariantScope with a fixed variant imposes on the first node inside it, or null. Nodes
    /// deeper in follow their parent node, as the media state passes down the node tree.
    /// </summary>
    public static bool? ScopeDark(StyledElement owner)
    {
        for (var v = (owner as Visual)?.GetVisualParent(); v is not null; v = v.GetVisualParent())
        {
            if (v.GetValue(Css.BinderProperty) is not null)
            {
                return null;
            }

            if (v is ThemeVariantScope { RequestedThemeVariant: { } t } && t != Avalonia.Styling.ThemeVariant.Default)
            {
                return t == Avalonia.Styling.ThemeVariant.Dark;
            }
        }

        return null;
    }

    private static void RefreshDescendants(Visual root)
    {
        foreach (var c in root.GetVisualChildren())
        {
            if (c.GetValue(Css.BinderProperty) is { } b)
            {
                b.Refresh();
                continue;
            }

            RefreshDescendants(c);
        }
    }

    /// <summary>The states every node in a window shares: <c>:backdrop</c> when the window is inactive.</summary>
    public static CssStates WindowStates(NodeBinder binder)
    {
        if (binder.Owner is not Visual v)
        {
            return CssStates.None;
        }

        var tl = TopLevel.GetTopLevel(v);
        if (tl is PopupRoot pr)
        {
            tl = pr.ParentTopLevel;
        }

        return (tl is WindowBase { IsActive: false } && AdwaitaEnvironment.BackdropEnabled ? CssStates.Backdrop : CssStates.None) | ExtraWindowStates;
    }

    private static void RefreshWindow(WindowBase w)
    {
        if (w.GetValue(Css.BinderProperty) is { } b)
        {
            RefreshSubtree(b.Node);
        }
    }

    public static void RefreshSubtree(CssNode node)
    {
        if (node.Owner is NodeBinder b)
        {
            b.Refresh();
        }

        foreach (var c in node.Children)
        {
            RefreshSubtree(c);
        }
    }
}
