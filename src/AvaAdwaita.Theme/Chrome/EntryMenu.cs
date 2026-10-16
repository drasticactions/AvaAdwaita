using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace AvaAdwaita.Chrome;

/// <summary>
/// A text box's context menu, as GtkText's (gtk_text_get_menu_model): Cut, Copy, Paste and Delete, then Select All
/// and Change Direction, each enabled as GTK enables its action. GTK's Insert Emoji item is left out: Avalonia has no
/// emoji chooser. An application's own context menu or flyout on a text box takes its place.
/// </summary>
internal static class EntryMenu
{
    private static readonly KeyGesture s_toggleDirection = new(Key.T, KeyModifiers.Control | KeyModifiers.Shift);

    public static void Register()
    {
        TemplatedControl.TemplateAppliedEvent.AddClassHandler<TextBox>((t, _) => Attach(t));

        // GtkText binds Shift+Ctrl+T to misc.toggle-direction.
        InputElement.KeyDownEvent.AddClassHandler<TextBox>((t, e) =>
        {
            if (!e.Handled && s_toggleDirection.Matches(e))
            {
                ToggleDirection(t);
                e.Handled = true;
            }
        });
    }

    private static void Attach(TextBox box)
    {
        if (box.ContextMenu is not null || box.ContextFlyout is not null)
        {
            return;
        }

        var cut = Item(box, "StringTextFlyoutCutText", () => box.Cut());
        var copy = Item(box, "StringTextFlyoutCopyText", () => box.Copy());
        var paste = Item(box, "StringTextFlyoutPasteText", () => box.Paste());
        var delete = Item(box, "AdwStringDelete", () => box.SelectedText = string.Empty);
        var selectAll = Item(box, "AdwStringSelectAll", () => box.SelectAll());
        var direction = Item(box, "AdwStringChangeDirection", () => ToggleDirection(box));
        direction.InputGesture = s_toggleDirection;

        var menu = new ContextMenu { Items = { cut, copy, paste, delete, new Separator(), selectAll, direction } };
        // Opening comes before a context request shows the menu; Opened also follows an Open() call.
        async void Update()
        {
            var editable = !box.IsReadOnly && box.IsEffectivelyEnabled;
            var selection = box.SelectionStart != box.SelectionEnd;
            // GTK's DISPLAY_NORMAL: a password's text is not cut or copied while it is hidden.
            var visible = box.PasswordChar == default || box.RevealPassword;
            cut.IsEnabled = visible && editable && selection;
            copy.IsEnabled = visible && selection;
            delete.IsEnabled = editable && selection;
            selectAll.IsEnabled = !string.IsNullOrEmpty(box.Text);
            paste.IsEnabled = false;
            if (editable && TopLevel.GetTopLevel(box)?.Clipboard is { } clipboard)
            {
                try
                {
                    paste.IsEnabled = (await clipboard.GetDataFormatsAsync()).Contains(DataFormat.Text);
                }
                catch (Exception)
                {
                    // No formats to read: nothing to paste, as GTK finds.
                }
            }
        }

        menu.Opening += (_, _) => Update();
        menu.Opened += (_, _) => Update();

        // At style priority, so an application's style or local menu still replaces it.
        box.SetValue(Control.ContextMenuProperty, menu, BindingPriority.Style);
    }

    private static MenuItem Item(TextBox box, string key, Action action)
    {
        var item = new MenuItem();
        item.Bind(HeaderedSelectingItemsControl.HeaderProperty, box.GetResourceObservable(key));
        item.Click += (_, _) => action();
        return item;
    }

    private static void ToggleDirection(TextBox box) =>
        box.FlowDirection = box.FlowDirection == FlowDirection.RightToLeft ? FlowDirection.LeftToRight : FlowDirection.RightToLeft;
}
