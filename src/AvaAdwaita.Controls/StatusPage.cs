using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;

namespace AvaAdwaita.Controls;

/// <summary>
/// A page that tells the state of a view (AdwStatusPage), as an empty folder or no search results: a large icon, a
/// title and a description, centered, with the content (often a button) under them. Add the <c>compact</c> class for
/// smaller spacing and icon.
/// </summary>
public class StatusPage : ContentControl
{
    /// <summary>Defines <see cref="Title"/>.</summary>
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<StatusPage, string?>(nameof(Title));

    /// <summary>Defines <see cref="Description"/>.</summary>
    public static readonly StyledProperty<string?> DescriptionProperty =
        AvaloniaProperty.Register<StatusPage, string?>(nameof(Description));

    /// <summary>Defines <see cref="IconName"/>.</summary>
    public static readonly StyledProperty<string?> IconNameProperty =
        AvaloniaProperty.Register<StatusPage, string?>(nameof(IconName));

    /// <summary>The title.</summary>
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

    /// <summary>The icon above the title.</summary>
    public string? IconName
    {
        get => GetValue(IconNameProperty);
        set => SetValue(IconNameProperty, value);
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new Automation.StatusPageAutomationPeer(this);
}
