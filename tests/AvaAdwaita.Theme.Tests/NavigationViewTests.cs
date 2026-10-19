using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using AvaAdwaita.Chrome;
using AvaAdwaita.Controls;
using NavigationPage = AvaAdwaita.Controls.NavigationPage;
using AvaAdwaita.Nodes;
using AvaAdwaita.Presenters;
using Xunit;

namespace AvaAdwaita.Theme.Tests;

/// <summary>AvaAdwaita.Controls' NavigationView and NavigationSplitView: pushing, popping, the back button and collapsing.</summary>
public class NavigationViewTests
{
    private static NavigationPage Page(string title, string? tag = null) => new()
    {
        Title = title,
        NavigationTag = tag,
        Content = new ToolbarView { TopBars = { new HeaderBar() }, Content = new Button { Content = title } },
    };

    private static HeaderBar Bar(NavigationPage page) => page.GetVisualDescendants().OfType<HeaderBar>().Single();

    [AvaloniaFact]
    public void Pages_Push_And_Pop()
    {
        var home = Page("Home", "home");
        var settings = Page("Settings", "settings");
        var view = new NavigationView { Pages = { home, settings } };
        var w = Shots.Show(view, 480, 320);
        AdwaitaEnvironment.SuppressTransitions(w);
        Assert.Same(home, view.VisiblePage);
        Assert.False(settings.IsVisible);
        Assert.False(Bar(home).HasBack);
        Assert.Equal("Home", Bar(home).WindowTitle);

        var popped = 0;
        view.Popped += (_, _) => popped++;
        view.PushByTag("settings");
        Shots.Pump(w);
        Assert.Same(settings, view.VisiblePage);
        Assert.False(home.IsVisible);
        Assert.True(Bar(settings).HasBack);

        // The back button pops, as do Escape and Alt+Left.
        Bar(settings).GoBack();
        Shots.Pump(w);
        Assert.Same(home, view.VisiblePage);
        Assert.Equal(1, popped);

        var details = Page("Details");
        view.Push(details);
        Shots.Pump(w);
        details.GetVisualDescendants().OfType<Button>().First(b => b.Content as string == "Details").Focus();
        w.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);
        Assert.Same(home, view.VisiblePage);

        // A page pushed without being added leaves the view when it is popped; an added one stays.
        Assert.DoesNotContain(details, view.GetVisualChildren());
        Assert.Contains(settings, view.GetVisualChildren());

        details.CanPop = false;
        view.Push(details);
        Assert.False(view.Pop());
        Assert.False(Bar(details).HasBack);
        w.Close();
    }

    [AvaloniaFact]
    public void Push_Slides_Over_The_Last_Page()
    {
        var home = Page("Home");
        var view = new NavigationView { Pages = { home } };
        var w = Shots.Show(view, 480, 320);
        for (var i = 0; i < 100 && AdwaitaEnvironment.TransitionsSuppressedFor(view); i++)
        {
            Frames.Run(16);
        }

        var next = Page("Next");
        view.Push(next);
        var sawSlide = false;
        for (var i = 0; i < 100 && home.IsVisible; i++)
        {
            Frames.Run(16);
            var x = next.Bounds.X;
            sawSlide |= x > 0 && x < view.Bounds.Width
                && view.GetVisualChildren().OfType<AdwBox>().Any(b => Css.GetCssNode(b)?.Name == "dimming" && b.IsVisible);
        }

        Assert.True(sawSlide, "the page did not slide over the last one with its shadow");
        Assert.False(home.IsVisible);
        Assert.Equal(0, next.Bounds.X);
        w.Close();
    }

    [AvaloniaFact]
    public void Split_View_Collapses_Into_A_Navigation_View()
    {
        var sidebar = Page("Folders");
        var content = Page("Messages");
        var split = new NavigationSplitView { Sidebar = sidebar, Content = content };
        var w = Shots.Show(split, 640, 400);
        AdwaitaEnvironment.SuppressTransitions(w);

        // Expanded, each pane's header bar keeps the window buttons on its outer side.
        Assert.False(Bar(sidebar).EndButtonsShown);
        Assert.True(Bar(sidebar).StartButtonsShown);
        Assert.False(Bar(content).StartButtonsShown);
        Assert.True(Bar(content).EndButtonsShown);

        split.IsCollapsed = true;
        Shots.Pump(w);
        Assert.NotNull(split.NavigationView);
        Assert.Same(sidebar, split.NavigationView!.VisiblePage);
        Assert.True(Bar(sidebar).EndButtonsShown);

        split.ShowContent = true;
        Shots.Pump(w);
        Assert.Same(content, split.NavigationView.VisiblePage);
        Assert.True(Bar(content).HasBack);

        split.NavigationView.Pop();
        Shots.Pump(w);
        Assert.False(split.ShowContent);

        split.IsCollapsed = false;
        Shots.Pump(w);
        Assert.Null(split.NavigationView);
        Assert.True(sidebar.IsEffectivelyVisible && content.IsEffectivelyVisible);
        w.Close();
    }

    [AvaloniaFact]
    public void Focus_Goes_Into_The_Page_Shown_And_Back_To_Where_It_Was()
    {
        var home = Page("Home", "home");
        var settings = Page("Settings", "settings");
        var view = new NavigationView { Pages = { home, settings } };
        var w = Shots.Show(view, 480, 320);
        AdwaitaEnvironment.SuppressTransitions(w);
        var homeButton = home.GetVisualDescendants().OfType<Button>().Single(b => b.Content is "Home");
        homeButton.Focus();

        // switch_page: the first widget of the page shown takes the focus; popping, the page has its own back.
        view.PushByTag("settings");
        Shots.Pump(w);
        Assert.True(settings.IsKeyboardFocusWithin);
        w.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);
        Shots.Pump(w);
        Assert.Same(home, view.VisiblePage);
        Assert.True(homeButton.IsFocused);
        w.Close();
    }

    [AvaloniaFact]
    public void Collapsing_With_The_Content_Showing_Has_No_Transition()
    {
        // adw_navigation_view_replace has no animation: collapsing shows the content at once, with the sidebar under
        // it, rather than sliding the sidebar away. Transitions are on here.
        var sidebar = Page("Folders");
        var content = Page("Messages");
        var split = new NavigationSplitView { Sidebar = sidebar, Content = content, ShowContent = true };
        var w = Shots.Show(split, 480, 320);
        Assert.False(split.IsCollapsed);

        split.IsCollapsed = true;
        Assert.True(content.IsVisible);
        Assert.False(sidebar.IsVisible);
        var view = split.GetVisualDescendants().OfType<NavigationView>().Single();
        Assert.Equal([sidebar, content], view.NavigationStack);

        // Going back still slides.
        view.Pop();
        Assert.True(content.IsVisible);
        Assert.True(sidebar.IsVisible);
        w.Close();
    }
}
