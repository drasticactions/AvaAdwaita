using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using AvaAdwaita.Animation;
using AvaAdwaita.Nodes;
using AvaAdwaita.Presenters;

namespace AvaAdwaita.Controls.Primitives;

/// <summary>
/// A tab bar's row of tabs (AdwTabBox), the pinned ones or the others: a <c>tabbox</c> node laying the tabs out 5 px
/// apart in equal shares of its width, with separators between them, scrolling them when they do not fit. Tabs open and
/// close with their width animating; while the pointer is over the row, closing a tab keeps the others' widths until it
/// leaves.
/// </summary>
public class TabBox : Control
{
    private const double Spacing = 5;
    private const double OpenAnimationDuration = 200;
    private const double CloseAnimationDuration = 200;
    private const double FocusAnimationDuration = 200;
    private const double ScrollAnimationDuration = 200;
    private const double ResizeAnimationDuration = 200;
    private const double MaxTabWidthNonExpand = 220;
    private const double DragThreshold = 8;
    private const double ReorderAnimationDuration = 250;

    private readonly List<TabInfo> _tabs = [];
    private readonly Revealer _attentionLeft = Indicator("left");
    private readonly Revealer _attentionRight = Indicator("right");
    private readonly TimedAnimation _resizeAnimation;
    private readonly TimedAnimation _scrollAnimation;
    private TabView? _view;
    private TabInfo? _selected;
    private double _allocatedWidth;
    private double _lastWidth;
    private double _endPadding;
    private double _initialEndPadding;
    private double _finalEndPadding;
    private ResizeMode _resizeMode;
    private bool _hovering;
    private double _offset;
    private double _scrollFrom;
    private double _scrollOffset;
    private TabInfo? _scrollTab;
    private bool _scrollDone;
    private TabInfo? _pressed;
    private Point _pressPoint;
    private TabInfo? _middleClicked;
    private readonly TimedAnimation _reorderAnimation;
    private TabInfo? _reordered;
    private TabInfo? _reorderDest;
    private double _reorderX;
    private double _reorderWindowX;
    private int _reorderIndex;
    private bool _dragging;
    private bool _continueReorder;
    private bool _reorderingPage;
    private double _dragOffset;
    private bool _expandTabs = true;
    private bool _inverted;

    /// <summary>Creates a row of tabs, the pinned ones or the others.</summary>
    public TabBox(bool pinned)
    {
        IsPinned = pinned;
        Css.SetNode(this, "tabbox");
        ClipToBounds = true;
        Focusable = false;
        VisualChildren.Add(_attentionLeft);
        VisualChildren.Add(_attentionRight);

        // The attention indicators are drawn over a dragged tab too.
        _attentionLeft.ZIndex = _attentionRight.ZIndex = 2;
        _resizeAnimation = new TimedAnimation(this, ResizeAnimationDuration, AdwEasing.Ease, OnResizeAnimation, () =>
        {
            _endPadding = 0;
            _finalEndPadding = 0;
            InvalidateMeasure();
        });
        _reorderAnimation = new TimedAnimation(this, ReorderAnimationDuration, AdwEasing.Ease, OnReorderAnimation, CheckEndReordering);
        _scrollAnimation = new TimedAnimation(this, ScrollAnimationDuration, AdwEasing.Ease, _ => InvalidateArrange(), () =>
        {
            _scrollDone = true;
            InvalidateArrange();
        });
    }

    private enum ResizeMode
    {
        Normal,
        FixedTabWidth,
        FixedEndPadding,
    }

    /// <summary>Whether the row holds the pinned tabs.</summary>
    public bool IsPinned { get; }

    /// <summary>The other row of the same bar, whose last pinned tab the first unpinned separator depends on.</summary>
    internal TabBox? PinnedBox { get; set; }

    /// <summary>The non-pinned row of the same bar, to update when this pinned row's tabs change.</summary>
    internal TabBox? UnpinnedBox { get; set; }

    /// <summary>The bar the row is in.</summary>
    internal TabBar? Bar { get; set; }

    /// <summary>Whether the tabs share the whole width rather than stopping at 220 px each.</summary>
    public bool ExpandTabs
    {
        get => _expandTabs;
        set
        {
            _expandTabs = value;
            UpdateSingleTabStyle();
            InvalidateMeasure();
        }
    }

    /// <summary>Whether the tabs' close buttons are at their start.</summary>
    public bool IsInverted
    {
        get => _inverted;
        set
        {
            _inverted = value;
            foreach (var info in _tabs)
            {
                info.Tab.IsInverted = value;
            }
        }
    }

    /// <summary>Whether the row is wider than it shows.</summary>
    internal bool IsOverflowing => _allocatedWidth > Bounds.Width + 0.5;

    /// <summary>Whether the tab widths are frozen while the pointer is over the row.</summary>
    internal bool IsResizeFrozen => _resizeMode != ResizeMode.Normal;

    /// <summary>The tabs, in order.</summary>
    internal IEnumerable<Tab> Tabs => _tabs.Where(i => i.Page is not null).Select(i => i.Tab);

    /// <summary>Shows <paramref name="view"/>'s pages (adw_tab_box_set_view).</summary>
    public void SetView(TabView? view)
    {
        if (view == _view)
        {
            return;
        }

        if (_view is { } old)
        {
            old.PageAttached -= OnPageAttached;
            old.PageDetached -= OnPageDetached;
            old.PageReordered -= OnPageReordered;
            old.PropertyChanged -= OnViewPropertyChanged;
            foreach (var info in _tabs.ToList())
            {
                Remove(info);
            }
        }

        _view = view;
        if (view is not null)
        {
            // The pages go in from the last, each at the start, as GTK attaches them.
            for (var i = view.PageCount - 1; i >= 0; i--)
            {
                Attach(view.GetNthPage(i), 0, animate: false);
            }

            view.PageAttached += OnPageAttached;
            view.PageDetached += OnPageDetached;
            view.PageReordered += OnPageReordered;
            view.PropertyChanged += OnViewPropertyChanged;
            UpdateSingleTabStyle();
        }

        InvalidateMeasure();
    }

    /// <summary>Selects the tab of <paramref name="page"/>, scrolling it into view (adw_tab_box_select_page).</summary>
    public void SelectPage(TabPage? page)
    {
        if (page is null)
        {
            _selected = null;
            UpdateTabStops();
            return;
        }

        _selected = _tabs.FirstOrDefault(i => i.Page == page);
        UpdateTabStops();
        if (_selected is null)
        {
            return;
        }

        if (Bar?.TabsHaveVisibleFocus() == true)
        {
            _selected.Container.Focus(NavigationMethod.Tab);
        }

        if (_selected.Width >= 0)
        {
            ScrollToTab(_selected, -1, FocusAnimationDuration);
        }
    }

    /// <summary>Attaches <paramref name="page"/>, moved here from the other row when pinned or unpinned.</summary>
    internal void AttachPage(TabPage page, int position) => Attach(page, position, animate: true);

    /// <summary>Detaches <paramref name="page"/>, moved to the other row.</summary>
    internal void DetachPage(TabPage page) => Detach(page);

    /// <summary>Whether the focus is on <paramref name="page"/>'s tab.</summary>
    internal bool IsPageFocused(TabPage page) => _tabs.FirstOrDefault(i => i.Page == page)?.Container.IsFocused == true;

    /// <summary>Focuses the selected tab, if it is in this row.</summary>
    internal void TryFocusSelectedTab() => _selected?.Container.Focus(NavigationMethod.Tab);

    /// <summary>Whether the focus is on one of the row's tabs.</summary>
    internal bool HasFocusedTab => _tabs.Any(i => i.Container.IsKeyboardFocusWithin);

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        var height = 0.0;
        foreach (var info in _tabs)
        {
            info.Container.Measure(Size.Infinity);
            info.Separator.Measure(Size.Infinity);
            height = Math.Max(height, Math.Max(info.Container.DesiredSize.Height, info.Separator.DesiredSize.Height));
        }

        _attentionLeft.Measure(Size.Infinity);
        _attentionRight.Measure(Size.Infinity);
        height = Math.Max(height, Math.Max(_attentionLeft.DesiredSize.Height, _attentionRight.DesiredSize.Height));

        // The node's box: max (content, min-size) and its padding.
        var box = Box();
        return new Size(Math.Max(MeasureWidth(), MinSize.Width) + box.Left + box.Right, Math.Max(height, MinSize.Height) + box.Top + box.Bottom);
    }

    private Size MinSize => Css.GetStyle(this) is { } s ? new Size(Math.Ceiling(s.Size.MinWidth), Math.Ceiling(s.Size.MinHeight)) : default;

    // The node's margin, border and padding.
    private Thickness Box() => Css.GetStyle(this) is { } s && new CssBoxGeometry(s, default) is var g
        ? new Thickness(g.Margin.Left + g.Border.Left + g.Padding.Left, g.Margin.Top + g.Border.Top + g.Padding.Top, g.Margin.Right + g.Border.Right + g.Padding.Right, g.Margin.Bottom + g.Border.Bottom + g.Padding.Bottom)
        : default;

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        // adw_tab_box_size_allocate, inside the node's box.
        var box = Box();
        var (width, height) = (Math.Max(0, finalSize.Width - box.Left - box.Right), Math.Max(0, finalSize.Height - box.Top - box.Bottom));
        var (left0, top) = (box.Left, box.Top);
        _allocatedWidth = Math.Max(MeasureWidth(), width);
        if (_tabs.Count == 0)
        {
            // Never allocated, as GTK leaves an empty row's indicators: nothing, at the content's corner.
            _attentionLeft.Arrange(new Rect(left0, top, 0, 0));
            _attentionRight.Arrange(new Rect(left0, top, 0, 0));
            return finalSize;
        }

        if (IsPinned)
        {
            foreach (var info in _tabs)
            {
                var natural = info.Container.DesiredSize.Width;
                info.Width = TabWidth(info, natural);
                info.FinalWidth = natural;
            }
        }
        else if (_resizeMode == ResizeMode.FixedTabWidth)
        {
            _endPadding = _allocatedWidth - Spacing;
            _finalEndPadding = _endPadding;
            foreach (var info in _tabs)
            {
                info.Width = TabWidth(info, info.LastWidth);
                _endPadding -= info.Width + Spacing;
                info.FinalWidth = info.LastWidth;
                _finalEndPadding -= info.FinalWidth + Spacing;
            }
        }
        else
        {
            var tabWidth = BaseTabWidth(false, false);
            var finalTabWidth = BaseTabWidth(false, true);
            var excess = _allocatedWidth - Spacing - _endPadding;
            var finalExcess = excess;
            foreach (var info in _tabs)
            {
                info.Width = TabWidth(info, tabWidth);
                info.FinalWidth = finalTabWidth;
                excess -= info.Width + Spacing;
                finalExcess -= info.FinalWidth + Spacing;
            }

            // The excess width comes off the first tabs.
            foreach (var info in _tabs)
            {
                if (excess >= 0 && finalExcess >= 0)
                {
                    break;
                }

                if (excess < 0)
                {
                    info.Width--;
                    excess++;
                }

                if (finalExcess < 0)
                {
                    info.FinalWidth--;
                    finalExcess++;
                }
            }
        }

        double pos = Spacing, finalPos = Spacing;
        foreach (var info in _tabs)
        {
            info.UnshiftedPos = finalPos;
            info.Pos = pos + TabOffset(info, false);
            info.FinalPos = finalPos + TabOffset(info, true);
            pos += info.Width + Spacing;
            finalPos += info.FinalWidth + Spacing;
        }

        _offset = Math.Clamp(ScrollAnimationValue(), 0, Math.Max(0, _allocatedWidth - width));
        if (_scrollDone)
        {
            _scrollTab = null;
            _scrollDone = false;
        }

        foreach (var info in _tabs)
        {
            var x = TabPosition(info) - Math.Floor(_offset);
            var tabWidth = Math.Max(0, info.Width);
            info.Container.FinalWidth = info.FinalWidth;
            info.Container.Arrange(new Rect(left0 + x, top, tabWidth, height));
            var separatorWidth = info.Separator.DesiredSize.Width;
            info.Separator.Arrange(new Rect(left0 + x - Math.Floor((Spacing + separatorWidth) / 2), top, separatorWidth, height));
        }

        ClipUnderReordered();

        var left = _attentionLeft.DesiredSize.Width;
        _attentionLeft.Arrange(new Rect(left0, top, left, height));
        var right = _attentionRight.DesiredSize.Width;
        _attentionRight.Arrange(new Rect(left0 + width - right, top, right, height));
        UpdateVisible(width);
        return finalSize;
    }

    /// <inheritdoc/>
    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        if (e.Pointer.Type != PointerType.Touch)
        {
            _hovering = true;
        }
    }

    /// <inheritdoc/>
    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        _hovering = false;
        UpdateHover();
    }

    /// <inheritdoc/>
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var point = e.GetCurrentPoint(this);
        var x = point.Position.X + _offset;
        if (TabAt(x) is not { Page: { } page } info || _view is null || e.Source is Button)
        {
            return;
        }

        if (point.Properties.IsRightButtonPressed)
        {
            // do_popup.
            e.Handled = true;
            _view.ShowMenu(page, info.Container, point.Position);
            return;
        }

        if (point.Properties.IsMiddleButtonPressed)
        {
            e.Handled = true;
            _middleClicked = info;
            return;
        }

        if (!point.Properties.IsLeftButtonPressed)
        {
            return;
        }

        e.Handled = true;
        _pressed = info;
        _pressPoint = point.Position;
        HandleClick(info);
    }

    /// <inheritdoc/>
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (e.Pointer.Type != PointerType.Touch && !_hovering)
        {
            _hovering = true;
        }

        var point = e.GetCurrentPoint(this);
        if (_pressed is not { Page: not null } pressed || !point.Properties.IsLeftButtonPressed)
        {
            return;
        }

        if (!_dragging)
        {
            if (Math.Abs(point.Position.X - _pressPoint.X) < DragThreshold)
            {
                return;
            }

            // reorder_begin_cb: the tab follows the pointer.
            _dragOffset = _pressPoint.X + _offset - TabPosition(pressed);
            StartDragReordering(pressed);
            e.Pointer.Capture(this);
        }

        _reorderX = point.Position.X + _offset - _dragOffset;
        UpdateDragReordering();
    }

    /// <inheritdoc/>
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        var point = e.GetCurrentPoint(this);
        if (e.InitialPressMouseButton == MouseButton.Middle)
        {
            // released_cb: a middle click closes the tab it started on.
            if (TabAt(point.Position.X + _offset) is { Page: { } page } info && info == _middleClicked)
            {
                _view?.ClosePage(page);
            }

            _middleClicked = null;
            return;
        }

        if (_dragging)
        {
            e.Pointer.Capture(null);
            EndDragReordering();
        }

        _pressed = null;
    }

    /// <inheritdoc/>
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        EndDragReordering();
        _pressed = null;
    }

    /// <inheritdoc/>
    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        if (_tabs.Count == 0 || e.Pointer.Type != PointerType.Mouse)
        {
            return;
        }

        // scroll_cb, with gtkrange.c's wheel step.
        var pageSize = Bounds.Width;
        var unit = Math.Min(Math.Pow(pageSize, 2.0 / 3.0), pageSize / 2);
        var delta = -(Math.Abs(e.Delta.Y) > 0 ? e.Delta.Y : e.Delta.X);
        if (FlowDirection == FlowDirection.RightToLeft)
        {
            delta = -delta;
        }

        e.Handled = true;
        var current = _scrollAnimation.IsRunning ? _scrollOffset + (_scrollTab?.FinalPos ?? 0) : _offset;
        AnimateScroll(null, current + (delta * unit), ScrollAnimationDuration);
    }

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Handled || _view is null || _selected?.Page is not { } page)
        {
            return;
        }

        // focus_tab_cb and reorder_tab_cb: arrows, Home and End move between tabs, with Shift they move the tab.
        var rtl = FlowDirection == FlowDirection.RightToLeft;
        bool? forward = e.Key switch
        {
            Key.Left => rtl,
            Key.Right => !rtl,
            Key.Home => false,
            Key.End => true,
            _ => null,
        };
        if (forward is { } f && e.KeyModifiers is KeyModifiers.None or KeyModifiers.Shift)
        {
            e.Handled = true;
            var last = e.Key is Key.Home or Key.End;
            if (e.KeyModifiers == KeyModifiers.Shift)
            {
                _ = (f, last) switch
                {
                    (false, false) => _view.ReorderBackward(page),
                    (true, false) => _view.ReorderForward(page),
                    (false, true) => _view.ReorderFirst(page),
                    (true, true) => _view.ReorderLast(page),
                };
            }
            else
            {
                _ = (f, last) switch
                {
                    (false, false) => _view.SelectPreviousPage(),
                    (true, false) => _view.SelectNextPage(),
                    (false, true) => _view.SelectFirstPage(),
                    (true, true) => _view.SelectLastPage(),
                };
            }

            return;
        }

        // popup_menu_cb: Menu and Shift+F10.
        if (e.Key == Key.Apps || (e.Key == Key.F10 && e.KeyModifiers == KeyModifiers.Shift))
        {
            e.Handled = true;
            _view.ShowMenu(page, _selected.Container);
        }
    }

    private static Revealer Indicator(string side)
    {
        var indicator = new AdwBox();
        Css.SetNode(indicator, "indicator");
        Css.SetClasses(indicator, side);
        return new Revealer { Transition = RevealerTransition.Crossfade, IsHitTestVisible = false, Focusable = false, Child = indicator };
    }

    private static double TabWidth(TabInfo info, double baseWidth) =>
        Math.Floor((baseWidth + Spacing) * info.AppearProgress) - Spacing;

    // measure_tab_box, horizontally: the tabs at their natural widths as far as they have opened.
    private double MeasureWidth()
    {
        if (_tabs.Count == 0)
        {
            return 0;
        }

        var width = _endPadding;
        foreach (var info in _tabs)
        {
            info.Container.Measure(Size.Infinity);
            width += TabWidth(info, info.Container.DesiredSize.Width) + Spacing;
        }

        if (!IsPinned)
        {
            width += Spacing;
        }

        return Math.Max(_lastWidth, width);
    }

    // get_base_tab_width.
    private double BaseTabWidth(bool targetEndPadding, bool targetAnimations)
    {
        double maxProgress = 0, n = 0, endPadding = 0;
        if (targetAnimations)
        {
            maxProgress = 1;
            n = _tabs.Count;
            if (!targetEndPadding)
            {
                endPadding = _finalEndPadding;
            }
        }
        else
        {
            foreach (var info in _tabs)
            {
                maxProgress = Math.Max(maxProgress, info.AppearProgress);
                n += info.AppearProgress;
            }

            if (!targetEndPadding)
            {
                endPadding = _endPadding;
            }
        }

        if (n <= 0)
        {
            return 0;
        }

        var used = (_allocatedWidth - ((n + 1) * Spacing) - endPadding) * maxProgress;
        var width = Math.Ceiling(used / n);
        return ExpandTabs ? width : Math.Min(width, MaxTabWidthNonExpand - Spacing);
    }

    // snapshot_tabs: the dragged tab is drawn over the others, which are cut away under it, the ones before it at its
    // start and the ones after it from its end.
    private void ClipUnderReordered()
    {
        var reordered = _reordered;
        var start = reordered?.Container.Bounds.Left ?? 0;
        var end = reordered?.Container.Bounds.Right ?? 0;
        var after = false;
        foreach (var info in _tabs)
        {
            var dragged = info == reordered;
            info.Container.ZIndex = info.Separator.ZIndex = dragged ? 1 : 0;
            if (reordered is null || dragged)
            {
                info.Container.Clip = info.Separator.Clip = null;
                continue;
            }

            after |= info.Container.Bounds.Right > end;
            Clip(info.Container);
            Clip(info.Separator);
        }

        void Clip(Control child)
        {
            var b = child.Bounds;
            var visible = after
                ? new Rect(Math.Max(0, end - b.X), 0, Math.Max(0, b.Right - Math.Max(end, b.X)), b.Height)
                : new Rect(0, 0, Math.Clamp(start - b.X, 0, b.Width), b.Height);
            child.Clip = visible.Width >= b.Width ? null : new RectangleGeometry(visible);
        }
    }

    private TabInfo? TabAt(double x)
    {
        if (_reordered is { } r && _reorderWindowX <= x && x < _reorderWindowX + r.Width)
        {
            return r;
        }

        return _tabs.FirstOrDefault(i => i != _reordered && i.Pos <= x && x < i.Pos + i.Width);
    }

    // handle_click: a click on a partly hidden tab scrolls it into view; otherwise it selects the tab's page.
    private void HandleClick(TabInfo info)
    {
        if (_view is null || info.Page is not { } page)
        {
            return;
        }

        var pageSize = Bounds.Width;
        if (_offset + pageSize < _allocatedWidth && (info.Pos - Spacing < _offset || info.Pos + Spacing + info.Width > _offset + pageSize))
        {
            ScrollToTab(info, -1, ScrollAnimationDuration);
            return;
        }

        var canFocus = Bar?.TabsHaveVisibleFocus() == true;
        if (info == _selected)
        {
            canFocus = true;
        }
        else
        {
            _view.SelectedPage = page;
        }

        if (canFocus)
        {
            info.Container.Focus();
        }
        else
        {
            page.Content?.Focus();
        }
    }

    // get_tab_position.
    private double TabPosition(TabInfo info) => info == _reordered ? _reorderWindowX : info.Pos;

    // calculate_tab_offset: how far a tab has moved aside for the reordered one.
    private double TabOffset(TabInfo info, bool target)
    {
        if (_reordered is not { } reordered)
        {
            return 0;
        }

        var width = (target ? reordered.FinalWidth : reordered.Width) + Spacing;
        return Math.Round(width * (target ? info.EndReorderOffset : info.ReorderOffset));
    }

    private bool AnimationsEnabled =>
        TopLevel.GetTopLevel(this) is not null && AdwaitaEnvironment.State.AnimationsEnabled && !AdwaitaEnvironment.TransitionsSuppressedFor(this);

    // get_reorder_position: the dragged tab stays within the visible tabs.
    private double ReorderPosition()
    {
        var lower = Math.Max(Spacing, Math.Floor(_offset) + Spacing);
        var upper = Math.Min(_allocatedWidth - Spacing, Math.Ceiling(_offset + Bounds.Width) - Spacing);
        if (IsPinned)
        {
            upper += Spacing;
        }

        return Math.Max(lower, Math.Min(_reorderX, upper - (_reordered?.Width ?? 0)));
    }

    // start_reordering.
    private void StartReordering(TabInfo info)
    {
        _reordered = info;
        InvalidateArrange();
    }

    // start_drag_reodering: a press on a tab that is still settling takes it up again.
    private void StartDragReordering(TabInfo info)
    {
        _continueReorder = info == _reordered;
        if (_continueReorder)
        {
            _reorderAnimation.Pause();
            ResetReorderAnimations();
        }
        else
        {
            ForceEndReordering();
        }

        _dragging = true;
        info.Tab.IsDragging = true;
        if (!_continueReorder)
        {
            StartReordering(info);
        }
    }

    // end_drag_reodering: the page moves to where the tab was dropped, and the tab slides into its place.
    private void EndDragReordering()
    {
        if (!_dragging || _reordered is not { } reordered)
        {
            return;
        }

        _dragging = false;
        reordered.Tab.IsDragging = false;
        var dest = _tabs[Math.Clamp(_reorderIndex, 0, _tabs.Count - 1)];
        if (reordered.Page is { } page && _view is not null)
        {
            // The tabs have moved already; the view's reorder only moves the page.
            _reorderingPage = true;
            try
            {
                _view.ReorderPage(page, _reorderIndex + (IsPinned ? 0 : _view.PinnedPageCount));
            }
            finally
            {
                _reorderingPage = false;
            }
        }

        AnimateReordering(dest);
        _continueReorder = false;
    }

    // update_drag_reodering: the dragged tab follows the pointer, and the tabs whose middle it passes move aside.
    private void UpdateDragReordering()
    {
        if (!_dragging || _reordered is not { } reordered)
        {
            return;
        }

        var x = ReorderPosition();
        var width = reordered.FinalWidth;
        _reorderWindowX = x;
        InvalidateArrange();

        int oldIndex = -1, newIndex = -1;
        for (var i = 0; i < _tabs.Count && (oldIndex < 0 || newIndex < 0); i++)
        {
            var info = _tabs[i];
            var center = info.UnshiftedPos + Math.Floor(info.FinalWidth / 2);
            if (info == reordered)
            {
                oldIndex = i;
            }

            if (x + width + Spacing > center && center > x - Spacing && newIndex < 0)
            {
                newIndex = i;
            }
        }

        if (newIndex < 0)
        {
            newIndex = _tabs.Count - 1;
        }

        for (var i = 0; i < _tabs.Count; i++)
        {
            var offset = (i > oldIndex && i <= newIndex) ? -1 : (i < oldIndex && i >= newIndex) ? 1 : 0;
            AnimateReorderOffset(_tabs[i], offset);
        }

        _reorderIndex = newIndex;
        UpdateSeparators();
    }

    // animate_reordering: the reordered tab slides into its new place.
    private void AnimateReordering(TabInfo dest)
    {
        _reorderAnimation.Skip();
        _reorderDest = dest;
        _reorderAnimation.Reset(0);
        _reorderAnimation.Animate(1);
        CheckEndReordering();
    }

    // reorder_animation_value_cb.
    private void OnReorderAnimation(double value)
    {
        if (_reordered is not { } reordered || _reorderDest is not { } dest)
        {
            return;
        }

        var x1 = ReorderPosition();
        var x2 = dest.Pos - TabOffset(dest, false);
        if (dest.EndReorderOffset < 0)
        {
            x2 += dest.Width - reordered.Width;
        }

        _reorderWindowX = Math.Round(x1 + ((x2 - x1) * value));
        InvalidateArrange();
    }

    // animate_reorder_offset.
    private void AnimateReorderOffset(TabInfo info, double offset)
    {
        if (info.EndReorderOffset == offset)
        {
            return;
        }

        info.EndReorderOffset = offset;
        info.ReorderAnimation ??= new TimedAnimation(this, ReorderAnimationDuration, AdwEasing.Ease, v =>
        {
            info.ReorderOffset = v;
            InvalidateArrange();
        }, CheckEndReordering);
        info.ReorderAnimation.Reset(info.ReorderOffset);
        info.ReorderAnimation.Animate(offset);
    }

    // reset_reorder_animations: the tabs between the reordered tab's place and where it was going go back.
    private void ResetReorderAnimations()
    {
        if (!AnimationsEnabled || _reordered is not { } reordered)
        {
            return;
        }

        var original = _tabs.IndexOf(reordered);
        for (var i = Math.Min(original, _reorderIndex); i <= Math.Max(original, _reorderIndex); i++)
        {
            if (i != original && i >= 0 && i < _tabs.Count)
            {
                AnimateReorderOffset(_tabs[i], 0);
            }
        }

        UpdateSeparators();
    }

    // force_end_reordering.
    private void ForceEndReordering()
    {
        if (_dragging || _reordered is null)
        {
            return;
        }

        _reorderAnimation.Skip();
        foreach (var info in _tabs.ToList())
        {
            info.ReorderAnimation?.Skip();
        }

        CheckEndReordering();
    }

    // check_end_reordering: once every tab has settled, the reordered one takes its place in the list.
    private void CheckEndReordering()
    {
        if (_dragging || _reordered is not { } reordered || _continueReorder || _reorderAnimation.IsRunning
            || _tabs.Any(i => i.ReorderAnimation?.IsRunning == true))
        {
            return;
        }

        foreach (var info in _tabs)
        {
            info.EndReorderOffset = info.ReorderOffset = 0;
        }

        _tabs.Remove(reordered);
        _tabs.Insert(Math.Clamp(_reorderIndex, 0, _tabs.Count), reordered);
        _reordered = null;
        _reorderDest = null;
        InvalidateArrange();
        UpdateSeparators();
        UpdateHover();
    }

    private void OnResizeAnimation(double value)
    {
        // resize_animation_value_cb: the end padding goes back to what the tabs leave.
        double target = 0;
        if (!ExpandTabs)
        {
            var predicted = BaseTabWidth(true, false);
            target = _allocatedWidth - Spacing;
            foreach (var info in _tabs)
            {
                target -= TabWidth(info, predicted) + Spacing;
            }

            target = Math.Max(target, 0);
        }

        _endPadding = Math.Floor(_initialEndPadding + ((target - _initialEndPadding) * value));
        InvalidateMeasure();
    }

    // set_tab_resize_mode.
    private void SetResizeMode(ResizeMode mode)
    {
        if (_resizeMode == mode)
        {
            return;
        }

        if (mode == ResizeMode.FixedTabWidth)
        {
            _lastWidth = _allocatedWidth;
            foreach (var info in _tabs)
            {
                info.LastWidth = info.AppearAnimation?.IsRunning == true ? info.FinalWidth : info.Width;
            }
        }
        else
        {
            _lastWidth = 0;
        }

        if (mode == ResizeMode.Normal)
        {
            _initialEndPadding = _endPadding;
            _resizeAnimation.Reset(0);
            _resizeAnimation.Animate(1);
        }

        var notify = (_resizeMode == ResizeMode.Normal) != (mode == ResizeMode.Normal);
        _resizeMode = mode;
        UpdateSingleTabStyle();
        if (notify)
        {
            Bar?.UpdateIsOverflowing();
        }
    }

    private void UpdateHover()
    {
        if (_reordered is null && !_hovering)
        {
            SetResizeMode(ResizeMode.Normal);
        }
    }

    // update_single_tab_style.
    private void UpdateSingleTabStyle()
    {
        if (IsPinned)
        {
            return;
        }

        var single = _view is { PageCount: <= 1, PinnedPageCount: 0 } && ExpandTabs && _resizeMode == ResizeMode.Normal;
        Css.SetClasses(this, single ? "single-tab" : null);
    }

    private void OnViewPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == TabView.PageCountProperty || e.Property == TabView.PinnedPageCountProperty)
        {
            UpdateSingleTabStyle();
        }
    }

    // update_visible: which tabs are wholly in view, and whether tabs needing attention are scrolled past either end.
    private void UpdateVisible(double pageSize)
    {
        bool left = false, right = false;
        foreach (var info in _tabs)
        {
            if (info.Page is not { } page)
            {
                continue;
            }

            var pos = TabPosition(info);
            info.Tab.IsFullyVisible = pos - Spacing >= _offset - 0.001 && pos + info.Width + Spacing <= _offset + pageSize + 0.001;
            if (!page.NeedsAttention)
            {
                continue;
            }

            if (pos + (info.Width / 2) <= _offset)
            {
                left = true;
            }

            if (pos + (info.Width / 2) >= _offset + pageSize)
            {
                right = true;
            }
        }

        _attentionLeft.IsRevealed = left;
        _attentionRight.IsRevealed = right;
    }

    // update_separators: a separator hides beside a hovered or selected tab, and before the first tab. While a tab is
    // reordered, the tabs either side of the gap it left are neighbors, and it neighbors the tabs either side of where
    // it is going.
    internal void UpdateSeparators()
    {
        var lastPinned = IsPinned ? null : PinnedBox?.LastVisiblyPinned();
        for (var i = 0; i < _tabs.Count; i++)
        {
            var info = _tabs[i];
            var previous = i > 0 ? _tabs[i - 1] : lastPinned;
            var previousPrevious = i > 1 ? _tabs[i - 2] : i == 1 ? lastPinned : null;
            TabInfo? visuallyPrevious = null;
            if (previous is not null && previousPrevious is not null && previous == _reordered
                && (previousPrevious.EndReorderOffset > 0 || info.EndReorderOffset < 0))
            {
                visuallyPrevious = previousPrevious;
            }

            if (previous is not null && _reordered is not null && info.EndReorderOffset - previous.EndReorderOffset > 0)
            {
                visuallyPrevious = _reordered;
            }

            visuallyPrevious ??= previous;
            var hidden = visuallyPrevious is null || Highlighted(info.Tab) || Highlighted(visuallyPrevious.Tab);
            Css.SetClasses(info.Separator, hidden ? "vertical hidden" : "vertical");
        }

        if (IsPinned)
        {
            UnpinnedBox?.UpdateSeparators();
        }

        static bool Highlighted(Tab tab) => tab.IsPointerOver || tab.IsSelected;
    }

    // The pinned tab the first unpinned one follows on screen, with a pinned tab being reordered.
    private TabInfo? LastVisiblyPinned()
    {
        if (_tabs.Count == 0)
        {
            return null;
        }

        var last = _tabs[^1];
        if (last.EndReorderOffset < 0)
        {
            return _reordered;
        }

        return _tabs.Count > 1 && last == _reordered && _tabs[^2].EndReorderOffset > 0 ? _tabs[^2] : last;
    }

    private void UpdateTabStops()
    {
        foreach (var info in _tabs)
        {
            info.Container.IsTabStop = info == _selected;
        }
    }

    private double ScrollAnimationValue()
    {
        if (!_scrollAnimation.IsRunning && !_scrollDone)
        {
            return _offset;
        }

        var to = _scrollOffset;
        if (_scrollTab is { } tab)
        {
            var pageSize = Bounds.Width;
            to += tab.FinalPos;
            to = Math.Clamp(to, 0, Math.Max(0, _allocatedWidth - pageSize));
        }

        return _scrollFrom + ((to - _scrollFrom) * _scrollAnimation.Value);
    }

    private void AnimateScroll(TabInfo? info, double offset, double duration)
    {
        _scrollDone = false;
        _scrollFrom = _offset;
        _scrollTab = info;
        _scrollOffset = offset;
        _scrollAnimation.Duration = duration;
        _scrollAnimation.Reset(0);
        _scrollAnimation.Animate(1);
    }

    // scroll_to_tab_full.
    private void ScrollToTab(TabInfo info, double pos, double duration)
    {
        var width = info.AppearAnimation?.IsRunning == true ? info.FinalWidth : info.Width;
        var pageSize = Bounds.Width;
        if (width <= 0 || pageSize <= 0)
        {
            return;
        }

        var padding = Math.Min(width, pageSize - width) / 2;
        if (pos < 0)
        {
            pos = info.FinalPos;
        }

        if (pos - Spacing < _offset)
        {
            AnimateScroll(info, -padding, duration);
        }
        else if (pos + width + Spacing > _offset + pageSize)
        {
            AnimateScroll(info, width + padding - pageSize, duration);
        }
    }

    private void OnPageAttached(object? sender, TabPageEventArgs e)
    {
        if (e.Page is { } page)
        {
            Attach(page, e.Position, animate: true);
        }
    }

    private void OnPageDetached(object? sender, TabPageEventArgs e)
    {
        if (e.Page is { } page)
        {
            Detach(page);
        }
    }

    // page_reordered_cb: the tab slides to its new place, and the tabs it passes slide the other way.
    private void OnPageReordered(object? sender, TabPageEventArgs e)
    {
        if (_reorderingPage || e.Page is not { } page || page.IsPinned != IsPinned || _view is null)
        {
            return;
        }

        _continueReorder = _reordered?.Page == page;
        if (_continueReorder)
        {
            ResetReorderAnimations();
        }
        else
        {
            ForceEndReordering();
        }

        var info = _tabs.FirstOrDefault(i => i.Page == page);
        if (info is null)
        {
            _continueReorder = false;
            return;
        }

        var original = _tabs.IndexOf(info);
        if (!_continueReorder)
        {
            StartReordering(info);
        }

        _reorderX = _continueReorder ? _reorderWindowX : info.Pos;
        _reorderIndex = Math.Clamp(e.Position - (IsPinned ? 0 : _view.PinnedPageCount), 0, _tabs.Count - 1);
        var dest = _tabs[_reorderIndex];
        if (info == _selected)
        {
            ScrollToTab(info, dest.FinalPos, ReorderAnimationDuration);
        }

        AnimateReordering(dest);
        if (AnimationsEnabled && IsEffectivelyVisible && _reordered == info)
        {
            for (var i = original + 1; i <= _reorderIndex; i++)
            {
                AnimateReorderOffset(_tabs[i], -1);
            }

            for (var i = original - 1; i >= _reorderIndex; i--)
            {
                AnimateReorderOffset(_tabs[i], 1);
            }
        }

        _continueReorder = false;
        CheckEndReordering();
        UpdateSeparators();
    }

    // page_attached_cb.
    private void Attach(TabPage page, int position, bool animate)
    {
        if (page.IsPinned != IsPinned || _view is null || _tabs.Any(i => i.Page == page))
        {
            return;
        }

        if (!IsPinned)
        {
            position -= _view.PinnedPageCount;
        }

        SetResizeMode(ResizeMode.Normal);
        var tab = new Tab(_view, IsPinned) { IsInverted = IsInverted };
        tab.Page = page;
        tab.CloseClicked += (_, p) => _view?.ClosePage(p);
        tab.StateChanged += (_, _) => UpdateSeparators();
        tab.PropertyChanged += (_, e) =>
        {
            if (e.Property == IsPointerOverProperty)
            {
                UpdateSeparators();
            }
        };
        var container = new TabBoxChild { Child = tab };
        var separator = new AdwBox();
        Css.SetNode(separator, "separator");
        Css.SetClasses(separator, "vertical");
        separator.IsHitTestVisible = false;
        var info = new TabInfo(page, tab, container, separator);
        info.AppearAnimation = new TimedAnimation(this, OpenAnimationDuration, AdwEasing.Ease, v =>
        {
            info.AppearProgress = v;
            InvalidateMeasure();
        });

        // In front of the attention indicators, after the tabs made before it.
        var at = VisualChildren.IndexOf(_attentionLeft);
        VisualChildren.Insert(at, container);
        VisualChildren.Insert(at, separator);
        LogicalChildren.Add(container);

        // find_nth_alive_tab, whose unsigned position puts a negative one at the end.
        var index = position < 0 ? _tabs.Count : FindNthAlive(position);
        _tabs.Insert(index, info);
        if (animate)
        {
            info.AppearAnimation.Animate(1);
        }
        else
        {
            info.AppearAnimation.Jump(1);
        }

        if (page == _view.SelectedPage)
        {
            SelectPage(page);
        }

        UpdateTabStops();
        UpdateSeparators();
        InvalidateMeasure();
    }

    private int FindNthAlive(int position)
    {
        for (var i = 0; i < _tabs.Count; i++)
        {
            if (_tabs[i].Page is null)
            {
                continue;
            }

            if (position-- == 0)
            {
                return i;
            }
        }

        return _tabs.Count;
    }

    // page_detached_cb: the tab closes, its width going to nothing; with the pointer over the row, the others keep theirs.
    private void Detach(TabPage page)
    {
        var info = _tabs.FirstOrDefault(i => i.Page == page);
        if (info is null)
        {
            return;
        }

        if (_hovering && !IsPinned)
        {
            var isLast = _tabs.Skip(_tabs.IndexOf(info) + 1).All(i => i.Page is null);
            SetResizeMode(isLast ? (IsInverted ? ResizeMode.Normal : ResizeMode.FixedEndPadding) : ResizeMode.FixedTabWidth);
        }

        if (info.Container.IsKeyboardFocusWithin)
        {
            TryFocusSelectedTab();
        }

        if (info == _selected)
        {
            SelectPage(null);
        }

        if (info == _reordered)
        {
            _dragging = false;
            info.Tab.IsDragging = false;
            ForceEndReordering();
        }

        info.Tab.Page = null;
        info.Page = null;
        var from = info.AppearProgress;
        info.AppearAnimation = new TimedAnimation(this, CloseAnimationDuration, AdwEasing.Ease, v =>
        {
            info.AppearProgress = v;
            InvalidateMeasure();
        }, () => Remove(info));
        info.AppearAnimation.Reset(from);
        info.AppearAnimation.Animate(0);
    }

    // close_animation_done_cb.
    private void Remove(TabInfo info)
    {
        if (!_tabs.Remove(info))
        {
            return;
        }

        if (_pressed == info)
        {
            _pressed = null;
        }

        if (_middleClicked == info)
        {
            _middleClicked = null;
        }

        VisualChildren.Remove(info.Container);
        VisualChildren.Remove(info.Separator);
        LogicalChildren.Remove(info.Container);
        UpdateSeparators();
        InvalidateMeasure();
    }
}
