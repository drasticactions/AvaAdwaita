using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using AvaAdwaita.Chrome;
using AvaAdwaita.Controls;
using AvaAdwaita.Nodes;
using Xunit;

namespace AvaAdwaita.Theme.Tests;

/// <summary>AvaAdwaita.Controls' Dialog: presenting it in the window's dialog host, its header bar, and closing it.</summary>
public class DialogTests
{
    private static Dialog Make() => new()
    {
        Title = "Preferences",
        ContentWidth = 300,
        ContentHeight = 200,
        PresentationMode = DialogPresentationMode.Floating,
        Content = new ToolbarView { TopBars = { new HeaderBar() }, Content = new TextBox() },
    };

    [AvaloniaFact]
    public void Dialog_Shows_In_The_Window_And_Closes()
    {
        var opener = new Button { Content = "Open" };
        var w = Shots.Show(opener, 640, 480);
        AdwaitaEnvironment.SuppressTransitions(w);
        opener.Focus();
        var dialog = Make();
        var task = dialog.ShowAsync(opener);
        Shots.Pump(w);

        Assert.Equal("dialog-host", Css.GetCssNode(dialog)!.Parent!.Name);
        Assert.Contains("floating", Css.GetCssNode(dialog)!.Classes);
        Assert.Equal(new Size(300, 200), dialog.Sheet!.Bounds.Size);
        var bar = dialog.GetVisualDescendants().OfType<HeaderBar>().Single();
        Assert.Equal("Preferences", bar.WindowTitle);
        var controls = bar.GetVisualDescendants().OfType<WindowControls>().Single(c => !c.IsStart);
        Assert.Equal(["close"], controls.Children.Select(b => b.Classes.First(c => !c.StartsWith(':'))));
        Assert.True(dialog.GetVisualDescendants().OfType<TextBox>().Single().IsFocused);

        // The close button closes the dialog, not the window, and the focus goes back.
        ((Button)controls.Children[0]).RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Shots.Pump(w);
        Assert.True(task.IsCompleted);
        Assert.Null(dialog.GetVisualParent());
        Assert.True(w.IsVisible);
        Assert.True(opener.IsFocused);
        w.Close();
    }

    [AvaloniaFact]
    public void Narrow_Window_Shows_A_Bottom_Sheet()
    {
        var w = Shots.Show(new Border(), 420, 480);
        AdwaitaEnvironment.SuppressTransitions(w);
        var dialog = Make();
        dialog.PresentationMode = DialogPresentationMode.Auto;
        dialog.Present(w);
        Shots.Pump(w);
        var classes = Css.GetCssNode(dialog)!.Classes;
        Assert.Contains("bottom-sheet", classes);
        Assert.Contains("narrow", classes);
        Assert.Equal(dialog.Bounds.Height, dialog.Sheet!.TranslatePoint(new Point(0, dialog.Sheet!.Bounds.Height), dialog)!.Value.Y, tolerance: 1);

        // A click on the dimmed window above the sheet closes it.
        var above = dialog.TranslatePoint(new Point(dialog.Bounds.Width / 2, 10), w)!.Value;
        w.MouseDown(above, MouseButton.Left);
        w.MouseUp(above, MouseButton.Left);
        Shots.Pump(w);
        Assert.Null(dialog.GetVisualParent());
        w.Close();
    }

    [AvaloniaFact]
    public void Dialog_That_Cannot_Close_Says_So()
    {
        var w = Shots.Show(new Border(), 640, 480);
        AdwaitaEnvironment.SuppressTransitions(w);
        var attempts = 0;
        var dialog = Make();
        dialog.CanClose = false;
        dialog.CloseAttempt += (_, _) => attempts++;
        dialog.Present(w);
        Shots.Pump(w);
        dialog.GetVisualDescendants().OfType<TextBox>().Single().Focus();
        w.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);
        Assert.Equal(1, attempts);
        Assert.NotNull(dialog.GetVisualParent());

        dialog.ForceClose();
        Shots.Pump(w);
        Assert.Null(dialog.GetVisualParent());
        w.Close();
    }

    [AvaloniaFact]
    public void Dialog_Keeps_The_Keyboard_And_Takes_The_Focus_Itself_With_Nothing_To_Focus()
    {
        var opener = new Button { Content = "Open" };
        var other = new Button { Content = "Other" };
        var w = Shots.Show(new StackPanel { Children = { opener, other } }, 640, 480);
        AdwaitaEnvironment.SuppressTransitions(w);
        opener.Focus();
        var dialog = new Dialog
        {
            ContentWidth = 300,
            ContentHeight = 200,
            PresentationMode = DialogPresentationMode.Floating,
            Content = new ToolbarView { TopBars = { new HeaderBar() }, Content = new Label { Content = "Nothing to focus" } },
        };
        dialog.Present(opener);
        Shots.Pump(w);

        // The header bar's close button takes no focus, so the dialog has it, and Escape reaches it.
        Assert.True(dialog.IsFocused);
        var attempts = 0;
        dialog.CanClose = false;
        dialog.CloseAttempt += (_, _) => attempts++;
        w.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);
        Assert.Equal(1, attempts);

        // Tab stays in the dialog: the window's content under it takes no focus.
        w.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.None);
        w.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.None);
        Assert.False(opener.IsFocused || other.IsFocused);

        // Closed, the content takes the focus again, and the opener has it back.
        dialog.ForceClose();
        Shots.Pump(w);
        Assert.True(opener.IsFocused);
        w.KeyPressQwerty(PhysicalKey.Tab, RawInputModifiers.None);
        Assert.True(other.IsFocused);
        w.Close();
    }

    [AvaloniaFact]
    public void Close_Button_Takes_Its_Press_From_The_Title_Bar()
    {
        var opener = new Button { Content = "Open" };
        var w = Shots.Show(opener, 640, 480);
        AdwaitaEnvironment.SuppressTransitions(w);
        var dialog = Make();
        dialog.Present(opener);
        Shots.Pump(w);
        var close = dialog.GetVisualDescendants().OfType<WindowControls>().Single(c => !c.IsStart).Children.OfType<Button>().Single();
        Assert.False(close.Focusable);
        Assert.Equal(WindowDecorationsElementRole.DecorationsElement, close.GetValue(Avalonia.Controls.Chrome.WindowDecorationProperties.ElementRoleProperty));
        w.Close();
    }
}
