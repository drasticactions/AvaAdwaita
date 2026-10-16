using System;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.VisualTree;
using AvaAdwaita.CssEngine.Selectors;
using AvaAdwaita.Nodes;
using AvaAdwaita.Presenters;
using Xunit;

namespace AvaAdwaita.Theme.Tests;

/// <summary>The calendar and pickers in other cultures, and the keyboard in their popovers.</summary>
public class CultureTests
{
    [AvaloniaFact]
    public void Escape_Closes_The_Date_Entry_Popover()
    {
        var picker = new CalendarDatePicker { SelectedDate = new DateTime(2026, 10, 15) };
        var w = Shots.Show(picker);
        var button = picker.GetVisualDescendants().OfType<Button>().First(b => b.Name == "PART_Button");
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Shots.Pump(w);
        Assert.True(picker.IsDropDownOpen);

        // GtkCalendar is focusable; the popover's calendar takes the focus, so Escape reaches it.
        var popup = picker.GetVisualDescendants().OfType<Popup>().First();
        var calendar = popup.Child!.GetVisualDescendants().OfType<Avalonia.Controls.Calendar>().Single();
        Assert.True(calendar.IsFocused);
        w.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);
        Shots.Pump(w);
        Assert.False(picker.IsDropDownOpen);
    }

    [AvaloniaFact]
    public void A_Picker_Focuses_Its_First_Column_As_It_Opens()
    {
        var picker = new DatePicker { SelectedDate = new DateTimeOffset(2026, 10, 15, 0, 0, 0, TimeSpan.Zero) };
        var w = Shots.Show(picker, 480, 400);
        var button = picker.GetVisualDescendants().OfType<Button>().First(b => b.Name == "PART_FlyoutButton");
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Shots.Pump(w);

        var popup = picker.GetVisualDescendants().OfType<Popup>().First();
        var presenter = popup.Child!.GetVisualDescendants().Prepend(popup.Child).OfType<DatePickerPresenter>().Single();
        var focused = TopLevel.GetTopLevel(presenter)!.FocusManager!.GetFocusedElement() as Visual;
        Assert.IsType<DateTimePickerPanel>(focused);

        // Only the focused column's spin button is :focus-within; its neighbors take their states from themselves.
        var spins = presenter.GetVisualDescendants().OfType<Control>().Where(c => Css.GetCssNode(c)?.Name == "spinbutton" && c.IsEffectivelyVisible).ToList();
        Assert.Equal(3, spins.Count);
        Assert.Single(spins, s => (Css.GetCssNode(s)!.States & CssStates.FocusWithin) != 0);
    }

    [AvaloniaFact]
    public void A_Homogeneous_Grid_Gives_Spare_Pixels_To_The_First_Columns()
    {
        // gtk_grid_request_allocate: 237 px in 7 columns are six of 34 and one of 33.
        var grid = new AdwHomogeneousGrid { ColumnDefinitions = new ColumnDefinitions("*,*,*,*,*,*,*"), Width = 237, Height = 20, HorizontalAlignment = HorizontalAlignment.Left };
        for (var i = 0; i < 7; i++)
        {
            var b = new Border();
            Grid.SetColumn(b, i);
            grid.Children.Add(b);
        }

        Shots.Show(grid);
        Assert.Equal(new double[] { 34, 34, 34, 34, 34, 34, 33 }, grid.Children.Select(c => c.Bounds.Width));
        Assert.Equal(new double[] { 0, 34, 68, 102, 136, 170, 204 }, grid.Children.Select(c => c.Bounds.X));
    }

    [Fact]
    public void The_Year_Label_Counts_In_The_Cultures_Era()
    {
        // The culture is passed rather than set: the UI thread's culture is shared with the tests running beside this.
        var date = new DateTime(2026, 10, 15);
        Assert.Equal("พ.ศ. 2569", CalendarYears.YearText(date, new CultureInfo("th-TH")));
        Assert.Equal("2026", CalendarYears.YearText(date, new CultureInfo("ja-JP")));
    }

    [AvaloniaFact]
    public void Day_Names_Come_From_The_C_Library_Where_It_Has_The_Locale()
    {
        // GtkCalendar shows strftime's %a: one letter in glibc's Arabic, where .NET has whole words.
        if (LocaleDayNames.Abbreviated(new CultureInfo("ar-EG")) is not { } names)
        {
            return;
        }

        Assert.Equal(7, names.Length);
        Assert.Equal("ح", names[0]);
    }

    [AvaloniaFact]
    public void A_Menu_Is_As_Wide_As_Its_Widest_Row()
    {
        // The rows share the indicator's width, as GtkModelButton's size group, but not the accelerator's or the
        // arrow's: this menu is 151 px wide in GTK.
        var recent = new MenuItem { Header = "Recent" };
        recent.Items.Add(new MenuItem { Header = "notes.txt" });
        var flyout = new MenuFlyout
        {
            Items =
            {
                new MenuItem { Header = "_New", InputGesture = KeyGesture.Parse("Ctrl+N") },
                new MenuItem { Header = "_Open…" },
                new Separator(),
                recent,
                new MenuItem { Header = "Show Hidden", ToggleType = MenuItemToggleType.CheckBox, IsChecked = true },
                new Separator(),
                new MenuItem { Header = "_Quit" },
            },
        };
        var button = new Button { Content = "File", Flyout = flyout };
        var w = Shots.Show(button, 480, 400);
        flyout.ShowAt(button);
        Shots.Pump(w);
        var contents = TopLevel.GetTopLevel(recent)!.GetVisualDescendants().OfType<Chrome.PopoverContents>().Single();
        Assert.Equal(new Size(151, 198), contents.Bounds.Size);
    }
}
