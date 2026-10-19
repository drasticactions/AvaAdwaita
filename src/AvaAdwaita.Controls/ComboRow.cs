using System.Collections;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Styling;

namespace AvaAdwaita.Controls;

/// <summary>
/// An action row that chooses one item from a list (AdwComboRow): the selected item shows after the title, and
/// activating the row opens the list in a popover. With <see cref="UseSubtitle"/> the selected item is the subtitle
/// instead.
/// </summary>
public class ComboRow : ActionRow
{
    /// <summary>Defines <see cref="ItemsSource"/>.</summary>
    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        ItemsControl.ItemsSourceProperty.AddOwner<ComboRow>();

    /// <summary>Defines <see cref="ItemTemplate"/>.</summary>
    public static readonly StyledProperty<IDataTemplate?> ItemTemplateProperty =
        ItemsControl.ItemTemplateProperty.AddOwner<ComboRow>();

    /// <summary>Defines <see cref="SelectedIndex"/>.</summary>
    public static readonly StyledProperty<int> SelectedIndexProperty =
        AvaloniaProperty.Register<ComboRow, int>(nameof(SelectedIndex), -1, defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Defines <see cref="SelectedItem"/>.</summary>
    public static readonly StyledProperty<object?> SelectedItemProperty =
        AvaloniaProperty.Register<ComboRow, object?>(nameof(SelectedItem), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Defines <see cref="UseSubtitle"/>.</summary>
    public static readonly StyledProperty<bool> UseSubtitleProperty =
        AvaloniaProperty.Register<ComboRow, bool>(nameof(UseSubtitle));

    /// <summary>Defines <see cref="IsDropDownOpen"/>.</summary>
    public static readonly StyledProperty<bool> IsDropDownOpenProperty =
        AvaloniaProperty.Register<ComboRow, bool>(nameof(IsDropDownOpen), defaultBindingMode: BindingMode.TwoWay);

    private const string ComboClass = "adw-combo-row";
    private const string SubtitleClass = "adw-use-subtitle";

    private readonly ComboBox _combo = new() { VerticalAlignment = VerticalAlignment.Center, Focusable = false, Classes = { ComboClass } };
    private bool _syncing;

    static ComboRow()
    {
        IsActivatableProperty.OverrideDefaultValue<ComboRow>(true);

        // The popover's rows are a list view's, which activates them with a single click.
        Nodes.CssNodeProviders.Register<ComboBoxItem>(i =>
            i.Parent is ComboBox owner && owner.Classes.Contains(ComboClass) ? ["activatable"] : []);
    }

    /// <summary>Creates a row.</summary>
    public ComboRow()
    {
        _combo.Bind(ItemsControl.ItemTemplateProperty, this.GetObservable(ItemTemplateProperty));
        _combo.SelectionChanged += (_, _) => Sync(() =>
        {
            SelectedIndex = _combo.SelectedIndex;
            SelectedItem = _combo.SelectedItem;
        });
        _combo.PropertyChanged += (_, e) =>
        {
            if (e.Property == ComboBox.IsDropDownOpenProperty)
            {
                IsDropDownOpen = _combo.IsDropDownOpen;
            }
        };
        Suffixes.Add(_combo);
    }

    /// <summary>The items to choose from.</summary>
    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>How an item shows, in the row and in the list.</summary>
    public IDataTemplate? ItemTemplate
    {
        get => GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    /// <summary>The index of the selected item, or -1.</summary>
    public int SelectedIndex
    {
        get => GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    /// <summary>The selected item.</summary>
    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    /// <summary>Shows the selected item as the subtitle rather than after the title.</summary>
    public bool UseSubtitle
    {
        get => GetValue(UseSubtitleProperty);
        set => SetValue(UseSubtitleProperty, value);
    }

    /// <summary>Whether the list is open.</summary>
    public bool IsDropDownOpen
    {
        get => GetValue(IsDropDownOpenProperty);
        set => SetValue(IsDropDownOpenProperty, value);
    }

    /// <summary>The combo box that holds the items and the popover.</summary>
    internal ComboBox ComboBox => _combo;

    /// <inheritdoc/>
    protected override void OnActivated() => IsDropDownOpen = true;

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        // The combo box is the row's current item and arrow, not a GTK drop-down.
        if (this.TryFindResource("AdwComboRowDropDown", out var theme) && theme is ControlTheme t)
        {
            _combo.Theme = t;
        }
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SelectedIndexProperty)
        {
            Sync(() => _combo.SelectedIndex = SelectedIndex);
            SelectedItem = _combo.SelectedItem;
            UpdateSubtitle();
        }
        else if (change.Property == SelectedItemProperty)
        {
            Sync(() => _combo.SelectedItem = SelectedItem);
            SelectedIndex = _combo.SelectedIndex;
            UpdateSubtitle();
        }
        else if (change.Property == ItemsSourceProperty)
        {
            _combo.ItemsSource = ItemsSource;

            // The combo box takes its items now; select again what was asked for before there were any.
            if (SelectedIndex >= 0)
            {
                Sync(() => _combo.SelectedIndex = SelectedIndex);
            }

            SelectedItem = _combo.SelectedItem;
            UpdateSubtitle();
        }
        else if (change.Property == UseSubtitleProperty)
        {
            // The subtitle replaces the current item, not the arrow.
            _combo.Classes.Set(SubtitleClass, UseSubtitle);
            UpdateSubtitle();
        }
        else if (change.Property == IsDropDownOpenProperty)
        {
            _combo.IsDropDownOpen = IsDropDownOpen;
        }
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new Automation.ComboRowAutomationPeer(this);

    private void Sync(System.Action action)
    {
        if (_syncing)
        {
            return;
        }

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

    private void UpdateSubtitle()
    {
        if (UseSubtitle)
        {
            Subtitle = SelectedItem?.ToString();
        }
    }
}
