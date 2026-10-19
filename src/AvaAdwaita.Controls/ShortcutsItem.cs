using Avalonia;
using Avalonia.Media;

namespace AvaAdwaita.Controls;

/// <summary>A shortcut in a <see cref="ShortcutsSection"/> (AdwShortcutsItem): what it does and its keys.</summary>
public class ShortcutsItem : AvaloniaObject
{
    /// <summary>Defines <see cref="Title"/>.</summary>
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<ShortcutsItem, string?>(nameof(Title));

    /// <summary>Defines <see cref="Subtitle"/>.</summary>
    public static readonly StyledProperty<string?> SubtitleProperty =
        AvaloniaProperty.Register<ShortcutsItem, string?>(nameof(Subtitle));

    /// <summary>Defines <see cref="Accelerator"/>.</summary>
    public static readonly StyledProperty<string?> AcceleratorProperty =
        AvaloniaProperty.Register<ShortcutsItem, string?>(nameof(Accelerator));

    /// <summary>Defines <see cref="Direction"/>.</summary>
    public static readonly StyledProperty<FlowDirection?> DirectionProperty =
        AvaloniaProperty.Register<ShortcutsItem, FlowDirection?>(nameof(Direction));

    /// <summary>Creates an item.</summary>
    public ShortcutsItem()
    {
    }

    /// <summary>Creates an item with a title and an accelerator.</summary>
    public ShortcutsItem(string title, string accelerator)
    {
        Title = title;
        Accelerator = accelerator;
    }

    /// <summary>What the shortcut does.</summary>
    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>More about what it does, below the title.</summary>
    public string? Subtitle
    {
        get => GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    /// <summary>The keys, in <see cref="ShortcutLabel"/>'s accelerator syntax.</summary>
    public string? Accelerator
    {
        get => GetValue(AcceleratorProperty);
        set => SetValue(AcceleratorProperty, value);
    }

    /// <summary>The text direction the shortcut applies in, such as an arrow key's; null for both.</summary>
    public FlowDirection? Direction
    {
        get => GetValue(DirectionProperty);
        set => SetValue(DirectionProperty, value);
    }
}
