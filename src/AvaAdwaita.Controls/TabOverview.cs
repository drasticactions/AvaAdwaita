using System;
using System.Linq;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Metadata;
using Avalonia.VisualTree;
using AvaAdwaita.Animation;
using AvaAdwaita.Controls.Primitives;
using AvaAdwaita.Controls.Resources;
using AvaAdwaita.Nodes;
using AvaAdwaita.Presenters;

namespace AvaAdwaita.Controls;

/// <summary>
/// An overview of a <see cref="TabView"/>'s pages as a grid of thumbnails (AdwTabOverview), shown over its
/// <see cref="Content"/>, which holds the view. Opening it zooms the selected page out into its thumbnail; clicking a
/// thumbnail opens that page. Its header bar searches the pages, and a button can open a new one.
/// </summary>
public class TabOverview : Control
{
    /// <summary>Defines <see cref="View"/>.</summary>
    public static readonly StyledProperty<TabView?> ViewProperty =
        AvaloniaProperty.Register<TabOverview, TabView?>(nameof(View));

    /// <summary>Defines <see cref="Content"/>.</summary>
    public static readonly StyledProperty<Control?> ContentProperty =
        AvaloniaProperty.Register<TabOverview, Control?>(nameof(Content));

    /// <summary>Defines <see cref="IsOpen"/>.</summary>
    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<TabOverview, bool>(nameof(IsOpen), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Defines <see cref="EnableSearch"/>.</summary>
    public static readonly StyledProperty<bool> EnableSearchProperty =
        AvaloniaProperty.Register<TabOverview, bool>(nameof(EnableSearch), true);

    /// <summary>Defines <see cref="EnableNewTab"/>.</summary>
    public static readonly StyledProperty<bool> EnableNewTabProperty =
        AvaloniaProperty.Register<TabOverview, bool>(nameof(EnableNewTab));

    /// <summary>Defines <see cref="IsInverted"/>.</summary>
    public static readonly StyledProperty<bool> IsInvertedProperty =
        AvaloniaProperty.Register<TabOverview, bool>(nameof(IsInverted));

    /// <summary>Defines <see cref="SecondaryMenu"/>.</summary>
    public static readonly StyledProperty<FlyoutBase?> SecondaryMenuProperty =
        AvaloniaProperty.Register<TabOverview, FlyoutBase?>(nameof(SecondaryMenu));

    /// <summary>Defines <see cref="ShowStartTitleButtons"/>.</summary>
    public static readonly StyledProperty<bool> ShowStartTitleButtonsProperty =
        AvaloniaProperty.Register<TabOverview, bool>(nameof(ShowStartTitleButtons), true);

    /// <summary>Defines <see cref="ShowEndTitleButtons"/>.</summary>
    public static readonly StyledProperty<bool> ShowEndTitleButtonsProperty =
        AvaloniaProperty.Register<TabOverview, bool>(nameof(ShowEndTitleButtons), true);

    /// <summary>Defines <see cref="IsSearchActive"/>.</summary>
    public static readonly DirectProperty<TabOverview, bool> IsSearchActiveProperty =
        AvaloniaProperty.RegisterDirect<TabOverview, bool>(nameof(IsSearchActive), o => o.IsSearchActive);

    /// <summary>Defines <see cref="CreateTab"/>.</summary>
    public static readonly RoutedEvent<TabOverviewCreateTabEventArgs> CreateTabEvent =
        RoutedEvent.Register<TabOverview, TabOverviewCreateTabEventArgs>(nameof(CreateTab), RoutingStrategies.Direct);

    private const double TransitionDuration = 400;
    private const double ThumbnailBorderRadius = 12;
    private const double WindowBorderRadius = 15;

    private readonly AdwBox _childBin = new();
    private readonly ToolbarView _overview = new() { Classes = { "overview" } };
    private readonly HeaderBar _header = new();
    private readonly ToggleButton _searchButton = new() { VerticalAlignment = VerticalAlignment.Center };
    private readonly WindowTitle _title = new() { Classes = { "numeric" } };
    private readonly DropDownButton _menuButton = new() { IsVisible = false, VerticalAlignment = VerticalAlignment.Center };
    private readonly SearchBar _searchBar = new();
    private readonly SearchEntry _searchEntry = new() { HorizontalAlignment = HorizontalAlignment.Stretch };
    private readonly TabGrid _pinnedGrid = new(pinned: true);
    private readonly TabGrid _grid = new(pinned: false);
    private readonly TabOverviewScrollable _scrollable;
    private readonly ScrollViewer _scroller = new() { HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, Classes = { "adw-scrollable-child" } };
    private readonly StatusPage _emptyState = new() { IconName = "view-grid-symbolic", IsVisible = false, IsHitTestVisible = false };
    private readonly StatusPage _searchEmptyState = new() { IconName = "edit-find-symbolic", IsVisible = false, IsHitTestVisible = false };
    private readonly Button _newTabButton = new() { IsVisible = false, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Bottom };
    private readonly TransitionLayer _transition;
    private readonly TimedAnimation _openAnimation;
    private TabView? _view;
    private bool _isSearchActive;
    private bool _animating;
    private IInputElement? _lastFocus;
    private TabThumbnail? _transitionThumbnail;

    /// <summary>Creates a tab overview: a <c>taboverview</c> node holding the content and the overview.</summary>
    public TabOverview()
    {
        SetValue(Css.NodeProperty, "taboverview", BindingPriority.Style);
        Css.SetNode(_childBin, "widget");
        _pinnedGrid.Other = _grid;
        _grid.Other = _pinnedGrid;
        _pinnedGrid.Overview = _grid.Overview = this;
        _scrollable = new TabOverviewScrollable(_pinnedGrid, _grid);

        // The header bar: search, the page count, and a menu.
        var searchIcon = new AdwIcon { IconName = "edit-find-symbolic" };
        Css.SetNode(searchIcon, "image");
        _searchButton.Content = searchIcon;
        _searchButton[!ToolTip.TipProperty] = _searchButton.GetResourceObservable("AdwStringSearchTabs").ToBinding();
        var menuIcon = new AdwIcon { IconName = "view-more-symbolic" };
        Css.SetNode(menuIcon, "image");
        _menuButton.Content = menuIcon;
        _header.Start.Add(_searchButton);
        _header.End.Add(_menuButton);
        _header.TitleWidget = _title;
        _header.Bind(HeaderBar.ShowStartTitleButtonsProperty, this.GetObservable(ShowStartTitleButtonsProperty));
        _header.Bind(HeaderBar.ShowEndTitleButtonsProperty, this.GetObservable(ShowEndTitleButtonsProperty));
        _searchEntry[!TextBox.PlaceholderTextProperty] = _searchEntry.GetResourceObservable("AdwStringSearchTabsPlaceholder").ToBinding();
        _searchBar.Content = new Clamp { MaximumSize = 400, Child = _searchEntry };
        _searchButton.IsCheckedChanged += (_, _) => _searchBar.IsSearchMode = _searchButton.IsChecked == true;
        _searchBar.PropertyChanged += (_, e) =>
        {
            if (e.Property == SearchBar.IsSearchModeProperty)
            {
                _searchButton.IsChecked = _searchBar.IsSearchMode;
            }
        };

        // The content: the grids, the empty states and the new tab button over them.
        _emptyState[!StatusPage.TitleProperty] = _emptyState.GetResourceObservable("AdwStringNoOpenTabs").ToBinding();
        _searchEmptyState[!StatusPage.TitleProperty] = _searchEmptyState.GetResourceObservable("AdwStringNoTabsFound").ToBinding();
        _searchEmptyState[!StatusPage.DescriptionProperty] = _searchEmptyState.GetResourceObservable("AdwStringTryADifferentSearch").ToBinding();
        _newTabButton.Content = NewTabContent();
        _newTabButton.Classes.AddRange(["pill", "suggested-action", "new-tab-button", "image-text-button"]);
        Css.SetClasses(_newTabButton, "bottom");
        _newTabButton.Click += (_, _) => OnNewTabClicked();
        _scrollable.NewButton = _newTabButton;
        _scroller.Content = _scrollable;
        var overlay = new AdwBox { Child = new OverlayPanel { Children = { _scroller, _emptyState, _searchEmptyState, _newTabButton } } };
        Css.SetNode(overlay, "overlay");
        _overview.TopBars.Add(_header);
        _overview.TopBars.Add(_searchBar);
        _overview.Content = overlay;
        _overview.IsVisible = false;
        _searchBar.KeyCaptureTarget = _overview;

        _transition = new TransitionLayer(this) { IsHitTestVisible = false, IsVisible = false };
        VisualChildren.Add(_childBin);
        VisualChildren.Add(_overview);
        VisualChildren.Add(_transition);
        LogicalChildren.Add(_childBin);
        LogicalChildren.Add(_overview);
        _openAnimation = new TimedAnimation(this, TransitionDuration, AdwEasing.Ease, v =>
        {
            Progress = v;
            _transition.InvalidateVisual();
        }, OnOpenAnimationDone);

        _searchEntry.SearchChanged += (_, _) => OnSearchChanged();
        _searchEntry.StopSearch += (_, _) => StopSearch();
        _searchEntry.Activated += (_, _) =>
        {
            if (_isSearchActive && !_pinnedGrid.FocusFirst())
            {
                _grid.FocusFirst();
            }
        };
        _pinnedGrid.EmptyChanged += (_, _) => UpdateEmptyStates();
        _grid.EmptyChanged += (_, _) => UpdateEmptyStates();
    }

    /// <summary>Raised when the new tab button is clicked, or Escape closes an overview with no pages: add a page and set it.</summary>
    public event EventHandler<TabOverviewCreateTabEventArgs>? CreateTab
    {
        add => AddHandler(CreateTabEvent, value);
        remove => RemoveHandler(CreateTabEvent, value);
    }

    /// <summary>The view whose pages the overview shows.</summary>
    public TabView? View
    {
        get => GetValue(ViewProperty);
        set => SetValue(ViewProperty, value);
    }

    /// <summary>What the overview shows over: the content holding <see cref="View"/>, usually with a tab bar.</summary>
    [Content]
    public Control? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    /// <summary>Whether the overview shows.</summary>
    public bool IsOpen
    {
        get => GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    /// <summary>Whether the pages can be searched.</summary>
    public bool EnableSearch
    {
        get => GetValue(EnableSearchProperty);
        set => SetValue(EnableSearchProperty, value);
    }

    /// <summary>Whether a button opens a new page, raising <see cref="CreateTab"/>.</summary>
    public bool EnableNewTab
    {
        get => GetValue(EnableNewTabProperty);
        set => SetValue(EnableNewTabProperty, value);
    }

    /// <summary>Whether the thumbnails' buttons are at their start.</summary>
    public bool IsInverted
    {
        get => GetValue(IsInvertedProperty);
        set => SetValue(IsInvertedProperty, value);
    }

    /// <summary>A menu in the header bar.</summary>
    public FlyoutBase? SecondaryMenu
    {
        get => GetValue(SecondaryMenuProperty);
        set => SetValue(SecondaryMenuProperty, value);
    }

    /// <summary>Whether the header bar shows the window buttons at its start.</summary>
    public bool ShowStartTitleButtons
    {
        get => GetValue(ShowStartTitleButtonsProperty);
        set => SetValue(ShowStartTitleButtonsProperty, value);
    }

    /// <summary>Whether the header bar shows the window buttons at its end.</summary>
    public bool ShowEndTitleButtons
    {
        get => GetValue(ShowEndTitleButtonsProperty);
        set => SetValue(ShowEndTitleButtonsProperty, value);
    }

    /// <summary>Whether a search is narrowing the thumbnails.</summary>
    public bool IsSearchActive
    {
        get => _isSearchActive;
        private set => SetAndRaise(IsSearchActiveProperty, ref _isSearchActive, value);
    }

    /// <summary>How far the overview has opened, 0 to 1.</summary>
    internal double Progress { get; private set; }

    /// <summary>The grid of the other pages.</summary>
    internal TabGrid Grid => _grid;

    /// <summary>The grid of the pinned pages.</summary>
    internal TabGrid PinnedGrid => _pinnedGrid;

    /// <summary>Opens the overview.</summary>
    public void Open() => IsOpen = true;

    /// <summary>Closes the overview.</summary>
    public void Close() => IsOpen = false;

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new Automation.TabOverviewAutomationPeer(this);

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ViewProperty)
        {
            SetView(View);
        }
        else if (change.Property == ContentProperty)
        {
            _childBin.Child = Content;
        }
        else if (change.Property == IsOpenProperty)
        {
            SetOpen(change.GetNewValue<bool>());
        }
        else if (change.Property == EnableSearchProperty)
        {
            _searchButton.IsVisible = EnableSearch;
            if (!EnableSearch)
            {
                _searchBar.IsSearchMode = false;
            }

            _searchBar.KeyCaptureTarget = EnableSearch ? _overview : null;
            UpdateHeaderBar();
        }
        else if (change.Property == EnableNewTabProperty)
        {
            UpdateNewTabButton();
        }
        else if (change.Property == IsInvertedProperty)
        {
            _grid.IsInverted = _pinnedGrid.IsInverted = IsInverted;
        }
        else if (change.Property == SecondaryMenuProperty)
        {
            _menuButton.Flyout = SecondaryMenu;
            _menuButton.IsVisible = SecondaryMenu is not null;
            UpdateHeaderBar();
        }
        else if (change.Property == ShowStartTitleButtonsProperty || change.Property == ShowEndTitleButtonsProperty)
        {
            UpdateHeaderBar();
        }
    }

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Handled || !IsOpen)
        {
            return;
        }

        if (e.Key == Key.Escape)
        {
            // escape_cb: the search closes first; an overview with no pages makes one first.
            e.Handled = true;
            if (_searchBar.IsSearchMode)
            {
                _searchBar.IsSearchMode = false;
                return;
            }

            if (_view is { PageCount: 0 } && RequestTab() is null)
            {
                return;
            }

            Close();
        }
        else if (e.Key == Key.F && e.KeyModifiers is KeyModifiers.Control or KeyModifiers.Meta && EnableSearch)
        {
            // start_search_cb.
            e.Handled = true;
            _searchBar.IsSearchMode = true;
            _searchEntry.Focus();
        }
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        // The overview lies over the content, scrolling what does not fit: the content's size is the overview's.
        _childBin.Measure(availableSize);
        _overview.Measure(availableSize);
        _transition.Measure(availableSize);
        return _childBin.DesiredSize;
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        _childBin.Arrange(new Rect(finalSize));
        _overview.Arrange(new Rect(finalSize));
        _transition.Arrange(new Rect(finalSize));
        return finalSize;
    }

    private Control NewTabContent()
    {
        var icon = new AdwIcon { IconName = "adw-tab-new-symbolic", VerticalAlignment = VerticalAlignment.Center };
        Css.SetNode(icon, "image");
        var label = new AdwLabel { RecognizesAccessKey = true };
        Css.SetNode(label, "label");
        label[!TextBlock.TextProperty] = label.GetResourceObservable("AdwStringNewTab").ToBinding();
        var box = new AdwBox { Child = new AdwStack { Orientation = Orientation.Horizontal, Children = { icon, label } } };
        Css.SetNode(box, "box");
        Css.SetClasses(box, "horizontal");
        var content = new AdwBox { Child = box };
        Css.SetNode(content, "buttoncontent");
        return content;
    }

    // adw_tab_overview_set_view.
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
        }

        _view = view;
        _pinnedGrid.SetView(view);
        _grid.SetView(view);
        if (view is not null)
        {
            view.PropertyChanged += OnViewPropertyChanged;
            view.PageAttached += OnPageAttached;
            view.PageDetached += OnPageDetached;
            foreach (var page in view.Pages)
            {
                page.PropertyChanged += OnPagePropertyChanged;
            }

            OnSelectedPageChanged();
        }

        UpdateTitle();
    }

    private void OnViewPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == TabView.PageCountProperty)
        {
            UpdateTitle();
        }
        else if (e.Property == TabView.SelectedPageProperty)
        {
            OnSelectedPageChanged();
        }
    }

    // notify_selected_page_cb.
    private void OnSelectedPageChanged()
    {
        if (_view?.SelectedPage is not { } page)
        {
            return;
        }

        if (page.IsPinned)
        {
            _pinnedGrid.SelectPage(page);
            _grid.SelectPage(page);
        }
        else
        {
            _grid.SelectPage(page);
            _pinnedGrid.SelectPage(page);
        }
    }

    // notify_n_pages_cb: "3 Tabs", in the user's language.
    private void UpdateTitle() =>
        _title.Title = _view is null ? string.Empty : AdwStrings.Plural("%u Tab", "%u Tabs", _view.PageCount);

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

    // notify_pinned_cb: a page pinned or unpinned moves to the other grid.
    private void OnPagePropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != TabPage.IsPinnedProperty || sender is not TabPage page || _view is null)
        {
            return;
        }

        var (from, to) = page.IsPinned ? (_grid, _pinnedGrid) : (_pinnedGrid, _grid);
        from.DetachPage(page);
        to.AttachPage(page, _view.PinnedPageCount);
        to.UpdatePictures(TakePicture);
        to.FocusPage(page);
    }

    // update_header_bar.
    private void UpdateHeaderBar() =>
        _header.IsVisible = EnableSearch || SecondaryMenu is not null || ShowStartTitleButtons || ShowEndTitleButtons;

    // update_new_tab_button.
    private void UpdateNewTabButton()
    {
        _newTabButton.IsVisible = EnableNewTab && !IsSearchActive;
        InvalidateArrange();
        _scrollable.InvalidateMeasure();
    }

    // empty_changed_cb.
    private void UpdateEmptyStates()
    {
        var empty = _grid.IsEmpty && _pinnedGrid.IsEmpty;
        _emptyState.IsVisible = empty && !IsSearchActive;
        _searchEmptyState.IsVisible = empty && IsSearchActive;
    }

    private void OnSearchChanged()
    {
        var text = _searchEntry.Text;
        _grid.SetSearchTerms(text);
        _pinnedGrid.SetSearchTerms(text);
        IsSearchActive = !string.IsNullOrEmpty(text);
        UpdateNewTabButton();
        UpdateEmptyStates();
    }

    private void StopSearch()
    {
        _searchEntry.Text = string.Empty;
        _grid.SetSearchTerms(null);
        _pinnedGrid.SetSearchTerms(null);
        IsSearchActive = false;
        UpdateNewTabButton();
        UpdateEmptyStates();
    }

    private TabPage? RequestTab()
    {
        var args = new TabOverviewCreateTabEventArgs(CreateTabEvent);
        RaiseEvent(args);
        return args.Page;
    }

    // new_tab_clicked_cb.
    private void OnNewTabClicked()
    {
        if (RequestTab() is not { } page || _view is null)
        {
            return;
        }

        _view.SelectedPage = page;
        Close();
        Avalonia.Threading.Dispatcher.UIThread.Post(() => page.Content?.Focus(), Avalonia.Threading.DispatcherPriority.Loaded);
    }

    /// <summary>
    /// The page as it shows in the view, for its thumbnail (AdwTabPaintable), drawn for <paramref name="width"/> when
    /// that is known: GTK draws the page itself at the thumbnail's scale, which a picture drawn at the page's size and
    /// shrunk does not match, as its text then falls between the pixels.
    /// </summary>
    internal IImage? TakePicture(TabPage page, double width = double.NaN)
    {
        var size = _view?.Bounds.Size ?? default;
        if (size.Width < 1 || size.Height < 1)
        {
            return null;
        }

        // A page that has never drawn has no picture yet; one that is not showing lays out at the view's size to be drawn.
        if (!page.Bin.HasRendered)
        {
            return null;
        }

        Visual target = page.Bin;
        if (!page.Bin.IsVisible)
        {
            if (page.Content is not { } content)
            {
                return null;
            }

            content.Measure(size);
            content.Arrange(new Rect(size));
            target = content;
        }

        // render_contents: the page over the window's background, as text drawn on a transparent bitmap loses its
        // subpixel antialiasing. Smaller, the page draws through a brush that scales its drawing rather than its pixels.
        var scale = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1;
        var picture = width > 0 && width < size.Width ? size * (width / size.Width) : size;
        var background = page.Bin.TryFindResource("Adw.WindowBgColor", out var c) && c is Color color ? color : Colors.White;
        var bitmap = new RenderTargetBitmap(PixelSize.FromSize(picture, scale));
        using (var context = bitmap.CreateDrawingContext())
        {
            // The whole bitmap, which rounds the picture up to whole pixels.
            var rect = new Rect(bitmap.Size);
            context.FillRectangle(new SolidColorBrush(background), rect);
            context.FillRectangle(new VisualBrush(target) { Stretch = Stretch.Fill, SourceRect = new RelativeRect(new Rect(size), RelativeUnit.Absolute) }, rect);
        }

        return bitmap;
    }

    private void TakePictures()
    {
        _grid.UpdatePictures(TakePicture);
        _pinnedGrid.UpdatePictures(TakePicture);
    }

    // adw_tab_overview_set_open.
    private void SetOpen(bool open)
    {
        if (_view is null || _view.PageCount == 0)
        {
            if (open || _view is null)
            {
                SetCurrentValue(IsOpenProperty, false);
                return;
            }
        }

        var selected = _view.SelectedPage;
        var grid = selected?.IsPinned == true ? _pinnedGrid : _grid;
        if (open)
        {
            var focus = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement();
            _lastFocus = focus is Visual v && _childBin.IsVisualAncestorOf(v) ? focus : null;
            TakePictures();
            SetOverviewVisible(visible: true, animating: true);
            Avalonia.Threading.Dispatcher.UIThread.Post(
                () =>
                {
                    // The thumbnails have their widths once the grid is laid out.
                    TakePictures();
                    grid.TryFocusSelectedTab();
                },
                Avalonia.Threading.DispatcherPriority.Loaded);
        }
        else
        {
            SetOverviewVisible(visible: false, animating: true);
        }

        // The selected page zooms between the view and its thumbnail, whose buttons fade meanwhile.
        _transitionThumbnail?.FadeIn();
        _transitionThumbnail = grid.TransitionThumbnail;
        _transitionThumbnail?.FadeOut();
        _animating = true;
        _transition.IsVisible = true;
        _openAnimation.Animate(open ? 1 : 0);
    }

    // set_overview_visible.
    private void SetOverviewVisible(bool visible, bool animating)
    {
        _overview.IsVisible = visible || animating;
        _overview.IsHitTestVisible = visible;
        _childBin.IsHitTestVisible = !visible && !animating;

        // An open overview covers the content, which does not draw (adw_tab_overview_snapshot).
        _childBin.Opacity = visible && !animating ? 0 : 1;
        KeyboardNavigation.SetTabNavigation(_childBin, visible ? KeyboardNavigationMode.None : KeyboardNavigationMode.Continue);
        Css.SetClasses(_childBin, visible || animating ? "background" : null);
    }

    // open_animation_done_cb.
    private void OnOpenAnimationDone()
    {
        if (!_animating)
        {
            return;
        }

        _transitionThumbnail?.FadeIn();
        _transitionThumbnail = null;
        _transition.IsVisible = false;
        _animating = false;
        SetOverviewVisible(IsOpen, animating: false);
        if (!IsOpen)
        {
            _searchBar.IsSearchMode = false;
            if (_lastFocus is { } last)
            {
                last.Focus();
                _lastFocus = null;
            }
            else
            {
                _view?.SelectedPage?.Content?.Focus();
            }
        }
    }

    // should_round_corners: which corners of the content are the window's own rounded ones, none when the window is
    // maximized, fullscreen or tiled.
    private (bool TopLeft, bool TopRight, bool BottomRight, bool BottomLeft) WindowCorners()
    {
        if (TopLevel.GetTopLevel(this) is not Window { WindowState: WindowState.Normal } window
            || _childBin.TranslatePoint(default, window) is not { } origin)
        {
            return default;
        }

        var bounds = new Rect(origin, _childBin.Bounds.Size);
        var (left, top) = (bounds.X <= 0, bounds.Y <= 0);
        var (right, bottom) = (bounds.Right >= window.Bounds.Width, bounds.Bottom >= window.Bounds.Height);
        return (top && left, top && right, bottom && right, bottom && left);
    }

    // adw_tab_overview_snapshot's transition: the dimmed window over the overview, and the content between its place and its
    // thumbnail, its corners rounding as it goes.
    private sealed class TransitionLayer(TabOverview overview) : Control
    {
        public override void Render(DrawingContext context)
        {
            if (!overview._animating || overview._view is not { } view)
            {
                return;
            }

            var progress = overview.Progress;
            var bounds = new Rect(Bounds.Size);
            if (overview.TryFindResource("Adw.ShadeColor", out var shade) && shade is Color c)
            {
                context.FillRectangle(new SolidColorBrush(c, c.A / 255.0 * (1 - progress)), bounds);
            }

            if (view.SelectedPage is not { } page || overview._transitionThumbnail?.Picture is not { } picture
                || overview._childBin.TranslatePoint(default, this) is not { } childOrigin)
            {
                return;
            }

            // calculate_bounds: the content shows from all of it to the view, cut to the thumbnail's shape where the
            // page aligns it, and from its place to the thumbnail.
            var viewBounds = view.TranslatePoint(default, this) is { } p ? new Rect(p, view.Bounds.Size) : bounds;
            var thumbnail = picture.TranslatePoint(default, this) is { } q ? new Rect(q, picture.Bounds.Size) : default;
            if (thumbnail.Width <= 0 || thumbnail.Height <= 0 || viewBounds.Width <= 0 || viewBounds.Height <= 0)
            {
                return;
            }

            var (viewRatio, thumbRatio) = (viewBounds.Width / viewBounds.Height, thumbnail.Width / thumbnail.Height);
            if (viewRatio > thumbRatio)
            {
                var width = viewBounds.Height * thumbRatio;
                var xalign = FlowDirection == FlowDirection.RightToLeft ? 1 - page.ThumbnailXAlign : page.ThumbnailXAlign;
                viewBounds = new Rect(viewBounds.X + ((viewBounds.Width - width) * xalign), viewBounds.Y, width, viewBounds.Height);
            }
            else if (viewRatio < thumbRatio)
            {
                var height = viewBounds.Width / thumbRatio;
                viewBounds = new Rect(viewBounds.X, viewBounds.Y + ((viewBounds.Height - height) * page.ThumbnailYAlign), viewBounds.Width, height);
            }

            var clip = Lerp(bounds, viewBounds, progress);
            var scale = new Vector(1 + (((thumbnail.Width / viewBounds.Width) - 1) * progress), 1 + (((thumbnail.Height / viewBounds.Height) - 1) * progress));
            var size = new Size(clip.Width * scale.X, clip.Height * scale.Y);
            var from = new Rect(
                thumbnail.X * InverseLerp(bounds.Width, thumbnail.Width, size.Width),
                thumbnail.Y * InverseLerp(bounds.Height, thumbnail.Height, size.Height),
                size.Width,
                size.Height);

            // The corners at the window's rounded corners start as round as those, the others square.
            var (topLeft, topRight, bottomRight, bottomLeft) = overview.WindowCorners();
            double Radius(bool window)
            {
                var start = window ? WindowBorderRadius : 0;
                return start + ((ThumbnailBorderRadius - start) * progress);
            }

            // The content draws live, with its background, as the page it shows may not have drawn before.
            var rect = new RoundedRect(from, new CornerRadius(Radius(topLeft), Radius(topRight), Radius(bottomRight), Radius(bottomLeft)));
            using (context.PushClip(rect))
            {
                var source = new RelativeRect(clip.Translate(-(Vector)childOrigin), RelativeUnit.Absolute);
                context.FillRectangle(new VisualBrush(overview._childBin) { Stretch = Stretch.Fill, SourceRect = source }, from);
            }

            // The outer outline in the shade color, and the inner one in white, as $window_outline_color.
            var hc = AdwaitaEnvironment.IsInitialized && AdwaitaEnvironment.State.HighContrast;
            var fade = Easing.Ease(AdwEasing.EaseOutExpo, progress);
            var outer = hc ? Color.FromArgb(128, 0, 0, 0) : shade as Color? ?? Colors.Transparent;
            Outline(context, rect, 0.5, new SolidColorBrush(outer, outer.A / 255.0 * fade));
            Outline(context, rect, -0.5, new SolidColorBrush(Colors.White, (hc ? 0.3 : 0.07) * fade));
        }

        private static void Outline(DrawingContext context, RoundedRect rect, double outset, IBrush brush) =>
            context.DrawRectangle(null, new Pen(brush, 1), rect.Inflate(outset, outset));

        private static double InverseLerp(double a, double b, double t) => b == a ? 0 : (t - a) / (b - a);

        private static Rect Lerp(Rect a, Rect b, double t) =>
            new(a.X + ((b.X - a.X) * t), a.Y + ((b.Y - a.Y) * t), a.Width + ((b.Width - a.Width) * t), a.Height + ((b.Height - a.Height) * t));
    }
}
