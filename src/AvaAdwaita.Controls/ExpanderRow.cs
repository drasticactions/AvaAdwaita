using System.Collections.Specialized;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Metadata;
using AvaAdwaita.CssEngine.Selectors;
using AvaAdwaita.Nodes;
using AvaAdwaita.Presenters;

namespace AvaAdwaita.Controls;

/// <summary>
/// A row that reveals nested rows (AdwExpanderRow): a header row with a title, a subtitle, prefixes, suffixes and an
/// arrow, which expands and collapses <see cref="Rows"/> when it is activated. With <see cref="ShowEnableSwitch"/> a
/// switch turns the expansion on and off.
/// </summary>
[PseudoClasses(":expanded")]
public class ExpanderRow : PreferencesRow
{
    /// <summary>Defines <see cref="Subtitle"/>.</summary>
    public static readonly StyledProperty<string?> SubtitleProperty =
        ActionRow.SubtitleProperty.AddOwner<ExpanderRow>();

    /// <summary>Defines <see cref="IsExpanded"/>.</summary>
    public static readonly StyledProperty<bool> IsExpandedProperty =
        AvaloniaProperty.Register<ExpanderRow, bool>(nameof(IsExpanded), defaultBindingMode: BindingMode.TwoWay,
            coerce: (o, v) => v && ((ExpanderRow)o).EnableExpansion);

    /// <summary>Defines <see cref="EnableExpansion"/>.</summary>
    public static readonly StyledProperty<bool> EnableExpansionProperty =
        AvaloniaProperty.Register<ExpanderRow, bool>(nameof(EnableExpansion), true, defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Defines <see cref="ShowEnableSwitch"/>.</summary>
    public static readonly StyledProperty<bool> ShowEnableSwitchProperty =
        AvaloniaProperty.Register<ExpanderRow, bool>(nameof(ShowEnableSwitch));

    /// <summary>Defines <see cref="Expanded"/>.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> ExpandedEvent =
        RoutedEvent.Register<ExpanderRow, RoutedEventArgs>(nameof(Expanded), RoutingStrategies.Bubble);

    /// <summary>Defines <see cref="Collapsed"/>.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> CollapsedEvent =
        RoutedEvent.Register<ExpanderRow, RoutedEventArgs>(nameof(Collapsed), RoutingStrategies.Bubble);

    private readonly ActionRow _header = new() { IsActivatable = true, Classes = { "header" } };
    private readonly ItemsControl _prefixes = Box("horizontal");
    private readonly ItemsControl _suffixes = Box("horizontal suffixes");
    private readonly ToggleSwitch _switch = new() { VerticalAlignment = VerticalAlignment.Center };
    private readonly AdwIcon _arrow = new() { IconName = "adw-expander-arrow-symbolic" };

    static ExpanderRow()
    {
        FocusableProperty.OverrideDefaultValue<ExpanderRow>(false);
        CssNodeProviders.Register<ExpanderRow>(r => r.Rows.Count == 0 ? ["empty", "expander"] : ["expander"]);
        CssNodeProviders.MapPseudoClass<ExpanderRow>(":expanded", CssStates.Checked);
    }

    /// <summary>Creates a row.</summary>
    public ExpanderRow()
    {
        Css.SetClasses(_arrow, "expander-row-arrow");
        _prefixes.ItemsSource = Prefixes;
        _suffixes.ItemsSource = Suffixes;
        _header.Bind(TitleProperty, this.GetObservable(TitleProperty));
        _header.Bind(UseUnderlineProperty, this.GetObservable(UseUnderlineProperty));
        _header.Bind(ActionRow.SubtitleProperty, this.GetObservable(SubtitleProperty));
        _switch.Bind(ToggleSwitch.IsCheckedProperty, this.GetObservable(EnableExpansionProperty, e => (bool?)e));
        _switch.Bind(IsVisibleProperty, this.GetObservable(ShowEnableSwitchProperty));
        _switch.IsCheckedChanged += (_, _) => EnableExpansion = _switch.IsChecked == true;
        _arrow.Bind(IsEnabledProperty, this.GetObservable(EnableExpansionProperty));
        _header.Suffixes.Add(_suffixes);
        _header.Suffixes.Add(_switch);
        _header.Suffixes.Add(_arrow);
        _header.Activated += (_, e) =>
        {
            e.Handled = true;
            IsExpanded = !IsExpanded;
        };
        Prefixes.CollectionChanged += (_, _) => UpdateParts();
        Suffixes.CollectionChanged += (_, _) => UpdateParts();
        Rows.CollectionChanged += OnRowsChanged;
    }

    /// <summary>Raised when the row expands.</summary>
    public event System.EventHandler<RoutedEventArgs>? Expanded
    {
        add => AddHandler(ExpandedEvent, value);
        remove => RemoveHandler(ExpandedEvent, value);
    }

    /// <summary>Raised when the row collapses.</summary>
    public event System.EventHandler<RoutedEventArgs>? Collapsed
    {
        add => AddHandler(CollapsedEvent, value);
        remove => RemoveHandler(CollapsedEvent, value);
    }

    /// <summary>The subtitle, under the title in smaller text.</summary>
    public string? Subtitle
    {
        get => GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    /// <summary>Whether the nested rows show. It stays false while <see cref="EnableExpansion"/> is false.</summary>
    public bool IsExpanded
    {
        get => GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    /// <summary>Whether the row can expand. Turning it on expands the row, and turning it off collapses it.</summary>
    public bool EnableExpansion
    {
        get => GetValue(EnableExpansionProperty);
        set => SetValue(EnableExpansionProperty, value);
    }

    /// <summary>Shows a switch for <see cref="EnableExpansion"/> before the arrow.</summary>
    public bool ShowEnableSwitch
    {
        get => GetValue(ShowEnableSwitchProperty);
        set => SetValue(ShowEnableSwitchProperty, value);
    }

    /// <summary>The nested rows, which show while the row is expanded.</summary>
    [Content]
    public Avalonia.Controls.Controls Rows { get; } = new();

    /// <summary>Widgets before the title.</summary>
    public Avalonia.Controls.Controls Prefixes { get; } = new();

    /// <summary>Widgets after the title, before the switch and the arrow.</summary>
    public Avalonia.Controls.Controls Suffixes { get; } = new();

    /// <summary>The header row.</summary>
    internal ActionRow Header => _header;

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (e.NameScope.Find<Decorator>("PART_Header") is { } host)
        {
            host.Child = _header;
        }
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsExpandedProperty)
        {
            var expanded = change.GetNewValue<bool>();
            PseudoClasses.Set(":expanded", expanded);
            RaiseEvent(new RoutedEventArgs(expanded ? ExpandedEvent : CollapsedEvent, this));
        }
        else if (change.Property == EnableExpansionProperty)
        {
            // Turning the expansion on expands the row, and turning it off collapses it (adw_expander_row_set_enable_expansion).
            CoerceValue(IsExpandedProperty);
            IsExpanded = change.GetNewValue<bool>();
        }
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new Automation.ExpanderRowAutomationPeer(this);

    // The header row's own prefixes and suffixes boxes hold a box each for the row's, as AdwExpanderRow's.
    private static ItemsControl Box(string classes)
    {
        var box = new ItemsControl
        {
            IsVisible = false,
            ItemsPanel = new FuncTemplate<Panel?>(() => new AdwStack { Orientation = Orientation.Horizontal }),
        };
        Css.SetNode(box, "box");
        Css.SetClasses(box, classes);
        return box;
    }

    private void UpdateParts()
    {
        _prefixes.IsVisible = Prefixes.Count > 0;
        _suffixes.IsVisible = Suffixes.Count > 0;
        if (_prefixes.IsVisible != _header.Prefixes.Contains(_prefixes))
        {
            if (_prefixes.IsVisible)
            {
                _header.Prefixes.Add(_prefixes);
            }
            else
            {
                _header.Prefixes.Remove(_prefixes);
            }
        }
    }

    private void OnRowsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (Css.GetCssNode(this) is { } node)
        {
            NodeTree.RefreshSubtree(node);
        }
    }
}
