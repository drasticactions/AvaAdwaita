using System.Linq;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using AvaAdwaita.Controls;
using AvaAdwaita.Controls.Primitives;
using AvaAdwaita.Nodes;
using Xunit;

namespace AvaAdwaita.Theme.Tests;

/// <summary>AvaAdwaita.Controls' EntryRow and PasswordEntryRow: the moving title, applying, activation and the peek button.</summary>
public class EntryRowTests
{
    private static (Window Window, EntryRow Row, EntryRowArea Area) Show(EntryRow row)
    {
        var w = Shots.Show(new StackPanel { Children = { new ListBox { Classes = { "boxed-list" }, Width = 360, Items = { row } }, new Button { Name = "Other", Content = "Other" } } });
        AdwaitaEnvironment.SuppressTransitions(w);
        return (w, row, row.GetVisualDescendants().OfType<EntryRowArea>().Single());
    }

    private static Control Part(EntryRow row, string name) => row.GetVisualDescendants().OfType<Control>().Single(c => c.Name == name);

    [AvaloniaFact]
    public void Title_Moves_Above_The_Text_While_Editing()
    {
        var (w, row, area) = Show(new EntryRow { Title = "Name" });
        Assert.Equal(0, area.EmptyProgress);
        Assert.True(Part(row, "PART_EditIcon").IsVisible);

        row.Activate();
        Shots.Pump(w);
        Assert.True(row.TextBox!.IsFocused);
        Assert.Contains("focused", Css.GetCssNode(row)!.Classes);
        Assert.Equal(1, area.EmptyProgress);
        Assert.False(Part(row, "PART_EditIcon").IsVisible);

        w.KeyTextInput("Ada");
        Assert.Equal("Ada", row.Text);
        w.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "Other").Focus();
        Shots.Pump(w);
        Assert.DoesNotContain("focused", Css.GetCssNode(row)!.Classes);
        Assert.Equal(1, area.EmptyProgress);

        row.Text = string.Empty;
        Shots.Pump(w);
        Assert.Equal(0, area.EmptyProgress);
        w.Close();
    }

    [AvaloniaFact]
    public void Apply_Button_Shows_For_Changed_Text()
    {
        var applied = 0;
        var activated = 0;
        var (w, row, _) = Show(new EntryRow { Title = "Name", ShowApplyButton = true });
        row.Applied += (_, _) => applied++;
        row.EntryActivated += (_, _) => activated++;
        var apply = Part(row, "PART_Apply");

        row.Activate();
        w.KeyTextInput("x");
        Shots.Pump(w);
        Assert.True(apply.IsVisible);
        w.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        Shots.Pump(w);
        Assert.Equal(1, applied);
        Assert.False(apply.IsVisible);

        w.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        Assert.Equal(1, applied);
        Assert.Equal(1, activated);

        var peer = ControlAutomationPeer.CreatePeerForElement(row);
        Assert.Equal(AutomationControlType.Edit, peer.GetAutomationControlType());
        Assert.Equal("x", peer.GetProvider<IValueProvider>()!.Value);
        w.Close();
    }

    [AvaloniaFact]
    public void Password_Row_Hides_The_Text_Until_Peeked()
    {
        var (w, row, _) = Show(new PasswordEntryRow { Title = "Password", Text = "secret" });
        var password = (PasswordEntryRow)row;
        Assert.Contains("password", Css.GetCssNode(row)!.Classes);
        Assert.Equal('●', row.TextBox!.PasswordChar);
        Assert.False(row.TextBox.RevealPassword);
        Assert.Equal("Show Password", ToolTip.GetTip(password.PeekButton));

        password.PeekButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Assert.True(password.RevealPassword);
        Assert.True(row.TextBox.RevealPassword);
        Assert.Equal("Hide Password", ToolTip.GetTip(password.PeekButton));
        Assert.Equal("view-conceal-symbolic", ((Presenters.AdwIcon)password.PeekButton.Content!).IconName);
        Assert.Null(ControlAutomationPeer.CreatePeerForElement(row).GetProvider<IValueProvider>()!.Value);
        w.Close();
    }
}
