using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls.Primitives;

namespace AvaAdwaita.Controls;

/// <summary>
/// A header bar's title and subtitle (AdwWindowTitle), each on one line and ended with an ellipsis when it does not
/// fit.
/// </summary>
public class WindowTitle : TemplatedControl
{
    /// <summary>Defines <see cref="Title"/>.</summary>
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<WindowTitle, string?>(nameof(Title));

    /// <summary>Defines <see cref="Subtitle"/>.</summary>
    public static readonly StyledProperty<string?> SubtitleProperty =
        AvaloniaProperty.Register<WindowTitle, string?>(nameof(Subtitle));

    /// <summary>The title.</summary>
    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>The subtitle, under the title in smaller text.</summary>
    public string? Subtitle
    {
        get => GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new Automation.WindowTitleAutomationPeer(this);
}
