using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using AvaAdwaita.Nodes;
using Xunit;

namespace AvaAdwaita.Theme.Tests;

public class ControlShotTests
{
    [AvaloniaFact]
    public void Controls_Render()
    {
        var left = new StackPanel { Spacing = 12, Margin = new Avalonia.Thickness(24), Width = 220 };
        left.Children.Add(new CheckBox { Content = "Check box", IsChecked = true });
        left.Children.Add(new CheckBox { Content = "Unchecked" });
        left.Children.Add(new RadioButton { Content = "Radio", IsChecked = true });
        left.Children.Add(new ToggleSwitch { IsChecked = true });
        left.Children.Add(new ToggleSwitch { IsChecked = false });
        left.Children.Add(new TextBox { PlaceholderText = "Placeholder" });
        left.Children.Add(new TextBox { Text = "Entry text" });
        left.Children.Add(new ComboBox { ItemsSource = new[] { "First", "Second" }, SelectedIndex = 0 });
        var right = new StackPanel { Spacing = 12, Margin = new Avalonia.Thickness(24), Width = 260 };
        var list = new ListBox { ItemsSource = new[] { "Row one", "Row two", "Row three" } };
        list.Classes.Add("boxed-list");
        right.Children.Add(list);
        right.Children.Add(new Slider { Minimum = 0, Maximum = 100, Value = 40 });
        right.Children.Add(new ProgressBar { Minimum = 0, Maximum = 100, Value = 60 });
        right.Children.Add(new Separator());
        var label = new Label { Content = "Title" };
        label.Classes.Add("title-2");
        right.Children.Add(label);
        var root = new StackPanel { Orientation = Orientation.Horizontal };
        root.Children.Add(left);
        root.Children.Add(right);
        var w = Shots.Show(root, 620, 520);
        Shots.Save(w, "controls");
    }

    [AvaloniaFact]
    public void File_Chooser_Renders()
    {
        var chooser = new Avalonia.Dialogs.ManagedFileChooser
        {
            DataContext = new Avalonia.Dialogs.Internal.ManagedFileChooserViewModel(
                new Avalonia.Platform.Storage.FilePickerOpenOptions { Title = "Open", SuggestedStartLocation = null },
                new Avalonia.Dialogs.ManagedFileDialogOptions()),
        };
        var w = Shots.Show(chooser, 800, 520);
        Shots.Save(w, "file-chooser");
        Assert.Equal("filechooser", Css.GetCssNode(chooser)?.Name);
        w.Close();
    }

    [AvaloniaFact]
    public void Buttons_Render()
    {
        var panel = new StackPanel { Spacing = 12, Margin = new Avalonia.Thickness(24), HorizontalAlignment = HorizontalAlignment.Left };
        panel.Children.Add(new Button { Content = "Default" });
        var suggested = new Button { Content = "Suggested" };
        suggested.Classes.Add("suggested-action");
        panel.Children.Add(suggested);
        var destructive = new Button { Content = "Destructive" };
        destructive.Classes.Add("destructive-action");
        panel.Children.Add(destructive);
        var pill = new Button { Content = "Pill" };
        pill.Classes.Add("pill");
        panel.Children.Add(pill);
        panel.Children.Add(new ToggleButton { Content = "Toggle", IsChecked = true });
        var w = Shots.Show(panel);
        Shots.Save(w, "buttons");
        var node = Css.GetCssNode(suggested)!;
        Assert.Equal("window.background.csd:dir(ltr) > dialog-host:dir(ltr) > widget:dir(ltr) > widget:dir(ltr) > toolbarview:dir(ltr) > button.suggested-action.text-button:dir(ltr)", node.Path);
    }
}
