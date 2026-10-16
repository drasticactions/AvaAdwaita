using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using AvaAdwaita.Presenters;

namespace AvaAdwaita.Theme.Tests;

/// <summary>Builds the Avalonia equivalent of a corpus scene's GTK widget.</summary>
internal static class ParityScene
{
    private static readonly System.Uri ExampleUri = new("https://example.com");

    public static Control Build(JsonObject spec)
    {
        var type = (string)spec["type"]!;
        var label = (string?)spec["label"];
        var icon = (string?)spec["icon"];
        var active = (bool?)spec["active"] ?? false;
        Control c = type switch
        {
            "button" => new Button { Content = icon is null ? label ?? "Button" : new AdwIcon { IconName = icon } },
            "togglebutton" => new ToggleButton { Content = icon is null ? label ?? "Toggle" : new AdwIcon { IconName = icon }, IsChecked = active },
            "checkbutton" => (bool?)spec["inconsistent"] == true
                ? new CheckBox { Content = label, IsThreeState = true, IsChecked = null }
                : new CheckBox { Content = label, IsChecked = active },
            "radiobutton" => new RadioButton { Content = label, IsChecked = active },
            "switch" => new ToggleSwitch { IsChecked = active },
            "entry" => new TextBox { Text = (string?)spec["text"], PlaceholderText = (string?)spec["placeholder"] },
            "label" => new Label { Content = label ?? "Label" },
            "listbox" => new ListBox { ItemsSource = spec["rows"]!.AsArray().Select(r => (string)r!).ToArray() },
            "scale" => new Slider
            {
                Minimum = 0,
                Maximum = 100,
                Value = (double?)spec["value"] ?? 40,
                TickFrequency = 25,
                TickPlacement = (string?)spec["marks"] switch { "top" => TickPlacement.TopLeft, "bottom" => TickPlacement.BottomRight, _ => TickPlacement.None },
            },
            "progressbar" => new ProgressBar { Minimum = 0, Maximum = 100, Value = (double?)spec["value"] ?? 40 },
            "separator" => new Separator(),
            "dropdown" => new ComboBox { ItemsSource = new[] { "First", "Second" }, SelectedIndex = 0 },
            "menubutton" => new DropDownButton { Content = label ?? "Menu", Flyout = new MenuFlyout { Items = { new MenuItem { Header = "Item" } } } },
            "splitbutton" => new SplitButton { Content = label ?? "Split", Flyout = new MenuFlyout { Items = { new MenuItem { Header = "Item" } } } },
            "spinbutton" => new NumericUpDown { Minimum = 0, Maximum = 100, Increment = 1, Value = (decimal)((double?)spec["value"] ?? 40) },
            "notebook" => new TabControl
            {
                TabStripPlacement = (string?)spec["position"] == "bottom" ? Dock.Bottom : Dock.Top,
                Items = { new TabItem { Header = "Tab 1", Content = "First page" }, new TabItem { Header = "Tab 2", Content = "Second page" } },
            },
            "expander" => new Expander { Header = label ?? "Expander", Content = "Content", IsExpanded = active },
            "frame" => new GroupBox { Header = label ?? "Frame", Content = "Content" },
            "splitview" => new SplitView { DisplayMode = SplitViewDisplayMode.Inline, IsPaneOpen = true, OpenPaneLength = 180, Pane = new Label { Content = "Sidebar", HorizontalContentAlignment = HorizontalAlignment.Center }, Content = new Label { Content = "Content", HorizontalContentAlignment = HorizontalAlignment.Center } },
            "treeview" => new TreeView
            {
                Items =
                {
                    new TreeViewItem { Header = "Folder", IsExpanded = active, Items = { new TreeViewItem { Header = "File one" }, new TreeViewItem { Header = "File two" } } },
                    new TreeViewItem { Header = "Other" },
                },
            }.Also(t => t.SelectedItem = t.Items[0]),
            // Avalonia's table view has no content-sized columns, so the columns take GTK's natural widths.
            "columnview" => new TableView
            {
                Columns =
                {
                    new TableViewColumn { Header = "Name", Binding = new Avalonia.Data.Binding("Name"), Width = new GridLength(69) },
                    new TableViewColumn { Header = "Size", Binding = new Avalonia.Data.Binding("Size"), Width = new GridLength(60) },
                },
                ItemsSource = new[] { new Row("Alpha", "15 KB"), new Row("Beta", "12 KB") },
            }.Also(t => t.SelectedIndex = 0),
            "datagrid" => new Avalonia.Controls.DataGrid
            {
                IsReadOnly = true,
                GridLinesVisibility = ((bool?)spec["row-separators"] == true ? DataGridGridLinesVisibility.Horizontal : DataGridGridLinesVisibility.None)
                    | ((bool?)spec["column-separators"] == true ? DataGridGridLinesVisibility.Vertical : DataGridGridLinesVisibility.None),
                Columns =
                {
                    new DataGridTextColumn { Header = "Name", Binding = new Avalonia.Data.Binding("Name"), Width = new DataGridLength(69) },
                    new DataGridTextColumn { Header = "Size", Binding = new Avalonia.Data.Binding("Size"), Width = new DataGridLength(60) },
                },
                ItemsSource = new[] { new Row("Alpha", "15 KB"), new Row("Beta", "12 KB") },
            }.Also(g => g.SelectedIndex = 0),
            "headerbar" => new Label(),
            "toast" => new Label(),
            "tooltip" => new ToolTip { Content = (string?)spec["label"] },
            "spinner" => new AdwSpinner { Width = 32, Height = 32 },
            "carouseldots" => new PipsPager { NumberOfPages = 3, SelectedPageIndex = 0, IsPreviousButtonVisible = false, IsNextButtonVisible = false },

            // A time picker's minute column against a vertical GtkSpinButton at 30.
            "vspinbutton" => new TimePickerPresenter { Time = new System.TimeSpan(12, 30, 0), MinuteIncrement = 1 },

            // GTK's C locale starts the week on Sunday. A past month keeps the real today off the grid.
            "calendar" => new Calendar
            {
                DisplayDate = new System.DateTime(2015, 10, 15),
                SelectedDate = new System.DateTime(2015, 10, 15),
                FirstDayOfWeek = System.DayOfWeek.Sunday,
            },
            "paned" => new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("Auto,Auto,Auto"),
                Children =
                {
                    new Label { Content = "Start" },
                    new GridSplitter { [Grid.ColumnProperty] = 1, ResizeDirection = GridResizeDirection.Columns },
                    new Label { Content = "End", [Grid.ColumnProperty] = 2 },
                },
            },

            // Away from the window's edge, so the popover's shadow and the golden's margin stay inside it.
            "popovermenu" => new DropDownButton
            {
                Margin = new Avalonia.Thickness(80, 0, 0, 0),
                Content = "Menu",
                Flyout = new MenuFlyout { Items = { new MenuItem { Header = "Open" }, new MenuItem { Header = "Save As" }, new MenuItem { Header = "Quit" } } },
            },
            "menubar" => new Menu
            {
                Items =
                {
                    new MenuItem { Header = "File", Items = { new MenuItem { Header = "Item" } } },
                    new MenuItem { Header = "Edit", Items = { new MenuItem { Header = "Item" } } },
                    new MenuItem { Header = "View", Items = { new MenuItem { Header = "Item" } } },
                },
            },
            "scrolledwindow" => new ScrollViewer
            {
                Width = 200,
                Height = 120,
                AllowAutoHide = (bool?)spec["classic"] != true,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = new Label { Width = 400, Height = 300 },
            },
            "textview" => new TextBox { AcceptsReturn = true, Text = (string?)spec["text"], Width = 200, Height = 80 },
            "togglegroup" => new TabControl
            {
                Classes = { "view-switcher" },
                Items = { new TabItem { Header = "First" }, new TabItem { Header = "Second" }, new TabItem { Header = "Third" } },
            },
            "linkbutton" => new HyperlinkButton { Content = label ?? "Link", NavigateUri = ExampleUri, IsVisited = (bool?)spec["visited"] ?? false },
            _ => new TextBlock { Text = type },
        };

        foreach (var cls in spec["classes"]?.AsArray() ?? [])
        {
            c.Classes.Add((string)cls!);
        }

        if ((double?)spec["width"] is { } w)
        {
            c.MinWidth = w;
        }

        c.HorizontalAlignment = HorizontalAlignment.Left;
        c.VerticalAlignment = VerticalAlignment.Top;
        return c;
    }

    /// <summary>
    /// The element GTK's widget corresponds to: the control, for a view switcher its toggle group, and for a menu
    /// the popover the button opened.
    /// </summary>
    public static Control Root(Control control, JsonObject spec) => (string)spec["type"]! switch
    {
        "togglegroup" => control.GetVisualDescendants().OfType<Control>().First(c => AvaAdwaita.Nodes.Css.GetNode(c) == "toggle-group"),
        "vspinbutton" => control.GetVisualDescendants().OfType<Control>().First(c => c.Name == "PART_MinuteHost").GetVisualDescendants().OfType<Control>().First(c => AvaAdwaita.Nodes.Css.GetCssNode(c)?.Name == "spinbutton"),
        "toast" => TopLevel.GetTopLevel(control)!.GetVisualDescendants().OfType<Avalonia.Controls.Notifications.NotificationCard>().First(),
        "headerbar" => TopLevel.GetTopLevel(control)!.GetVisualDescendants().OfType<Control>().First(c => AvaAdwaita.Nodes.Css.GetCssNode(c)?.Name == "headerbar"),
        "popovermenu" => TopLevel.GetTopLevel(control)!.GetVisualDescendants().OfType<Control>().First(c => AvaAdwaita.Nodes.Css.GetCssNode(c)?.Name == "popover"),
        _ => control,
    };

    private static double? s_windowWidth;
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<TopLevel, Avalonia.Controls.Notifications.WindowNotificationManager> s_notifications = new();

    /// <summary>
    /// Opens what the scene shows open: a menu button's menu. A header bar scene is as wide as GTK's window, so
    /// the window is sized to give the header bar GTK's width.
    /// </summary>
    public static void Open(Control control, JsonObject spec, JsonObject gtk)
    {
        if ((string)spec["type"]! == "headerbar" && TopLevel.GetTopLevel(control) is Window w && Root(control, spec) is { } bar)
        {
            // GTK's size is the header bar's border box, which its negative margins make wider than the element.
            double Width() => new AvaAdwaita.Presenters.CssBoxGeometry(AvaAdwaita.Nodes.Css.GetCssNode(bar)!.Style!, bar.Bounds.Size).BorderBox.Width;
            s_windowWidth = w.Width;
            for (var i = 0; i < 3 && Math.Abs((double)gtk["size"]![0]! - Width()) > 0.5; i++)
            {
                w.Width += (double)gtk["size"]![0]! - Width();
                ParityRunner.Settle();
            }
        }

        // A toast is a notification the window shows; it stays until the scene closes it.
        if ((string)spec["type"]! == "toast" && TopLevel.GetTopLevel(control) is { } top)
        {
            var manager = s_notifications.GetValue(top, t => new Avalonia.Controls.Notifications.WindowNotificationManager(t)
            {
                Position = Avalonia.Controls.Notifications.NotificationPosition.BottomCenter,

                // Clear of the window's bottom edge, so the golden's margin below the toast is inside the window.
                Margin = new Avalonia.Thickness(0, 0, 0, 40),
            });
            ParityRunner.Settle();
            manager.Show(new Avalonia.Controls.Notifications.Notification((string?)spec["label"], null, Avalonia.Controls.Notifications.NotificationType.Information, System.TimeSpan.Zero));

            // Let the toast finish its 200 ms fade-in, as GTK's has shown.
            var start = System.Environment.TickCount64;
            while (System.Environment.TickCount64 - start < 2000
                && (System.Environment.TickCount64 - start < 300
                    || top.GetVisualDescendants().OfType<Avalonia.Controls.Notifications.NotificationCard>().FirstOrDefault() is not { Opacity: >= 1 }))
            {
                System.Threading.Thread.Sleep(16);
                ParityRunner.Settle();
            }

        }

        // GTK's spin button stands alone: the rest of the picker does not show.
        if ((string)spec["type"]! == "vspinbutton")
        {
            foreach (var c in control.GetVisualDescendants().OfType<Control>())
            {
                if (c.Name is "PART_HourHost" or "PART_PeriodHost" or "PART_SecondHost" or "PART_AcceptButton" or "PART_DismissButton")
                {
                    c.Opacity = 0;
                }
            }
        }

        if ((string)spec["type"]! == "popovermenu" && control is Button { Flyout: { } flyout })
        {
            // GTK renders the popover's surface alone, so the button that opened it does not show.
            flyout.ShowAt(control);
            control.Opacity = 0;
        }
    }

    /// <summary>Closes what <see cref="Open"/> opened.</summary>
    public static void Close(Control control)
    {
        if (TopLevel.GetTopLevel(control) is { } top)
        {
            foreach (var card in top.GetVisualDescendants().OfType<Avalonia.Controls.Notifications.NotificationCard>().ToList())
            {
                card.Close();
            }
        }

        if (s_windowWidth is { } width && TopLevel.GetTopLevel(control) is Window w)
        {
            w.Width = width;
            s_windowWidth = null;
        }

        if (control is Button { Flyout: { IsOpen: true } flyout })
        {
            flyout.Hide();
        }
    }

    /// <summary>Sets the scene's states; <c>backdrop</c> is returned for the caller to apply to the window.</summary>
    public static bool ApplyStates(Control c, JsonObject spec)
    {
        var backdrop = false;
        if (c is Avalonia.Controls.DataGrid grid && (bool?)spec["active"] == true)
        {
            grid.Columns[0].Sort(System.ComponentModel.ListSortDirection.Ascending);
        }

        var pseudo = (IPseudoClasses)c.Classes;
        foreach (var s in spec["states"]?.AsArray().Select(x => (string)x!) ?? [])
        {
            switch (s)
            {
                case "hover":
                    pseudo.Set(":pointerover", true);
                    break;
                case "active":
                    pseudo.Set(":pressed", true);
                    break;
                case "disabled":
                    c.IsEnabled = false;
                    break;
                case "focus-visible":
                    pseudo.Set(":focus", true);
                    pseudo.Set(":focus-visible", true);
                    pseudo.Set(":focus-within", true);
                    break;
                case "selected":
                    pseudo.Set(":selected", true);
                    break;
                case "backdrop":
                    backdrop = true;
                    break;
            }
        }

        return backdrop;
    }

    public static IEnumerable<string> States(JsonObject spec) => spec["states"]?.AsArray().Select(x => (string)x!) ?? [];

    public static FlowDirection Direction(JsonObject entry) => (bool?)entry["rtl"] == true ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

    // GTK's single-selection models select the first item; the scenes do the same.
    private static T Also<T>(this T value, System.Action<T> action)
    {
        action(value);
        return value;
    }

    /// <summary>A column view row: a name and a size.</summary>
    public sealed record Row(string Name, string Size);
}
