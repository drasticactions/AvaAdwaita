using System;
using System.Linq;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using AvaAdwaita.Controls.Primitives;
using AvaAdwaita.Nodes;
using AvaAdwaita.Presenters;

namespace AvaAdwaita.Controls;

/// <summary>
/// A bar of tabs for a <see cref="TabView"/> (AdwTabBar): the pinned tabs, then the others sharing the rest of the width,
/// with optional widgets at either end. Clicking a tab selects its page, middle-clicking closes it, dragging moves it, and
/// a context click opens the view's menu. With <see cref="Autohide"/> it shows only for two or more pages, or a pinned one.
/// </summary>
public class TabBar : Control
{
    /// <summary>Defines <see cref="View"/>.</summary>
    public static readonly StyledProperty<TabView?> ViewProperty =
        AvaloniaProperty.Register<TabBar, TabView?>(nameof(View));

    /// <summary>Defines <see cref="Autohide"/>.</summary>
    public static readonly StyledProperty<bool> AutohideProperty =
        AvaloniaProperty.Register<TabBar, bool>(nameof(Autohide), true);

    /// <summary>Defines <see cref="ExpandTabs"/>.</summary>
    public static readonly StyledProperty<bool> ExpandTabsProperty =
        AvaloniaProperty.Register<TabBar, bool>(nameof(ExpandTabs), true);

    /// <summary>Defines <see cref="IsInverted"/>.</summary>
    public static readonly StyledProperty<bool> IsInvertedProperty =
        AvaloniaProperty.Register<TabBar, bool>(nameof(IsInverted));

    /// <summary>Defines <see cref="StartActionWidget"/>.</summary>
    public static readonly StyledProperty<Control?> StartActionWidgetProperty =
        AvaloniaProperty.Register<TabBar, Control?>(nameof(StartActionWidget));

    /// <summary>Defines <see cref="EndActionWidget"/>.</summary>
    public static readonly StyledProperty<Control?> EndActionWidgetProperty =
        AvaloniaProperty.Register<TabBar, Control?>(nameof(EndActionWidget));

    /// <summary>Defines <see cref="TabsRevealed"/>.</summary>
    public static readonly DirectProperty<TabBar, bool> TabsRevealedProperty =
        AvaloniaProperty.RegisterDirect<TabBar, bool>(nameof(TabsRevealed), b => b.TabsRevealed);

    /// <summary>Defines <see cref="IsOverflowing"/>.</summary>
    public static readonly DirectProperty<TabBar, bool> IsOverflowingProperty =
        AvaloniaProperty.RegisterDirect<TabBar, bool>(nameof(IsOverflowing), b => b.IsOverflowing);

    private readonly Revealer _revealer = new() { Transition = RevealerTransition.SlideDown, TransitionDuration = TimeSpan.FromMilliseconds(200) };
    private readonly AdwBox _startBin = new() { IsVisible = false };
    private readonly AdwBox _endBin = new() { IsVisible = false };
    private readonly TabScroller _pinnedScroller = new() { PropagatesNaturalWidth = true };
    private readonly TabScroller _scroller = new() { MinContentWidth = 100 };
    private readonly TabBox _pinnedBox = new(pinned: true);
    private readonly TabBox _box = new(pinned: false);
    private TabView? _view;
    private bool _tabsRevealed;
    private bool _isOverflowing;

    /// <summary>Creates a tab bar: a <c>tabbar</c> node holding <c>revealer &gt; box</c>.</summary>
    public TabBar()
    {
        SetValue(Css.NodeProperty, "tabbar", BindingPriority.Style);
        Css.SetNode(_startBin, "widget");
        Css.SetClasses(_startBin, "start-action");
        Css.SetNode(_endBin, "widget");
        Css.SetClasses(_endBin, "end-action");
        Css.SetClasses(_pinnedScroller, "pinned");
        _pinnedScroller.Child = _pinnedBox;
        _scroller.Child = _box;
        _pinnedBox.Bar = _box.Bar = this;
        _box.PinnedBox = _pinnedBox;
        _pinnedBox.UnpinnedBox = _box;
        AdwStack.SetExpand(_scroller, true);
        AdwStack.SetShrink(_scroller, true);
        var box = new AdwBox { Child = new AdwStack { Orientation = Orientation.Horizontal, Children = { _startBin, _pinnedScroller, _scroller, _endBin } } };
        Css.SetNode(box, "box");
        Css.SetClasses(box, "box horizontal");
        _revealer.Child = box;
        VisualChildren.Add(_revealer);
        LogicalChildren.Add(_revealer);
        _box.LayoutUpdated += (_, _) => UpdateIsOverflowing();
    }

    /// <summary>The view whose pages the bar shows.</summary>
    public TabView? View
    {
        get => GetValue(ViewProperty);
        set => SetValue(ViewProperty, value);
    }

    /// <summary>Whether the tabs hide while there is one page and none is pinned.</summary>
    public bool Autohide
    {
        get => GetValue(AutohideProperty);
        set => SetValue(AutohideProperty, value);
    }

    /// <summary>Whether the tabs share the bar's width, rather than stopping at 220 px each.</summary>
    public bool ExpandTabs
    {
        get => GetValue(ExpandTabsProperty);
        set => SetValue(ExpandTabsProperty, value);
    }

    /// <summary>Whether the tabs' close buttons are at their start, as on macOS.</summary>
    public bool IsInverted
    {
        get => GetValue(IsInvertedProperty);
        set => SetValue(IsInvertedProperty, value);
    }

    /// <summary>A widget before the tabs.</summary>
    public Control? StartActionWidget
    {
        get => GetValue(StartActionWidgetProperty);
        set => SetValue(StartActionWidgetProperty, value);
    }

    /// <summary>A widget after the tabs, as a button that opens a new tab.</summary>
    public Control? EndActionWidget
    {
        get => GetValue(EndActionWidgetProperty);
        set => SetValue(EndActionWidgetProperty, value);
    }

    /// <summary>Whether the tabs show.</summary>
    public bool TabsRevealed
    {
        get => _tabsRevealed;
        private set => SetAndRaise(TabsRevealedProperty, ref _tabsRevealed, value);
    }

    /// <summary>Whether the tabs do not all fit, or their widths are frozen while the pointer is over them.</summary>
    public bool IsOverflowing
    {
        get => _isOverflowing;
        private set => SetAndRaise(IsOverflowingProperty, ref _isOverflowing, value);
    }

    /// <summary>The row of unpinned tabs.</summary>
    internal TabBox TabBox => _box;

    /// <summary>The row of pinned tabs.</summary>
    internal TabBox PinnedTabBox => _pinnedBox;

    /// <summary>Whether a tab has the focus and shows it (adw_tab_bar_tabs_have_visible_focus).</summary>
    internal bool TabsHaveVisibleFocus() =>
        TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() is TabBoxChild { Classes: var classes } focused
        && classes.Contains(":focus-visible") && (_box.HasFocusedTab || _pinnedBox.HasFocusedTab);

    // update_is_overflowing.
    internal void UpdateIsOverflowing() =>
        IsOverflowing = _box.IsOverflowing || _pinnedBox.IsOverflowing || _box.IsResizeFrozen || _pinnedBox.IsResizeFrozen;

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new Automation.TabBarAutomationPeer(this);

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ViewProperty)
        {
            SetView(View);
        }
        else if (change.Property == AutohideProperty)
        {
            UpdateAutohide();
        }
        else if (change.Property == ExpandTabsProperty)
        {
            _box.ExpandTabs = ExpandTabs;
        }
        else if (change.Property == IsInvertedProperty)
        {
            _box.IsInverted = IsInverted;
        }
        else if (change.Property == StartActionWidgetProperty)
        {
            _startBin.Child = StartActionWidget;
            _startBin.IsVisible = StartActionWidget is not null;
        }
        else if (change.Property == EndActionWidgetProperty)
        {
            _endBin.Child = EndActionWidget;
            _endBin.IsVisible = EndActionWidget is not null;
        }
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        _revealer.Measure(availableSize);
        return _revealer.DesiredSize;
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        _revealer.Arrange(new Rect(finalSize));
        return finalSize;
    }

    // adw_tab_bar_set_view.
    private void SetView(TabView? view)
    {
        if (_view is { } old)
        {
            old.PropertyChanged -= OnViewPropertyChanged;
            old.PageAttached -= OnPageAttached;
            old.PageDetached -= OnPageDetached;
            foreach (var page in old.Pages)
            {
                page.PropertyChanged -= OnPagePropertyChanged;
            }

            _pinnedBox.SetView(null);
            _box.SetView(null);
        }

        _view = view;
        if (view is not null)
        {
            _pinnedBox.SetView(view);
            _box.SetView(view);
            view.PropertyChanged += OnViewPropertyChanged;
            view.PageAttached += OnPageAttached;
            view.PageDetached += OnPageDetached;
            foreach (var page in view.Pages)
            {
                page.PropertyChanged += OnPagePropertyChanged;
            }
        }

        UpdateAutohide();
    }

    // update_autohide_cb.
    private void UpdateAutohide()
    {
        TabsRevealed = _view is not null && (!Autohide || _view.PageCount > 1 || _view.PinnedPageCount >= 1);
        _revealer.IsRevealed = TabsRevealed;
    }

    private void OnViewPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == TabView.PageCountProperty || e.Property == TabView.PinnedPageCountProperty)
        {
            UpdateAutohide();
        }
        else if (e.Property == TabView.SelectedPageProperty && _view?.SelectedPage is { } page)
        {
            // notify_selected_page_cb.
            if (page.IsPinned)
            {
                _pinnedBox.SelectPage(page);
                _box.SelectPage(page);
            }
            else
            {
                _box.SelectPage(page);
                _pinnedBox.SelectPage(page);
            }
        }
    }

    private void OnPageAttached(object? sender, TabPageEventArgs e)
    {
        if (e.Page is { } page)
        {
            page.PropertyChanged += OnPagePropertyChanged;
        }
    }

    private void OnPageDetached(object? sender, TabPageEventArgs e)
    {
        if (e.Page is { } page)
        {
            page.PropertyChanged -= OnPagePropertyChanged;
        }
    }

    // notify_pinned_cb: a page pinned or unpinned moves to the other row.
    private void OnPagePropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != TabPage.IsPinnedProperty || sender is not TabPage page || _view is null)
        {
            return;
        }

        var (from, to) = page.IsPinned ? (_box, _pinnedBox) : (_pinnedBox, _box);
        var focus = from.IsPageFocused(page);
        from.DetachPage(page);
        to.AttachPage(page, _view.PinnedPageCount);
        if (focus)
        {
            to.TryFocusSelectedTab();
        }
    }
}
