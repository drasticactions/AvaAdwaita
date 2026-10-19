using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using AvaAdwaita.Animation;
using AvaAdwaita.Nodes;

namespace AvaAdwaita.Controls.Primitives;

/// <summary>
/// A tab overview's grid of thumbnails (AdwTabGrid), the pinned pages' or the others': from 2 to 8 columns as the width
/// allows, the thumbnails between 100 and 500 px wide and centered. Clicking a thumbnail opens its page, middle-clicking
/// closes it, dragging moves it, and the arrow keys move between thumbnails.
/// </summary>
public class TabGrid : Control
{
    private const double Spacing = 5;
    private const double OpenAnimationDuration = 200;
    private const double CloseAnimationDuration = 200;
    private const int MinColumns = 2;
    private const int MaxColumns = 8;
    private const double MinThumbnailWidth = 100;
    private const double MaxThumbnailWidth = 500;
    private const double SingleTabMaxPercentage = 0.5;
    private const double SmallGridWidth = 360;
    private const double SmallGridPercentage = 1;
    private const double SmallNatThumbnailWidth = 200;
    private const double LargeGridWidth = 2560;
    private const double LargeGridPercentage = 0.85;
    private const double LargeNatThumbnailWidth = 360;
    private const double DragThreshold = 8;

    private readonly List<GridTab> _tabs = [];
    private TabView? _view;
    private GridTab? _selected;
    private double _maxColumns;
    private double _columns;
    private double _tabWidth;
    private double _tabHeight;
    private double _allocatedWidth;
    private string? _searchTerms;
    private GridTab? _pressed;
    private Point _pressPoint;
    private GridTab? _middleClicked;
    private GridTab? _reordered;
    private Point _reorderPos;
    private Point _dragOffset;
    private bool _empty = true;
    private bool _inverted;

    /// <summary>Creates a grid, of the pinned pages or the others.</summary>
    public TabGrid(bool pinned)
    {
        IsPinned = pinned;
        Css.SetNode(this, "tabgrid");
        Focusable = false;
    }

    /// <summary>Raised when the grid becomes empty or no longer is.</summary>
    public event EventHandler? EmptyChanged;

    /// <summary>Whether the grid holds the pinned pages.</summary>
    public bool IsPinned { get; }

    /// <summary>The other grid, whose thumbnail count the column count also follows.</summary>
    internal TabGrid? Other { get; set; }

    /// <summary>The overview the grid is in.</summary>
    internal TabOverview? Overview { get; set; }

    /// <summary>Whether no thumbnail shows.</summary>
    public bool IsEmpty
    {
        get => _empty;
        private set
        {
            if (_empty != value)
            {
                _empty = value;
                EmptyChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>Whether the thumbnails' buttons swap sides.</summary>
    public bool IsInverted
    {
        get => _inverted;
        set
        {
            _inverted = value;
            foreach (var tab in _tabs)
            {
                tab.Thumbnail.IsInverted = value;
            }
        }
    }

    /// <summary>The thumbnails, in order.</summary>
    internal IEnumerable<TabThumbnail> Thumbnails => _tabs.Where(t => t.Page is not null).Select(t => t.Thumbnail);

    /// <summary>The selected page's thumbnail, which the overview's transition goes to.</summary>
    internal TabThumbnail? TransitionThumbnail => _selected?.Thumbnail;

    /// <summary>Shows <paramref name="view"/>'s pages (adw_tab_grid_set_view).</summary>
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
            foreach (var tab in _tabs.ToList())
            {
                Remove(tab);
            }
        }

        _view = view;
        if (view is not null)
        {
            for (var i = view.PageCount - 1; i >= 0; i--)
            {
                Attach(view.GetNthPage(i), 0, animate: false);
            }

            view.PageAttached += OnPageAttached;
            view.PageDetached += OnPageDetached;
            view.PageReordered += OnPageReordered;
        }

        UpdateEmpty();
        InvalidateMeasure();
    }

    /// <summary>Selects <paramref name="page"/>'s thumbnail.</summary>
    public void SelectPage(TabPage? page)
    {
        _selected = page is null ? null : _tabs.FirstOrDefault(t => t.Page == page);
        foreach (var tab in _tabs)
        {
            tab.Container.IsTabStop = tab == _selected;
        }
    }

    /// <summary>Shows only the thumbnails whose title, tooltip or keyword holds <paramref name="terms"/>.</summary>
    public void SetSearchTerms(string? terms)
    {
        _searchTerms = string.IsNullOrEmpty(terms) ? null : terms;
        foreach (var tab in _tabs.Where(t => t.Page is not null))
        {
            tab.Container.IsVisible = Matches(tab.Page!);
        }

        UpdateEmpty();
        InvalidateMeasure();
    }

    /// <summary>Attaches <paramref name="page"/>, moved here from the other grid when pinned or unpinned.</summary>
    internal void AttachPage(TabPage page, int position) => Attach(page, position, animate: true);

    /// <summary>Detaches <paramref name="page"/>, moved to the other grid.</summary>
    internal void DetachPage(TabPage page) => Detach(page);

    /// <summary>Focuses the selected thumbnail, if it is in this grid, showing the focus as the window does.</summary>
    internal bool TryFocusSelectedTab()
    {
        if (_selected is null)
        {
            return false;
        }

        _selected.Container.Focus();
        _selected.Container.BringIntoView();
        return true;
    }

    /// <summary>Focuses the first thumbnail showing, if any (adw_tab_grid_focus_first_row).</summary>
    internal bool FocusFirst()
    {
        if (_tabs.FirstOrDefault(t => t.Page is not null && t.Container.IsVisible) is not { } tab)
        {
            return false;
        }

        tab.Container.Focus(NavigationMethod.Tab);
        return true;
    }

    /// <summary>Focuses <paramref name="page"/>'s thumbnail.</summary>
    internal void FocusPage(TabPage page)
    {
        if (_tabs.FirstOrDefault(t => t.Page == page) is { } tab)
        {
            tab.Container.Focus(NavigationMethod.Tab);
            tab.Container.BringIntoView();
        }
    }

    /// <summary>Takes the pages' pictures, as AdwTabPaintable does before the overview shows.</summary>
    internal void UpdatePictures(Func<TabPage, double, IImage?> picture)
    {
        foreach (var tab in _tabs.Where(t => t.Page is not null))
        {
            tab.Thumbnail.Picture.Source = picture(tab.Page!, tab.Thumbnail.Picture.Bounds.Width);
        }
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        _maxColumns = MaxColumnCount();
        var width = double.IsInfinity(availableSize.Width) ? -1 : availableSize.Width;
        var tabWidth = width >= 0 ? TabWidth(width) : double.PositiveInfinity;
        foreach (var tab in _tabs)
        {
            tab.Container.Measure(new Size(tabWidth, double.PositiveInfinity));
        }

        return new Size(MeasureWidth(), MeasureHeight(width));
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        // adw_tab_grid_size_allocate and calculate_tab_layout.
        _allocatedWidth = Math.Max(MeasureWidth(), finalSize.Width);
        _maxColumns = MaxColumnCount();
        _columns = ColumnCount(_allocatedWidth, _maxColumns);
        _tabWidth = TabWidth(_allocatedWidth);
        _tabHeight = TabHeight(_tabWidth);
        double index = 0;
        foreach (var tab in _tabs)
        {
            if (!tab.Container.IsVisible)
            {
                continue;
            }

            tab.Position = PositionFor(index, false);
            index += tab.AppearProgress;
            var pos = tab == _reordered ? _reorderPos : tab.Position;
            tab.Container.RenderTransformOrigin = RelativePoint.Center;
            tab.Container.AppearProgress = tab.AppearProgress;
            tab.Container.Arrange(new Rect(pos.X, pos.Y, Math.Max(0, _tabWidth), Math.Max(0, _tabHeight)));
        }

        return finalSize;
    }

    /// <inheritdoc/>
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var point = e.GetCurrentPoint(this);
        if (TabAt(point.Position) is not { Page: { } page } tab || _view is null || e.Source is Button)
        {
            return;
        }

        e.Handled = true;
        if (point.Properties.IsRightButtonPressed)
        {
            _view.ShowMenu(page, tab.Container, point.Position);
        }
        else if (point.Properties.IsMiddleButtonPressed)
        {
            _middleClicked = tab;
        }
        else if (point.Properties.IsLeftButtonPressed)
        {
            _pressed = tab;
            _pressPoint = point.Position;
        }
    }

    /// <inheritdoc/>
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var point = e.GetCurrentPoint(this);
        if (_pressed is not { Page: { } page } pressed || !point.Properties.IsLeftButtonPressed || _view is null)
        {
            return;
        }

        if (_reordered is null)
        {
            if (Distance(point.Position, _pressPoint) < DragThreshold)
            {
                return;
            }

            _reordered = pressed;
            _dragOffset = _pressPoint - pressed.Position;
            e.Pointer.Capture(this);
        }

        // update_drag_reodering: the dragged thumbnail follows the pointer, and its page takes the place it is over.
        _reorderPos = point.Position - _dragOffset;
        var center = _reorderPos + new Point(_tabWidth / 2, _tabHeight / 2);
        var index = 0;
        var best = double.MaxValue;
        var visible = _tabs.Where(t => t.Page is not null && t.Container.IsVisible).ToList();
        for (var i = 0; i < visible.Count; i++)
        {
            var p = PositionFor(i, false) + new Point(_tabWidth / 2, _tabHeight / 2);
            var d = Distance(p, center);
            if (d < best)
            {
                (best, index) = (d, i);
            }
        }

        var position = _view.GetPagePosition(visible[index].Page!);
        if (visible[index] != pressed)
        {
            _view.ReorderPage(page, position);
        }

        InvalidateArrange();
    }

    /// <inheritdoc/>
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        var point = e.GetCurrentPoint(this);
        var tab = TabAt(point.Position);
        if (e.InitialPressMouseButton == MouseButton.Middle)
        {
            if (tab is { Page: { } closing } && tab == _middleClicked)
            {
                _view?.ClosePage(closing);
            }

            _middleClicked = null;
            return;
        }

        if (_reordered is not null)
        {
            _reordered = null;
            _pressed = null;
            e.Pointer.Capture(null);
            InvalidateArrange();
            return;
        }

        // released_cb: the page shows and the overview closes.
        if (e.InitialPressMouseButton == MouseButton.Left && tab is { Page: { } page } && tab == _pressed && _view is not null)
        {
            e.Handled = true;
            _view.SelectedPage = page;
            Overview?.Close();
        }

        _pressed = null;
    }

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Handled || _view is null)
        {
            return;
        }

        var focused = _tabs.FirstOrDefault(t => t.Container.IsFocused);
        if (focused?.Page is not { } page)
        {
            return;
        }

        var visible = _tabs.Where(t => t.Page is not null && t.Container.IsVisible).ToList();
        var index = visible.IndexOf(focused);
        var columns = (int)Math.Ceiling(_columns);
        var rtl = FlowDirection == FlowDirection.RightToLeft;
        if (e.Key is Key.Enter or Key.Space)
        {
            e.Handled = true;
            _view.SelectedPage = page;
            Overview?.Close();
            return;
        }

        if (e.KeyModifiers == KeyModifiers.Shift)
        {
            // reorder_tab_cb.
            _ = e.Key switch
            {
                Key.Left => rtl ? _view.ReorderForward(page) : _view.ReorderBackward(page),
                Key.Right => rtl ? _view.ReorderBackward(page) : _view.ReorderForward(page),
                Key.Up => _view.ReorderPage(page, _view.GetPagePosition(page) - columns),
                Key.Down => _view.ReorderPage(page, _view.GetPagePosition(page) + columns),
                Key.Home => _view.ReorderFirst(page),
                Key.End => _view.ReorderLast(page),
                _ => false,
            };
            e.Handled = e.Key is Key.Left or Key.Right or Key.Up or Key.Down or Key.Home or Key.End;
            return;
        }

        var target = e.Key switch
        {
            Key.Left => index + (rtl ? 1 : -1),
            Key.Right => index + (rtl ? -1 : 1),
            Key.Up => index - columns,
            Key.Down => index + columns,
            Key.Home => 0,
            Key.End => visible.Count - 1,
            _ => -2,
        };
        if (target == -2)
        {
            if (e.Key == Key.Apps || (e.Key == Key.F10 && e.KeyModifiers == KeyModifiers.Shift))
            {
                e.Handled = true;
                _view.ShowMenu(page, focused.Container);
            }

            return;
        }

        e.Handled = true;
        if (target >= 0 && target < visible.Count)
        {
            visible[target].Container.Focus(NavigationMethod.Directional);
            visible[target].Container.BringIntoView();
        }
        else if (target >= visible.Count && Other is { } other && !IsPinned)
        {
            return;
        }
        else if (target >= visible.Count && IsPinned)
        {
            Other?.FocusFirst();
        }
    }

    private static double Distance(Point a, Point b) => Math.Sqrt(((a.X - b.X) * (a.X - b.X)) + ((a.Y - b.Y) * (a.Y - b.Y)));

    private static double EaseOutCubic(double t) => Easing.Ease(AdwEasing.EaseOutCubic, t);

    private bool Matches(TabPage page)
    {
        if (_searchTerms is not { } terms)
        {
            return true;
        }

        bool Has(string? s) => s?.Contains(terms, StringComparison.CurrentCultureIgnoreCase) == true;
        return Has(page.Title) || Has(page.Tooltip) || Has(page.Keyword);
    }

    private void UpdateEmpty() => IsEmpty = !_tabs.Any(t => t.Page is not null && t.Container.IsVisible);

    private GridTab? TabAt(Point p)
    {
        if (_reordered is { } r && new Rect(_reorderPos, new Size(_tabWidth, _tabHeight)).Contains(p))
        {
            return r;
        }

        return _tabs.FirstOrDefault(t => t.Container.IsVisible && t != _reordered && new Rect(t.Position, new Size(_tabWidth, _tabHeight)).Contains(p));
    }

    // get_max_n_columns: as many as there are thumbnails, in either grid.
    private double MaxColumnCount()
    {
        double max = 0, otherMax = 0;
        int n = 0, otherN = 0;
        foreach (var t in _tabs)
        {
            max += t.AppearProgress;
            n += t.Page is not null ? 1 : 0;
        }

        foreach (var t in Other?._tabs ?? [])
        {
            otherMax += t.AppearProgress;
            otherN += t.Page is not null ? 1 : 0;
        }

        max = Math.Max(1, max);
        otherMax = Math.Max(1, otherMax);
        if ((n == otherN + 1 && max < n && otherMax > otherN) || (otherN == n + 1 && max > n && otherMax < otherN))
        {
            return Math.Max(n, otherN);
        }

        return Math.Max(max, otherMax);
    }

    // get_n_columns.
    private static double ColumnCount(double forWidth, double maxColumns)
    {
        if (forWidth < 0)
        {
            return Math.Max(maxColumns, 1);
        }

        maxColumns = Math.Clamp(maxColumns, 1, MaxColumns);
        var t = Math.Clamp((forWidth - SmallGridWidth) / (LargeGridWidth - SmallGridWidth), 0, 1);
        var natWidth = SmallNatThumbnailWidth + ((LargeNatThumbnailWidth - SmallNatThumbnailWidth) * EaseOutCubic(t));
        return Math.Clamp(Math.Ceiling(forWidth / natWidth), Math.Min(MinColumns, maxColumns), maxColumns);
    }

    // get_tab_width.
    private double TabWidth(double forWidth)
    {
        var n = ColumnCount(forWidth, _maxColumns);
        var total = forWidth;
        var t = Math.Clamp((total - SmallGridWidth) / (LargeGridWidth - SmallGridWidth), 0, 1);
        total *= SmallGridPercentage + ((LargeGridPercentage - SmallGridPercentage) * EaseOutCubic(t));
        if (n <= _maxColumns + 1e-9)
        {
            var max = ColumnCount(forWidth, MaxColumns);
            total *= SingleTabMaxPercentage + ((1 - SingleTabMaxPercentage) * n / max);
        }

        return Math.Clamp(Math.Ceiling((total - (Spacing * (n + 1))) / n), MinThumbnailWidth, MaxThumbnailWidth);
    }

    // get_tab_height: the tallest thumbnail at that width.
    private double TabHeight(double tabWidth)
    {
        var height = 0.0;
        foreach (var t in _tabs)
        {
            t.Thumbnail.Measure(new Size(tabWidth < 0 ? double.PositiveInfinity : tabWidth, double.PositiveInfinity));
            height = Math.Max(height, t.Thumbnail.DesiredSize.Height);
        }

        return height;
    }

    // measure_tab_grid, horizontally.
    private double MeasureWidth()
    {
        double min = 0;
        foreach (var t in _tabs.Where(t => t.Container.IsVisible))
        {
            min = Math.Max(min, Math.Floor((0 + Spacing) * t.AppearProgress) - Spacing);
        }

        return min + Spacing;
    }

    // measure_tab_grid, vertically: the rows' heights, a row appearing as its first thumbnail does.
    private double MeasureHeight(double forWidth)
    {
        var childWidth = forWidth >= 0 ? TabWidth(forWidth) : -1;
        var childHeight = TabHeight(childWidth);
        var index = _tabs.Where(t => t.Container.IsVisible).Sum(t => t.AppearProgress);
        var columns = ColumnCount(forWidth, _maxColumns);
        var rows = Math.Ceiling(index / columns);
        var col = index % columns;
        if (col > 0 && col < 1)
        {
            rows = rows + col - 1;
        }

        return rows < 1 ? ((childHeight + (Spacing * 2)) * rows) : ((childHeight + Spacing) * rows) + Spacing;
    }

    // get_position_for_index: rows of columns centered in the width; a fraction of a column is a thumbnail on its way.
    private Point PositionFor(double index, bool rtl)
    {
        var columns = (int)Math.Ceiling(_columns);
        var col = index % columns;
        var row = (index - col) / columns;
        var offset = (_allocatedWidth - ((_columns * (_tabWidth + Spacing)) - Spacing)) / 2;
        double x, y;
        if (col > columns - 1)
        {
            var (start, end) = rtl ? (_allocatedWidth - offset - _tabWidth, offset) : (offset, _allocatedWidth - offset - _tabWidth);
            var t = columns - col;
            x = start + ((end - start) * t);
            y = Spacing + ((row + 1 - t) * (_tabHeight + Spacing));
        }
        else
        {
            x = rtl ? _allocatedWidth - offset - _tabWidth - (col * (_tabWidth + Spacing)) : offset + (col * (_tabWidth + Spacing));
            y = Spacing + (row * (_tabHeight + Spacing));
        }

        return new Point(Math.Truncate(x), Math.Truncate(y));
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

    private void OnPageReordered(object? sender, TabPageEventArgs e)
    {
        if (e.Page is not { } page || page.IsPinned != IsPinned || _view is null || _tabs.FirstOrDefault(t => t.Page == page) is not { } tab)
        {
            return;
        }

        _tabs.Remove(tab);
        _tabs.Insert(Math.Clamp(e.Position - (IsPinned ? 0 : _view.PinnedPageCount), 0, _tabs.Count), tab);
        InvalidateArrange();
    }

    // page_attached_cb.
    private void Attach(TabPage page, int position, bool animate)
    {
        if (page.IsPinned != IsPinned || _view is null || _tabs.Any(t => t.Page == page))
        {
            return;
        }

        if (!IsPinned)
        {
            position -= _view.PinnedPageCount;
        }

        var thumbnail = new TabThumbnail(_view, IsPinned) { IsInverted = IsInverted, Page = page };
        var container = new TabGridChild { Child = thumbnail, IsVisible = Matches(page) };
        var tab = new GridTab(page, thumbnail, container);
        tab.Animation = new TimedAnimation(this, OpenAnimationDuration, AdwEasing.Ease, v =>
        {
            tab.AppearProgress = v;
            InvalidateMeasure();
        });
        VisualChildren.Add(container);
        LogicalChildren.Add(container);

        // find_nth_alive_tab, whose unsigned position puts a negative one at the end.
        var index = _tabs.Count;
        if (position >= 0)
        {
            var alive = -1;
            for (var i = 0; i < _tabs.Count; i++)
            {
                if (_tabs[i].Page is not null && ++alive == position)
                {
                    index = i;
                    break;
                }
            }
        }

        _tabs.Insert(index, tab);
        if (animate)
        {
            tab.Animation.Animate(1);
        }
        else
        {
            tab.Animation.Jump(1);
        }

        if (page == _view.SelectedPage)
        {
            SelectPage(page);
        }

        UpdateEmpty();
        InvalidateMeasure();
    }

    // page_detached_cb: the thumbnail shrinks away.
    private void Detach(TabPage page)
    {
        if (_tabs.FirstOrDefault(t => t.Page == page) is not { } tab)
        {
            return;
        }

        if (tab == _selected)
        {
            _selected = null;
        }

        if (tab == _reordered)
        {
            _reordered = null;
        }

        tab.Page = null;
        tab.Thumbnail.Page = null;
        var from = tab.AppearProgress;
        tab.Animation = new TimedAnimation(this, CloseAnimationDuration, AdwEasing.Ease, v =>
        {
            tab.AppearProgress = v;
            InvalidateMeasure();
        }, () => Remove(tab));
        tab.Animation.Reset(from);
        tab.Animation.Animate(0);
        UpdateEmpty();
    }

    private void Remove(GridTab tab)
    {
        if (!_tabs.Remove(tab))
        {
            return;
        }

        if (_pressed == tab)
        {
            _pressed = null;
        }

        VisualChildren.Remove(tab.Container);
        LogicalChildren.Remove(tab.Container);
        UpdateEmpty();
        InvalidateMeasure();
    }

    private sealed class GridTab(TabPage page, TabThumbnail thumbnail, TabGridChild container)
    {
        public TabPage? Page { get; set; } = page;

        public TabThumbnail Thumbnail { get; } = thumbnail;

        public TabGridChild Container { get; } = container;

        public Point Position { get; set; }

        public double AppearProgress { get; set; }

        public TimedAnimation? Animation { get; set; }
    }
}
