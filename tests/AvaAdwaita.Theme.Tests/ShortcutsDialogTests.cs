using System.Linq;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using AvaAdwaita.Controls;
using AvaAdwaita.Controls.Primitives;
using Xunit;

namespace AvaAdwaita.Theme.Tests;

/// <summary>AvaAdwaita.Controls' ShortcutsDialog: its sections, searching them, the navigation buttons and Escape.</summary>
public class ShortcutsDialogTests
{
    private static ShortcutsDialog Make(int sections, int perSection)
    {
        var dialog = new ShortcutsDialog { ContentWidth = 420, PresentationMode = DialogPresentationMode.Floating };
        for (var s = 0; s < sections; s++)
        {
            var section = new ShortcutsSection($"Section {s}");
            for (var i = 0; i < perSection; i++)
            {
                section.Add(new ShortcutsItem($"Action {s}.{i}", $"<Control>{(char)('a' + i)}") { Subtitle = i == 0 ? $"First of {s}" : null });
            }

            dialog.Add(section);
        }

        return dialog;
    }

    private static (Window, ShortcutsDialog) Show(ShortcutsDialog dialog, double height = 480)
    {
        var opener = new Button { Content = "Open" };
        var w = Shots.Show(opener, 640, height);
        AdwaitaEnvironment.SuppressTransitions(w);
        dialog.Present(opener);
        Shots.Pump(w);
        return (w, dialog);
    }

    private static void Search(Window w, ShortcutsDialog dialog, string text)
    {
        dialog.SearchEntry.SearchDelay = 0;
        dialog.SearchEntry.Text = text;
        Shots.Pump(w);
    }

    [AvaloniaFact]
    public void Sections_Are_Groups_Of_Shortcut_Rows_And_The_Search_Entry_Has_Focus()
    {
        var (w, dialog) = Show(Make(2, 2));
        Assert.Equal("Keyboard Shortcuts", dialog.Title);
        Assert.Equal(["Section 0", "Section 1"], dialog.Groups.Select(g => g.Title));
        var rows = dialog.Groups.First().Children.OfType<ShortcutRow>().ToList();
        Assert.Equal(["Action 0.0", "Action 0.1"], rows.Select(r => r.Title));
        Assert.All(rows, r => Assert.False(r.IsActivatable));
        Assert.Equal("<Control>a", rows[0].Keys.Accelerator);
        Assert.True(dialog.SearchEntry.IsFocused);

        // Changing an item changes its row.
        rows[1].Item.Accelerator = null;
        Shots.Pump(w);
        Assert.Equal(string.Empty, rows[1].Keys.Accelerator);
        Assert.Equal("No Shortcut", rows[1].Keys.DisabledText);
        w.Close();
    }

    [AvaloniaFact]
    public void Content_Height_Goes_When_The_Dialog_Shows()
    {
        var dialog = Make(1, 1);
        dialog.ContentHeight = 200;
        var (w, _) = Show(dialog);
        Assert.True(double.IsNaN(dialog.ContentHeight));
        w.Close();
    }

    [AvaloniaFact]
    public void Search_Finds_Titles_And_Subtitles()
    {
        var (w, dialog) = Show(Make(2, 2));
        Search(w, dialog, "first of 1");
        Assert.Equal(["Action 1.0"], dialog.Results.Select(r => r.Title));
        Assert.All(dialog.Results, r => Assert.True(r.IsActivatable));
        Assert.IsType<PreferencesPage>(dialog.VisibleChild);

        Search(w, dialog, "nothing like it");
        Assert.IsType<StatusPage>(dialog.VisibleChild);

        // Escape in the entry ends the search, and then closes the dialog.
        var attempts = 0;
        dialog.CanClose = false;
        dialog.CloseAttempt += (_, _) => attempts++;
        dialog.SearchEntry.Focus();
        w.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);
        Shots.Pump(w);
        Assert.Equal(string.Empty, dialog.SearchEntry.Text);
        Assert.Contains("contents", Nodes.Css.GetCssNode(dialog.VisibleChild!)!.Classes);
        Assert.Equal(0, attempts);
        w.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);
        Shots.Pump(w);
        Assert.Equal(1, attempts);
        w.Close();
    }

    [AvaloniaFact]
    public void Activating_A_Result_Ends_The_Search_On_Its_Row()
    {
        var (w, dialog) = Show(Make(2, 2));
        Search(w, dialog, "action 1.1");
        var result = Assert.Single(dialog.Results);
        result.Activate();
        Shots.Pump(w);
        Assert.Equal(string.Empty, dialog.SearchEntry.Text);
        var source = dialog.Groups.Last().Children.OfType<ShortcutRow>().Last();
        Assert.Same(result.Item, source.Item);
        Assert.True(source.IsFocused);
        w.Close();
    }

    [AvaloniaFact]
    public void Typing_And_Ctrl_F_Search()
    {
        var (w, dialog) = Show(Make(1, 2));
        var row = dialog.Groups.First().Children.OfType<ShortcutRow>().First();
        row.Focusable = true;
        row.Focus();
        w.KeyTextInput("q");
        Shots.Pump(w);
        Assert.Equal("q", dialog.SearchEntry.Text);
        Assert.True(dialog.SearchEntry.IsFocused);

        row.Focus();
        w.KeyPressQwerty(PhysicalKey.F, RawInputModifiers.Control);
        Shots.Pump(w);
        Assert.True(dialog.SearchEntry.IsFocused);
        w.Close();
    }

    [AvaloniaFact]
    public void Navigation_Buttons_Show_One_Section_At_A_Time()
    {
        // Three sections of seven: not enough sections for the buttons.
        var (w, dialog) = Show(Make(3, 7));
        Assert.False(dialog.ShowsNavigation);
        w.Close();

        // Four of five, with an untitled section after the second, which goes with it.
        var many = Make(4, 5);
        many.Sections.Insert(2, new ShortcutsSection { Items = { new ShortcutsItem("More", "<Alt>m") } });
        (w, dialog) = Show(many);
        Assert.True(dialog.ShowsNavigation);
        var buttons = dialog.NavigationButtons.ToList();
        Assert.Equal(["Section 0", "Section 1", "Section 2", "Section 3"], buttons.Select(b => (string)b.Content!));

        buttons[1].RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Avalonia.Controls.Primitives.ToggleButton.ClickEvent));
        Shots.Pump(w);
        Assert.Equal([false, true, true, false, false], dialog.Groups.Select(g => g.IsVisible));

        buttons[2].RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Avalonia.Controls.Primitives.ToggleButton.ClickEvent));
        Shots.Pump(w);
        Assert.Equal([false, false, false, true, false], dialog.Groups.Select(g => g.IsVisible));
        Assert.False(buttons[1].IsChecked);
        Assert.True(buttons[2].IsChecked);

        // Escape shows them all again, before it would close the dialog.
        dialog.SearchEntry.Focus();
        w.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);
        Shots.Pump(w);
        Assert.All(dialog.Groups, g => Assert.True(g.IsVisible));
        Assert.False(buttons[2].IsChecked);

        // So does searching.
        buttons[0].RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Avalonia.Controls.Primitives.ToggleButton.ClickEvent));
        Search(w, dialog, "action");
        Search(w, dialog, string.Empty);
        Assert.All(dialog.Groups, g => Assert.True(g.IsVisible));
        w.Close();
    }

    [AvaloniaFact]
    public void Shortcuts_For_The_Other_Direction_Do_Not_Show()
    {
        var dialog = Make(1, 0);
        dialog.Sections[0].Add(new ShortcutsItem("Back", "<Alt>Left") { Direction = FlowDirection.LeftToRight });
        dialog.Sections[0].Add(new ShortcutsItem("Back", "<Alt>Right") { Direction = FlowDirection.RightToLeft });
        var (w, _) = Show(dialog);
        Assert.Equal(["<Alt>Left"], dialog.Groups.Single().Children.OfType<ShortcutRow>().Select(r => r.Item.Accelerator));

        dialog.FlowDirection = FlowDirection.RightToLeft;
        Shots.Pump(w);
        Assert.Equal(["<Alt>Right"], dialog.Groups.Single().Children.OfType<ShortcutRow>().Select(r => r.Item.Accelerator));
        w.Close();
    }

    [AvaloniaFact]
    public void Peer_Is_A_Window_Named_By_The_Title()
    {
        var (w, dialog) = Show(Make(1, 1));
        var peer = ControlAutomationPeer.CreatePeerForElement(dialog);
        Assert.Equal(AutomationControlType.Window, peer.GetAutomationControlType());
        Assert.Equal("Keyboard Shortcuts", peer.GetName());
        w.Close();
    }

    [AvaloniaFact]
    public void Dialog_Grows_Back_When_Every_Section_Shows_Again()
    {
        // Tall enough for every section, so the sheet takes its content's height.
        var (w, dialog) = Show(Make(4, 5), 1600);
        var all = dialog.Sheet!.Bounds.Height;
        var button = dialog.NavigationButtons.Last();
        button.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Avalonia.Controls.Primitives.ToggleButton.ClickEvent));
        Shots.Pump(w);
        Assert.True(dialog.Sheet!.Bounds.Height < all);

        button.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Avalonia.Controls.Primitives.ToggleButton.ClickEvent));
        Shots.Pump(w);
        Assert.Equal(all, dialog.Sheet!.Bounds.Height);
        w.Close();
    }
}
