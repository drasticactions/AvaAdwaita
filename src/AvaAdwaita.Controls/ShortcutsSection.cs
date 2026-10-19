using Avalonia;
using Avalonia.Collections;
using Avalonia.Metadata;

namespace AvaAdwaita.Controls;

/// <summary>
/// A group of shortcuts in a <see cref="ShortcutsDialog"/> (AdwShortcutsSection). An untitled section continues the
/// titled one before it.
/// </summary>
public class ShortcutsSection : AvaloniaObject
{
    /// <summary>Defines <see cref="Title"/>.</summary>
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<ShortcutsSection, string?>(nameof(Title));

    /// <summary>Creates a section.</summary>
    public ShortcutsSection()
    {
    }

    /// <summary>Creates a section with a title.</summary>
    public ShortcutsSection(string? title)
    {
        Title = title;
    }

    /// <summary>The section's title, above its shortcuts and on its navigation button.</summary>
    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>The shortcuts.</summary>
    [Content]
    public AvaloniaList<ShortcutsItem> Items { get; } = new();

    /// <summary>Adds a shortcut.</summary>
    public void Add(ShortcutsItem item) => Items.Add(item);

    /// <summary>Removes a shortcut.</summary>
    public void Remove(ShortcutsItem item) => Items.Remove(item);
}
