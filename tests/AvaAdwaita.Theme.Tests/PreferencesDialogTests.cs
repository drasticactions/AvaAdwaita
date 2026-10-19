using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using AvaAdwaita.Controls;
using Xunit;
using NavigationPage = AvaAdwaita.Controls.NavigationPage;

namespace AvaAdwaita.Theme.Tests;

/// <summary>AvaAdwaita.Controls' PreferencesDialog: the view switcher or the title, searching the rows, and subpages.</summary>
public class PreferencesDialogTests
{
    private static PreferencesDialog Make(double width)
    {
        var dialog = new PreferencesDialog { ContentWidth = width, ContentHeight = 300, PresentationMode = DialogPresentationMode.Floating, SearchEnabled = true };
        dialog.Add(new PreferencesPage
        {
            Title = "Appearance",
            IconName = "preferences-system-symbolic",
            Children = { new PreferencesGroup { Title = "Style", Children = { new SwitchRow { Title = "Dark Style" }, new ActionRow { Title = "Font", Subtitle = "Adwaita Sans" } } } },
        });
        dialog.Add(new PreferencesPage
        {
            Title = "Behavior",
            IconName = "view-grid-symbolic",
            Children = { new PreferencesGroup { Title = "Windows", Children = { new SwitchRow { Title = "Remember _Size", UseUnderline = true } } } },
        });
        return dialog;
    }

    private static (Window, PreferencesDialog) Show(double width)
    {
        var opener = new Button { Content = "Open" };
        var w = Shots.Show(opener, 640, 480);
        AdwaitaEnvironment.SuppressTransitions(w);
        var dialog = Make(width);
        dialog.Present(opener);
        Shots.Pump(w);
        return (w, dialog);
    }

    private static bool Shows(Control c) => c.IsEffectivelyVisible && c.Bounds.Height > 0;

    [AvaloniaFact]
    public void Wide_Dialog_Shows_The_View_Switcher_And_Narrow_The_Title()
    {
        var (w, dialog) = Show(500);
        Assert.Equal("Preferences", dialog.Title);
        Assert.True(Shows(dialog.GetVisualDescendants().OfType<ViewSwitcher>().First(s => s.Policy == ViewSwitcherPolicy.Wide)));
        Assert.False(dialog.GetVisualDescendants().OfType<ViewSwitcherBar>().Single().Reveal);
        Assert.Same(dialog.Pages[0], dialog.VisiblePage);

        dialog.VisiblePage = dialog.Pages[1];
        Shots.Pump(w);
        Assert.True(dialog.Pages[1].IsEffectivelyVisible);
        Assert.False(dialog.Pages[0].IsEffectivelyVisible);

        dialog.ContentWidth = 360;
        Shots.Pump(w);
        Assert.True(dialog.GetVisualDescendants().OfType<ViewSwitcherBar>().Single().Reveal);
        Assert.True(dialog.GetVisualDescendants().OfType<WindowTitle>().Single().IsEffectivelyVisible);
        w.Close();
    }

    [AvaloniaFact]
    public void Search_Finds_Rows_And_Goes_To_Them()
    {
        var (w, dialog) = Show(500);
        var button = dialog.GetVisualDescendants().OfType<Avalonia.Controls.Primitives.ToggleButton>().Single(b => b.Content is Presenters.AdwIcon);
        Assert.True(button.IsVisible);

        // Typing starts a search with what was typed.
        dialog.Focus();
        w.KeyTextInput("s");
        Shots.Pump(w);
        Assert.True(button.IsChecked);
        var entry = dialog.GetVisualDescendants().OfType<SearchEntry>().Single();
        Assert.Equal("s", entry.Text);

        entry.Text = "size";
        entry.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(SearchEntry.SearchChangedEvent, entry));
        Shots.Pump(w);
        var result = Assert.Single(dialog.GetVisualDescendants().OfType<ActionRow>(), r => r.IsEffectivelyVisible && r.Tag is not null);
        Assert.Equal("Behavior → Windows", result.Subtitle);

        // A subtitle matches too.
        entry.Text = "adwaita";
        entry.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(SearchEntry.SearchChangedEvent, entry));
        Shots.Pump(w);
        Assert.Equal("Font", Assert.Single(dialog.GetVisualDescendants().OfType<ActionRow>(), r => r.IsEffectivelyVisible && r.Tag is not null).Title);

        entry.Text = "nothing";
        entry.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(SearchEntry.SearchChangedEvent, entry));
        Shots.Pump(w);
        Assert.True(dialog.GetVisualDescendants().OfType<StatusPage>().Single(p => p.IconName == "edit-find-symbolic").IsEffectivelyVisible);

        entry.Text = "size";
        entry.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(SearchEntry.SearchChangedEvent, entry));
        Shots.Pump(w);
        result = dialog.GetVisualDescendants().OfType<ActionRow>().Single(r => r.IsEffectivelyVisible && r.Tag is not null);
        result.Activate();
        Shots.Pump(w);
        Assert.False(button.IsChecked);
        Assert.Same(dialog.Pages[1], dialog.VisiblePage);
        Assert.True(dialog.Pages[1].GetVisualDescendants().OfType<SwitchRow>().Single().IsFocused);
        w.Close();
    }

    [AvaloniaFact]
    public void Subpage_Shows_Over_The_Pages()
    {
        var (w, dialog) = Show(500);
        var subpage = new NavigationPage { Title = "Details", Content = new ToolbarView { TopBars = { new HeaderBar() }, Content = new Label { Content = "Details" } } };
        dialog.PushSubpage(subpage);
        Shots.Pump(w);
        Assert.True(subpage.IsEffectivelyVisible);
        Assert.True(dialog.PopSubpage());
        Shots.Pump(w);
        Assert.False(dialog.PopSubpage());
        w.Close();
    }
}
