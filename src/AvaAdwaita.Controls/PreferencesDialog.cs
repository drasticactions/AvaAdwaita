using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaAdwaita.Controls.Primitives;
using AvaAdwaita.Nodes;
using AvaAdwaita.Presenters;

namespace AvaAdwaita.Controls;

/// <summary>
/// A dialog of preferences pages (AdwPreferencesDialog): a view switcher in the header bar, or the title with a view
/// switcher bar at the bottom when the dialog is narrow, over the visible page. With <see cref="SearchEnabled"/>, a search
/// button (or typing) searches the pages' rows by title and subtitle. <see cref="PushSubpage"/> shows a page of its own.
/// </summary>
public class PreferencesDialog : Dialog
{
    /// <summary>Defines <see cref="SearchEnabled"/>.</summary>
    public static readonly StyledProperty<bool> SearchEnabledProperty =
        AvaloniaProperty.Register<PreferencesDialog, bool>(nameof(SearchEnabled));

    /// <summary>Defines <see cref="VisiblePage"/>.</summary>
    public static readonly DirectProperty<PreferencesDialog, PreferencesPage?> VisiblePageProperty =
        AvaloniaProperty.RegisterDirect<PreferencesDialog, PreferencesPage?>(nameof(VisiblePage), d => d.VisiblePage, (d, v) => d.VisiblePage = v);

    // The view switcher gives way to the title below 110pt a page, or 400 wide.
    private const double ViewSwitcherPageThreshold = 110 * 96 / 72.0;
    private const double ViewSwitcherFallbackThreshold = 400;

    private readonly NavigationView _subpages = new() { };
    private readonly NavigationPage _main = new();
    private readonly AdwBox _breakpointBin = new() { MinWidth = 360, MinHeight = 150 };
    private readonly HeaderBar _header = new() { CenteringPolicy = CenteringPolicy.Strict };
    private readonly StackBox _titleStack = new();
    private readonly StackBox _switcherStack = new();
    private readonly ViewSwitcher _switcher = new() { Policy = ViewSwitcherPolicy.Wide, HorizontalAlignment = HorizontalAlignment.Center };
    private readonly WindowTitle _title = new();
    private readonly Clamp _searchClamp = new() { MaximumSize = 400, TighteningThreshold = 300 };
    private readonly SearchEntry _searchEntry = new();
    private readonly ToggleButton _searchButton = new() { VerticalAlignment = VerticalAlignment.Center, IsVisible = false };
    private readonly StackBox _contentStack = new();
    private readonly ToolbarView _pagesView = new();
    private readonly ViewStack _pagesStack = new();
    private readonly ViewSwitcherBar _switcherBar = new();
    private readonly StackBox _searchStack = new();
    private readonly PreferencesPage _resultsPage = new();
    private readonly ListBox _results = new() { Classes = { "boxed-list" }, VerticalAlignment = VerticalAlignment.Top };
    private readonly StatusPage _noResults = new() { IconName = "edit-find-symbolic" };
    private bool _narrow;
    private PreferencesPage? _visiblePage;

    static PreferencesDialog()
    {
        CssNodeProviders.Register<PreferencesDialog>(_ => ["preferences"]);
        ContentWidthProperty.OverrideDefaultValue<PreferencesDialog>(640);
    }

    /// <summary>Creates a preferences dialog.</summary>
    public PreferencesDialog()
    {
        this[!TitleProperty] = this.GetResourceObservable("AdwStringPreferences").ToBinding();
        _title.Bind(WindowTitle.TitleProperty, this.GetObservable(TitleProperty));
        _main.Bind(NavigationPage.TitleProperty, this.GetObservable(TitleProperty));
        _searchEntry[!TextBox.PlaceholderTextProperty] = _searchEntry.GetResourceObservable("AdwStringSearchPreferences").ToBinding();
        _searchButton[!ToolTip.TipProperty] = _searchButton.GetResourceObservable("AdwStringSearch").ToBinding();
        _noResults[!StatusPage.TitleProperty] = _noResults.GetResourceObservable("AdwStringNoResultsFound").ToBinding();
        _noResults[!StatusPage.DescriptionProperty] = _noResults.GetResourceObservable("AdwStringTryADifferentSearch").ToBinding();
        var searchIcon = new AdwIcon { IconName = "edit-find-symbolic" };
        Css.SetNode(searchIcon, "image");
        _searchButton.Content = searchIcon;

        // The header bar: the view switcher or the title, or the search entry while searching.
        _switcher.Stack = _pagesStack;
        _switcherStack.Children.AddRange([_switcher, _title]);
        _searchClamp.Child = _searchEntry;
        _titleStack.Children.AddRange([_switcherStack, _searchClamp]);
        AdwStack.SetExpand(_titleStack, true);
        _header.TitleWidget = _titleStack;
        _header.Start.Add(_searchButton);

        // The content: the pages over the view switcher bar, or the search results.
        _switcherBar.Stack = _pagesStack;
        _pagesView.Content = _pagesStack;
        _pagesView.BottomBars.Add(_switcherBar);
        _resultsPage.Children.Add(new PreferencesGroup { Children = { _results } });
        _searchStack.Children.AddRange([_resultsPage, _noResults]);
        _contentStack.Children.AddRange([_pagesView, _searchStack]);
        _breakpointBin.Child = new ToolbarView { TopBars = { _header }, Content = _contentStack };
        Css.SetNode(_breakpointBin, "widget");
        _main.Content = _breakpointBin;
        _subpages.Pages.Add(_main);
        var overlay = new AdwBox { Child = _subpages };
        Css.SetNode(overlay, "toastoverlay");
        Content = overlay;

        _searchButton.IsCheckedChanged += (_, _) => OnSearchToggled();
        _searchEntry.SearchChanged += (_, _) => UpdateResults();
        _searchEntry.StopSearch += (_, _) => _searchButton.IsChecked = false;
        _searchEntry.Activated += (_, _) => (_results.ContainerFromIndex(0) as Control)?.Focus(NavigationMethod.Tab);
        _results.AddHandler(PreferencesRow.ActivatedEvent, OnResultActivated);
        _pagesStack.PropertyChanged += (_, e) =>
        {
            if (e.Property == ViewStack.VisiblePageProperty)
            {
                VisiblePage = _pagesStack.VisiblePage?.Content as PreferencesPage;
            }
        };
        _breakpointBin.PropertyChanged += (_, e) =>
        {
            if (e.Property == BoundsProperty)
            {
                UpdateViewSwitcher();
            }
        };
        Pages.CollectionChanged += OnPagesChanged;
        AddHandler(TextInputEvent, OnTypeToSearch, RoutingStrategies.Tunnel);
    }

    /// <summary>The pages, each in the view switcher by its title and icon.</summary>
    public AvaloniaList<PreferencesPage> Pages { get; } = new();

    /// <summary>Whether the search button shows and typing searches.</summary>
    public bool SearchEnabled
    {
        get => GetValue(SearchEnabledProperty);
        set => SetValue(SearchEnabledProperty, value);
    }

    /// <summary>The page that shows.</summary>
    public PreferencesPage? VisiblePage
    {
        get => _visiblePage;
        set
        {
            if (SetAndRaise(VisiblePageProperty, ref _visiblePage, value) && value is not null
                && _pagesStack.Pages.FirstOrDefault(p => ReferenceEquals(p.Content, value)) is { } page)
            {
                _pagesStack.VisiblePage = page;
            }
        }
    }

    /// <summary>Adds a page.</summary>
    public void Add(PreferencesPage page) => Pages.Add(page);

    /// <summary>Removes a page.</summary>
    public void Remove(PreferencesPage page) => Pages.Remove(page);

    /// <summary>Shows <paramref name="page"/> over the pages, with a back button to them.</summary>
    public void PushSubpage(NavigationPage page) => _subpages.Push(page);

    /// <summary>Goes back from a subpage; whether there was one.</summary>
    public bool PopSubpage() => _subpages.NavigationStack.Count > 1 && _subpages.Pop();

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new Automation.PreferencesDialogAutomationPeer(this);

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SearchEnabledProperty)
        {
            _searchButton.IsVisible = SearchEnabled;
            if (!SearchEnabled)
            {
                _searchButton.IsChecked = false;
            }
        }
    }

    private void OnPagesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Each page goes in the view stack, which shows its title and icon.
        foreach (var page in _pagesStack.Pages.Where(p => p.Content is not PreferencesPage content || !Pages.Contains(content)).ToList())
        {
            page.Content = null;
            _pagesStack.Pages.Remove(page);
        }

        for (var i = 0; i < Pages.Count; i++)
        {
            var page = Pages[i];
            if (_pagesStack.Pages.FirstOrDefault(p => ReferenceEquals(p.Content, page)) is { } existing)
            {
                continue;
            }

            var stackPage = new ViewStackPage { Content = page, Name = page.Name };
            stackPage.Bind(ViewStackPage.TitleProperty, page.GetObservable(PreferencesPage.TitleProperty));
            stackPage.Bind(ViewStackPage.IconNameProperty, page.GetObservable(PreferencesPage.IconNameProperty));
            stackPage.Bind(ViewStackPage.IsPageVisibleProperty, page.GetObservable(IsVisibleProperty));
            _pagesStack.Pages.Insert(i, stackPage);
        }

        UpdateViewSwitcher();
    }

    private int VisiblePageCount => _pagesStack.Pages.Count(p => p.IsPageVisible);

    // update_view_switcher: below the breakpoint, or with one page, the title shows and the bar reveals.
    private void UpdateViewSwitcher()
    {
        var width = _breakpointBin.Bounds.Width;
        _narrow = width > 0 && (width <= ViewSwitcherPageThreshold * Math.Max(1, VisiblePageCount) || width <= ViewSwitcherFallbackThreshold);
        _switcherBar.Reveal = _narrow;
        _switcherStack.VisibleChild = !_narrow && VisiblePageCount > 1 ? _switcher : _title;
    }

    private void OnSearchToggled()
    {
        var searching = _searchButton.IsChecked == true;
        _titleStack.VisibleChild = searching ? _searchClamp : _switcherStack;
        _contentStack.VisibleChild = searching ? _searchStack : _pagesView;
        if (searching)
        {
            UpdateResults();
            Dispatcher.UIThread.Post(
                () =>
                {
                    _searchEntry.Focus();
                    _searchEntry.CaretIndex = _searchEntry.Text?.Length ?? 0;
                },
                DispatcherPriority.Loaded);
        }
        else
        {
            _searchEntry.Text = string.Empty;
        }
    }

    // search_open_cb and the entry's key capture: typing starts a search.
    private void OnTypeToSearch(object? sender, TextInputEventArgs e)
    {
        if (!SearchEnabled || _searchButton.IsChecked == true || e.Source is TextBox || string.IsNullOrEmpty(e.Text) || char.IsControl(e.Text[0]))
        {
            return;
        }

        e.Handled = true;
        _searchEntry.Text = e.Text;
        _searchButton.IsChecked = true;
    }

    private IEnumerable<(PreferencesPage Page, PreferencesGroup Group, PreferencesRow Row)> Rows() =>
        from page in _pagesStack.Pages.Where(p => p.IsPageVisible).Select(p => p.Content).OfType<PreferencesPage>()
        from @group in page.Children.OfType<PreferencesGroup>().Where(g => g.IsVisible)
        from row in @group.Children.OfType<PreferencesRow>().Concat(@group.List?.GetRealizedContainers().OfType<PreferencesRow>() ?? []).Distinct()
        select (page, @group, row);

    // filter_search_results: the rows whose title, or an action row's subtitle, holds the terms.
    private static bool Matches(PreferencesRow row, string terms)
    {
        static string Comparable(string? text, bool underline) =>
            (underline ? StripMnemonic(text ?? string.Empty) : text ?? string.Empty).ToLower(CultureInfo.CurrentCulture);

        var t = terms.ToLower(CultureInfo.CurrentCulture);
        return Comparable(row.Title, row.UseUnderline).Contains(t, StringComparison.Ordinal)
            || (row is ActionRow action && Comparable(action.Subtitle, false).Contains(t, StringComparison.Ordinal));
    }

    // adw_strip_mnemonic: a lone underscore goes, a doubled one stays single.
    private static string StripMnemonic(string text)
    {
        var builder = new System.Text.StringBuilder(text.Length);
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == '_' && i + 1 < text.Length)
            {
                i++;
            }

            builder.Append(text[i]);
        }

        return builder.ToString();
    }

    private void UpdateResults()
    {
        var terms = _searchEntry.Text ?? string.Empty;
        var multiple = VisiblePageCount > 1;
        var results = new List<ActionRow>();
        foreach (var (page, group, row) in Rows().Where(r => Matches(r.Row, terms)))
        {
            // create_search_row_subtitle: the group, after its page when there are several.
            string? subtitle = group.Title is { Length: > 0 } groupTitle
                ? (multiple ? $"{(page.Title is { Length: > 0 } p ? p : this.FindResource("AdwStringUntitledPage") as string)} → {groupTitle}" : groupTitle)
                : page.Title is { Length: > 0 } pageTitle ? pageTitle : null;
            var result = new ActionRow { Title = row.Title, UseUnderline = row.UseUnderline, Subtitle = subtitle, IsActivatable = true, Tag = (page, row) };
            results.Add(result);
        }

        _results.ItemsSource = results;
        _searchStack.VisibleChild = results.Count > 0 ? _resultsPage : _noResults;
    }

    // search_result_activated_cb: the search ends and the row's page shows, with the row focused.
    private void OnResultActivated(object? sender, RoutedEventArgs e)
    {
        if (e.Source is not ActionRow { Tag: (PreferencesPage page, PreferencesRow row) })
        {
            return;
        }

        _searchButton.IsChecked = false;
        VisiblePage = page;
        Dispatcher.UIThread.Post(() => row.Focus(NavigationMethod.Tab), DispatcherPriority.Loaded);
    }
}
