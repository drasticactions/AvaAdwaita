using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using AvaAdwaita.Nodes;
using AvaAdwaita.Presenters;

namespace AvaAdwaita.Controls.Primitives;

/// <summary>
/// A shortcuts dialog's row for a <see cref="ShortcutsItem"/> (adw-shortcut-row.c): the title and subtitle, then the
/// keys, which wrap below the title when the row is narrow.
/// </summary>
public class ShortcutRow : PreferencesRow
{
    private readonly AdwLabel _title = new() { TextWrapping = TextWrapping.Wrap, WidthChars = 10 };
    private readonly AdwLabel _subtitle = new() { TextWrapping = TextWrapping.Wrap, WidthChars = 10 };
    private readonly ShortcutLabel _keys = new() { VerticalAlignment = VerticalAlignment.Center, IsWrapping = true };

    /// <summary>Creates a row for <paramref name="item"/>: a <c>row.shortcut-row</c> node.</summary>
    public ShortcutRow(ShortcutsItem item)
    {
        Item = item;
        Css.SetClasses(this, "shortcut-row");
        Css.SetNode(_title, "label");
        Css.SetClasses(_title, "title");
        Css.SetNode(_subtitle, "label");
        Css.SetClasses(_subtitle, "subtitle");
        var titleBox = new AdwBox
        {
            VerticalAlignment = VerticalAlignment.Center,
            Child = new AdwStack { Orientation = Orientation.Vertical, Children = { _title, _subtitle } },
        };
        Css.SetNode(titleBox, "box");
        Css.SetClasses(titleBox, "vertical title-box");

        // The labels expand, so the title box takes the line's spare room and the keys go to its end.
        AdwStack.SetExpand(titleBox, true);
        Content = new WrapBox { ChildSpacing = 9, LineSpacing = 6, VerticalAlignment = VerticalAlignment.Center, Children = { titleBox, _keys } };
        _keys[!ShortcutLabel.DisabledTextProperty] = _keys.GetResourceObservable("AdwStringNoShortcut").ToBinding();

        Bind(TitleProperty, item.GetObservable(ShortcutsItem.TitleProperty));
        _title.Bind(TextBlock.TextProperty, item.GetObservable(ShortcutsItem.TitleProperty));
        _subtitle.Bind(TextBlock.TextProperty, item.GetObservable(ShortcutsItem.SubtitleProperty));
        _subtitle.Bind(IsVisibleProperty, item.GetObservable(ShortcutsItem.SubtitleProperty, s => !string.IsNullOrEmpty(s)));
        _keys.Bind(ShortcutLabel.AcceleratorProperty, item.GetObservable(ShortcutsItem.AcceleratorProperty, a => a ?? string.Empty));
    }

    /// <summary>The shortcut the row shows.</summary>
    public ShortcutsItem Item { get; }

    /// <summary>The row's keys.</summary>
    internal ShortcutLabel Keys => _keys;

    /// <inheritdoc/>
    protected override Type StyleKeyOverride => typeof(ListBoxItem);
}
