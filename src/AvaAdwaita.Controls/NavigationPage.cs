using System;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace AvaAdwaita.Controls;

/// <summary>
/// A page of a <see cref="NavigationView"/> or a <see cref="NavigationSplitView"/> (AdwNavigationPage). Its
/// <see cref="Title"/> is the title of a header bar inside it.
/// </summary>
public class NavigationPage : ContentControl
{
    /// <summary>Defines <see cref="Title"/>.</summary>
    public static readonly StyledProperty<string?> TitleProperty =
        AvaloniaProperty.Register<NavigationPage, string?>(nameof(Title));

    /// <summary>Defines <see cref="NavigationTag"/>.</summary>
    public static readonly StyledProperty<string?> NavigationTagProperty =
        AvaloniaProperty.Register<NavigationPage, string?>(nameof(NavigationTag));

    /// <summary>Defines <see cref="CanPop"/>.</summary>
    public static readonly StyledProperty<bool> CanPopProperty =
        AvaloniaProperty.Register<NavigationPage, bool>(nameof(CanPop), true);

    /// <summary>Defines <see cref="Showing"/>.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> ShowingEvent =
        RoutedEvent.Register<NavigationPage, RoutedEventArgs>(nameof(Showing), RoutingStrategies.Direct);

    /// <summary>Defines <see cref="Shown"/>.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> ShownEvent =
        RoutedEvent.Register<NavigationPage, RoutedEventArgs>(nameof(Shown), RoutingStrategies.Direct);

    /// <summary>Defines <see cref="Hiding"/>.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> HidingEvent =
        RoutedEvent.Register<NavigationPage, RoutedEventArgs>(nameof(Hiding), RoutingStrategies.Direct);

    /// <summary>Defines <see cref="Hidden"/>.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> HiddenEvent =
        RoutedEvent.Register<NavigationPage, RoutedEventArgs>(nameof(Hidden), RoutingStrategies.Direct);

    /// <summary>Raised when the page starts to show.</summary>
    public event System.EventHandler<RoutedEventArgs>? Showing
    {
        add => AddHandler(ShowingEvent, value);
        remove => RemoveHandler(ShowingEvent, value);
    }

    /// <summary>Raised when the page has finished showing.</summary>
    public event System.EventHandler<RoutedEventArgs>? Shown
    {
        add => AddHandler(ShownEvent, value);
        remove => RemoveHandler(ShownEvent, value);
    }

    /// <summary>Raised when the page starts to hide.</summary>
    public event System.EventHandler<RoutedEventArgs>? Hiding
    {
        add => AddHandler(HidingEvent, value);
        remove => RemoveHandler(HidingEvent, value);
    }

    /// <summary>Raised when the page has finished hiding.</summary>
    public event System.EventHandler<RoutedEventArgs>? Hidden
    {
        add => AddHandler(HiddenEvent, value);
        remove => RemoveHandler(HiddenEvent, value);
    }

    /// <summary>The title, as a header bar inside the page shows it.</summary>
    public string? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>A name to push or pop the page by (AdwNavigationPage:tag).</summary>
    public string? NavigationTag
    {
        get => GetValue(NavigationTagProperty);
        set => SetValue(NavigationTagProperty, value);
    }

    /// <summary>Whether the page can be popped, by a back button, Escape or a gesture.</summary>
    public bool CanPop
    {
        get => GetValue(CanPopProperty);
        set => SetValue(CanPopProperty, value);
    }

    internal void Raise(RoutedEvent<RoutedEventArgs> e) => RaiseEvent(new RoutedEventArgs(e, this));

    /// <summary>What had the focus in the page when another covered it, to have it again when the page comes back.</summary>
    internal WeakReference<InputElement>? LastFocus { get; set; }

    /// <inheritdoc/>
    protected override AutomationPeer OnCreateAutomationPeer() => new Automation.NavigationPageAutomationPeer(this);
}
