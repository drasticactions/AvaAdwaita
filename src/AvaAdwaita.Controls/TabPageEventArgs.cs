using Avalonia.Interactivity;

namespace AvaAdwaita.Controls;

/// <summary>A page of a tab view, at a position: one attached, detached, reordered or about to close.</summary>
public class TabPageEventArgs(RoutedEvent routedEvent, TabPage? page, int position) : RoutedEventArgs(routedEvent)
{
    /// <summary>The page; none for a menu that closed.</summary>
    public TabPage? Page { get; } = page;

    /// <summary>The page's position.</summary>
    public int Position { get; } = position;
}
