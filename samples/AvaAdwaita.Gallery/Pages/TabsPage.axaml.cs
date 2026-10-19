using Avalonia.Controls;
using AvaAdwaita.Controls;

namespace AvaAdwaita.Gallery.Pages;

public sealed partial class TabsPage : UserControl
{
    private int _count;
    private TabPage? _menuPage;

    public TabsPage()
    {
        InitializeComponent();
        for (var i = 0; i < 3; i++)
        {
            AddTab();
        }

        NewTab.Click += (_, _) => Tabs.SelectedPage = AddTab();
        Counter.Click += (_, _) => Overview.Open();
        Overview.CreateTab += (_, e) => e.Page = AddTab();

        // The tab menu acts on the tab it opened on.
        var pin = new MenuItem { Header = "Pin Tab" };
        pin.Click += (_, _) =>
        {
            if (_menuPage is { } page)
            {
                Tabs.SetPagePinned(page, !page.IsPinned);
            }
        };
        var close = new MenuItem { Header = "Close Tab" };
        close.Click += (_, _) =>
        {
            if (_menuPage is { } page)
            {
                Tabs.ClosePage(page);
            }
        };
        var closeOthers = new MenuItem { Header = "Close Other Tabs" };
        closeOthers.Click += (_, _) =>
        {
            if (_menuPage is { } page)
            {
                Tabs.CloseOtherPages(page);
            }
        };
        Tabs.Menu = new MenuFlyout { Items = { pin, close, closeOthers } };
        Tabs.SetupMenu += (_, e) =>
        {
            if (e.Page is { } page)
            {
                _menuPage = page;
                pin.Header = page.IsPinned ? "Unpin Tab" : "Pin Tab";
            }
        };
    }

    private TabPage AddTab()
    {
        _count++;
        var page = Tabs.Append(new Label { Content = $"Page {_count}", Classes = { "title-1", "centered" } });
        page.Title = $"Page {_count}";
        page.IconName = _count % 2 == 0 ? "folder-symbolic" : null;
        return page;
    }
}
