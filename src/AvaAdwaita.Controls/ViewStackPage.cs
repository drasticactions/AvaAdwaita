using Avalonia;
using Avalonia.Controls;

namespace AvaAdwaita.Controls;

/// <summary>
/// A page of a <see cref="ViewStack"/> (AdwViewStackPage): its content, and the name, title, icon, badge and attention
/// mark a view switcher shows for it.
/// </summary>
public class ViewStackPage : ContentControl
{
    /// <summary>Defines <see cref="Title"/>.</summary>
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<ViewStackPage, string?>(nameof(Title));

    /// <summary>Defines <see cref="IconName"/>.</summary>
    public static readonly StyledProperty<string?> IconNameProperty =
        AvaloniaProperty.Register<ViewStackPage, string?>(nameof(IconName));

    /// <summary>Defines <see cref="BadgeNumber"/>.</summary>
    public static readonly StyledProperty<int> BadgeNumberProperty =
        AvaloniaProperty.Register<ViewStackPage, int>(nameof(BadgeNumber));

    /// <summary>Defines <see cref="NeedsAttention"/>.</summary>
    public static readonly StyledProperty<bool> NeedsAttentionProperty =
        AvaloniaProperty.Register<ViewStackPage, bool>(nameof(NeedsAttention));

    /// <summary>Defines <see cref="UseUnderline"/>.</summary>
    public static readonly StyledProperty<bool> UseUnderlineProperty =
        AvaloniaProperty.Register<ViewStackPage, bool>(nameof(UseUnderline));

    /// <summary>Defines <see cref="IsPageVisible"/>.</summary>
    public static readonly StyledProperty<bool> IsPageVisibleProperty =
        AvaloniaProperty.Register<ViewStackPage, bool>(nameof(IsPageVisible), true);

    /// <summary>The title a switcher shows.</summary>
    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>The icon a switcher shows.</summary>
    public string? IconName
    {
        get => GetValue(IconNameProperty);
        set => SetValue(IconNameProperty, value);
    }

    /// <summary>A number on the page's switcher button; 0 shows none.</summary>
    public int BadgeNumber
    {
        get => GetValue(BadgeNumberProperty);
        set => SetValue(BadgeNumberProperty, value);
    }

    /// <summary>Marks the page's switcher button with a dot, or colors its badge.</summary>
    public bool NeedsAttention
    {
        get => GetValue(NeedsAttentionProperty);
        set => SetValue(NeedsAttentionProperty, value);
    }

    /// <summary>An <c>_</c> in the title marks the access key.</summary>
    public bool UseUnderline
    {
        get => GetValue(UseUnderlineProperty);
        set => SetValue(UseUnderlineProperty, value);
    }

    /// <summary>Whether the page is one of the stack's pages a switcher offers.</summary>
    public bool IsPageVisible
    {
        get => GetValue(IsPageVisibleProperty);
        set => SetValue(IsPageVisibleProperty, value);
    }
}
