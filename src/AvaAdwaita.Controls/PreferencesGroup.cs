using System.Collections;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Metadata;
using AvaAdwaita.Nodes;

namespace AvaAdwaita.Controls;

/// <summary>
/// A titled group of rows (AdwPreferencesGroup): a title, a description and a header suffix above a boxed list. The
/// rows among <see cref="Children"/> go in the list, and anything else goes below it. <see cref="ItemsSource"/> fills
/// the list from data instead, with <see cref="ItemTemplate"/> making each row.
/// </summary>
public class PreferencesGroup : TemplatedControl
{
    /// <summary>Defines <see cref="Title"/>.</summary>
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<PreferencesGroup, string?>(nameof(Title));

    /// <summary>Defines <see cref="Description"/>.</summary>
    public static readonly StyledProperty<string?> DescriptionProperty =
        AvaloniaProperty.Register<PreferencesGroup, string?>(nameof(Description));

    /// <summary>Defines <see cref="HeaderSuffix"/>.</summary>
    public static readonly StyledProperty<object?> HeaderSuffixProperty =
        AvaloniaProperty.Register<PreferencesGroup, object?>(nameof(HeaderSuffix));

    /// <summary>Defines <see cref="SeparateRows"/>.</summary>
    public static readonly StyledProperty<bool> SeparateRowsProperty =
        AvaloniaProperty.Register<PreferencesGroup, bool>(nameof(SeparateRows));

    /// <summary>Defines <see cref="ItemsSource"/>.</summary>
    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        ItemsControl.ItemsSourceProperty.AddOwner<PreferencesGroup>();

    /// <summary>Defines <see cref="ItemTemplate"/>.</summary>
    public static readonly StyledProperty<IDataTemplate?> ItemTemplateProperty =
        ItemsControl.ItemTemplateProperty.AddOwner<PreferencesGroup>();

    private readonly AvaloniaList<object> _rows = new();
    private readonly Avalonia.Controls.Controls _others = new();
    private ListBox? _list;
    private Control? _header;
    private Control? _labels;
    private Panel? _othersHost;

    /// <summary>Creates a group.</summary>
    public PreferencesGroup()
    {
        Children.CollectionChanged += OnChildrenChanged;
    }

    /// <summary>The title, above the list.</summary>
    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>The description, under the title.</summary>
    public string? Description
    {
        get => GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    /// <summary>A widget after the title and description, as a button or a spinner for the whole group.</summary>
    public object? HeaderSuffix
    {
        get => GetValue(HeaderSuffixProperty);
        set => SetValue(HeaderSuffixProperty, value);
    }

    /// <summary>Separates the rows, as the <c>boxed-list-separate</c> class.</summary>
    public bool SeparateRows
    {
        get => GetValue(SeparateRowsProperty);
        set => SetValue(SeparateRowsProperty, value);
    }

    /// <summary>Data for the rows, instead of the rows in <see cref="Children"/>.</summary>
    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>Makes a row for each item of <see cref="ItemsSource"/>.</summary>
    public IDataTemplate? ItemTemplate
    {
        get => GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    /// <summary>The rows, and any other widgets, which go under the list.</summary>
    [Content]
    public Avalonia.Controls.Controls Children { get; } = new();

    /// <summary>The list that holds the rows.</summary>
    internal ListBox? List => _list;

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        ClearOthers();
        if (_list is not null)
        {
            _list.ItemsSource = null;
        }

        _list = e.NameScope.Find<ListBox>("PART_List");
        _header = e.NameScope.Find<Control>("PART_Header");
        _labels = e.NameScope.Find<Control>("PART_Labels");
        _othersHost = e.NameScope.Find<Panel>("PART_Others");
        _othersHost?.Children.AddRange(_others);
        UpdateList();
        UpdateHeader();
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TitleProperty || change.Property == DescriptionProperty || change.Property == HeaderSuffixProperty)
        {
            UpdateHeader();
        }
        else if (change.Property == SeparateRowsProperty || change.Property == ItemsSourceProperty || change.Property == ItemTemplateProperty)
        {
            UpdateList();
        }
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new Automation.PreferencesGroupAutomationPeer(this);

    private void OnChildrenChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Rows go in the list, anything else under it (adw_preferences_group_add).
        ClearOthers();
        _rows.Clear();
        _others.Clear();
        foreach (var c in Children)
        {
            if (c is ListBoxItem)
            {
                _rows.Add(c);
            }
            else
            {
                _others.Add(c);
            }
        }

        _othersHost?.Children.AddRange(_others);
        UpdateList();
    }

    // The list shares its box with the other children, after it.
    private void ClearOthers()
    {
        foreach (var c in _others)
        {
            _othersHost?.Children.Remove(c);
        }
    }

    private void UpdateList()
    {
        if (_list is null)
        {
            return;
        }

        _list.Classes.Set("boxed-list", !SeparateRows);
        _list.Classes.Set("boxed-list-separate", SeparateRows);
        _list.ItemTemplate = ItemTemplate;
        _list.ItemsSource = ItemsSource ?? _rows;
        _list.IsVisible = _list.ItemCount > 0 || (ItemsSource ?? _rows).GetEnumerator().MoveNext();
    }

    // update_header_visibility in adw-preferences-group.c.
    private void UpdateHeader()
    {
        if (_header is null)
        {
            return;
        }

        var title = !string.IsNullOrEmpty(Title);
        var description = !string.IsNullOrEmpty(Description);
        var suffix = HeaderSuffix is not null;
        _header.IsVisible = title || description || suffix;
        if (_labels is not null)
        {
            _labels.IsVisible = title || description;
        }

        var singleLine = !description && (suffix || title);
        Css.SetClasses(_header, singleLine ? "header horizontal single-line" : "header horizontal");
    }
}
