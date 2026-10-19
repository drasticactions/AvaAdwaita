using System.Linq;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using AvaAdwaita.Controls;
using AvaAdwaita.Nodes;
using AvaAdwaita.Presenters;
using Xunit;

namespace AvaAdwaita.Theme.Tests;

/// <summary>AvaAdwaita.Controls' SearchEntry and SearchBar: their icons, their events and showing the bar.</summary>
public class SearchTests
{
    private static AdwIcon Clear(SearchEntry e) => e.GetVisualDescendants().OfType<AdwIcon>().Single(i => i.IconName == "edit-clear-symbolic");

    [AvaloniaFact]
    public void Entry_Clears_And_Raises_Its_Events()
    {
        var entry = new SearchEntry { PlaceholderText = "Search", SearchDelay = 0 };
        var w = Shots.Show(entry);
        Assert.Contains("search", Css.GetCssNode(entry)!.Classes);
        Assert.False(Clear(entry).IsVisible);

        var changed = 0;
        var activated = 0;
        var stopped = 0;
        entry.SearchChanged += (_, _) => changed++;
        entry.Activated += (_, _) => activated++;
        entry.StopSearch += (_, _) => stopped++;
        entry.Focus();
        entry.Text = "Query";
        Shots.Pump(w);
        Avalonia.Threading.Dispatcher.UIThread.RunJobs();
        Frames.Run(50);
        Assert.Equal(1, changed);
        Assert.True(Clear(entry).IsVisible);

        w.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        Assert.Equal(1, activated);
        w.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);
        Assert.Equal(1, stopped);

        // The clear icon empties the entry, which searches at once.
        var icon = Clear(entry);
        var center = icon.TranslatePoint(new Point(icon.Bounds.Width / 2, icon.Bounds.Height / 2), w)!.Value;
        w.MouseDown(center, MouseButton.Left);
        w.MouseUp(center, MouseButton.Left);
        Assert.Equal(string.Empty, entry.Text);
        Assert.Equal(2, changed);
        Assert.False(Clear(entry).IsVisible);

        var peer = ControlAutomationPeer.CreatePeerForElement(entry);
        Assert.Equal("Search", peer.GetName());
        Assert.Equal(AutomationControlType.Edit, peer.GetAutomationControlType());
        w.Close();
    }

    [AvaloniaFact]
    public void Bar_Shows_Focuses_And_Hides_On_Escape()
    {
        var entry = new SearchEntry { PlaceholderText = "Search" };
        var bar = new SearchBar { Content = entry, ShowCloseButton = true };
        var button = new Button { Content = "Content" };
        var panel = new StackPanel { Children = { bar, button } };
        bar.KeyCaptureTarget = panel;
        var w = Shots.Show(panel, 480, 400);
        AdwaitaEnvironment.SuppressTransitions(w);
        Shots.Pump(w);
        Assert.Equal(0, bar.Bounds.Height);

        bar.IsSearchMode = true;
        Shots.Pump(w);
        Assert.True(bar.Bounds.Height > 0);
        Assert.True(entry.IsFocused);
        Assert.Contains(bar.GetVisualDescendants().OfType<Button>(), b => b.Classes.Contains("close") && b.IsVisible);

        entry.Text = "Two";
        w.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);
        Shots.Pump(w);
        Assert.False(bar.IsSearchMode);
        Assert.Equal(string.Empty, entry.Text);

        // Typing in the key capture target starts a search with the text.
        button.Focus();
        w.KeyTextInput("T");
        Shots.Pump(w);
        Assert.True(bar.IsSearchMode);
        Assert.Equal("T", entry.Text);
        Assert.True(entry.IsFocused);
        w.Close();
    }

    [AvaloniaFact]
    public void Typing_Fast_Keeps_Every_Character()
    {
        var entry = new SearchEntry();
        var button = new Button { Content = "Content" };
        var bar = new SearchBar { Content = entry };
        var panel = new StackPanel { Children = { bar, button } };
        bar.KeyCaptureTarget = panel;
        var w = Shots.Show(panel, 480, 320);
        AdwaitaEnvironment.SuppressTransitions(w);
        Shots.Pump(w);
        button.Focus();

        // The second character comes before the bar shows its entry, which cannot take the focus until then.
        w.KeyTextInput("p");
        w.KeyTextInput("e");
        Shots.Pump(w);
        Assert.True(bar.IsSearchMode);
        Assert.Equal("pe", entry.Text);
        w.Close();
    }
}
