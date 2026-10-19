using System.Collections.Specialized;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using AvaAdwaita.Animation;
using AvaAdwaita.Controls.Primitives;

namespace AvaAdwaita.Controls;

/// <summary>
/// A row with a text field (AdwEntryRow). While the row is empty and not being edited its title is the placeholder;
/// once there is text, or the text has the focus, the title moves above the text. With <see cref="ShowApplyButton"/>
/// a changed text shows an apply button, and Enter or the button raises <see cref="Applied"/>; otherwise Enter raises
/// <see cref="EntryActivated"/>.
/// </summary>
public class EntryRow : PreferencesRow
{
    /// <summary>Defines <see cref="Text"/>.</summary>
    public static readonly StyledProperty<string?> TextProperty =
        TextBox.TextProperty.AddOwner<EntryRow>(new(defaultBindingMode: BindingMode.TwoWay));

    /// <summary>Defines <see cref="ShowApplyButton"/>.</summary>
    public static readonly StyledProperty<bool> ShowApplyButtonProperty =
        AvaloniaProperty.Register<EntryRow, bool>(nameof(ShowApplyButton));

    /// <summary>Defines <see cref="IsEditable"/>.</summary>
    public static readonly StyledProperty<bool> IsEditableProperty =
        AvaloniaProperty.Register<EntryRow, bool>(nameof(IsEditable), true);

    /// <summary>Defines <see cref="MaxLength"/>.</summary>
    public static readonly StyledProperty<int> MaxLengthProperty =
        TextBox.MaxLengthProperty.AddOwner<EntryRow>();

    /// <summary>Defines <see cref="Applied"/>.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> AppliedEvent =
        RoutedEvent.Register<EntryRow, RoutedEventArgs>(nameof(Applied), RoutingStrategies.Bubble);

    /// <summary>Defines <see cref="EntryActivated"/>.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> EntryActivatedEvent =
        RoutedEvent.Register<EntryRow, RoutedEventArgs>(nameof(EntryActivated), RoutingStrategies.Bubble);

    internal static readonly DirectProperty<EntryRow, bool> HasPrefixesProperty =
        AvaloniaProperty.RegisterDirect<EntryRow, bool>(nameof(HasPrefixes), r => r.HasPrefixes);

    internal static readonly DirectProperty<EntryRow, bool> HasSuffixesProperty =
        AvaloniaProperty.RegisterDirect<EntryRow, bool>(nameof(HasSuffixes), r => r.HasSuffixes);

    private TimedAnimation? _empty;
    private EntryRowArea? _area;
    private TextBox? _text;
    private Control? _editIcon;
    private Control? _indicator;
    private Control? _apply;
    private bool _textChanged;
    private bool _editing;
    private bool _hasPrefixes;
    private bool _hasSuffixes;
    private bool _showIndicator;

    static EntryRow()
    {
        IsActivatableProperty.OverrideDefaultValue<EntryRow>(true);

        // The text takes the focus, as AdwEntryRow passes its own to the text.
        FocusableProperty.OverrideDefaultValue<EntryRow>(false);
    }

    /// <summary>Creates a row.</summary>
    public EntryRow()
    {
        Prefixes.CollectionChanged += OnPartsChanged;
        Suffixes.CollectionChanged += OnPartsChanged;
    }

    /// <summary>Raised when the changed text is applied, by Enter or the apply button.</summary>
    public event System.EventHandler<RoutedEventArgs>? Applied
    {
        add => AddHandler(AppliedEvent, value);
        remove => RemoveHandler(AppliedEvent, value);
    }

    /// <summary>Raised when Enter is pressed in the text and there is no apply button.</summary>
    public event System.EventHandler<RoutedEventArgs>? EntryActivated
    {
        add => AddHandler(EntryActivatedEvent, value);
        remove => RemoveHandler(EntryActivatedEvent, value);
    }

    /// <summary>The text.</summary>
    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>Shows an apply button once the text is edited; Enter then applies too.</summary>
    public bool ShowApplyButton
    {
        get => GetValue(ShowApplyButtonProperty);
        set => SetValue(ShowApplyButtonProperty, value);
    }

    /// <summary>Whether the text can be edited.</summary>
    public bool IsEditable
    {
        get => GetValue(IsEditableProperty);
        set => SetValue(IsEditableProperty, value);
    }

    /// <summary>The most characters the text takes; 0 for no limit.</summary>
    public int MaxLength
    {
        get => GetValue(MaxLengthProperty);
        set => SetValue(MaxLengthProperty, value);
    }

    /// <summary>Widgets before the text.</summary>
    public Avalonia.Controls.Controls Prefixes { get; } = new();

    /// <summary>Widgets after the text.</summary>
    public Avalonia.Controls.Controls Suffixes { get; } = new();

    internal bool HasPrefixes
    {
        get => _hasPrefixes;
        private set => SetAndRaise(HasPrefixesProperty, ref _hasPrefixes, value);
    }

    internal bool HasSuffixes
    {
        get => _hasSuffixes;
        private set => SetAndRaise(HasSuffixesProperty, ref _hasSuffixes, value);
    }

    /// <summary>The text box.</summary>
    internal TextBox? TextBox => _text;

    /// <summary>Shows the indicator icon while editing, as a password row's Caps Lock warning.</summary>
    internal bool ShowIndicator
    {
        get => _showIndicator;
        set
        {
            _showIndicator = value;
            UpdateEmpty();
        }
    }

    /// <inheritdoc/>
    protected override void OnActivated() => FocusText();

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (_text is not null)
        {
            _text.PropertyChanged -= OnTextPropertyChanged;
            _text.KeyDown -= OnTextKeyDown;
        }

        if (_apply is Button oldApply)
        {
            oldApply.Click -= OnApplyClick;
        }

        _area = e.NameScope.Find<EntryRowArea>("PART_Area");
        _text = e.NameScope.Find<TextBox>("PART_Text");
        _editIcon = e.NameScope.Find<Control>("PART_EditIcon");
        _indicator = e.NameScope.Find<Control>("PART_Indicator");
        _apply = e.NameScope.Find<Control>("PART_Apply");
        if (_text is not null)
        {
            _text.PropertyChanged += OnTextPropertyChanged;
            _text.AddHandler(KeyDownEvent, OnTextKeyDown, RoutingStrategies.Tunnel);
        }

        if (_apply is Button apply)
        {
            apply.Click += OnApplyClick;
        }

        _empty = _area is null ? null : new TimedAnimation(_area, 150, AdwEasing.Ease, v => _area.EmptyProgress = v);
        UpdateEmpty(immediate: true);
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TextProperty)
        {
            if (ShowApplyButton && _editing)
            {
                _textChanged = true;
            }

            UpdateEmpty();
        }
        else if (change.Property == IsEditableProperty)
        {
            UpdateEmpty();
        }
        else if (change.Property == ShowApplyButtonProperty && !ShowApplyButton && _textChanged)
        {
            _textChanged = false;
            UpdateEmpty();
        }
    }

    /// <inheritdoc/>
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        // A press anywhere but on a part that takes it puts the caret in the text.
        if (!e.Handled && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            e.Handled = true;
            FocusText();
        }
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new Automation.EntryRowAutomationPeer(this);

    private void FocusText()
    {
        if (_text is { } t)
        {
            t.Focus();
            t.ClearSelection();
            t.CaretIndex = t.Text?.Length ?? 0;
        }
    }

    private void OnApplyClick(object? sender, RoutedEventArgs e) => Apply();

    private void Apply()
    {
        _textChanged = false;
        UpdateEmpty();
        RaiseEvent(new RoutedEventArgs(AppliedEvent, this));
    }

    private void OnTextKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key is not (Key.Enter or Key.Return))
        {
            return;
        }

        e.Handled = true;
        if (_apply is { IsVisible: true })
        {
            Apply();
        }
        else
        {
            RaiseEvent(new RoutedEventArgs(EntryActivatedEvent, this));
        }
    }

    private void OnTextPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == IsKeyboardFocusWithinProperty)
        {
            _editing = _text!.IsKeyboardFocusWithin;
            Classes.Set("focused", _editing);
            UpdateEmpty();
        }
    }

    // update_empty in adw-entry-row.c.
    private void UpdateEmpty(bool immediate = false)
    {
        if (_area is null)
        {
            return;
        }

        var editable = IsEditable;
        if (_editIcon is not null)
        {
            _editIcon.IsVisible = !_textChanged && (!_editing || !editable);
            _editIcon.IsEnabled = editable;
        }

        if (_indicator is not null)
        {
            _indicator.IsVisible = _editing && _showIndicator;
        }

        if (_apply is not null)
        {
            _apply.IsVisible = _textChanged;
        }

        var empty = string.IsNullOrEmpty(Text) && !(_editing && editable) && !_textChanged;
        if (immediate)
        {
            _empty?.Jump(empty ? 0 : 1);
        }
        else
        {
            _empty?.Animate(empty ? 0 : 1);
        }

        _area.InvalidateMeasure();
    }

    private void OnPartsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        HasPrefixes = Prefixes.Count > 0;
        HasSuffixes = Suffixes.Count > 0;
    }
}
