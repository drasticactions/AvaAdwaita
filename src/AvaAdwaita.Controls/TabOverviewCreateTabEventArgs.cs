using Avalonia.Interactivity;

namespace AvaAdwaita.Controls;

/// <summary>A tab overview asking for a new page: the handler adds one to the view and sets <see cref="Page"/>.</summary>
public class TabOverviewCreateTabEventArgs(RoutedEvent routedEvent) : RoutedEventArgs(routedEvent)
{
    /// <summary>The page the handler created.</summary>
    public TabPage? Page { get; set; }
}
