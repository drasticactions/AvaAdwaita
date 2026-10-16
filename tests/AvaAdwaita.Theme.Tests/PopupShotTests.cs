using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using AvaAdwaita.Nodes;
using Xunit;

namespace AvaAdwaita.Theme.Tests;

public class PopupShotTests
{
    [AvaloniaFact]
    public void Dark_Controls_And_Open_Dropdown()
    {
        var theme = TestApplication.Instance.Theme;
        theme.ColorScheme = AdwColorScheme.ForceDark;
        try
        {
            var panel = new StackPanel { Spacing = 12, Margin = new Avalonia.Thickness(24), HorizontalAlignment = HorizontalAlignment.Left };
            var suggested = new Button { Content = "Suggested" };
            suggested.Classes.Add("suggested-action");
            panel.Children.Add(suggested);
            panel.Children.Add(new CheckBox { Content = "Dark check", IsChecked = true });
            var combo = new ComboBox { ItemsSource = new[] { "First", "Second", "Third" }, SelectedIndex = 1 };
            panel.Children.Add(combo);
            var w = Shots.Show(panel, 400, 360);
            combo.IsDropDownOpen = true;
            Shots.Pump(w);
            Shots.Save(w, "dark-dropdown");
            var popupRow = combo.ContainerFromIndex(0)!;
            var node = Css.GetCssNode(popupRow)!;
            Assert.StartsWith("window.background.csd", node.Path);
            Assert.Contains("popover.menu.background", node.Path);
            Assert.True(node.Path.EndsWith("> listview.view:dir(ltr) > row:dir(ltr)", System.StringComparison.Ordinal), node.Path);
        }
        finally
        {
            theme.ColorScheme = AdwColorScheme.Default;
        }
    }

    [AvaloniaFact]
    public void Popover_Flyout_Renders()
    {
        var panel = new StackPanel { Margin = new Avalonia.Thickness(24), HorizontalAlignment = HorizontalAlignment.Left };
        var content = new StackPanel { Spacing = 12 };
        content.Children.Add(new Label { Content = "A popover" });
        content.Children.Add(new Button { Content = "Done" });
        var button = new Button { Content = "Open", Flyout = new Flyout { Content = content } };
        panel.Children.Add(button);
        var w = Shots.Show(panel, 400, 300);
        button.Flyout!.ShowAt(button);
        Shots.Pump(w);
        Shots.Save(w, "popover");
        var frame = System.Linq.Enumerable.OfType<AvaAdwaita.Chrome.PopoverFrame>(Avalonia.VisualTree.VisualExtensions.GetVisualAncestors(content)).Single();
        Assert.Equal(AvaAdwaita.Chrome.PopoverSide.Top, frame.Side);
        var label = content.Children[0];
        Assert.Matches(@"popover\.background[^>]* > contents", Css.GetCssNode(label)!.Path);
    }

    [AvaloniaFact]
    public void Date_And_Time_Pickers_Open()
    {
        var panel = new StackPanel { Spacing = 12, Margin = new Avalonia.Thickness(24), HorizontalAlignment = HorizontalAlignment.Left };
        var date = new DatePicker { SelectedDate = new System.DateTimeOffset(2026, 10, 15, 0, 0, 0, System.TimeSpan.Zero) };
        var time = new TimePicker { SelectedTime = new System.TimeSpan(9, 30, 0) };
        var calendar = new CalendarDatePicker { SelectedDate = new System.DateTime(2026, 10, 15), Width = 200 };
        panel.Children.Add(date);
        panel.Children.Add(time);
        panel.Children.Add(calendar);
        var w = Shots.Show(panel, 520, 560);
        Shots.Save(w, "pickers");
        var flyoutButton = Avalonia.VisualTree.VisualExtensions.GetVisualDescendants(date).OfType<Button>().First(b => b.Name == "PART_FlyoutButton");
        flyoutButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Shots.Pump(w);
        Shots.Save(w, "datepicker-open");
        flyoutButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Shots.Pump(w);
        calendar.IsDropDownOpen = true;
        Shots.Pump(w);
        Shots.Save(w, "calendar-open");
        Assert.Equal("menubutton", Css.GetNode(date));
        Assert.Equal("box", Css.GetNode(calendar));
    }
}
