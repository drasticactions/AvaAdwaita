using System;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;

namespace AvaAdwaita.Controls;

/// <summary>
/// A title bar (AdwHeaderBar): widgets at the start and end, a centered title, and the window buttons in the user's
/// layout. Without <see cref="TitleWidget"/> the title is its window's. Usually the top bar of a
/// toolbar view; dragging it moves the window, and a double-click maximizes it.
/// </summary>
public class HeaderBar : TemplatedControl
{
    /// <summary>Defines <see cref="TitleWidget"/>.</summary>
    public static readonly StyledProperty<object?> TitleWidgetProperty =
        AvaloniaProperty.Register<HeaderBar, object?>(nameof(TitleWidget));

    /// <summary>Defines <see cref="ShowTitle"/>.</summary>
    public static readonly StyledProperty<bool> ShowTitleProperty =
        AvaloniaProperty.Register<HeaderBar, bool>(nameof(ShowTitle), true);

    /// <summary>Defines <see cref="ShowStartTitleButtons"/>.</summary>
    public static readonly StyledProperty<bool> ShowStartTitleButtonsProperty =
        AvaloniaProperty.Register<HeaderBar, bool>(nameof(ShowStartTitleButtons), true);

    /// <summary>Defines <see cref="ShowEndTitleButtons"/>.</summary>
    public static readonly StyledProperty<bool> ShowEndTitleButtonsProperty =
        AvaloniaProperty.Register<HeaderBar, bool>(nameof(ShowEndTitleButtons), true);

    /// <summary>Defines <see cref="CenteringPolicy"/>.</summary>
    public static readonly StyledProperty<CenteringPolicy> CenteringPolicyProperty =
        AvaloniaProperty.Register<HeaderBar, CenteringPolicy>(nameof(CenteringPolicy));

    internal static readonly DirectProperty<HeaderBar, bool> StrictCenteringProperty =
        AvaloniaProperty.RegisterDirect<HeaderBar, bool>(nameof(StrictCentering), h => h.StrictCentering);

    internal static readonly DirectProperty<HeaderBar, bool> ExpandsTitleProperty =
        AvaloniaProperty.RegisterDirect<HeaderBar, bool>(nameof(ExpandsTitle), h => h.ExpandsTitle);

    internal static readonly DirectProperty<HeaderBar, string?> WindowTitleProperty =
        AvaloniaProperty.RegisterDirect<HeaderBar, string?>(nameof(WindowTitle), h => h.WindowTitle);

    internal static readonly DirectProperty<HeaderBar, bool> HasBackProperty =
        AvaloniaProperty.RegisterDirect<HeaderBar, bool>(nameof(HasBack), h => h.HasBack);

    internal static readonly DirectProperty<HeaderBar, bool> StartButtonsShownProperty =
        AvaloniaProperty.RegisterDirect<HeaderBar, bool>(nameof(StartButtonsShown), h => h.StartButtonsShown);

    internal static readonly DirectProperty<HeaderBar, bool> EndButtonsShownProperty =
        AvaloniaProperty.RegisterDirect<HeaderBar, bool>(nameof(EndButtonsShown), h => h.EndButtonsShown);

    internal static readonly DirectProperty<HeaderBar, bool> InDialogProperty =
        AvaloniaProperty.RegisterDirect<HeaderBar, bool>(nameof(InDialog), h => h.InDialog);

    internal static readonly DirectProperty<HeaderBar, bool> HasStartProperty =
        AvaloniaProperty.RegisterDirect<HeaderBar, bool>(nameof(HasStart), h => h.HasStart);

    internal static readonly DirectProperty<HeaderBar, bool> HasEndProperty =
        AvaloniaProperty.RegisterDirect<HeaderBar, bool>(nameof(HasEnd), h => h.HasEnd);

    private readonly Avalonia.Controls.Controls _endShown = new();
    private string? _windowTitle;
    private bool _strictCentering;
    private bool _expandsTitle;
    private bool _hasStart;
    private bool _hasEnd;
    private Window? _window;
    private NavigationPage? _titlePage;
    private Dialog? _dialog;
    private BottomSheet? _sheet;
    private bool _inDialog;
    private bool _hasBack;
    private bool _startButtonsShown = true;
    private bool _endButtonsShown = true;
    private (NavigationView View, NavigationPage Page)[] _navigation = [];
    private (Control SplitView, bool IsSidebar)[] _splitViews = [];
    private NavigationView? _backView;
    private NavigationPage? _backPage;

    /// <summary>Creates a header bar.</summary>
    public HeaderBar()
    {
        Start.CollectionChanged += (_, e) =>
        {
            WatchVisibility(e);
            UpdateHasParts();
        };
        End.CollectionChanged += OnEndChanged;
    }

    /// <summary>Widgets at the start, from the start.</summary>
    public Avalonia.Controls.Controls Start { get; } = new();

    /// <summary>Widgets at the end, from the end: the first is the last on the bar, as adw_header_bar_pack_end.</summary>
    public Avalonia.Controls.Controls End { get; } = new();

    /// <summary>Replaces the window's title in the middle, as a <see cref="WindowTitle"/> or a view switcher.</summary>
    public object? TitleWidget
    {
        get => GetValue(TitleWidgetProperty);
        set => SetValue(TitleWidgetProperty, value);
    }

    /// <summary>Shows the title or <see cref="TitleWidget"/>.</summary>
    public bool ShowTitle
    {
        get => GetValue(ShowTitleProperty);
        set => SetValue(ShowTitleProperty, value);
    }

    /// <summary>Shows the window buttons the layout puts at the start.</summary>
    public bool ShowStartTitleButtons
    {
        get => GetValue(ShowStartTitleButtonsProperty);
        set => SetValue(ShowStartTitleButtonsProperty, value);
    }

    /// <summary>Shows the window buttons the layout puts at the end.</summary>
    public bool ShowEndTitleButtons
    {
        get => GetValue(ShowEndTitleButtonsProperty);
        set => SetValue(ShowEndTitleButtonsProperty, value);
    }

    /// <summary>How the title is centered between the sides.</summary>
    public CenteringPolicy CenteringPolicy
    {
        get => GetValue(CenteringPolicyProperty);
        set => SetValue(CenteringPolicyProperty, value);
    }

    internal bool StrictCentering
    {
        get => _strictCentering;
        private set => SetAndRaise(StrictCenteringProperty, ref _strictCentering, value);
    }

    /// <summary>Whether the title widget takes the space the sides leave: one marked <see cref="Presenters.AdwStack.ExpandProperty"/>.</summary>
    internal bool ExpandsTitle
    {
        get => _expandsTitle;
        private set => SetAndRaise(ExpandsTitleProperty, ref _expandsTitle, value);
    }

    internal string? WindowTitle
    {
        get => _windowTitle;
        private set => SetAndRaise(WindowTitleProperty, ref _windowTitle, value);
    }

    /// <summary>Whether the bar is in a dialog, whose close button its window controls are.</summary>
    internal bool InDialog
    {
        get => _inDialog;
        private set => SetAndRaise(InDialogProperty, ref _inDialog, value);
    }

    internal bool HasBack
    {
        get => _hasBack;
        private set => SetAndRaise(HasBackProperty, ref _hasBack, value);
    }

    internal bool StartButtonsShown
    {
        get => _startButtonsShown;
        private set => SetAndRaise(StartButtonsShownProperty, ref _startButtonsShown, value);
    }

    internal bool EndButtonsShown
    {
        get => _endButtonsShown;
        private set => SetAndRaise(EndButtonsShownProperty, ref _endButtonsShown, value);
    }

    internal bool HasStart
    {
        get => _hasStart;
        private set => SetAndRaise(HasStartProperty, ref _hasStart, value);
    }

    internal bool HasEnd
    {
        get => _hasEnd;
        private set => SetAndRaise(HasEndProperty, ref _hasEnd, value);
    }

    /// <summary>The end widgets in the order they show, from the start.</summary>
    internal Avalonia.Controls.Controls EndShown => _endShown;

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _window = TopLevel.GetTopLevel(this) as Window;
        if (_window is not null)
        {
            _window.PropertyChanged += OnWatchedPropertyChanged;
        }

        // adw_header_bar_root: the navigation page the bar is in names it, and the split views it is in hide the window
        // buttons beside the other pane.
        _titlePage = this.FindAncestorOfType<NavigationPage>();
        if (_titlePage is not null)
        {
            _titlePage.PropertyChanged += OnWatchedPropertyChanged;
        }

        _dialog = this.FindAncestorOfType<Dialog>();
        if (_dialog is not null)
        {
            _dialog.PropertyChanged += OnWatchedPropertyChanged;
        }

        // find_sheet: a bottom sheet whose sheet the bar is in; its close button closes the sheet.
        _sheet = this.GetVisualAncestors().OfType<BottomSheet>().FirstOrDefault(s => s.SheetNode.IsVisualAncestorOf(this));
        if (_sheet is not null)
        {
            _sheet.PropertyChanged += OnWatchedPropertyChanged;
        }

        InDialog = _dialog is not null || _sheet is not null;

        // adw_back_button_root: each navigation view the bar is in, through its pages.
        var navigation = new List<(NavigationView, NavigationPage)>();
        for (var page = _titlePage; page is not null; page = (page.GetVisualParent() as Visual)?.FindAncestorOfType<NavigationPage>())
        {
            if (page.GetVisualParent() is NavigationView view)
            {
                navigation.Add((view, page));
                view.PropertyChanged += OnWatchedPropertyChanged;
                if (!ReferenceEquals(page, _titlePage))
                {
                    page.PropertyChanged += OnWatchedPropertyChanged;
                }
            }
        }

        _navigation = [.. navigation];
        var splitViews = new List<(Control, bool)>();
        foreach (var ancestor in this.GetVisualAncestors())
        {
            if (ancestor is OverlaySplitView overlay)
            {
                splitViews.Add((overlay, overlay.IsInSidebar(this)));
                overlay.PropertyChanged += OnWatchedPropertyChanged;
            }
            else if (ancestor is NavigationSplitView split)
            {
                splitViews.Add((split, split.IsInSidebar(this)));
                split.PropertyChanged += OnWatchedPropertyChanged;
            }
        }

        _splitViews = [.. splitViews];
        UpdateTitle();
        UpdateBack();
        UpdateTitleButtons();
    }

    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (_window is not null)
        {
            _window.PropertyChanged -= OnWatchedPropertyChanged;
            _window = null;
        }

        if (_titlePage is not null)
        {
            _titlePage.PropertyChanged -= OnWatchedPropertyChanged;
            _titlePage = null;
        }

        if (_dialog is not null)
        {
            _dialog.PropertyChanged -= OnWatchedPropertyChanged;
            _dialog = null;
        }

        if (_sheet is not null)
        {
            _sheet.PropertyChanged -= OnWatchedPropertyChanged;
            _sheet = null;
        }

        foreach (var (view, page) in _navigation)
        {
            view.PropertyChanged -= OnWatchedPropertyChanged;
            page.PropertyChanged -= OnWatchedPropertyChanged;
        }

        foreach (var (split, _) in _splitViews)
        {
            split.PropertyChanged -= OnWatchedPropertyChanged;
        }

        _navigation = [];
        _splitViews = [];
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ShowStartTitleButtonsProperty || change.Property == ShowEndTitleButtonsProperty)
        {
            UpdateTitleButtons();
        }
        else if (change.Property == CenteringPolicyProperty)
        {
            StrictCentering = CenteringPolicy == CenteringPolicy.Strict;
        }
        else if (change.Property == TitleWidgetProperty)
        {
            ExpandsTitle = TitleWidget is Control c && Presenters.AdwStack.GetExpand(c);
        }
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (e.NameScope.Find<Button>("PART_Back") is { } back)
        {
            back.Click += (_, _) => GoBack();
        }
    }

    /// <summary>Pops the page the back button goes back from.</summary>
    internal void GoBack()
    {
        if (_backView is { } view && _backPage is { } page && view.GetPreviousPage(page) is { } previous)
        {
            view.PopTo(previous);
        }
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new Automation.HeaderBarAutomationPeer(this);

    private void OnWatchedPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Window.TitleProperty || e.Property == NavigationPage.TitleProperty || e.Property == Dialog.TitleProperty
            || e.Property == BottomSheet.ShowDragHandleProperty)
        {
            UpdateTitle();
        }
        else if (e.Property == NavigationView.VisiblePageProperty || e.Property == NavigationPage.CanPopProperty)
        {
            UpdateBack();
        }
        else if (e.Property == OverlaySplitView.IsCollapsedProperty || e.Property == OverlaySplitView.ShowSidebarProperty
            || e.Property == OverlaySplitView.SidebarPositionProperty || e.Property == NavigationSplitView.IsCollapsedProperty
            || e.Property == NavigationSplitView.SidebarPositionProperty)
        {
            UpdateTitleButtons();
        }
    }

    // update_title: none under a bottom sheet's drag handle, else the navigation page's title, the dialog's, or the window's.
    private void UpdateTitle() => WindowTitle = _sheet is { ShowDragHandle: true } ? null : _titlePage?.Title ?? _dialog?.Title ?? _window?.Title;

    // update_page in adw-back-button.c: the innermost navigation view with a page to go back to, unless a page on the
    // way cannot be popped.
    private void UpdateBack()
    {
        _backView = null;
        _backPage = null;
        foreach (var (view, page) in _navigation)
        {
            if (!page.CanPop)
            {
                break;
            }

            if (view.GetPreviousPage(page) is not null)
            {
                (_backView, _backPage) = (view, page);
                break;
            }
        }

        HasBack = _backView is not null;
    }

    // update_start_title_buttons and update_end_title_buttons: in an expanded split view, each pane keeps the window
    // buttons on its outer side.
    private void UpdateTitleButtons()
    {
        var start = ShowStartTitleButtons;
        var end = ShowEndTitleButtons;
        foreach (var (view, isSidebar) in _splitViews)
        {
            if (view is NavigationSplitView split)
            {
                var atStart = split.SidebarPosition == PackType.Start;
                start &= split.IsCollapsed || (isSidebar ? atStart : !atStart);
                end &= split.IsCollapsed || (isSidebar ? !atStart : atStart);
            }
            else if (view is OverlaySplitView overlay)
            {
                var atStart = overlay.SidebarPosition == PackType.Start;
                var covered = overlay.IsCollapsed || !overlay.ShowSidebar;
                start &= isSidebar ? atStart : covered || !atStart;
                end &= isSidebar ? !atStart : covered || atStart;
            }
        }

        StartButtonsShown = start;
        EndButtonsShown = end;
    }


    private void OnEndChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        WatchVisibility(e);
        _endShown.Clear();
        _endShown.AddRange(End.Reverse().ToList());
        UpdateHasParts();
    }

    // update_box_visibility: a side's box shows while one of its children does.
    private void UpdateHasParts()
    {
        HasStart = Start.Any(c => c.IsVisible);
        HasEnd = End.Any(c => c.IsVisible);
    }

    private void WatchVisibility(NotifyCollectionChangedEventArgs e)
    {
        foreach (var c in e.OldItems?.OfType<Control>() ?? [])
        {
            c.PropertyChanged -= OnPartPropertyChanged;
        }

        foreach (var c in e.NewItems?.OfType<Control>() ?? [])
        {
            c.PropertyChanged += OnPartPropertyChanged;
        }
    }

    private void OnPartPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == IsVisibleProperty)
        {
            UpdateHasParts();
        }
    }
}
