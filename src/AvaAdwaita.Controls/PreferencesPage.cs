using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Metadata;

namespace AvaAdwaita.Controls;

/// <summary>
/// A page of preferences groups (AdwPreferencesPage): the groups in a clamped column that scrolls, under an optional
/// description and banner. <see cref="Title"/> and <see cref="IconName"/> name the page in a preferences dialog.
/// </summary>
public class PreferencesPage : TemplatedControl
{
    /// <summary>Defines <see cref="Title"/>.</summary>
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<PreferencesPage, string?>(nameof(Title));

    /// <summary>Defines <see cref="IconName"/>.</summary>
    public static readonly StyledProperty<string?> IconNameProperty =
        AvaloniaProperty.Register<PreferencesPage, string?>(nameof(IconName));

    /// <summary>Defines <see cref="Description"/>.</summary>
    public static readonly StyledProperty<string?> DescriptionProperty =
        AvaloniaProperty.Register<PreferencesPage, string?>(nameof(Description));

    /// <summary>Defines <see cref="DescriptionCentered"/>.</summary>
    public static readonly StyledProperty<bool> DescriptionCenteredProperty =
        AvaloniaProperty.Register<PreferencesPage, bool>(nameof(DescriptionCentered));

    /// <summary>Defines <see cref="Banner"/>.</summary>
    public static readonly StyledProperty<Control?> BannerProperty =
        AvaloniaProperty.Register<PreferencesPage, Control?>(nameof(Banner));

    /// <summary>The page's title, as a preferences dialog's view switcher shows it.</summary>
    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>The page's icon, as a preferences dialog's view switcher shows it.</summary>
    public string? IconName
    {
        get => GetValue(IconNameProperty);
        set => SetValue(IconNameProperty, value);
    }

    /// <summary>A description at the top of the page.</summary>
    public string? Description
    {
        get => GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    /// <summary>Centers the description.</summary>
    public bool DescriptionCentered
    {
        get => GetValue(DescriptionCenteredProperty);
        set => SetValue(DescriptionCenteredProperty, value);
    }

    /// <summary>A banner above the page, outside what scrolls.</summary>
    public Control? Banner
    {
        get => GetValue(BannerProperty);
        set => SetValue(BannerProperty, value);
    }

    /// <summary>The groups.</summary>
    [Content]
    public Avalonia.Controls.Controls Children { get; } = new();

    private TextBlock? _description;

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _description = e.NameScope.Find<TextBlock>("PART_Description");
        UpdateDescription();
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == DescriptionCenteredProperty)
        {
            UpdateDescription();
        }
    }

    private void UpdateDescription()
    {
        if (_description is not null)
        {
            _description.TextAlignment = DescriptionCentered ? Avalonia.Media.TextAlignment.Center : Avalonia.Media.TextAlignment.Start;
        }
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new Automation.PreferencesPageAutomationPeer(this);
}
