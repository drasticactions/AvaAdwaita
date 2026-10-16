using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;
using AvaAdwaita.Presenters;

namespace AvaAdwaita.Chrome;

/// <summary>
/// A popover's <c>contents</c> node. Its layout margin is as large as its shadow, and the tail's height on the
/// side facing the anchor, so the <c>popover</c> node spans the popup's surface as GTK's does. The popup's child
/// gets the shadow as its margin, which Avalonia leaves out when it places the popup (as GTK's shadow width),
/// and the <c>popover</c> node the negative of it, so the contents rather than the shadow meet the anchor.
/// </summary>
public class PopoverContents : AdwBox
{
    static PopoverContents()
    {
        Nodes.Css.StyleProperty.Changed.AddClassHandler<PopoverContents>((c, _) => c.UpdateShadowSpace());
    }

    private PopoverSide _tailSide;

    private Thickness _shadow;

    /// <summary>The side that also leaves room for the arrow's tail, set by <see cref="PopoverFrame"/>.</summary>
    public PopoverSide TailSide
    {
        get => _tailSide;
        set
        {
            if (_tailSide != value)
            {
                _tailSide = value;
                UpdateShadowSpace();
            }
        }
    }

    /// <summary>Creates the contents node.</summary>
    public PopoverContents()
    {
        Nodes.Css.SetNode(this, "contents");
    }

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        UpdateShadowSpace();
        PlaceShadow();
    }

    private void PlaceShadow()
    {
        Control? popover = null;
        for (var v = (Visual)this; v.GetVisualParent() is { } parent; v = parent)
        {
            if (v is Control c && Nodes.Css.GetNode(c) == "popover")
            {
                popover = c;
            }

            if (parent is ContentPresenter { TemplatedParent: PopupRoot or OverlayPopupHost } && v is Control child && popover is not null && popover != child)
            {
                var shadow = ShadowSpace();
                child.Margin = shadow;
                popover.Margin = new Thickness(-shadow.Left, -shadow.Top, -shadow.Right, -shadow.Bottom);

                // Avalonia places a popup by its child's margin taken as the screen's sides, while a right-to-left
                // popup lays the margin out mirrored: move it by the difference, so the contents meet the anchor.
                if (OwningPopup(this) is { FlowDirection: Avalonia.Media.FlowDirection.RightToLeft } popup
                    && _tailSide == PopoverSide.None && shadow.Left != shadow.Right)
                {
                    popup.HorizontalOffset = shadow.Left - shadow.Right;
                }

                return;
            }
        }
    }

    /// <summary>The popup whose window or overlay holds <paramref name="visual"/>.</summary>
    internal static Popup? OwningPopup(Visual visual) =>
        visual.GetSelfAndVisualAncestors().FirstOrDefault(v => v is PopupRoot or OverlayPopupHost) is StyledElement { Parent: Popup popup } ? popup : null;

    /// <summary>
    /// The shadow's room in the popup. An arrowless popover placed beside its anchor (a menu bar's menu below its
    /// item, a submenu beside its row) leaves the shadow out on the anchor's side: GTK takes no input in a shadow,
    /// while Avalonia's popup surface would take the pointer over the menu bar or the parent menu there.
    /// </summary>
    private Thickness ShadowSpace()
    {
        if (_tailSide != PopoverSide.None || OwningPopup(this) is not { } popup)
        {
            return _shadow;
        }

        return popup.Placement switch
        {
            PlacementMode.Bottom or PlacementMode.BottomEdgeAlignedLeft or PlacementMode.BottomEdgeAlignedRight => new Thickness(_shadow.Left, 0, _shadow.Right, _shadow.Bottom),
            PlacementMode.Top or PlacementMode.TopEdgeAlignedLeft or PlacementMode.TopEdgeAlignedRight => new Thickness(_shadow.Left, _shadow.Top, _shadow.Right, 0),
            PlacementMode.Right or PlacementMode.RightEdgeAlignedTop or PlacementMode.RightEdgeAlignedBottom => new Thickness(0, _shadow.Top, _shadow.Right, _shadow.Bottom),
            PlacementMode.Left or PlacementMode.LeftEdgeAlignedTop or PlacementMode.LeftEdgeAlignedBottom => new Thickness(_shadow.Left, _shadow.Top, 0, _shadow.Bottom),
            _ => _shadow,
        };
    }

    private void UpdateShadowSpace()
    {
        if (CurrentStyle is { } s || GetValue(Nodes.Css.StyleProperty) is { } s2 && (s = s2) is not null)
        {
            _shadow = ShadowExtents.Of(s.Background.BoxShadow);
            var extents = PopoverArrow.WithTail(ShadowSpace(), _tailSide, PopoverArrow.TailHeight);
            if (Margin != extents)
            {
                Margin = extents;
            }

            PlaceShadow();
        }
    }
}
