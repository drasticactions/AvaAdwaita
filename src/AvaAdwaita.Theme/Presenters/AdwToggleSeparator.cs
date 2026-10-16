using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace AvaAdwaita.Presenters;

/// <summary>
/// The separator a toggle group draws before each toggle but the first: <c>separator.horizontal</c>, with
/// <c>.hidden</c> next to the active toggle, as AdwToggleGroup updates its separators.
/// </summary>
public class AdwToggleSeparator : AdwBox
{
    private SelectingItemsControl? _owner;

    /// <summary>Creates the separator node.</summary>
    public AdwToggleSeparator()
    {
        Nodes.Css.SetNode(this, "separator");
    }

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (TemplatedParent is Control item && ItemsControl.ItemsControlFromItemContainer(item) is SelectingItemsControl owner)
        {
            _owner = owner;
            owner.SelectionChanged += OnSelectionChanged;
        }

        Update();
    }

    /// <inheritdoc/>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (_owner is not null)
        {
            _owner.SelectionChanged -= OnSelectionChanged;
            _owner = null;
        }
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e) => Update();

    private void Update()
    {
        if (_owner is null || TemplatedParent is not Control item)
        {
            return;
        }

        var index = _owner.IndexFromContainer(item);
        IsVisible = index > 0;
        var selected = _owner.SelectedIndex;
        var hidden = selected >= 0 && (index == selected || index - 1 == selected);
        Nodes.Css.SetClasses(this, hidden ? "horizontal hidden" : "horizontal");
    }
}
