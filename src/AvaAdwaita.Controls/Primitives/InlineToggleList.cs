using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace AvaAdwaita.Controls.Primitives;

/// <summary>
/// An inline view switcher's toggle group: one toggle per page, the selected one checked. Its containers are tab items,
/// whose selection is GTK's <c>:checked</c>.
/// </summary>
public class InlineToggleList : SelectingItemsControl
{
    static InlineToggleList()
    {
        SelectionModeProperty.OverrideDefaultValue<InlineToggleList>(SelectionMode.Single | SelectionMode.AlwaysSelected);
    }

    /// <inheritdoc/>
    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) => new TabItem();

    /// <inheritdoc/>
    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey) => NeedsContainer<TabItem>(item, out recycleKey);
}
