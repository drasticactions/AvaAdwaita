using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;

namespace AvaAdwaita.Controls;

/// <summary>
/// An action row with a spin button (AdwSpinRow). The spin button takes the row's width beyond the title, and
/// activating the row focuses it.
/// </summary>
public class SpinRow : ActionRow
{
    /// <summary>Defines <see cref="Value"/>.</summary>
    public static readonly StyledProperty<decimal?> ValueProperty =
        AvaloniaProperty.Register<SpinRow, decimal?>(nameof(Value), 0m, defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Defines <see cref="Minimum"/>.</summary>
    public static readonly StyledProperty<decimal> MinimumProperty =
        NumericUpDown.MinimumProperty.AddOwner<SpinRow>();

    /// <summary>Defines <see cref="Maximum"/>.</summary>
    public static readonly StyledProperty<decimal> MaximumProperty =
        NumericUpDown.MaximumProperty.AddOwner<SpinRow>();

    /// <summary>Defines <see cref="Increment"/>.</summary>
    public static readonly StyledProperty<decimal> IncrementProperty =
        NumericUpDown.IncrementProperty.AddOwner<SpinRow>();

    /// <summary>Defines <see cref="FormatString"/>.</summary>
    public static readonly StyledProperty<string> FormatStringProperty =
        NumericUpDown.FormatStringProperty.AddOwner<SpinRow>();

    /// <summary>Defines <see cref="Digits"/>.</summary>
    public static readonly StyledProperty<int> DigitsProperty =
        AvaloniaProperty.Register<SpinRow, int>(nameof(Digits), coerce: (_, v) => System.Math.Clamp(v, 0, 20));

    private readonly NumericUpDown _spin = new()
    {
        VerticalAlignment = VerticalAlignment.Center,
        HorizontalAlignment = HorizontalAlignment.Stretch,
        TextAlignment = TextAlignment.Right,
    };

    static SpinRow()
    {
        IsActivatableProperty.OverrideDefaultValue<SpinRow>(true);
    }

    /// <summary>Creates a row.</summary>
    public SpinRow()
    {
        ExpandSuffixes = true;
        Presenters.AdwStack.SetExpand(_spin, true);
        _spin.Bind(NumericUpDown.MinimumProperty, this.GetObservable(MinimumProperty));
        _spin.Bind(NumericUpDown.MaximumProperty, this.GetObservable(MaximumProperty));
        _spin.Bind(NumericUpDown.IncrementProperty, this.GetObservable(IncrementProperty));
        UpdateFormat();
        _spin.Bind(NumericUpDown.ValueProperty, this.GetObservable(ValueProperty));
        _spin.ValueChanged += (_, e) => Value = e.NewValue;
        Suffixes.Add(_spin);
    }

    /// <summary>The value.</summary>
    public decimal? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>The smallest value.</summary>
    public decimal Minimum
    {
        get => GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    /// <summary>The largest value.</summary>
    public decimal Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    /// <summary>How much a step changes the value.</summary>
    public decimal Increment
    {
        get => GetValue(IncrementProperty);
        set => SetValue(IncrementProperty, value);
    }

    /// <summary>How the value is shown, as <see cref="NumericUpDown.FormatString"/>.</summary>
    public string FormatString
    {
        get => GetValue(FormatStringProperty);
        set => SetValue(FormatStringProperty, value);
    }

    /// <summary>The row's spin button.</summary>
    /// <summary>How many decimals the value shows when <see cref="FormatString"/> is not set (GtkSpinButton:digits).</summary>
    public int Digits
    {
        get => GetValue(DigitsProperty);
        set => SetValue(DigitsProperty, value);
    }

    internal NumericUpDown SpinButton => _spin;

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == FormatStringProperty || change.Property == DigitsProperty)
        {
            UpdateFormat();
        }
    }

    // GtkSpinButton:digits: without a format, the value shows that many decimals, whatever the decimal's scale.
    private void UpdateFormat() => _spin.FormatString = string.IsNullOrEmpty(FormatString) ? $"F{Digits}" : FormatString;

    /// <inheritdoc/>
    protected override void OnActivated() => _spin.Focus();

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new Automation.ActionRowAutomationPeer(this);
}
