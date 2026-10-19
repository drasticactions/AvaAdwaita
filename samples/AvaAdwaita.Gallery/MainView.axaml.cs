using System;
using System.Collections.Generic;
using Avalonia.Controls;
using AvaAdwaita.Gallery.Pages;

namespace AvaAdwaita.Gallery;

public partial class MainView : UserControl
{
    private static readonly (string Title, Func<Control> Create)[] s_pages =
    [
        ("Buttons", () => new ButtonsPage()),
        ("Toggles", () => new TogglesPage()),
        ("Text", () => new TextPage()),
        ("Lists", () => new ListsPage()),
        ("Rows & Preferences", () => new RowsPage()),
        ("Status & Feedback", () => new FeedbackPage()),
        ("Layout & Navigation", () => new LayoutPage()),
        ("Dialogs", () => new DialogsPage()),
        ("Search", () => new SearchPage()),
        ("Shortcuts", () => new ShortcutsPage()),
        ("Data Grid", () => new DataGridPage()),
        ("Menus & Popovers", () => new MenusPage()),
        ("Ranges", () => new RangesPage()),
        ("Tabs & Pages", () => new TabsPage()),
        ("Calendar & Pickers", () => new PickersPage()),
        ("Toasts & More", () => new ToastsPage()),
        ("Typography", () => new TypographyPage()),
        ("Icons", () => new IconsPage()),
        ("Colors", () => new ColorsPage()),
    ];

    private readonly Dictionary<int, Control> _created = new();

    public MainView()
    {
        InitializeComponent();
        var titles = new List<string>();
        foreach (var p in s_pages)
        {
            titles.Add(p.Title);
        }

        Pages.ItemsSource = titles;
        Pages.SelectionChanged += (_, _) => Show(Pages.SelectedIndex);
        Pages.SelectedIndex = 0;
    }

    public IReadOnlyList<string> PageTitles => (IReadOnlyList<string>)Pages.ItemsSource!;

    public void SelectPage(int index) => Pages.SelectedIndex = index;

    public double PageExtentHeight => PageHost.Extent.Height;

    public bool ShowsOwnHeader
    {
        get => OwnHeader.IsVisible;
        set => OwnHeader.IsVisible = value;
    }

    public bool IsSidebarVisible
    {
        get => Split.IsPaneOpen;
        set => Split.IsPaneOpen = value;
    }

    private void Show(int index)
    {
        if (index < 0)
        {
            return;
        }

        if (!_created.TryGetValue(index, out var page))
        {
            page = s_pages[index].Create();
            _created[index] = page;
        }

        PageHost.Content = page;
        PageHost.Offset = default;
        Avalonia.Threading.Dispatcher.UIThread.Post(() => PageHost.Offset = default, Avalonia.Threading.DispatcherPriority.Background);
    }
}
