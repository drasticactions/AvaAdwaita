using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using AvaAdwaita.Controls.Primitives;
using AvaAdwaita.Nodes;
using AvaAdwaita.Presenters;

namespace AvaAdwaita.Controls;

/// <summary>
/// A dialog listing an application's keyboard shortcuts (AdwShortcutsDialog): each <see cref="ShortcutsSection"/> is a
/// group of rows, under a search entry that finds shortcuts by title and subtitle. With at least four titled sections
/// and twenty shortcuts, buttons at the top show one section at a time. Escape unselects the section, ends the search,
/// or closes the dialog; Ctrl+F and typing search.
/// </summary>
public class ShortcutsDialog : Dialog
{
    // N_MIN_SECTIONS and N_MIN_SHORTCUTS.
    private const int MinSections = 4;
    private const int MinShortcuts = 20;

    private readonly SearchEntry _searchEntry = new();
    private readonly Clamp _searchClamp = new() { MaximumSize = 400 };
    private readonly StackBox _stack = new();
    private readonly PreferencesPage _contents = new();
    private readonly PreferencesGroup _navGroup = new();
    private readonly WrapBox _navBox = new() { HorizontalAlignment = HorizontalAlignment.Center, ChildSpacing = 6, LineSpacing = 6, Align = 0.5 };
    private readonly PreferencesPage _search = new();
    private readonly ListBox _searchList = new() { Classes = { "boxed-list" } };
    private readonly StatusPage _empty = new() { IconName = "edit-find-symbolic" };
    private readonly List<(ShortcutsSection Section, PreferencesGroup Group)> _groups = [];
    private ShortcutsSection? _selectedSection;
    private ToggleButton? _selectedButton;

    static ShortcutsDialog()
    {
        CssNodeProviders.Register<ShortcutsDialog>(_ => ["shortcuts"]);
        ContentWidthProperty.OverrideDefaultValue<ShortcutsDialog>(600);
    }

    /// <summary>Creates a shortcuts dialog.</summary>
    public ShortcutsDialog()
    {
        this[!TitleProperty] = this.GetResourceObservable("AdwStringKeyboardShortcuts").ToBinding();
        _searchEntry[!TextBox.PlaceholderTextProperty] = _searchEntry.GetResourceObservable("AdwStringSearchShortcuts").ToBinding();
        _searchEntry[!Avalonia.Automation.AutomationProperties.NameProperty] = _searchEntry.GetResourceObservable("AdwStringSearchShortcuts").ToBinding();
        _empty[!StatusPage.TitleProperty] = _empty.GetResourceObservable("AdwStringNoResultsFound").ToBinding();
        _empty[!StatusPage.DescriptionProperty] = _empty.GetResourceObservable("AdwStringTryADifferentSearch").ToBinding();

        // adw-shortcuts-dialog.ui: the search entry in the header bar, over the sections, the results, or no results.
        _searchClamp.Child = _searchEntry;
        AdwStack.SetExpand(_searchClamp, true);
        var header = new HeaderBar { TitleWidget = _searchClamp };
        Css.SetClasses(_navBox, "navigation-box");
        _navGroup.Children.Add(_navBox);
        _navGroup.IsVisible = false;
        Css.SetClasses(_contents, "contents");
        _contents.Children.Add(_navGroup);
        _search.Children.Add(new PreferencesGroup { Children = { _searchList } });
        _stack.Children.AddRange([_contents, _search, _empty]);
        Content = new ToolbarView { TopBars = { header }, Content = _stack };

        _searchEntry.SearchChanged += (_, _) => UpdateStack();
        _searchEntry.StopSearch += (_, _) => OnStopSearch();
        _searchEntry.Activated += (_, _) => OnSearchActivated();
        _searchList.AddHandler(PreferencesRow.ActivatedEvent, OnSearchRowActivated);
        Sections.CollectionChanged += OnSectionsChanged;
        AddHandler(KeyDownEvent, OnEscapeCapture, RoutingStrategies.Tunnel);
        AddHandler(TextInputEvent, OnTypeToSearch, RoutingStrategies.Tunnel);
    }

    /// <summary>The sections, in order.</summary>
    public AvaloniaList<ShortcutsSection> Sections { get; } = new();

    /// <summary>Adds a section.</summary>
    public void Add(ShortcutsSection section) => Sections.Add(section);

    /// <summary>The search entry, for tests.</summary>
    internal SearchEntry SearchEntry => _searchEntry;

    /// <summary>What the dialog shows: the sections, the search results or no results, for tests.</summary>
    internal Control? VisibleChild => _stack.VisibleChild;

    /// <summary>The navigation buttons, for tests.</summary>
    internal IEnumerable<ToggleButton> NavigationButtons => _navBox.Children.OfType<ToggleButton>();

    /// <summary>Whether the navigation buttons show, for tests.</summary>
    internal bool ShowsNavigation => _navGroup.IsVisible;

    /// <summary>The sections' groups, for tests.</summary>
    internal IEnumerable<PreferencesGroup> Groups => _groups.Select(g => g.Group);

    /// <summary>The search results, for tests.</summary>
    internal IEnumerable<ShortcutRow> Results => _searchList.Items.OfType<ShortcutRow>();

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new Automation.ShortcutsDialogAutomationPeer(this);

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // adw_shortcuts_dialog_root: the height set before the content was there goes, for the content's.
        SetCurrentValue(ContentHeightProperty, double.NaN);
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == FlowDirectionProperty)
        {
            // The direction filter: shortcuts for the other direction go.
            Rebuild();
        }
    }

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        // Ctrl+F searches.
        if (!e.Handled && e.Key == Key.F && e.KeyModifiers == KeyModifiers.Control)
        {
            e.Handled = true;
            _searchEntry.Focus();
            _searchEntry.SelectAll();
            return;
        }

        base.OnKeyDown(e);
    }

    private void OnSectionsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        foreach (var section in e.OldItems?.OfType<ShortcutsSection>() ?? [])
        {
            section.PropertyChanged -= OnSectionPropertyChanged;
            section.Items.CollectionChanged -= OnItemsChanged;
        }

        foreach (var section in e.NewItems?.OfType<ShortcutsSection>() ?? [])
        {
            section.PropertyChanged += OnSectionPropertyChanged;
            section.Items.CollectionChanged += OnItemsChanged;
        }

        Rebuild();
    }

    private void OnSectionPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == ShortcutsSection.TitleProperty)
        {
            Rebuild();
        }
    }

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e) => Rebuild();

    private bool InDirection(ShortcutsItem item) => item.Direction is not { } d || d == FlowDirection;

    private IEnumerable<ShortcutsItem> AllItems => Sections.SelectMany(s => s.Items).Where(InDirection);

    // sections_changed_cb and titles_changed_cb: a group of rows per section, a navigation button per titled one.
    private void Rebuild()
    {
        foreach (var (_, group) in _groups)
        {
            _contents.Children.Remove(group);
        }

        _groups.Clear();
        foreach (var section in Sections)
        {
            var group = new PreferencesGroup { Title = section.Title };
            foreach (var item in section.Items.Where(InDirection))
            {
                group.Children.Add(new ShortcutRow(item));
            }

            _groups.Add((section, group));
            _contents.Children.Add(group);
        }

        _navBox.Children.Clear();
        _selectedButton = null;
        foreach (var section in Sections.Where(s => !string.IsNullOrEmpty(s.Title)))
        {
            var button = new ToggleButton { Content = section.Title, Tag = section };
            button.Click += (_, _) => OnNavButtonClicked(button);
            _navBox.Children.Add(button);
            if (section == _selectedSection)
            {
                button.IsChecked = true;
                _selectedButton = button;
            }
        }

        if (_selectedSection is not null && !Sections.Contains(_selectedSection))
        {
            _selectedSection = null;
        }

        SelectSection(_selectedSection);
        UpdateResults();
        UpdateStack();
    }

    // select_section: the section and the untitled ones after it, or every section.
    private void SelectSection(ShortcutsSection? section)
    {
        _selectedSection = section;
        var start = section is null ? -1 : Sections.IndexOf(section);
        var end = start;
        if (start >= 0)
        {
            while (end + 1 < Sections.Count && string.IsNullOrEmpty(Sections[end + 1].Title))
            {
                end++;
            }
        }

        for (var i = 0; i < _groups.Count; i++)
        {
            _groups[i].Group.IsVisible = section is null || (i >= start && i <= end);
        }
    }

    private void UnselectSection()
    {
        SelectSection(null);
        if (_selectedButton is not null)
        {
            _selectedButton.IsChecked = false;
            _selectedButton = null;
        }
    }

    // nav_button_clicked_cb: a button shows its section; clicking it again shows them all.
    private void OnNavButtonClicked(ToggleButton button)
    {
        var section = (ShortcutsSection)button.Tag!;
        if (_selectedSection != section)
        {
            SelectSection(section);
            if (_selectedButton is not null && _selectedButton != button)
            {
                _selectedButton.IsChecked = false;
            }

            button.IsChecked = true;
            _selectedButton = button;
        }
        else
        {
            UnselectSection();
        }
    }

    // update_nav_visibility.
    private void UpdateNavigation()
    {
        var manySections = Sections.Count(s => !string.IsNullOrEmpty(s.Title)) >= MinSections;
        var manyShortcuts = AllItems.Count() >= MinShortcuts;
        _navGroup.IsVisible = manySections && manyShortcuts;
        if (_stack.VisibleChild != _contents && _selectedSection is not null)
        {
            UnselectSection();
        }
    }

    // The title and subtitle filters: either holds the text, case aside.
    private static bool Matches(ShortcutsItem item, string text) =>
        (item.Title?.Contains(text, StringComparison.CurrentCultureIgnoreCase) ?? false)
        || (item.Subtitle?.Contains(text, StringComparison.CurrentCultureIgnoreCase) ?? false);

    private void UpdateResults()
    {
        var text = _searchEntry.Text ?? string.Empty;
        _searchList.ItemsSource = text.Length == 0
            ? null
            : AllItems.Where(i => Matches(i, text)).Select(i => new ShortcutRow(i) { IsActivatable = true }).ToList();
    }

    // update_stack.
    private void UpdateStack()
    {
        var text = _searchEntry.Text ?? string.Empty;
        UpdateResults();
        _stack.VisibleChild = text.Length == 0 ? _contents : _searchList.ItemCount > 0 ? _search : _empty;
        UpdateNavigation();
    }

    private void StopSearching()
    {
        _searchEntry.Text = string.Empty;
        UpdateStack();
    }

    // stop_search_cb: Escape ends the search, or closes the dialog.
    private void OnStopSearch()
    {
        if (_stack.VisibleChild != _contents)
        {
            StopSearching();
            return;
        }

        Close();
    }

    // search_activated_cb: Enter goes to the first result.
    private void OnSearchActivated()
    {
        if (_stack.VisibleChild == _search && _searchList.ContainerFromIndex(0) is Control row)
        {
            row.Focus(NavigationMethod.Tab);
        }
    }

    // search_row_activated_cb: the search ends, with the shortcut's row focused.
    private void OnSearchRowActivated(object? sender, RoutedEventArgs e)
    {
        if (e.Source is not ShortcutRow { Item: var item })
        {
            return;
        }

        StopSearching();
        var source = _groups.SelectMany(g => g.Group.Children.OfType<ShortcutRow>()).FirstOrDefault(r => r.Item == item);
        if (source is not null)
        {
            Dispatcher.UIThread.Post(() => source.Focus(NavigationMethod.Tab), DispatcherPriority.Loaded);
        }
    }

    // unselect_section_cb, in the capture phase: Escape first shows every section.
    private void OnEscapeCapture(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && e.KeyModifiers == KeyModifiers.None && _selectedSection is not null)
        {
            e.Handled = true;
            UnselectSection();
        }
    }

    // The entry's key capture: typing elsewhere in the dialog searches.
    private void OnTypeToSearch(object? sender, TextInputEventArgs e)
    {
        if (e.Source is TextBox || string.IsNullOrEmpty(e.Text) || char.IsControl(e.Text[0]))
        {
            return;
        }

        e.Handled = true;
        _searchEntry.Focus();
        _searchEntry.Text += e.Text;
        _searchEntry.CaretIndex = _searchEntry.Text.Length;
    }
}
