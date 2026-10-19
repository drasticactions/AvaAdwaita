using System.Windows.Input;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using AvaAdwaita.Nodes;

namespace AvaAdwaita.Controls;

/// <summary>
/// The base of libadwaita's preference rows (AdwPreferencesRow): a list row with a title, for a
/// <c>ListBox</c> with the <c>boxed-list</c> class or a preferences group. Rows are not selectable, as
/// the rows of a boxed list. An activatable row raises <see cref="Activated"/> and runs <see cref="Command"/> when it
/// is clicked or activated with Enter or Space.
/// </summary>
public class PreferencesRow : ListBoxItem, IOwnActivatable
{
    /// <summary>Defines <see cref="Title"/>.</summary>
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<PreferencesRow, string?>(nameof(Title));

    /// <summary>Defines <see cref="UseUnderline"/>.</summary>
    public static readonly StyledProperty<bool> UseUnderlineProperty =
        AvaloniaProperty.Register<PreferencesRow, bool>(nameof(UseUnderline));

    /// <summary>Defines <see cref="IsActivatable"/>.</summary>
    public static readonly StyledProperty<bool> IsActivatableProperty =
        AvaloniaProperty.Register<PreferencesRow, bool>(nameof(IsActivatable));

    /// <summary>Defines <see cref="Command"/>.</summary>
    public static readonly StyledProperty<ICommand?> CommandProperty =
        Button.CommandProperty.AddOwner<PreferencesRow>();

    /// <summary>Defines <see cref="CommandParameter"/>.</summary>
    public static readonly StyledProperty<object?> CommandParameterProperty =
        Button.CommandParameterProperty.AddOwner<PreferencesRow>();

    /// <summary>Defines <see cref="Activated"/>.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> ActivatedEvent =
        RoutedEvent.Register<PreferencesRow, RoutedEventArgs>(nameof(Activated), RoutingStrategies.Bubble);

    static PreferencesRow()
    {
        CssNodeProviders.Register<PreferencesRow>(r => r.IsActivatable ? ["activatable"] : [], IsActivatableProperty);
    }

    /// <summary>Raised when an activatable row is activated.</summary>
    public event System.EventHandler<RoutedEventArgs>? Activated
    {
        add => AddHandler(ActivatedEvent, value);
        remove => RemoveHandler(ActivatedEvent, value);
    }

    /// <summary>The row's title.</summary>
    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>An <c>_</c> in the title marks the access key, as GtkLabel's use-underline.</summary>
    public bool UseUnderline
    {
        get => GetValue(UseUnderlineProperty);
        set => SetValue(UseUnderlineProperty, value);
    }

    /// <summary>Whether the row reacts to clicks and Enter, and shows that it does on hover (GtkListBoxRow:activatable).</summary>
    public bool IsActivatable
    {
        get => GetValue(IsActivatableProperty);
        set => SetValue(IsActivatableProperty, value);
    }

    /// <summary>The command an activation runs.</summary>
    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    /// <summary>The parameter of <see cref="Command"/>.</summary>
    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    /// <summary>Activates the row, as a click on an activatable row does.</summary>
    public void Activate()
    {
        OnActivated();
        RaiseEvent(new RoutedEventArgs(ActivatedEvent, this));
        if (Command is { } c && c.CanExecute(CommandParameter))
        {
            c.Execute(CommandParameter);
        }
    }

    /// <summary>What activating the row does before <see cref="Activated"/> is raised, as toggling a switch row's switch.</summary>
    protected virtual void OnActivated()
    {
    }

    /// <inheritdoc/>
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        // A boxed list has no selection; the press makes the row :active and gives it the focus, as GtkListBox does.
        if (!e.Handled && Focusable && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            Focus(NavigationMethod.Pointer);
        }
    }

    /// <inheritdoc/>
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        // A button in the row takes its click, as GTK's gesture on it claims the press: the row does not activate too.
        if (IsActivatable && e.InitialPressMouseButton == MouseButton.Left && new Rect(Bounds.Size).Contains(e.GetPosition(this)) && !e.Handled
            && !InButton(e.Source as Visual))
        {
            e.Handled = true;
            Activate();
        }
    }

    private bool InButton(Visual? source)
    {
        for (var v = source; v is not null && v != this; v = v.GetVisualParent())
        {
            if (v is Button)
            {
                return true;
            }
        }

        return false;
    }

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (IsActivatable && !e.Handled && e.Source == this && (e.Key is Key.Enter or Key.Space))
        {
            e.Handled = true;
            Activate();
        }
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new Automation.PreferencesRowAutomationPeer(this);
}
