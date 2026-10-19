using System.Collections.Generic;
using System.Linq;
using Avalonia.VisualTree;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Controls.Primitives;
using Avalonia;
using AvaAdwaita.CssEngine.Selectors;

namespace AvaAdwaita.Nodes;

/// <summary>The automatic classes and state mappings of the stock controls.</summary>
internal static class ControlHints
{
    private static bool s_registered;

    private static void MakePaned(GridSplitter g)
    {
        if (g.Parent is not Grid grid || (Css.GetNode(grid) is { } n && n != "paned"))
        {
            return;
        }

        Css.SetNode(grid, "paned");
        Css.SetClasses(grid, g.ResizeDirection == GridResizeDirection.Rows ? "vertical" : "horizontal");
    }

    private static void BuildOnAttach(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (sender is ContextMenu menu)
        {
            menu.AttachedToVisualTree -= BuildOnAttach;
            menu.ApplyStyling();
            menu.ApplyTemplate();
        }
    }

    private static void FocusFirstColumn(Popup popup)
    {
        if (!popup.IsOpen || popup.Child is not { } child)
        {
            return;
        }

        DateTimePickerPanel? first = null;
        var firstColumn = int.MaxValue;
        foreach (var panel in child.GetVisualDescendants())
        {
            if (panel is not DateTimePickerPanel { IsEffectivelyVisible: true } p)
            {
                continue;
            }

            // The column host is the panel's ancestor in the presenter's picker grid.
            var host = p.GetVisualAncestors().OfType<Control>().FirstOrDefault(a => a.Parent is Grid { Name: "PART_PickerContainer" });
            var column = host is null ? int.MaxValue - 1 : Grid.GetColumn(host);
            if (column < firstColumn)
            {
                firstColumn = column;
                first = p;
            }
        }

        first?.Focus(Avalonia.Input.NavigationMethod.Pointer);
    }

    private static void RefreshOwner(ContextMenu menu)
    {
        if (menu.Parent is StyledElement owner && Css.GetCssNode(owner) is { } node)
        {
            NodeTree.RefreshSubtree(node);
        }
    }

    public static void Register()
    {
        if (s_registered)
        {
            return;
        }

        s_registered = true;
        Chrome.PopoverShadowInput.Register();
        Chrome.EntryMenu.Register();
        Chrome.MenuFirstItem.Register();
        MenuBase.OpenedEvent.AddClassHandler<ContextMenu>((m, _) => RefreshOwner(m));
        MenuBase.ClosedEvent.AddClassHandler<ContextMenu>((m, _) => RefreshOwner(m));

        // GtkButton sets .text-button, .image-button or .image-text-button from its child.
        CssNodeProviders.Register<Button>(ButtonClasses, ContentControl.ContentProperty);
        CssNodeProviders.Register<ToggleButton>(ButtonClasses, ContentControl.ContentProperty);
        CssNodeProviders.Register<Separator>(SeparatorClasses);
        CssNodeProviders.Register<TabControl>(t => t.Classes.Contains("view-switcher") ? [] : ["frame"]);
        // GtkPaned: the grid holding a splitter is the paned, with the splitter's neighbors as its children, and the
        // splitter is its separator.
        TemplatedControl.TemplateAppliedEvent.AddClassHandler<GridSplitter>((g, _) => MakePaned(g));
        GridSplitter.ResizeDirectionProperty.Changed.AddClassHandler<GridSplitter>((g, _) => MakePaned(g));

        // A context menu opens at the pointer, and Avalonia sizes and places such a popup on its first layout only.
        // Its popover node is in the template, so build the template as the menu attaches, before that layout: the
        // node's style, and so the shadow's margin that placement leaves out, are then there for it.
        Control.ContextRequestedEvent.AddClassHandler<Control>((c, _) =>
        {
            if (c.ContextMenu is { } menu && !menu.IsAttachedToVisualTree())
            {
                menu.AttachedToVisualTree += BuildOnAttach;
            }
        });

        // A date or time picker shifts its popup up to center Fluent's looping selector on the button; the
        // popover here sits below the button, pointing at it, so keep it where the placement puts it.
        Popup.VerticalOffsetProperty.Changed.AddClassHandler<Popup>((p, _) =>
        {
            if (p.TemplatedParent is DatePicker or TimePicker && p.VerticalOffset != 0)
            {
                p.VerticalOffset = 0;
            }
        });

        // A picker focuses its first column when its date or time changes, not when it opens; a popover takes the
        // focus as it opens, so focus the first column then, and Escape and the arrow keys reach the picker.
        Popup.IsOpenProperty.Changed.AddClassHandler<Popup>((p, _) =>
        {
            if (p.IsOpen && p.TemplatedParent is DatePicker or TimePicker)
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() => FocusFirstColumn(p), Avalonia.Threading.DispatcherPriority.Loaded);
            }
        });

        // GTK 4's automatic font rendering: grayscale antialiasing, never subpixel, and slight hinting
        // (gtk_widget_update_pango_context). Windows and popups pass it to their content.
        TemplatedControl.TemplateAppliedEvent.AddClassHandler<TopLevel>((t, _) =>
        {
            Avalonia.Media.TextOptions.SetTextRenderingMode(t, Avalonia.Media.TextRenderingMode.Antialias);
            Avalonia.Media.TextOptions.SetTextHintingMode(t, Avalonia.Media.TextHintingMode.Light);
        });

        // A tree item holds its children, so it is :pointerover while the pointer is on any of them; GTK hovers only
        // the row under the pointer.
        CssNodeProviders.MapPseudoClass<TreeViewItem>(":pointerover", CssStates.Hover, (_, owner) => owner is Avalonia.Input.InputElement { IsPointerOver: true });
        CssNodeProviders.MapPseudoClass<TreeViewItem>(":pointerover", CssStates.None, (_, owner) => owner is not Avalonia.Input.InputElement { IsPointerOver: true });

        CssNodeProviders.MapPseudoClass<TabItem>(":selected", CssStates.Checked);

        // A button with a flyout is a GtkMenuButton, whose button is :checked while its popover is open; a drop-down
        // button's template shows that on its own button.
        CssNodeProviders.MapPseudoClass<Button>(":flyout-open", CssStates.Checked, (b, owner) => ReferenceEquals(b, owner) && b is not DropDownButton);
        CssNodeProviders.Register<CalendarDayButton>(b => DayClasses(b));
        CssNodeProviders.Register<CommandBarButton>(b => CommandClasses(b.Icon, b.Label), CommandBarButton.IconProperty, CommandBarButton.LabelProperty);
        CssNodeProviders.Register<CommandBarToggleButton>(b => CommandClasses(b.Icon, b.Label), CommandBarToggleButton.IconProperty, CommandBarToggleButton.LabelProperty);
        CssNodeProviders.Register<Avalonia.Controls.Notifications.NotificationCard>(c =>
            c.Classes.Contains(":error") ? ["error"] : c.Classes.Contains(":warning") ? ["warning"] : []);
        CssNodeProviders.Register<TextSelectionHandle>(h =>
            h.Classes.Contains(":caret") ? ["insertion-cursor"] : h.Classes.Contains(":start") ? ["top"] : ["bottom"]);
        CssNodeProviders.Register<CalendarButton>(b => b.Classes.Contains(":inactive") ? ["other-month"] : []);
        CssNodeProviders.MapPseudoClass<TabStripItem>(":selected", CssStates.Checked);
        CssNodeProviders.Register<SplitButton>(s => s.Content switch
        {
            string => ["text-button"],
            AvaAdwaita.Presenters.AdwIcon or PathIcon or Image => ["image-button"],
            _ => [],
        }, ContentControl.ContentProperty);
        CssNodeProviders.Register<ScrollBar>(ScrollBarClasses, ScrollBar.OrientationProperty, ScrollBar.AllowAutoHideProperty, ScrollBar.IsExpandedProperty);
        CssNodeProviders.Register<Slider>(s => [Orientation(s.Orientation)], Slider.OrientationProperty);

        // gtk_scale_add_mark: the scale is .marks-before and/or .marks-after while it has marks on that side.
        CssNodeProviders.Register<Slider>(
            s => (Presenters.AdwScaleMarks.Ticks(s, before: true).Count > 0, Presenters.AdwScaleMarks.Ticks(s, before: false).Count > 0) switch
            {
                (true, true) => ["marks-before", "marks-after"],
                (true, false) => ["marks-before"],
                (false, true) => ["marks-after"],
                _ => [],
            },
            Slider.TickPlacementProperty,
            Slider.TicksProperty,
            Slider.TickFrequencyProperty,
            Avalonia.Controls.Primitives.RangeBase.MinimumProperty,
            Avalonia.Controls.Primitives.RangeBase.MaximumProperty);
        CssNodeProviders.Register<ProgressBar>(p => [Orientation(p.Orientation)], ProgressBar.OrientationProperty);
        // A row that says itself whether it is activatable (a libadwaita preference row) adds .activatable itself.
        CssNodeProviders.Register<ListBoxItem>(i => i is ComboBoxItem or IOwnActivatable ? [] : ["activatable"]);
        CssNodeProviders.Register<Control>(c => DataValidationErrors.GetHasErrors(c) ? ["error"] : [], DataValidationErrors.HasErrorsProperty);
        CssNodeProviders.Register<MenuItem>(MenuItemClasses, MenuItem.ToggleTypeProperty);
        CssNodeProviders.MapPseudoClass<ComboBox>(":dropdownopen", CssStates.Checked, (_, owner) => Css.GetNode(owner) == "button");

        // A drop-down's toggle is :checked while the popup is open, as GtkDropDown's.
        CssNodeProviders.MapPseudoClass<ComboBox>(":pressed", CssStates.None);
        CssNodeProviders.MapPseudoClass<ComboBoxItem>(":selected", CssStates.Selected);
        CssNodeProviders.MapPseudoClass<ListBoxItem>(":selected", CssStates.Selected);
        CssNodeProviders.MapPseudoClass<MenuItem>(":open", CssStates.Selected);
        CssNodeProviders.MapPseudoClass<MenuItem>(":selected", CssStates.Hover);
        CssNodeProviders.MapPseudoClass<ToggleSwitch>(":checked", CssStates.Checked);
    }

    // In a vertical stack a separator is horizontal; in a horizontal one, vertical.
    private static IEnumerable<string> SeparatorClasses(Separator s)
    {
        yield return s.Parent is StackPanel { Orientation: Avalonia.Layout.Orientation.Horizontal } ? "vertical" : "horizontal";
    }

    internal static string Orientation(Avalonia.Layout.Orientation o) => o == Avalonia.Layout.Orientation.Vertical ? "vertical" : "horizontal";

    private static IEnumerable<string> ScrollBarClasses(ScrollBar s)
    {
        yield return Orientation(s.Orientation);
        if (s.AllowAutoHide)
        {
            yield return "overlay-indicator";
            if (s.IsExpanded)
            {
                yield return "hovering";
            }
        }
    }

    private static IEnumerable<string> MenuItemClasses(MenuItem m)
    {
        // A menu bar's items are item nodes; the others are flat model buttons.
        if (Css.GetNode(m) == "modelbutton")
        {
            yield return "flat";
        }
    }

    // A toolbar command is an image, text or image-text button by what it shows.
    private static IEnumerable<string> CommandClasses(object? icon, string? label)
    {
        var text = !string.IsNullOrEmpty(label);
        yield return icon is null ? "text-button" : text ? "image-text-button" : "image-button";
    }

    // GtkCalendar marks days outside the month and today.
    private static IEnumerable<string> DayClasses(CalendarDayButton b)
    {
        if (b.Classes.Contains(":inactive"))
        {
            yield return "other-month";
        }

        if (b.Classes.Contains(":today"))
        {
            yield return "today";
        }
    }

    private static IEnumerable<string> ButtonClasses(ContentControl b)
    {
        // Only button and check button nodes get GtkButton's classes; a calendar day is a label.
        if (Css.GetNode(b) is not ("button" or "checkbutton"))
        {
            yield break;
        }

        // A menu button carries its classes on the inner button; a hyperlink keeps .text-button.
        if (b.Classes.Contains("adw-no-auto-class") || b is DropDownButton)
        {
            yield break;
        }

        // gtk_button_set_child: a label child makes a text button, as a string does.
        switch (b.Content)
        {
            case string or Label:
                yield return "text-button";
                break;
            case Avalonia.Controls.Shapes.Path or PathIcon or Image or AvaAdwaita.Presenters.AdwIcon:
                // gtk_button_set_icon_name centers the image vertically; it fills the width.
                ((Control)b.Content).SetValue(Layoutable.VerticalAlignmentProperty, Avalonia.Layout.VerticalAlignment.Center, Avalonia.Data.BindingPriority.Style);
                yield return "image-button";
                break;
        }
    }
}
