using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Layout;

namespace AvaAdwaita.Controls;

/// <summary>
/// An action row with a switch as its first suffix (AdwSwitchRow). Activating the row, by a click anywhere on it,
/// Enter or Space, toggles the switch.
/// </summary>
public class SwitchRow : ActionRow
{
    /// <summary>Defines <see cref="IsChecked"/>.</summary>
    public static readonly StyledProperty<bool> IsCheckedProperty =
        AvaloniaProperty.Register<SwitchRow, bool>(nameof(IsChecked), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Defines <see cref="IsCheckedChanged"/>.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> IsCheckedChangedEvent =
        RoutedEvent.Register<SwitchRow, RoutedEventArgs>(nameof(IsCheckedChanged), RoutingStrategies.Bubble);

    private readonly ToggleSwitch _switch = new() { VerticalAlignment = VerticalAlignment.Center, Focusable = false };

    static SwitchRow()
    {
        IsActivatableProperty.OverrideDefaultValue<SwitchRow>(true);
    }

    /// <summary>Creates a row.</summary>
    public SwitchRow()
    {
        _switch.IsCheckedChanged += (_, _) => IsChecked = _switch.IsChecked == true;
        Suffixes.Add(_switch);
    }

    /// <summary>Raised when <see cref="IsChecked"/> changes.</summary>
    public event System.EventHandler<RoutedEventArgs>? IsCheckedChanged
    {
        add => AddHandler(IsCheckedChangedEvent, value);
        remove => RemoveHandler(IsCheckedChangedEvent, value);
    }

    /// <summary>Whether the switch is on.</summary>
    public bool IsChecked
    {
        get => GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }

    /// <summary>The row's switch.</summary>
    internal ToggleSwitch Switch => _switch;

    /// <inheritdoc/>
    protected override void OnActivated() => IsChecked = !IsChecked;

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsCheckedProperty)
        {
            _switch.IsChecked = IsChecked;
            RaiseEvent(new RoutedEventArgs(IsCheckedChangedEvent, this));
        }
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new Automation.SwitchRowAutomationPeer(this);
}
