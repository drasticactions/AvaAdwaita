using System;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using AvaAdwaita.CssEngine.Selectors;
using AvaAdwaita.Nodes;
using Xunit;

namespace AvaAdwaita.Theme.Tests;

/// <summary>A text box's context menu and selection, as GtkText's, and the menus' text in other languages.</summary>
public class EntryMenuTests
{
    private static string[] Headers(ContextMenu menu) =>
        menu.Items.OfType<MenuItem>().Select(i => i.Header as string ?? string.Empty).ToArray();

    [AvaloniaFact]
    public void A_Text_Box_Has_Gtks_Context_Menu()
    {
        var box = new TextBox { Text = "Some text" };
        var w = Shots.Show(box);
        var menu = box.ContextMenu!;
        Assert.Equal(new[] { "Cu_t", "_Copy", "_Paste", "_Delete", "Select _All", "Change di_rection" }, Headers(menu));
        Assert.Equal(new KeyGesture(Key.T, KeyModifiers.Control | KeyModifiers.Shift), menu.Items.OfType<MenuItem>().Last().InputGesture);

        // Cut, Copy and Delete need a selection; Select All needs text.
        box.Focus();
        box.SelectAll();
        menu.Open(box);
        Shots.Pump(w);
        var items = menu.Items.OfType<MenuItem>().ToArray();
        Assert.True(items[0].IsEnabled && items[1].IsEnabled && items[3].IsEnabled && items[4].IsEnabled);

        // GtkText's popover is inside it, so the entry stays :focus-within while its menu is open.
        Assert.True((Css.GetCssNode(box)!.States & CssStates.FocusWithin) != 0);

        // The menu selects its first item as it opens, as GtkPopoverMenu focuses it.
        Assert.True(items[0].IsSelected);
        menu.Close();
        Shots.Pump(w);
        Assert.False(menu.IsOpen);

        box.ClearSelection();
        Assert.Equal(box.SelectionStart, box.SelectionEnd);
        menu.Open(box);
        Shots.Pump(w);
        Assert.False(items[0].IsEnabled || items[1].IsEnabled || items[3].IsEnabled);
        Assert.True(items[4].IsEnabled);
        menu.Close();
        w.Close();
    }

    [AvaloniaFact]
    public void An_Entry_Is_As_Wide_As_Gtks_However_Long_Its_Text()
    {
        // gtk_text_measure: the text's natural width is NAT_ENTRY_WIDTH, 150 px, and does not follow the text.
        var shortText = new TextBox { Text = "a", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left };
        var longText = new TextBox { Text = new string('m', 80), HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left };
        var panel = new StackPanel { HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left, Children = { shortText, longText } };
        var w = Shots.Show(panel, 800, 200);
        double Text(TextBox b) => b.GetVisualDescendants().OfType<Presenters.AdwEntryWidth>().Single().Bounds.Width;
        Assert.Equal(150, Text(shortText));
        Assert.Equal(150, Text(longText));
        w.Close();
    }

    [AvaloniaFact]
    public void An_Applications_Own_Menu_Wins()
    {
        var own = new ContextMenu();
        var box = new TextBox { ContextMenu = own };
        Shots.Show(box);
        Assert.Same(own, box.ContextMenu);
    }

    [AvaloniaFact]
    public void Selected_Text_Stays_Visible_Under_The_Selection()
    {
        // libadwaita's selection color is transparent: GTK draws the text, then the selection over it, and nothing
        // more. The selected text is the text tinted by the selection, never transparent.
        var box = new TextBox { Text = "Some text" };
        var w = Shots.Show(box);
        box.Focus();
        box.SelectAll();
        Shots.Pump(w);
        var presenter = box.GetVisualDescendants().OfType<TextPresenter>().Single();
        var selected = Assert.IsAssignableFrom<ISolidColorBrush>(presenter.SelectionForegroundBrush).Color;
        var text = ((ISolidColorBrush)presenter.Foreground!).Color;
        Assert.True(selected.A >= text.A);
        Assert.True(selected.B > selected.R, "tinted by the accent");
        w.Close();
    }

    [AvaloniaFact]
    public void Accelerators_Read_As_Gtk_Labels_Them()
    {
        // gtk_accelerator_print_label: Shift before Ctrl, joined by "+", the key's character in upper case.
        Assert.Equal("Shift+Ctrl+T", Markup.AcceleratorLabel.Of(new KeyGesture(Key.T, KeyModifiers.Control | KeyModifiers.Shift)));
        Assert.Equal("Ctrl+.", Markup.AcceleratorLabel.Of(new KeyGesture(Key.OemPeriod, KeyModifiers.Control)));
        Assert.Equal("Ctrl+Page Up", Markup.AcceleratorLabel.Of(new KeyGesture(Key.PageUp, KeyModifiers.Control)));
    }

    [AvaloniaFact]
    public void A_Message_Catalog_Reads_Back()
    {
        // A two-message .mo file, as msgfmt writes one, with a context entry.
        var messages = Resources.GtkCatalog.Parse(Mo(("_Cancel", "_Abbrechen"), ("keyboard label\u0004Shift", "Umschalt")));
        Assert.NotNull(messages);
        Assert.Equal("_Abbrechen", messages!["_Cancel"]);
        Assert.Equal("Umschalt", messages["keyboard label\u0004Shift"]);
        Assert.Null(Resources.GtkCatalog.Parse([1, 2, 3]));
    }

    [AvaloniaFact]
    public void Gtks_Installed_Translations_Name_The_Theme_Strings()
    {
        // Where the system has GTK's German catalog, the picker's buttons read as GTK's do.
        var strings = Resources.GtkStrings.For(new CultureInfo("de-DE"));
        if (strings.Count == 0)
        {
            return;
        }

        Assert.Equal("_Abbrechen", strings["AdwStringCancel"]);
        Assert.Equal("Aus_wählen", strings["AdwStringSelect"]);
    }

    private static byte[] Mo(params (string Id, string Text)[] messages)
    {
        var sorted = messages.OrderBy(m => m.Id, StringComparer.Ordinal).ToArray();
        var utf8 = System.Text.Encoding.UTF8;
        var header = 28;
        var originals = header;
        var translations = originals + (sorted.Length * 8);
        var data = translations + (sorted.Length * 8);
        var stream = new System.IO.MemoryStream();
        var writer = new System.IO.BinaryWriter(stream);
        writer.Write(0x950412deu);
        writer.Write(0u);
        writer.Write((uint)sorted.Length);
        writer.Write((uint)originals);
        writer.Write((uint)translations);
        writer.Write(0u);
        writer.Write(0u);
        var strings = sorted.Select(m => m.Id).Concat(sorted.Select(m => m.Text)).Select(s => utf8.GetBytes(s)).ToArray();
        var offset = data;
        foreach (var s in strings)
        {
            writer.Write((uint)s.Length);
            writer.Write((uint)offset);
            offset += s.Length + 1;
        }

        foreach (var s in strings)
        {
            writer.Write(s);
            writer.Write((byte)0);
        }

        return stream.ToArray();
    }
}
