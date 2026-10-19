using Avalonia;
using Avalonia.Controls;

namespace AvaAdwaita.Chrome;

/// <summary>
/// The header bar of a window, which is part of the Window theme: the content packed at its start and
/// end, its title, and whether it shows window buttons.
/// </summary>
public static class WindowHeaderBar
{
    /// <summary>Content at the start, after the start window buttons.</summary>
    public static readonly AttachedProperty<object?> StartProperty =
        AvaloniaProperty.RegisterAttached<Window, object?>("Start", typeof(WindowHeaderBar));

    /// <summary>Content at the end, before the end window buttons.</summary>
    public static readonly AttachedProperty<object?> EndProperty =
        AvaloniaProperty.RegisterAttached<Window, object?>("End", typeof(WindowHeaderBar));

    /// <summary>Replaces the centered title. Null shows the window title and <see cref="SubtitleProperty"/>.</summary>
    public static readonly AttachedProperty<object?> TitleProperty =
        AvaloniaProperty.RegisterAttached<Window, object?>("Title", typeof(WindowHeaderBar));

    /// <summary>A subtitle under the window title.</summary>
    public static readonly AttachedProperty<string?> SubtitleProperty =
        AvaloniaProperty.RegisterAttached<Window, string?>("Subtitle", typeof(WindowHeaderBar));

    /// <summary>False hides the header bar; the content then fills the window.</summary>
    public static readonly AttachedProperty<bool> IsVisibleProperty =
        AvaloniaProperty.RegisterAttached<Window, bool>("IsVisible", typeof(WindowHeaderBar), true);

    /// <summary>Classes for the <c>headerbar</c> node, as <c>flat</c>.</summary>
    public static readonly AttachedProperty<string?> ClassesProperty =
        AvaloniaProperty.RegisterAttached<Window, string?>("Classes", typeof(WindowHeaderBar));

    /// <summary>Show the window buttons at the start.</summary>
    public static readonly AttachedProperty<bool> ShowStartTitleButtonsProperty =
        AvaloniaProperty.RegisterAttached<Window, bool>("ShowStartTitleButtons", typeof(WindowHeaderBar), true);

    /// <summary>Show the window buttons at the end.</summary>
    public static readonly AttachedProperty<bool> ShowEndTitleButtonsProperty =
        AvaloniaProperty.RegisterAttached<Window, bool>("ShowEndTitleButtons", typeof(WindowHeaderBar), true);

    /// <summary>Gets <see cref="StartProperty"/>.</summary>
    public static object? GetStart(Window w) => w.GetValue(StartProperty);

    /// <summary>Sets <see cref="StartProperty"/>.</summary>
    public static void SetStart(Window w, object? v) => w.SetValue(StartProperty, v);

    /// <summary>Gets <see cref="EndProperty"/>.</summary>
    public static object? GetEnd(Window w) => w.GetValue(EndProperty);

    /// <summary>Sets <see cref="EndProperty"/>.</summary>
    public static void SetEnd(Window w, object? v) => w.SetValue(EndProperty, v);

    /// <summary>Gets <see cref="TitleProperty"/>.</summary>
    public static object? GetTitle(Window w) => w.GetValue(TitleProperty);

    /// <summary>Sets <see cref="TitleProperty"/>.</summary>
    public static void SetTitle(Window w, object? v) => w.SetValue(TitleProperty, v);

    /// <summary>Gets <see cref="SubtitleProperty"/>.</summary>
    public static string? GetSubtitle(Window w) => w.GetValue(SubtitleProperty);

    /// <summary>Sets <see cref="SubtitleProperty"/>.</summary>
    public static void SetSubtitle(Window w, string? v) => w.SetValue(SubtitleProperty, v);

    /// <summary>Gets <see cref="IsVisibleProperty"/>.</summary>
    public static bool GetIsVisible(Window w) => w.GetValue(IsVisibleProperty);

    /// <summary>Sets <see cref="IsVisibleProperty"/>.</summary>
    public static void SetIsVisible(Window w, bool v) => w.SetValue(IsVisibleProperty, v);

    /// <summary>Gets <see cref="ClassesProperty"/>.</summary>
    public static string? GetClasses(Window w) => w.GetValue(ClassesProperty);

    /// <summary>Sets <see cref="ClassesProperty"/>.</summary>
    public static void SetClasses(Window w, string? v) => w.SetValue(ClassesProperty, v);

    /// <summary>Gets <see cref="ShowStartTitleButtonsProperty"/>.</summary>
    public static bool GetShowStartTitleButtons(Window w) => w.GetValue(ShowStartTitleButtonsProperty);

    /// <summary>Sets <see cref="ShowStartTitleButtonsProperty"/>.</summary>
    public static void SetShowStartTitleButtons(Window w, bool v) => w.SetValue(ShowStartTitleButtonsProperty, v);

    /// <summary>Gets <see cref="ShowEndTitleButtonsProperty"/>.</summary>
    public static bool GetShowEndTitleButtons(Window w) => w.GetValue(ShowEndTitleButtonsProperty);

    /// <summary>Sets <see cref="ShowEndTitleButtonsProperty"/>.</summary>
    public static void SetShowEndTitleButtons(Window w, bool v) => w.SetValue(ShowEndTitleButtonsProperty, v);
}
