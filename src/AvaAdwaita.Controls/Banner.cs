using System.Windows.Input;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;

namespace AvaAdwaita.Controls;

/// <summary>
/// A bar across the top of a view with a message and an optional button (AdwBanner). It slides down when
/// <see cref="IsRevealed"/> turns on and up when it turns off.
/// </summary>
public class Banner : TemplatedControl
{
    /// <summary>Defines <see cref="Title"/>.</summary>
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<Banner, string?>(nameof(Title));

    /// <summary>Defines <see cref="ButtonLabel"/>.</summary>
    public static readonly StyledProperty<string?> ButtonLabelProperty =
        AvaloniaProperty.Register<Banner, string?>(nameof(ButtonLabel));

    /// <summary>Defines <see cref="ButtonStyle"/>.</summary>
    public static readonly StyledProperty<BannerButtonStyle> ButtonStyleProperty =
        AvaloniaProperty.Register<Banner, BannerButtonStyle>(nameof(ButtonStyle));

    /// <summary>Defines <see cref="IsRevealed"/>.</summary>
    public static readonly StyledProperty<bool> IsRevealedProperty =
        Revealer.IsRevealedProperty.AddOwner<Banner>();

    /// <summary>Defines <see cref="ButtonCommand"/>.</summary>
    public static readonly StyledProperty<ICommand?> ButtonCommandProperty =
        AvaloniaProperty.Register<Banner, ICommand?>(nameof(ButtonCommand));

    /// <summary>Defines <see cref="ButtonClicked"/>.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> ButtonClickedEvent =
        RoutedEvent.Register<Banner, RoutedEventArgs>(nameof(ButtonClicked), RoutingStrategies.Bubble);

    private Button? _button;

    /// <summary>Raised when the button is clicked.</summary>
    public event System.EventHandler<RoutedEventArgs>? ButtonClicked
    {
        add => AddHandler(ButtonClickedEvent, value);
        remove => RemoveHandler(ButtonClickedEvent, value);
    }

    /// <summary>The message.</summary>
    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>The button's label, with <c>_</c> before its access key; no label, no button.</summary>
    public string? ButtonLabel
    {
        get => GetValue(ButtonLabelProperty);
        set => SetValue(ButtonLabelProperty, value);
    }

    /// <summary>The button's look.</summary>
    public BannerButtonStyle ButtonStyle
    {
        get => GetValue(ButtonStyleProperty);
        set => SetValue(ButtonStyleProperty, value);
    }

    /// <summary>Whether the banner shows.</summary>
    public bool IsRevealed
    {
        get => GetValue(IsRevealedProperty);
        set => SetValue(IsRevealedProperty, value);
    }

    /// <summary>The command the button runs.</summary>
    public ICommand? ButtonCommand
    {
        get => GetValue(ButtonCommandProperty);
        set => SetValue(ButtonCommandProperty, value);
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (_button is not null)
        {
            _button.Click -= OnButtonClick;
        }

        _button = e.NameScope.Find<Button>("PART_Button");
        if (_button is not null)
        {
            _button.Click += OnButtonClick;
        }

        UpdateButtonStyle();
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ButtonStyleProperty)
        {
            UpdateButtonStyle();
        }
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new Automation.BannerAutomationPeer(this);

    private void UpdateButtonStyle() => _button?.Classes.Set("suggested-action", ButtonStyle == BannerButtonStyle.Suggested);

    private void OnButtonClick(object? sender, RoutedEventArgs e)
    {
        RaiseEvent(new RoutedEventArgs(ButtonClickedEvent, this));
        if (ButtonCommand is { } c && c.CanExecute(null))
        {
            c.Execute(null);
        }
    }
}
