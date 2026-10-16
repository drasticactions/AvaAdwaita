using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using AvaAdwaita.Presenters;

namespace AvaAdwaita.Chrome;

/// <summary>
/// Lays out a popover as GtkPopover: the <c>arrow</c> node and the <see cref="PopoverContents"/> it holds.
/// The side facing the anchor gets the tail's height as well as the shadow space; the side comes from where
/// Avalonia put the popup relative to its anchor, after any flip.
/// </summary>
public class PopoverFrame : Decorator
{
    /// <summary>Draws the arrow; false for drop-downs, menu bars and context menus, as GTK.</summary>
    public static readonly StyledProperty<bool> HasArrowProperty =
        AvaloniaProperty.Register<PopoverFrame, bool>(nameof(HasArrow), true);

    private readonly PopoverArrow _arrow = new();

    static PopoverFrame()
    {
        AffectsMeasure<PopoverFrame>(HasArrowProperty);
    }

    /// <summary>Creates the frame.</summary>
    public PopoverFrame()
    {
        // GTK draws the arrow over the contents and their shadow (gtk_popover_snapshot).
        _arrow.ZIndex = 1;
        VisualChildren.Insert(0, _arrow);
        LogicalChildren.Insert(0, _arrow);
    }

    /// <summary>Gets or sets whether the popover has an arrow.</summary>
    public bool HasArrow
    {
        get => GetValue(HasArrowProperty);
        set => SetValue(HasArrowProperty, value);
    }

    /// <summary>The side the arrow is on.</summary>
    public PopoverSide Side { get; private set; }

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        SetSide(HasArrow ? IntendedSide() : PopoverSide.None);
        LayoutUpdated += OnLayoutUpdated;
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        _arrow.Measure(availableSize);
        return base.MeasureOverride(availableSize);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        var size = base.ArrangeOverride(finalSize);
        _arrow.Arrange(new Rect(finalSize));
        return size;
    }

    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        LayoutUpdated -= OnLayoutUpdated;
    }

    // The popup is placed after its content is arranged, so the side and the tip are read once layout settles.
    private void OnLayoutUpdated(object? sender, EventArgs e)
    {
        if (!HasArrow)
        {
            SetSide(PopoverSide.None);
            return;
        }

        var actual = ActualSide();
        if (actual is { Side: not PopoverSide.None } a && a.Side != Side)
        {
            SetSide(a.Side);
        }

        if (Child is PopoverContents contents && contents.GetValue(Nodes.Css.StyleProperty) is { } s)
        {
            var g = new CssBoxGeometry(s, contents.Bounds.Size);
            var shadow = PopoverArrow.WithTail(contents.Margin, Side, -PopoverArrow.TailHeight);
            var tip = actual is { Side: not PopoverSide.None } found ? found.Tip : Side is PopoverSide.Top or PopoverSide.Bottom ? Bounds.Width / 2 : Bounds.Height / 2;
            _arrow.Update(Side, tip, shadow, g.Outer.RadiiTopLeft.X, Markup.CssConvert.Thickness(g.Border));
        }
    }

    private void SetSide(PopoverSide side)
    {
        Side = side;
        if (Child is PopoverContents contents)
        {
            contents.TailSide = side;
        }
    }

    private Popup? OwnerPopup => this.FindLogicalAncestorOfType<Popup>();

    // The side Avalonia aims for before it measures the popup and maybe flips it.
    private PopoverSide IntendedSide() => OwnerPopup?.Placement switch
    {
        PlacementMode.Top or PlacementMode.TopEdgeAlignedLeft or PlacementMode.TopEdgeAlignedRight => PopoverSide.Bottom,
        PlacementMode.Left or PlacementMode.LeftEdgeAlignedTop or PlacementMode.LeftEdgeAlignedBottom => PopoverSide.Right,
        PlacementMode.Right or PlacementMode.RightEdgeAlignedTop or PlacementMode.RightEdgeAlignedBottom => PopoverSide.Left,
        PlacementMode.Center or PlacementMode.Pointer => PopoverSide.None,
        PlacementMode.AnchorAndGravity => OwnerPopup.PlacementGravity switch
        {
            PopupGravity.Top or PopupGravity.TopLeft or PopupGravity.TopRight => PopoverSide.Bottom,
            PopupGravity.Left => PopoverSide.Right,
            PopupGravity.Right => PopoverSide.Left,
            _ => PopoverSide.Top,
        },
        _ => PopoverSide.Top,
    };

    // Where the popup ended up: compares the contents' and the anchor's rectangles on screen. A backend that
    // cannot tell where its popup is (Wayland keeps the configured position to itself) reports a rectangle
    // that does not touch the anchor; the placement is then worked out from the window, as the positioner does.
    private (PopoverSide Side, double Tip)? ActualSide()
    {
        if (OwnerPopup is not { } popup || (popup.PlacementTarget ?? popup.Parent as Visual) is not { } anchor
            || Child is not PopoverContents contents || ScreenRect(anchor) is not { } a || ScreenRect(this) is not { } frame)
        {
            return null;
        }

        var c = new Rect(frame.X + contents.Bounds.X, frame.Y + contents.Bounds.Y, contents.Bounds.Width, contents.Bounds.Height);
        var side = SideOf(c, a);
        if (side is not { } found || !Touches(c, a, found))
        {
            return Estimate(popup, anchor);
        }

        if (found == PopoverSide.None)
        {
            return (PopoverSide.None, 0);
        }

        var tip = found is PopoverSide.Top or PopoverSide.Bottom ? a.Center.X - frame.X : a.Center.Y - frame.Y;
        return (found, tip);
    }

    private static PopoverSide? SideOf(Rect c, Rect a)
    {
        const double Slack = 1;
        if (c.Top >= a.Bottom - Slack)
        {
            return PopoverSide.Top;
        }

        if (c.Bottom <= a.Top + Slack)
        {
            return PopoverSide.Bottom;
        }

        if (c.Left >= a.Right - Slack)
        {
            return PopoverSide.Left;
        }

        return c.Right <= a.Left + Slack ? PopoverSide.Right : null;
    }

    // The contents sit a tail's height from the anchor, across from it.
    private static bool Touches(Rect c, Rect a, PopoverSide side)
    {
        const double Reach = PopoverArrow.TailHeight + 2;
        return side switch
        {
            PopoverSide.Top => c.Top - a.Bottom <= Reach && c.Left < a.Right && c.Right > a.Left,
            PopoverSide.Bottom => a.Top - c.Bottom <= Reach && c.Left < a.Right && c.Right > a.Left,
            PopoverSide.Left => c.Left - a.Right <= Reach && c.Top < a.Bottom && c.Bottom > a.Top,
            PopoverSide.Right => a.Left - c.Right <= Reach && c.Top < a.Bottom && c.Bottom > a.Top,
            _ => false,
        };
    }

    // The side and the tip the positioner gives a popup of this size against the anchor in its window: it flips
    // when the intended side has no room and the other has, then slides along the window.
    private (PopoverSide Side, double Tip)? Estimate(Popup popup, Visual anchor)
    {
        if (TopLevel.GetTopLevel(anchor) is not { } top || anchor.TranslatePoint(default, top) is not { } origin)
        {
            return null;
        }

        // The positioner places the frame less its shadow, the part of the contents' margin that is not the tail.
        var a = new Rect(origin, anchor.Bounds.Size);
        var window = top.Bounds.Size;
        var shadow = Child is PopoverContents contents ? PopoverArrow.WithTail(contents.Margin, Side, -PopoverArrow.TailHeight) : default;
        var size = Bounds.Size.Deflate(shadow);
        var side = IntendedSide();
        side = side switch
        {
            PopoverSide.Top when a.Bottom + size.Height > window.Height && a.Top - size.Height >= 0 => PopoverSide.Bottom,
            PopoverSide.Bottom when a.Top - size.Height < 0 && a.Bottom + size.Height <= window.Height => PopoverSide.Top,
            PopoverSide.Left when a.Right + size.Width > window.Width && a.Left - size.Width >= 0 => PopoverSide.Right,
            PopoverSide.Right when a.Left - size.Width < 0 && a.Right + size.Width <= window.Width => PopoverSide.Left,
            _ => side,
        };

        var vertical = side is PopoverSide.Top or PopoverSide.Bottom;
        var (start, length, extent, center, span, before) = vertical
            ? (a.Left, a.Width, size.Width, a.Center.X, window.Width, shadow.Left)
            : (a.Top, a.Height, size.Height, a.Center.Y, window.Height, shadow.Top);
        var placed = popup.Placement switch
        {
            PlacementMode.BottomEdgeAlignedLeft or PlacementMode.TopEdgeAlignedLeft
                or PlacementMode.RightEdgeAlignedTop or PlacementMode.LeftEdgeAlignedTop => start,
            PlacementMode.BottomEdgeAlignedRight or PlacementMode.TopEdgeAlignedRight
                or PlacementMode.RightEdgeAlignedBottom or PlacementMode.LeftEdgeAlignedBottom => start + length - extent,
            _ => center - (extent / 2),
        };
        placed = Math.Clamp(placed, 0, Math.Max(0, span - extent));
        return (side, center - placed + before);
    }

    private static Rect? ScreenRect(Visual v)
    {
        if (TopLevel.GetTopLevel(v) is not { } top)
        {
            return null;
        }

        var origin = v.PointToScreen(default);
        var scale = top.RenderScaling;
        return new Rect(origin.X / scale, origin.Y / scale, v.Bounds.Width, v.Bounds.Height);
    }
}
