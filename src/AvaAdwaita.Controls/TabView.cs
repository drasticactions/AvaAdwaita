using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Metadata;
using Avalonia.VisualTree;
using AvaAdwaita.Nodes;

namespace AvaAdwaita.Controls;

/// <summary>
/// A view of pages, one at a time, for a <see cref="TabBar"/> or <see cref="TabOverview"/> to switch between (AdwTabView).
/// Pinned pages come first. Closing a page asks <see cref="ClosePageRequested"/> first; Ctrl+Tab, Ctrl+Page Up and Down,
/// Alt+digits and the rest of <see cref="Shortcuts"/> work anywhere in the window.
/// </summary>
public class TabView : Control
{
    /// <summary>Defines <see cref="SelectedPage"/>.</summary>
    public static readonly DirectProperty<TabView, TabPage?> SelectedPageProperty =
        AvaloniaProperty.RegisterDirect<TabView, TabPage?>(nameof(SelectedPage), v => v.SelectedPage, (v, p) => v.SelectedPage = p, defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Defines <see cref="PageCount"/>.</summary>
    public static readonly DirectProperty<TabView, int> PageCountProperty =
        AvaloniaProperty.RegisterDirect<TabView, int>(nameof(PageCount), v => v.PageCount);

    /// <summary>Defines <see cref="PinnedPageCount"/>.</summary>
    public static readonly DirectProperty<TabView, int> PinnedPageCountProperty =
        AvaloniaProperty.RegisterDirect<TabView, int>(nameof(PinnedPageCount), v => v.PinnedPageCount);

    /// <summary>Defines <see cref="DefaultIconName"/>.</summary>
    public static readonly StyledProperty<string> DefaultIconNameProperty =
        AvaloniaProperty.Register<TabView, string>(nameof(DefaultIconName), "adw-tab-icon-missing-symbolic");

    /// <summary>Defines <see cref="Menu"/>.</summary>
    public static readonly StyledProperty<FlyoutBase?> MenuProperty =
        AvaloniaProperty.Register<TabView, FlyoutBase?>(nameof(Menu));

    /// <summary>Defines <see cref="Shortcuts"/>.</summary>
    public static readonly StyledProperty<TabViewShortcuts> ShortcutsProperty =
        AvaloniaProperty.Register<TabView, TabViewShortcuts>(nameof(Shortcuts), TabViewShortcuts.AllShortcuts);

    /// <summary>Defines <see cref="PageAttached"/>.</summary>
    public static readonly RoutedEvent<TabPageEventArgs> PageAttachedEvent =
        RoutedEvent.Register<TabView, TabPageEventArgs>(nameof(PageAttached), RoutingStrategies.Direct);

    /// <summary>Defines <see cref="PageDetached"/>.</summary>
    public static readonly RoutedEvent<TabPageEventArgs> PageDetachedEvent =
        RoutedEvent.Register<TabView, TabPageEventArgs>(nameof(PageDetached), RoutingStrategies.Direct);

    /// <summary>Defines <see cref="PageReordered"/>.</summary>
    public static readonly RoutedEvent<TabPageEventArgs> PageReorderedEvent =
        RoutedEvent.Register<TabView, TabPageEventArgs>(nameof(PageReordered), RoutingStrategies.Direct);

    /// <summary>Defines <see cref="ClosePageRequested"/>.</summary>
    public static readonly RoutedEvent<TabPageEventArgs> ClosePageRequestedEvent =
        RoutedEvent.Register<TabView, TabPageEventArgs>(nameof(ClosePageRequested), RoutingStrategies.Direct);

    /// <summary>Defines <see cref="SetupMenu"/>.</summary>
    public static readonly RoutedEvent<TabPageEventArgs> SetupMenuEvent =
        RoutedEvent.Register<TabView, TabPageEventArgs>(nameof(SetupMenu), RoutingStrategies.Direct);

    /// <summary>Defines <see cref="IndicatorActivated"/>.</summary>
    public static readonly RoutedEvent<TabPageEventArgs> IndicatorActivatedEvent =
        RoutedEvent.Register<TabView, TabPageEventArgs>(nameof(IndicatorActivated), RoutingStrategies.Direct);

    private readonly List<TabPage> _pages = [];
    private TabPage? _selectedPage;
    private int _pageCount;
    private int _pinnedPageCount;
    private TopLevel? _topLevel;
    private bool _syncing;

    /// <summary>Creates a tab view: a <c>tabview</c> node holding each page's bin.</summary>
    public TabView()
    {
        SetValue(Css.NodeProperty, "tabview", BindingPriority.Style);
        Pages.CollectionChanged += (_, e) =>
        {
            if (_syncing)
            {
                return;
            }

            // Pages added to or removed from the list from outside the view's methods, as a XAML view's pages.
            foreach (var page in e.OldItems?.OfType<TabPage>() ?? [])
            {
                if (_pages.Contains(page))
                {
                    Detach(page);
                }
            }

            foreach (var page in e.NewItems?.OfType<TabPage>() ?? [])
            {
                if (!_pages.Contains(page))
                {
                    Insert(page, Math.Max(_pinnedPageCount, Math.Min(Pages.IndexOf(page), _pages.Count)), pinned: false, fromList: true);
                }
            }
        };
    }

    /// <summary>Raised when a page is added, or moved into the view.</summary>
    public event EventHandler<TabPageEventArgs>? PageAttached
    {
        add => AddHandler(PageAttachedEvent, value);
        remove => RemoveHandler(PageAttachedEvent, value);
    }

    /// <summary>Raised when a page is closed, or moved out of the view.</summary>
    public event EventHandler<TabPageEventArgs>? PageDetached
    {
        add => AddHandler(PageDetachedEvent, value);
        remove => RemoveHandler(PageDetachedEvent, value);
    }

    /// <summary>Raised when a page moves within the view.</summary>
    public event EventHandler<TabPageEventArgs>? PageReordered
    {
        add => AddHandler(PageReorderedEvent, value);
        remove => RemoveHandler(PageReorderedEvent, value);
    }

    /// <summary>
    /// Raised when a page is asked to close. Handle it to decide yourself, calling <see cref="ClosePageFinish"/> then or
    /// later; otherwise the page closes unless it is pinned.
    /// </summary>
    public event EventHandler<TabPageEventArgs>? ClosePageRequested
    {
        add => AddHandler(ClosePageRequestedEvent, value);
        remove => RemoveHandler(ClosePageRequestedEvent, value);
    }

    /// <summary>Raised before <see cref="Menu"/> opens for a page, and with no page once it has closed.</summary>
    public event EventHandler<TabPageEventArgs>? SetupMenu
    {
        add => AddHandler(SetupMenuEvent, value);
        remove => RemoveHandler(SetupMenuEvent, value);
    }

    /// <summary>Raised when an activatable page indicator is clicked.</summary>
    public event EventHandler<TabPageEventArgs>? IndicatorActivated
    {
        add => AddHandler(IndicatorActivatedEvent, value);
        remove => RemoveHandler(IndicatorActivatedEvent, value);
    }

    /// <summary>The pages, pinned first. Pages added to the list are appended unpinned.</summary>
    [Content]
    public AvaloniaList<TabPage> Pages { get; } = new();

    /// <summary>The page that shows; the first page added until another is selected.</summary>
    public TabPage? SelectedPage
    {
        get => _selectedPage;
        set
        {
            if (value is not null && !_pages.Contains(value))
            {
                throw new ArgumentException("The page is not in this view.", nameof(value));
            }

            SetSelected(value);
        }
    }

    /// <summary>How many pages there are.</summary>
    public int PageCount
    {
        get => _pageCount;
        private set => SetAndRaise(PageCountProperty, ref _pageCount, value);
    }

    /// <summary>How many of the pages are pinned.</summary>
    public int PinnedPageCount
    {
        get => _pinnedPageCount;
        private set => SetAndRaise(PinnedPageCountProperty, ref _pinnedPageCount, value);
    }

    /// <summary>The icon a pinned page without an icon of its own shows.</summary>
    public string DefaultIconName
    {
        get => GetValue(DefaultIconNameProperty);
        set => SetValue(DefaultIconNameProperty, value);
    }

    /// <summary>The menu a tab's context click shows, after <see cref="SetupMenu"/>.</summary>
    public FlyoutBase? Menu
    {
        get => GetValue(MenuProperty);
        set => SetValue(MenuProperty, value);
    }

    /// <summary>The keyboard shortcuts the view handles.</summary>
    public TabViewShortcuts Shortcuts
    {
        get => GetValue(ShortcutsProperty);
        set => SetValue(ShortcutsProperty, value);
    }

    /// <summary>The page holding <paramref name="content"/>, if any.</summary>
    public TabPage? GetPage(Control content) => _pages.FirstOrDefault(p => ReferenceEquals(p.Content, content));

    /// <summary>The page at <paramref name="position"/>.</summary>
    public TabPage GetNthPage(int position) => _pages[position];

    /// <summary>Where <paramref name="page"/> is, or -1.</summary>
    public int GetPagePosition(TabPage page) => _pages.IndexOf(page);

    /// <summary>Adds a page after the unpinned pages.</summary>
    public TabPage Append(Control content) => Insert(new TabPage(content), _pages.Count, pinned: false);

    /// <summary>Adds a page before the unpinned pages.</summary>
    public TabPage Prepend(Control content) => Insert(new TabPage(content), _pinnedPageCount, pinned: false);

    /// <summary>Adds a page at <paramref name="position"/> among all pages, which must be at or after the pinned ones.</summary>
    public TabPage Insert(Control content, int position) => Insert(new TabPage(content), position, pinned: false);

    /// <summary>Adds a pinned page after the pinned pages.</summary>
    public TabPage AppendPinned(Control content) => Insert(new TabPage(content), _pinnedPageCount, pinned: true);

    /// <summary>Adds a pinned page before the pinned pages.</summary>
    public TabPage PrependPinned(Control content) => Insert(new TabPage(content), 0, pinned: true);

    /// <summary>Adds a pinned page at <paramref name="position"/>, which must be among the pinned pages.</summary>
    public TabPage InsertPinned(Control content, int position) => Insert(new TabPage(content), position, pinned: true);

    /// <summary>
    /// Adds a page opened from <paramref name="parent"/> (adw_tab_view_add_page): after the parent and the pages opened from
    /// it, or at the end without a parent.
    /// </summary>
    public TabPage AddPage(Control content, TabPage? parent = null)
    {
        var position = _pages.Count;
        if (parent is not null)
        {
            position = parent.IsPinned ? _pinnedPageCount - 1 : _pages.IndexOf(parent);
            do
            {
                position++;
            }
            while (position < _pages.Count && IsDescendantOf(_pages[position], parent));
        }

        var page = new TabPage(content) { Parent = parent };
        return Insert(page, position, pinned: false);
    }

    /// <summary>Asks to close <paramref name="page"/>: raises <see cref="ClosePageRequested"/>.</summary>
    public void ClosePage(TabPage page)
    {
        if (page.IsClosing)
        {
            return;
        }

        page.IsClosing = true;
        var args = new TabPageEventArgs(ClosePageRequestedEvent, page, _pages.IndexOf(page));
        RaiseEvent(args);
        if (!args.Handled)
        {
            // close_page_cb: a pinned page stays.
            ClosePageFinish(page, !page.IsPinned);
        }
    }

    /// <summary>Finishes closing <paramref name="page"/>, closing it when <paramref name="confirm"/> is true.</summary>
    public void ClosePageFinish(TabPage page, bool confirm)
    {
        page.IsClosing = false;
        if (confirm && _pages.Contains(page))
        {
            Detach(page);
        }
    }

    /// <summary>Asks to close every page but <paramref name="page"/>.</summary>
    public void CloseOtherPages(TabPage page)
    {
        foreach (var p in _pages.AsEnumerable().Reverse().Where(p => p != page).ToList())
        {
            ClosePage(p);
        }
    }

    /// <summary>Asks to close the pages before <paramref name="page"/>.</summary>
    public void ClosePagesBefore(TabPage page)
    {
        foreach (var p in _pages.Take(_pages.IndexOf(page)).Reverse().ToList())
        {
            ClosePage(p);
        }
    }

    /// <summary>Asks to close the pages after <paramref name="page"/>.</summary>
    public void ClosePagesAfter(TabPage page)
    {
        foreach (var p in _pages.Skip(_pages.IndexOf(page) + 1).Reverse().ToList())
        {
            ClosePage(p);
        }
    }

    /// <summary>Pins or unpins <paramref name="page"/>: it moves to the end of the pinned pages, or the start of the others.</summary>
    public void SetPagePinned(TabPage page, bool pinned)
    {
        if (page.IsPinned == pinned || !_pages.Contains(page))
        {
            return;
        }

        var newPosition = pinned ? _pinnedPageCount : _pinnedPageCount - 1;
        Sync(() =>
        {
            _pages.Remove(page);
            _pages.Insert(newPosition, page);
            Pages.Remove(page);
            Pages.Insert(newPosition, page);
        });
        PinnedPageCount = newPosition + (pinned ? 1 : 0);
        page.IsPinned = pinned;
    }

    /// <summary>Moves <paramref name="page"/> to <paramref name="position"/>, among the pinned pages or the others as it is; whether it moved.</summary>
    public bool ReorderPage(TabPage page, int position)
    {
        var original = _pages.IndexOf(page);
        var (first, last) = page.IsPinned ? (0, _pinnedPageCount - 1) : (_pinnedPageCount, _pages.Count - 1);
        position = Math.Clamp(position, first, last);
        if (original < 0 || original == position)
        {
            return false;
        }

        Sync(() =>
        {
            _pages.RemoveAt(original);
            _pages.Insert(position, page);
            Pages.Move(original, position);
        });
        RaiseEvent(new TabPageEventArgs(PageReorderedEvent, page, position));
        return true;
    }

    /// <summary>Moves <paramref name="page"/> back by one.</summary>
    public bool ReorderBackward(TabPage page)
    {
        var position = _pages.IndexOf(page);
        return position > (page.IsPinned ? 0 : _pinnedPageCount) && ReorderPage(page, position - 1);
    }

    /// <summary>Moves <paramref name="page"/> forward by one.</summary>
    public bool ReorderForward(TabPage page)
    {
        var position = _pages.IndexOf(page);
        return position < (page.IsPinned ? _pinnedPageCount : _pages.Count) - 1 && ReorderPage(page, position + 1);
    }

    /// <summary>Moves <paramref name="page"/> first, among the pinned pages or the others.</summary>
    public bool ReorderFirst(TabPage page) => ReorderPage(page, page.IsPinned ? 0 : _pinnedPageCount);

    /// <summary>Moves <paramref name="page"/> last, among the pinned pages or the others.</summary>
    public bool ReorderLast(TabPage page) => ReorderPage(page, (page.IsPinned ? _pinnedPageCount : _pages.Count) - 1);

    /// <summary>Selects the page before the selected one; whether there was one.</summary>
    public bool SelectPreviousPage()
    {
        var position = _selectedPage is null ? -1 : _pages.IndexOf(_selectedPage);
        if (position <= 0)
        {
            return false;
        }

        SelectedPage = _pages[position - 1];
        return true;
    }

    /// <summary>Selects the page after the selected one; whether there was one.</summary>
    public bool SelectNextPage()
    {
        var position = _selectedPage is null ? int.MaxValue : _pages.IndexOf(_selectedPage);
        if (position >= _pages.Count - 1)
        {
            return false;
        }

        SelectedPage = _pages[position + 1];
        return true;
    }

    /// <summary>Selects the first page, among the pinned pages or the others, then the very first.</summary>
    public bool SelectFirstPage()
    {
        if (_selectedPage is not { } selected)
        {
            return false;
        }

        var page = _pages[selected.IsPinned ? 0 : _pinnedPageCount];
        if (page == selected && !selected.IsPinned)
        {
            page = _pages[0];
        }

        if (page == selected)
        {
            return false;
        }

        SelectedPage = page;
        return true;
    }

    /// <summary>Selects the last page, among the pinned pages or the others, then the very last.</summary>
    public bool SelectLastPage()
    {
        if (_selectedPage is not { } selected)
        {
            return false;
        }

        var page = _pages[(selected.IsPinned ? _pinnedPageCount : _pages.Count) - 1];
        if (page == selected && selected.IsPinned)
        {
            page = _pages[^1];
        }

        if (page == selected)
        {
            return false;
        }

        SelectedPage = page;
        return true;
    }

    /// <summary>Moves <paramref name="page"/> to <paramref name="other"/> at <paramref name="position"/>.</summary>
    public void TransferPage(TabPage page, TabView other, int position)
    {
        var pinned = page.IsPinned;
        Detach(page);
        page.IsPinned = false;
        other.Insert(page, position, pinned);
    }

    /// <summary>Opens the menu for a page, at <paramref name="target"/>.</summary>
    internal void ShowMenu(TabPage page, Control target, Point? at = null)
    {
        if (Menu is not { } menu)
        {
            return;
        }

        RaiseEvent(new TabPageEventArgs(SetupMenuEvent, page, _pages.IndexOf(page)));
        void Closed(object? sender, EventArgs e)
        {
            menu.Closed -= Closed;
            RaiseEvent(new TabPageEventArgs(SetupMenuEvent, null, -1));
        }

        menu.Closed += Closed;
        if (menu is PopupFlyoutBase popup)
        {
            popup.ShowAt(target, showAtPointer: at is not null);
        }
        else
        {
            menu.ShowAt(target);
        }
    }

    /// <summary>Raises <see cref="IndicatorActivated"/>.</summary>
    internal void ActivateIndicator(TabPage page) =>
        RaiseEvent(new TabPageEventArgs(IndicatorActivatedEvent, page, _pages.IndexOf(page)));

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new Automation.TabViewAutomationPeer(this);

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _topLevel = TopLevel.GetTopLevel(this);
        _topLevel?.AddHandler(KeyDownEvent, OnWindowKeyDown, RoutingStrategies.Tunnel);
    }

    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _topLevel?.RemoveHandler(KeyDownEvent, OnWindowKeyDown);
        _topLevel = null;
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        // adw_tab_view_measure: the largest page.
        var size = default(Size);
        foreach (var page in _pages)
        {
            page.Bin.Measure(availableSize);
            size = new Size(Math.Max(size.Width, page.Bin.DesiredSize.Width), Math.Max(size.Height, page.Bin.DesiredSize.Height));
        }

        return size;
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        foreach (var page in _pages.Where(p => p.Bin.IsVisible))
        {
            page.Bin.Arrange(new Rect(finalSize));
        }

        return finalSize;
    }

    private static bool IsDescendantOf(TabPage? page, TabPage parent)
    {
        while (page is not null && page != parent)
        {
            page = page.Parent;
        }

        return page == parent;
    }

    private void Sync(Action action)
    {
        _syncing = true;
        try
        {
            action();
        }
        finally
        {
            _syncing = false;
        }
    }

    // insert_page and attach_page.
    private TabPage Insert(TabPage page, int position, bool pinned, bool fromList = false)
    {
        position = pinned ? Math.Clamp(position, 0, _pinnedPageCount) : Math.Clamp(position, _pinnedPageCount, _pages.Count);
        page.IsPinned = pinned;
        Sync(() =>
        {
            _pages.Insert(position, page);
            if (fromList)
            {
                Pages.Remove(page);
            }

            Pages.Insert(position, page);
        });
        page.Bin.IsVisible = false;
        VisualChildren.Add(page.Bin);
        LogicalChildren.Add(page.Bin);
        PageCount = _pages.Count;
        if (pinned)
        {
            PinnedPageCount++;
        }

        if (page.Parent is { } parent && !_pages.Contains(parent))
        {
            page.Parent = null;
        }

        RaiseEvent(new TabPageEventArgs(PageAttachedEvent, page, position));
        if (_selectedPage is null)
        {
            SetSelected(page);
        }

        InvalidateMeasure();
        return page;
    }

    // detach_page: the page before it is selected when it was selected, as select_previous_page chooses it.
    private void Detach(TabPage page)
    {
        var position = _pages.IndexOf(page);
        SelectPreviousPage(page);
        if (_pages.Count == 1)
        {
            SetSelected(null);
        }

        Sync(() =>
        {
            _pages.RemoveAt(position);
            Pages.Remove(page);
        });
        PageCount = _pages.Count;
        if (page.IsPinned)
        {
            PinnedPageCount--;
        }

        VisualChildren.Remove(page.Bin);
        LogicalChildren.Remove(page.Bin);
        page.IsSelected = false;
        RaiseEvent(new TabPageEventArgs(PageDetachedEvent, page, position));
        InvalidateMeasure();
    }

    // select_previous_page: closing the selected page goes to the one it was opened from, or the next, or the previous.
    private void SelectPreviousPage(TabPage page)
    {
        if (page != _selectedPage)
        {
            return;
        }

        var position = _pages.IndexOf(page);
        if (page.Parent is { } parent && position > 0)
        {
            var previous = _pages[position - 1];
            if (IsDescendantOf(previous, parent))
            {
                SelectedPage = previous;
                return;
            }

            if (previous.IsPinned && parent.IsPinned)
            {
                SelectedPage = parent;
                return;
            }
        }

        if (!SelectNextPage())
        {
            SelectPreviousPage();
        }
    }

    // set_selected_page: the focus moves with the page.
    private void SetSelected(TabPage? page)
    {
        if (_selectedPage == page)
        {
            return;
        }

        var hadFocus = false;
        if (_selectedPage is { } old)
        {
            var focus = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement();
            if (focus is Visual v && old.Bin.IsVisualAncestorOf(v))
            {
                hadFocus = true;
                old.LastFocus = focus;
            }

            old.Bin.IsVisible = false;
            old.IsSelected = false;
        }

        var previous = _selectedPage;
        _selectedPage = page;
        if (page is not null)
        {
            page.Bin.IsVisible = true;
            page.IsSelected = true;
            if (hadFocus)
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(
                    () =>
                    {
                        if (page.LastFocus is { } last)
                        {
                            last.Focus();
                        }
                        else
                        {
                            page.Bin.GetVisualDescendants().OfType<InputElement>()
                                .FirstOrDefault(i => i.Focusable && i.IsEffectivelyVisible && i.IsEffectivelyEnabled)?.Focus();
                        }
                    },
                    Avalonia.Threading.DispatcherPriority.Loaded);
            }
        }

        RaisePropertyChanged(SelectedPageProperty, previous, page);
        InvalidateArrange();
    }

    // init_shortcuts: page switching and reordering, and Alt+digits.
    private void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Handled || _selectedPage is null || !IsEffectivelyVisible)
        {
            return;
        }

        var ctrl = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        var shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        var alt = e.KeyModifiers.HasFlag(KeyModifiers.Alt);
        bool Has(TabViewShortcuts s) => (Shortcuts & s) != 0;

        if (alt && !ctrl && !shift && e.Key is >= Key.D0 and <= Key.D9 or >= Key.NumPad0 and <= Key.NumPad9)
        {
            var digit = e.Key is >= Key.D0 and <= Key.D9 ? e.Key - Key.D0 : e.Key - Key.NumPad0;
            var n = (digit + 9) % 10;
            if (n < _pages.Count && Has(n == 9 ? TabViewShortcuts.AltZero : TabViewShortcuts.AltDigits) && _pages[n] != _selectedPage)
            {
                SelectedPage = _pages[n];
                e.Handled = true;
            }

            return;
        }

        if (!ctrl || alt || _pages.Count <= 1)
        {
            return;
        }

        var (mask, forward, last, reorder) = (e.Key, shift) switch
        {
            (Key.Tab, false) => (TabViewShortcuts.ControlTab, true, false, false),
            (Key.Tab, true) => (TabViewShortcuts.ControlShiftTab, false, false, false),
            (Key.PageUp, false) => (TabViewShortcuts.ControlPageUp, false, false, false),
            (Key.PageDown, false) => (TabViewShortcuts.ControlPageDown, true, false, false),
            (Key.Home, false) => (TabViewShortcuts.ControlHome, false, true, false),
            (Key.End, false) => (TabViewShortcuts.ControlEnd, true, true, false),
            (Key.PageUp, true) => (TabViewShortcuts.ControlShiftPageUp, false, false, true),
            (Key.PageDown, true) => (TabViewShortcuts.ControlShiftPageDown, true, false, true),
            (Key.Home, true) => (TabViewShortcuts.ControlShiftHome, false, true, true),
            (Key.End, true) => (TabViewShortcuts.ControlShiftEnd, true, true, true),
            _ => (TabViewShortcuts.None, false, false, false),
        };
        if (mask == TabViewShortcuts.None || !Has(mask))
        {
            return;
        }

        e.Handled = true;
        if (reorder)
        {
            var page = _selectedPage;
            _ = (forward, last) switch
            {
                (false, false) => ReorderBackward(page),
                (true, false) => ReorderForward(page),
                (false, true) => ReorderFirst(page),
                (true, true) => ReorderLast(page),
            };
            return;
        }

        // select_page_cb: next and previous wrap around.
        var moved = (forward, last) switch
        {
            (false, false) => SelectPreviousPage(),
            (true, false) => SelectNextPage(),
            (false, true) => SelectFirstPage(),
            (true, true) => SelectLastPage(),
        };
        if (!moved && !last)
        {
            SelectedPage = forward ? _pages[0] : _pages[^1];
        }
    }
}
