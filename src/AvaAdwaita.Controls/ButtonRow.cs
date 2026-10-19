using Avalonia;
using Avalonia.Automation.Peers;

namespace AvaAdwaita.Controls;

/// <summary>
/// A row that acts as a button (AdwButtonRow): its title in the middle, with an icon before or after it. Add the
/// <c>suggested-action</c> or <c>destructive-action</c> class for the colored kinds.
/// </summary>
public class ButtonRow : PreferencesRow
{
    /// <summary>Defines <see cref="StartIconName"/>.</summary>
    public static readonly StyledProperty<string?> StartIconNameProperty =
        AvaloniaProperty.Register<ButtonRow, string?>(nameof(StartIconName));

    /// <summary>Defines <see cref="EndIconName"/>.</summary>
    public static readonly StyledProperty<string?> EndIconNameProperty =
        AvaloniaProperty.Register<ButtonRow, string?>(nameof(EndIconName));

    static ButtonRow()
    {
        IsActivatableProperty.OverrideDefaultValue<ButtonRow>(true);
    }

    /// <summary>The icon before the title.</summary>
    public string? StartIconName
    {
        get => GetValue(StartIconNameProperty);
        set => SetValue(StartIconNameProperty, value);
    }

    /// <summary>The icon after the title.</summary>
    public string? EndIconName
    {
        get => GetValue(EndIconNameProperty);
        set => SetValue(EndIconNameProperty, value);
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new Automation.ButtonRowAutomationPeer(this);
}
