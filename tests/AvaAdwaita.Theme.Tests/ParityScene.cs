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
            "searchentry" => new Controls.SearchEntry { Text = (string?)spec["text"], PlaceholderText = (string?)spec["placeholder"] },
            "searchbar" => new Controls.SearchBar
            {
                Content = new Controls.SearchEntry { PlaceholderText = "Search" },
                ShowCloseButton = (bool?)spec["button"] == true,
                IsSearchMode = true,
            },
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
            "actionrow" => Rows(PrefRow(new Controls.ActionRow(), spec)),
            "buttonrow" => Rows(PrefRow(new Controls.ButtonRow { StartIconName = (string?)spec["start-icon"], EndIconName = (string?)spec["end-icon"] }, spec)),
            "spinrow" => Rows(PrefRow(new Controls.SpinRow { Minimum = 0, Maximum = 100, Increment = 1, Value = (decimal)((double?)spec["value"] ?? 40) }, spec)),
            "comborow" => Rows(PrefRow(new Controls.ComboRow { ItemsSource = new[] { "First", "Second" }, SelectedIndex = 0 }, spec)),
            "expanderrow" => Rows(PrefRow(new Controls.ExpanderRow { ShowEnableSwitch = (bool?)spec["switch"] == true, Rows = { new Controls.ActionRow { Title = "Nested" } } }, spec).Also(r => r.IsExpanded = active)),
            "entryrow" => Rows(PrefRow(new Controls.EntryRow { Text = (string?)spec["text"] }, spec)),
            "passwordentryrow" => Rows(PrefRow(new Controls.PasswordEntryRow { Text = (string?)spec["text"] }, spec)),
            "preferencesgroup" => new Controls.PreferencesGroup
            {
                Title = (string?)spec["title"] ?? "Group",
                Description = (string?)spec["description"],
                HeaderSuffix = (bool?)spec["suffix"] == true ? RowSuffix() : null,
                SeparateRows = (bool?)spec["separate"] == true,
                Children = { new Controls.ActionRow { Title = "First" }, new Controls.ActionRow { Title = "Second" } },
            },
            "clamp" => new Controls.Clamp { Child = new Button { Content = "Clamped", HorizontalAlignment = HorizontalAlignment.Stretch } },
            "preferencespage" => new Controls.PreferencesPage
            {
                Description = (string?)spec["description"],
                DescriptionCentered = (bool?)spec["centered"] == true,
                Children = { new Controls.PreferencesGroup { Title = "Group", Children = { new Controls.ActionRow { Title = "First" }, new Controls.ActionRow { Title = "Second" } } } },
            },
            "levelbar" => new Controls.LevelBar
            {
                Minimum = 0,
                Maximum = (double?)spec["max"] ?? 1,
                Value = (double?)spec["value"] ?? 0.4,
                Mode = spec["discrete"] is null ? Controls.LevelBarMode.Continuous : Controls.LevelBarMode.Discrete,
            },
            "avatar" => new Controls.Avatar { Size = (double?)spec["size"] ?? 48, Text = (string?)spec["text"], ShowInitials = (bool?)spec["initials"] == true },
            "statuspage" => new Controls.StatusPage
            {
                Title = (string?)spec["title"] ?? "No Results",
                Description = (string?)spec["description"],
                IconName = icon,
                Content = (bool?)spec["suffix"] == true ? new Button { Content = "Retry" } : null,
            },
            "banner" => new Controls.Banner
            {
                Title = (string?)spec["title"] ?? "Banner title",
                ButtonLabel = (string?)spec["button"],
                ButtonStyle = (bool?)spec["suggested"] == true ? Controls.BannerButtonStyle.Suggested : Controls.BannerButtonStyle.Default,
                IsRevealed = true,
            },
            "windowtitle" => new Controls.WindowTitle { Title = (string?)spec["title"] ?? "Title", Subtitle = (string?)spec["subtitle"] },
            "adwheaderbar" => new Controls.HeaderBar
            {
                TitleWidget = spec["title"] is null ? null : new Controls.WindowTitle { Title = (string?)spec["title"], Subtitle = (string?)spec["subtitle"] },
                ShowStartTitleButtons = (bool?)spec["nobuttons"] != true,
                ShowEndTitleButtons = (bool?)spec["nobuttons"] != true,
            }.Also(h =>
            {
                if ((bool?)spec["prefix"] == true)
                {
                    h.Start.Add(new Button { Content = new AdwIcon { IconName = "list-add-symbolic" } });
                }

                if ((bool?)spec["suffix"] == true)
                {
                    h.End.Add(new Button { Content = new AdwIcon { IconName = "open-menu-symbolic" } });
                }
            }),
            "actionbar" => new Controls.ActionBar
            {
                Start = { new Button { Content = "Start" } },
                CenterWidget = spec["title"] is null ? null : new Label { Content = (string?)spec["title"] },
            }.Also(b =>
            {
                if ((bool?)spec["suffix"] == true)
                {
                    b.End.Add(new Button { Content = new AdwIcon { IconName = "open-menu-symbolic" } });
                    b.End.Add(new Button { Content = "End" });
                }
            }),
            "toolbarview" => new Controls.ToolbarView
            {
                Content = new Label { Content = "Content", HorizontalContentAlignment = HorizontalAlignment.Center },
                TopBars = { new Controls.HeaderBar { ShowStartTitleButtons = false, ShowEndTitleButtons = false, TitleWidget = new Controls.WindowTitle { Title = "Title" } } },
                TopBarStyle = ToolbarStyleOf(spec),
                BottomBarStyle = ToolbarStyleOf(spec),
            }.Also(v =>
            {
                if ((bool?)spec["bottom"] == true)
                {
                    v.BottomBars.Add(new Controls.ActionBar { Start = { new Button { Content = "Action" } } });
                }
            }),
            "viewstack" => ViewStackOf(spec),
            "viewswitcher" => new Controls.ViewSwitcher
            {
                Stack = ViewStackOf(spec),
                Policy = (bool?)spec["wide"] == true ? Controls.ViewSwitcherPolicy.Wide : Controls.ViewSwitcherPolicy.Narrow,
            },
            "viewswitcherbar" => new Controls.ViewSwitcherBar { Stack = ViewStackOf(spec), Reveal = true },
            "inlineviewswitcher" => new Controls.InlineViewSwitcher
            {
                Stack = ViewStackOf(spec),
                DisplayMode = (string?)spec["mode"] switch
                {
                    "icons" => Controls.InlineViewSwitcherDisplayMode.Icons,
                    "both" => Controls.InlineViewSwitcherDisplayMode.Both,
                    _ => Controls.InlineViewSwitcherDisplayMode.Labels,
                },
            },
            // GtkStackSwitcher over a GtkStack whose pages have titles and no icons.
            "stackswitcher" => new Controls.StackSwitcher { Stack = ViewStackOf(spec).Also(s => { foreach (var p in s.Pages) { p.IconName = null; } }) },
            "overlaysplitview" => new Controls.OverlaySplitView
            {
                Sidebar = new Label { Content = "Sidebar", HorizontalContentAlignment = HorizontalAlignment.Center },
                Content = new Label { Content = "Content", HorizontalContentAlignment = HorizontalAlignment.Center },
                IsCollapsed = (bool?)spec["collapsed"] == true,
                SidebarPosition = (bool?)spec["end"] == true ? Controls.PackType.End : Controls.PackType.Start,
            }.Also(v => v.ShowSidebar = (bool?)spec["hidden"] != true),
            "navigationview" => new Controls.NavigationView { AnimateTransitions = false, Pages = { NavPage("Home", "home") } }.Also(v =>
            {
                if (active)
                {
                    v.Push(NavPage("Details", null));
                }
            }),
            "tabbar" => new Controls.TabBar
            {
                View = TabViewOf(spec),
                Autohide = false,
                IsInverted = (bool?)spec["inverted"] == true,
                ExpandTabs = (bool?)spec["fixed"] != true,
                EndActionWidget = (bool?)spec["button"] == true ? new Button { Content = new AdwIcon { IconName = "list-add-symbolic" } } : null,
            },
            "wrapbox" => WrapBoxOf(spec),
            "shortcutlabel" => new Controls.ShortcutLabel { Accelerator = label ?? string.Empty, DisabledText = "Disabled" },
            "tabview" => TabViewOf(spec),
            "tabbutton" => new Controls.TabButton { View = TabViewOf(spec) },
            "taboverview" => TabOverviewOf(spec),
            "bottomsheet" => new Controls.BottomSheet
            {
                Content = new Label { Content = "Content", HorizontalContentAlignment = HorizontalAlignment.Center, VerticalContentAlignment = VerticalAlignment.Center },
                Sheet = new Controls.ToolbarView
                {
                    TopBars = { new Controls.HeaderBar() },
                    Content = new Label { Content = "Sheet", HorizontalContentAlignment = HorizontalAlignment.Center, VerticalContentAlignment = VerticalAlignment.Center },
                },
                BottomBar = (bool?)spec["bar"] == true ? new Label { Content = "Bottom Bar", HorizontalContentAlignment = HorizontalAlignment.Center } : null,
                FullWidth = (bool?)spec["narrow"] != true,
                ShowDragHandle = (bool?)spec["nohandle"] != true,
            }.Also(v => v.IsOpen = active),
            "navigationsplitview" => new Controls.NavigationSplitView
            {
                Sidebar = NavPage("Sidebar", null),
                Content = NavPage("Content", null),
                IsCollapsed = (bool?)spec["collapsed"] == true,
                ShowContent = active,
            },
            // A dialog scene presents its dialog when it opens; the control is where it is presented from.
            var t when IsDialog(t) => new Border { Width = 1, Height = 1 },
            "switchrow" => Rows(PrefRow(new Controls.SwitchRow { IsChecked = active }, spec)),
            "linkbutton" => new HyperlinkButton { Content = label ?? "Link", NavigateUri = ExampleUri, IsVisited = (bool?)spec["visited"] ?? false },
            _ => new TextBlock { Text = type },
        };

        // A row scene's classes are the row's.
        var classed = c is ListBox rows && type.EndsWith("row", StringComparison.Ordinal) ? (Control)rows.Items[0]! : c;
        foreach (var cls in spec["classes"]?.AsArray() ?? [])
        {
            classed.Classes.Add((string)cls!);
        }

        if ((double?)spec["width"] is { } w)
        {
            c.MinWidth = w;
        }

        if ((double?)spec["height"] is { } h)
        {
            c.MinHeight = h;
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
        var t when IsDialog(t) => s_dialog!.Sheet!,
        _ when (bool?)spec["open"] == true => TopLevel.GetTopLevel(control)!.GetVisualDescendants().OfType<Control>().First(c => AvaAdwaita.Nodes.Css.GetCssNode(c)?.Name == "popover"),
        "popovermenu" => TopLevel.GetTopLevel(control)!.GetVisualDescendants().OfType<Control>().First(c => AvaAdwaita.Nodes.Css.GetCssNode(c)?.Name == "popover"),
        _ => control,
    };

    private static double? s_windowWidth;
    private static Controls.Dialog? s_dialog;

    // A dialog scene, as the oracle tells them: its type ends in "dialog".
    private static bool IsDialog(string type) => type.EndsWith("dialog", System.StringComparison.Ordinal);
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

        if (IsDialog((string)spec["type"]!))
        {
            s_dialog = DialogOf(spec);
            s_dialog.Present(control);
            if (s_dialog is Controls.ShortcutsDialog)
            {
                // Its height goes when it is shown, so the scene sets it then, as the oracle does.
                s_dialog.ContentHeight = (double?)spec["content-height"] ?? double.NaN;
            }

            ParityRunner.Settle();
        }

        // An open row scene: the row's popover, which GTK renders alone.
        if ((bool?)spec["open"] == true && control is ListBox { Items: [Controls.PreferencesRow row, ..] })
        {
            row.Activate();
            control.Opacity = 0;
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

        if (s_dialog is { } dialog)
        {
            dialog.ForceClose();
            s_dialog = null;
        }

        if (control is ListBox { Items: [Controls.ComboRow { IsDropDownOpen: true } combo, ..] })
        {
            combo.IsDropDownOpen = false;
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

        // A row scene's states are the row's, not its list's.
        if (c is ListBox list && ((string)spec["type"]!).EndsWith("row", StringComparison.Ordinal) && list.Items[0] is Control row)
        {
            c = row;
        }

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

    // Three pages, the first visible, as the oracle's view_stack: a badge on the second and attention on the third.
    private static Controls.ViewStack ViewStackOf(JsonObject spec)
    {
        var stack = new Controls.ViewStack();
        string[] titles = ["Albums", "Artists", "Songs"];
        string[] icons = ["view-grid-symbolic", "view-list-symbolic", "audio-volume-high-symbolic"];
        for (var i = 0; i < 3; i++)
        {
            stack.Pages.Add(new Controls.ViewStackPage
            {
                Name = titles[i],
                Title = titles[i],
                IconName = icons[i],
                Content = titles[i],
                BadgeNumber = i == 1 && (bool?)spec["badge"] == true ? 3 : 0,
                NeedsAttention = i == 2 && (bool?)spec["attention"] == true,
            });
        }

        return stack;
    }

    // The oracle's wrap box of seven buttons.
    private static Controls.WrapBox WrapBoxOf(JsonObject spec)
    {
        var box = new Controls.WrapBox
        {
            ChildSpacing = 6,
            LineSpacing = 6,
            NaturalLineLength = (double?)spec["length"] ?? -1,
            Align = (bool?)spec["centered"] == true ? 0.5 : 0,
            Justify = (string?)spec["mode"] switch
            {
                "fill" => Controls.JustifyMode.Fill,
                "spread" => Controls.JustifyMode.Spread,
                _ => Controls.JustifyMode.None,
            },
        };
        foreach (var l in new[] { "One", "Two", "Three", "Four", "Five", "Six", "Seven" })
        {
            box.Children.Add(new Button { Content = l });
        }

        return box;
    }

    // The dialog a dialog scene presents, as the oracle's make_dialog.
    private static Controls.Dialog DialogOf(JsonObject spec) => (string)spec["type"]! switch
    {
        "alertdialog" => AlertOf(spec),
        "preferencesdialog" => PreferencesOf(spec),
        "aboutdialog" => AboutOf(spec),
        "shortcutsdialog" => ShortcutsOf(spec),
        _ => PlainDialogOf(spec),
    };

    // The oracle's shortcuts dialog: a section of two shortcuts, one with a subtitle, and a section of one.
    private static Controls.ShortcutsDialog ShortcutsOf(JsonObject spec)
    {
        var dialog = new Controls.ShortcutsDialog { ContentWidth = (double?)spec["content-width"] ?? 600 };
        dialog.Add(new Controls.ShortcutsSection("General")
        {
            Items = { new("Open", "<Control>O"), new("Quit", "<Control>Q") { Subtitle = "Close every window" } },
        });
        dialog.Add(new Controls.ShortcutsSection("Editing") { Items = { new("Copy", "<Control>C") } });
        return dialog;
    }

    private static Controls.Dialog PlainDialogOf(JsonObject spec) => new Controls.Dialog
    {
        Title = (string?)spec["title"] ?? "Dialog",
        ContentWidth = (double?)spec["content-width"] ?? 300,
        ContentHeight = (double?)spec["content-height"] ?? 200,
        PresentationMode = (bool?)spec["sheet"] == true ? Controls.DialogPresentationMode.BottomSheet : Controls.DialogPresentationMode.Floating,
        Content = new Controls.ToolbarView
        {
            TopBars = { new Controls.HeaderBar() },
            Content = new Label { Content = "Content", HorizontalContentAlignment = HorizontalAlignment.Center, VerticalContentAlignment = VerticalAlignment.Center },
        },
    };

    // The oracle's tab overview, over its tab view.
    private static Controls.TabOverview TabOverviewOf(JsonObject spec)
    {
        var view = TabViewOf(spec);
        return new Controls.TabOverview
        {
            View = view,
            Content = view,
            EnableNewTab = (bool?)spec["button"] == true,
            IsOpen = (bool?)spec["active"] == true,
        };
    }

    // The oracle's tab view: pages "Page 1" on, the last one pinned with an icon, the second with an icon or needing
    // attention.
    private static Controls.TabView TabViewOf(JsonObject spec)
    {
        var view = new Controls.TabView();
        var n = (int?)(double?)spec["pages"] ?? 3;
        for (var i = 0; i < n; i++)
        {
            var title = $"Page {i + 1}";
            var page = view.Append(new Label { Content = title, HorizontalContentAlignment = HorizontalAlignment.Center, VerticalContentAlignment = VerticalAlignment.Center });
            page.Title = title;
            if (i == 1 && (bool?)spec["attention"] == true)
            {
                page.NeedsAttention = true;
            }

            if (i == 1 && (bool?)spec["page-icon"] == true)
            {
                page.IconName = "folder-symbolic";
            }
        }

        if ((bool?)spec["pinned"] == true)
        {
            var page = view.GetNthPage(n - 1);
            page.IconName = "folder-symbolic";
            view.SetPagePinned(page, true);
        }

        if ((double?)spec["selected"] is { } selected)
        {
            view.SelectedPage = view.GetNthPage((int)selected);
        }

        return view;
    }

    // The oracle's about dialog: the application, its website and issue page, two developers and a license.
    private static Controls.AboutDialog AboutOf(JsonObject spec)
    {
        var dialog = new Controls.AboutDialog
        {
            ApplicationIcon = "help-about-symbolic",
            ApplicationName = "Example",
            DeveloperName = "Example Developers",
            Version = "1.2.3",
            Website = "https://example.org",
            IssueUrl = "https://example.org/issues",
            Developers = { "Ada Lovelace", "Grace Hopper https://example.org/grace" },
            Copyright = "© 2026 Example Developers",
            LicenseType = Controls.License.Gpl30,
            Comments = (bool?)spec["comments"] == true ? "An example application." : null,
            ContentHeight = (double?)spec["content-height"] ?? double.NaN,
            PresentationMode = Controls.DialogPresentationMode.Floating,
        };
        return dialog;
    }

    // The oracle's preferences dialog: two pages, each a group with a switch row and an action row.
    private static Controls.PreferencesDialog PreferencesOf(JsonObject spec)
    {
        var dialog = new Controls.PreferencesDialog
        {
            ContentWidth = (double?)spec["content-width"] ?? 640,
            ContentHeight = (double?)spec["content-height"] ?? double.NaN,
            SearchEnabled = (bool?)spec["search"] == true,
            PresentationMode = Controls.DialogPresentationMode.Floating,
        };
        foreach (var (title, icon) in new[] { ("General", "preferences-system-symbolic"), ("Behavior", "view-grid-symbolic") })
        {
            dialog.Add(new Controls.PreferencesPage
            {
                Title = title,
                IconName = icon,
                Children =
                {
                    new Controls.PreferencesGroup
                    {
                        Title = "Group",
                        Children = { new Controls.SwitchRow { Title = "Switch" }, new Controls.ActionRow { Title = "Action", Subtitle = "Subtitle" } },
                    },
                },
            });
        }

        return dialog;
    }

    private static Controls.AlertDialog AlertOf(JsonObject spec)
    {
        var alert = new Controls.AlertDialog
        {
            Heading = (string?)spec["title"] ?? "Save Changes?",
            Body = (string?)spec["description"] ?? "Unsaved changes will be lost.",
            PresentationMode = (bool?)spec["sheet"] == true ? Controls.DialogPresentationMode.BottomSheet : Controls.DialogPresentationMode.Floating,
        };
        alert.AddResponse("cancel", "_Cancel");
        alert.AddResponse("save", "_Save", Controls.ResponseAppearance.Suggested);
        if ((bool?)spec["destructive"] == true)
        {
            alert.AddResponse("discard", "_Discard", Controls.ResponseAppearance.Destructive);
        }

        return alert;
    }

    // A page as the oracle's navigation_page: a toolbar view with a header bar (no window buttons) over a label.
    private static Controls.NavigationPage NavPage(string title, string? tag) => new()
    {
        Title = title,
        NavigationTag = tag,
        Content = new Controls.ToolbarView
        {
            TopBars = { new Controls.HeaderBar { ShowStartTitleButtons = false, ShowEndTitleButtons = false } },
            Content = new Label { Content = title, HorizontalContentAlignment = HorizontalAlignment.Center, VerticalContentAlignment = VerticalAlignment.Center },
        },
    };

    private static Controls.ToolbarStyle ToolbarStyleOf(JsonObject spec) => (string?)spec["style"] switch
    {
        "raised" => Controls.ToolbarStyle.Raised,
        "raised-border" => Controls.ToolbarStyle.RaisedBorder,
        _ => Controls.ToolbarStyle.Flat,
    };

    // A row in a boxed list, as the oracle shows each row scene.
    private static ListBox Rows(params Control[] rows)
    {
        var list = new ListBox { Classes = { "boxed-list" } };
        foreach (var r in rows)
        {
            list.Items.Add(r);
        }

        return list;
    }

    // The title, subtitle, a folder icon prefix and a flat edit button suffix, as the oracle's rows.
    private static T PrefRow<T>(T row, JsonObject spec)
        where T : Controls.PreferencesRow
    {
        row.Title = (string?)spec["title"] ?? "Title";
        if (row is Controls.ActionRow action)
        {
            action.Subtitle = (string?)spec["subtitle"];
            action.IsActivatable |= (bool?)spec["activatable"] == true;
            if ((bool?)spec["prefix"] == true)
            {
                action.Prefixes.Add(new AdwIcon { IconName = "folder-symbolic" });
            }

            if ((bool?)spec["suffix"] == true)
            {
                action.Suffixes.Add(RowSuffix());
            }
        }

        else if (row is Controls.EntryRow entry)
        {
            if ((bool?)spec["prefix"] == true)
            {
                entry.Prefixes.Add(new AdwIcon { IconName = "folder-symbolic" });
            }

            if ((bool?)spec["suffix"] == true)
            {
                entry.Suffixes.Add(RowSuffix());
            }
        }
        else if (row is Controls.ExpanderRow expander)
        {
            expander.Subtitle = (string?)spec["subtitle"];
            if ((bool?)spec["prefix"] == true)
            {
                expander.Prefixes.Add(new AdwIcon { IconName = "folder-symbolic" });
            }

            if ((bool?)spec["suffix"] == true)
            {
                expander.Suffixes.Add(RowSuffix());
            }
        }

        return row;
    }

    private static Button RowSuffix() => new() { Classes = { "flat" }, VerticalAlignment = VerticalAlignment.Center, Content = new AdwIcon { IconName = "edit-copy-symbolic" } };

    // GTK's single-selection models select the first item; the scenes do the same.
    private static T Also<T>(this T value, System.Action<T> action)
    {
        action(value);
        return value;
    }

    /// <summary>A column view row: a name and a size.</summary>
    public sealed record Row(string Name, string Size);
}
