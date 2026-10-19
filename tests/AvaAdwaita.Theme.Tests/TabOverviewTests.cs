using System.Linq;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.VisualTree;
using AvaAdwaita.Controls;
using AvaAdwaita.Controls.Primitives;
using AvaAdwaita.Nodes;
using AvaAdwaita.Presenters;
using Xunit;

namespace AvaAdwaita.Theme.Tests;

/// <summary>AvaAdwaita.Controls' TabButton and TabOverview: the page count, opening the overview and choosing a page.</summary>
public class TabOverviewTests
{
    private static Point Center(Visual v, Visual root) => v.TranslatePoint(new Point(v.Bounds.Width / 2, v.Bounds.Height / 2), root)!.Value;

    private static TabView View(int pages)
    {
        var view = new TabView();
        for (var i = 0; i < pages; i++)
        {
            view.Append(new TextBox { Text = $"Page {i + 1}" }).Title = $"Page {i + 1}";
        }

        return view;
    }

    private static (Window, TabOverview, TabView) Show(int pages)
    {
        var view = View(pages);
        var overview = new TabOverview { View = view, Content = view };
        var w = Shots.Show(overview, 540, 360);
        AdwaitaEnvironment.SuppressTransitions(w);
        Shots.Pump(w);
        return (w, overview, view);
    }

    private static TabThumbnail Thumbnail(TabOverview o, TabPage page) =>
        o.GetVisualDescendants().OfType<TabThumbnail>().Single(t => t.Page == page);

    [AvaloniaFact]
    public void Button_Counts_The_Pages_And_Marks_Attention()
    {
        var view = View(3);
        var button = new TabButton { View = view };
        var w = Shots.Show(new StackPanel { Children = { button, view } }, 300, 200);
        AdwLabel Count() => button.GetVisualDescendants().OfType<AdwBox>().Single(b => Css.GetNode(b) == "overlay").GetVisualDescendants().OfType<AdwLabel>().Single();
        Assert.Equal("3", Count().Text);
        Assert.DoesNotContain("small", Css.GetCssNode(Count())!.Classes);

        for (var i = 0; i < 9; i++)
        {
            view.Append(new Border());
        }

        Shots.Pump(w);
        Assert.Equal("12", Count().Text);
        Assert.Contains("small", Css.GetCssNode(Count())!.Classes);

        var indicator = button.GetVisualDescendants().OfType<IndicatorBin>().Single();
        Assert.False(indicator.NeedsAttention);
        view.Pages[1].NeedsAttention = true;
        Assert.True(indicator.NeedsAttention);
        view.SelectedPage = view.Pages[1];
        Assert.False(indicator.NeedsAttention);

        var clicked = 0;
        button.Click += (_, _) => clicked++;
        w.MouseDown(Center(button, w), MouseButton.Left);
        w.MouseUp(Center(button, w), MouseButton.Left);
        Assert.Equal(1, clicked);
        w.Close();
    }

    [AvaloniaFact]
    public void Clicking_A_Thumbnail_Opens_Its_Page()
    {
        var (w, overview, view) = Show(3);
        Assert.False(overview.GetVisualDescendants().OfType<ToolbarView>().Single(t => t.Classes.Contains("overview")).IsVisible);
        overview.IsOpen = true;
        Shots.Pump(w);
        var thumbnails = view.Pages.Select(p => Thumbnail(overview, p)).ToList();
        Assert.All(thumbnails, t => Assert.True(t.Bounds.Width > 100));

        // The first page has shown, so its thumbnail pictures it; the others have not.
        Assert.NotNull(thumbnails[0].Picture.Source);
        Assert.Null(thumbnails[1].Picture.Source);

        w.MouseDown(Center(thumbnails[2], w), MouseButton.Left);
        w.MouseUp(Center(thumbnails[2], w), MouseButton.Left);
        Shots.Pump(w);
        Assert.False(overview.IsOpen);
        Assert.Same(view.Pages[2], view.SelectedPage);
        w.Close();
    }

    [AvaloniaFact]
    public void A_Page_Pictures_Over_The_Window_Background()
    {
        var (w, overview, view) = Show(2);
        overview.IsOpen = true;
        Shots.Pump(w);

        // render_contents fills the window's background first: drawn on a transparent bitmap, text loses its antialiasing.
        var picture = Assert.IsAssignableFrom<Bitmap>(Thumbnail(overview, view.Pages[0]).Picture.Source);
        var bytes = new byte[picture.PixelSize.Width * picture.PixelSize.Height * 4];
        var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        try
        {
            picture.CopyPixels(new PixelRect(picture.PixelSize), handle.AddrOfPinnedObject(), bytes.Length, picture.PixelSize.Width * 4);
        }
        finally
        {
            handle.Free();
        }

        Assert.All(Enumerable.Range(0, bytes.Length / 4), i => Assert.Equal(255, bytes[(i * 4) + 3]));
        w.Close();
    }

    [AvaloniaFact]
    public void Search_Narrows_The_Thumbnails_And_Escape_Closes()
    {
        var (w, overview, view) = Show(3);
        view.Pages[1].Keyword = "fruit";
        overview.IsOpen = true;
        Shots.Pump(w);
        overview.GetVisualDescendants().OfType<Avalonia.Controls.Primitives.ToggleButton>().Single().IsChecked = true;
        Shots.Pump(w);
        var entry = overview.GetVisualDescendants().OfType<SearchEntry>().Single();
        Assert.True(entry.IsFocused);
        entry.Text = "fruit";
        entry.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(SearchEntry.SearchChangedEvent, entry));
        Shots.Pump(w);
        Assert.True(overview.IsSearchActive);
        Assert.Equal([view.Pages[1]], overview.GetVisualDescendants().OfType<TabThumbnail>().Where(t => t.IsEffectivelyVisible).Select(t => t.Page));

        entry.Text = "nothing";
        entry.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(SearchEntry.SearchChangedEvent, entry));
        Shots.Pump(w);
        Assert.True(overview.GetVisualDescendants().OfType<StatusPage>().Single(p => p.IconName == "edit-find-symbolic").IsVisible);

        // Escape ends the search, then closes the overview.
        w.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);
        Shots.Pump(w);
        Assert.True(overview.IsOpen);
        overview.GetVisualDescendants().OfType<TabGridChild>().First(c => c.IsEffectivelyVisible).Focus();
        w.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);
        Shots.Pump(w);
        Assert.False(overview.IsOpen);
        w.Close();
    }

    [AvaloniaFact]
    public void New_Tab_Button_Asks_For_A_Page()
    {
        var (w, overview, view) = Show(1);
        overview.EnableNewTab = true;
        overview.CreateTab += (_, e) =>
        {
            e.Page = view.Append(new TextBox());
            e.Page.Title = "New";
        };
        overview.IsOpen = true;
        Shots.Pump(w);
        var button = overview.GetVisualDescendants().OfType<Button>().Single(b => b.Classes.Contains("new-tab-button"));
        Assert.True(button.IsEffectivelyVisible);
        button.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Shots.Pump(w);
        Assert.Equal(2, view.PageCount);
        Assert.Equal("New", view.SelectedPage!.Title);
        Assert.False(overview.IsOpen);
        w.Close();
    }

    [AvaloniaFact]
    public void Pinned_Pages_Have_Their_Own_Grid()
    {
        var (w, overview, view) = Show(3);
        view.SetPagePinned(view.Pages[2], true);
        overview.IsOpen = true;
        Shots.Pump(w);
        var pinned = Thumbnail(overview, view.Pages[0]);
        Assert.True(pinned.IsPinned);
        Assert.Contains("pinned", Css.GetCssNode(pinned)!.Classes);
        Assert.False(Thumbnail(overview, view.Pages[1]).IsPinned);
        w.Close();
    }
}
