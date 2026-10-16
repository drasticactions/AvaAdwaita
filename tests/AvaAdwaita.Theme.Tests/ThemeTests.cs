using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using Avalonia.Threading;
using AvaAdwaita.CssEngine.Selectors;
using AvaAdwaita.Nodes;
using Xunit;

namespace AvaAdwaita.Theme.Tests;

/// <summary>Every stock control the theme covers gets a template and its GTK node, and its pseudo-classes map to GTK states.</summary>
public class ThemeTests
{
    public static TheoryData<Type, string> Controls => new()
    {
        { typeof(Button), "button" },
        { typeof(RepeatButton), "button" },
        { typeof(ToggleButton), "button" },
        { typeof(CheckBox), "checkbutton" },
        { typeof(RadioButton), "checkbutton" },
        { typeof(ToggleSwitch), "switch" },
        { typeof(TextBox), "entry" },
        { typeof(ComboBox), "dropdown" },
        { typeof(ListBox), "list" },
        { typeof(ScrollViewer), "scrolledwindow" },
        { typeof(Slider), "scale" },
        { typeof(ProgressBar), "progressbar" },
        { typeof(Menu), "menubar" },
        { typeof(Separator), "separator" },
        { typeof(Label), "label" },
    };

    [AvaloniaTheory]
    [MemberData(nameof(Controls))]
    public void Control_Has_A_Template_And_Its_Node(Type type, string node)
    {
        var control = (TemplatedControl)Activator.CreateInstance(type)!;
        var w = Shots.Show(control);
        Assert.NotNull(control.Template);
        Assert.NotEmpty(control.GetVisualChildren());
        Assert.Equal(node, Css.GetCssNode(control)?.Name);
        w.Close();
    }

    [AvaloniaFact]
    public void Switch_Marks_Follow_Status_Shapes()
    {
        var toggle = new ToggleSwitch { IsChecked = true };
        var w = Shots.Show(toggle);
        Control[] Marks() => toggle.GetVisualDescendants().OfType<Control>().Where(c => Presenters.SwitchLayout.GetIsOnImage(c) is not null).ToArray();
        Assert.Equal(2, Marks().Length);
        Assert.All(Marks(), m => Assert.Equal(0, m.Opacity));

        var shapes = new FixedSettings(new Settings.SettingsValues { ShowStatusShapes = true });
        AdwaitaEnvironment.State.AddSource(shapes);
        try
        {
            Dispatcher.UIThread.RunJobs();
            Assert.All(Marks(), m => Assert.Equal(1, m.Opacity));
        }
        finally
        {
            AdwaitaEnvironment.State.RemoveSource(shapes);
            Dispatcher.UIThread.RunJobs();
        }

        Assert.All(Marks(), m => Assert.Equal(0, m.Opacity));
        w.Close();
    }

    // Every control Avalonia's own themes style.
    private static readonly Type[] s_stockControls =
    [
        typeof(Avalonia.Controls.Primitives.AdornerLayer),
        typeof(AutoCompleteBox),
        typeof(Button),
        typeof(ButtonSpinner),
        typeof(Calendar),
        typeof(Avalonia.Controls.Primitives.CalendarButton),
        typeof(CalendarDatePicker),
        typeof(Avalonia.Controls.Primitives.CalendarDayButton),
        typeof(Avalonia.Controls.Primitives.CalendarItem),
        typeof(Carousel),
        typeof(CarouselPage),
        typeof(CheckBox),
        typeof(ComboBox),
        typeof(ComboBoxItem),
        typeof(CommandBar),
        typeof(CommandBarButton),
        typeof(CommandBarSeparator),
        typeof(CommandBarToggleButton),
        typeof(ContentPage),
        typeof(ContextMenu),
        typeof(Avalonia.Controls.DataValidationErrors),
        typeof(DatePicker),
        typeof(Avalonia.Controls.DatePickerPresenter),
        typeof(Avalonia.Dialogs.ManagedFileChooser),
        typeof(Avalonia.Dialogs.ManagedFileChooserOverwritePrompt),
        typeof(DrawerPage),
        typeof(DropDownButton),
        typeof(Avalonia.Controls.Embedding.EmbeddableControlRoot),
        typeof(Expander),
        typeof(FlyoutPresenter),
        typeof(GridSplitter),
        typeof(GroupBox),
        typeof(HeaderedContentControl),
        typeof(HyperlinkButton),
        typeof(ItemsControl),
        typeof(Label),
        typeof(ListBox),
        typeof(ListBoxItem),
        typeof(Menu),
        typeof(MenuFlyoutPresenter),
        typeof(MenuItem),
        typeof(NavigationPage),
        typeof(Avalonia.Controls.Notifications.NotificationCard),
        typeof(NumericUpDown),
        typeof(Avalonia.Controls.Primitives.OverlayPopupHost),
        typeof(PathIcon),
        typeof(PipsPager),
        typeof(Avalonia.Controls.Primitives.PopupRoot),
        typeof(ProgressBar),
        typeof(RadioButton),
        typeof(RefreshContainer),
        typeof(RefreshVisualizer),
        typeof(RepeatButton),
        typeof(ScrollBar),
        typeof(ScrollViewer),
        typeof(SelectableTextBlock),
        typeof(Separator),
        typeof(Slider),
        typeof(SplitButton),
        typeof(SplitView),
        typeof(TabbedPage),
        typeof(TabControl),
        typeof(TabItem),
        typeof(TableView),
        typeof(TableViewCell),
        typeof(TableViewColumnHeader),
        typeof(TableViewRow),
        typeof(TabStrip),
        typeof(TabStripItem),
        typeof(TextBox),
        typeof(Avalonia.Controls.Primitives.TextSelectionHandle),
        typeof(Avalonia.Controls.ThemeVariantScope),
        typeof(TimePicker),
        typeof(Avalonia.Controls.TimePickerPresenter),
        typeof(ToggleButton),
        typeof(ToggleSwitch),
        typeof(ToolTip),
        typeof(TransitioningContentControl),
        typeof(TreeView),
        typeof(TreeViewItem),
        typeof(Window),
        typeof(Avalonia.Controls.Chrome.WindowDrawnDecorations),
        typeof(Avalonia.Controls.Notifications.WindowNotificationManager),
    ];

    [AvaloniaFact]
    public void Every_Stock_Control_Has_A_Theme()
    {
        var missing = s_stockControls.Where(t => !Avalonia.Application.Current!.TryFindResource(t, out var r) || r is not Avalonia.Styling.ControlTheme).Select(t => t.Name).ToList();
        Assert.True(missing.Count == 0, "no ControlTheme for " + string.Join(", ", missing));
    }

    [AvaloniaFact]
    public void Every_Stock_Templated_Control_Builds()
    {
        var failures = new System.Collections.Generic.List<string>();
        foreach (var t in s_stockControls.Where(t => typeof(TemplatedControl).IsAssignableFrom(t) && !typeof(Avalonia.Controls.TopLevel).IsAssignableFrom(t) && t.GetConstructor(Type.EmptyTypes) is not null))
        {
            try
            {
                var control = (TemplatedControl)Activator.CreateInstance(t)!;
                var w = Shots.Show(control);
                if (control.Template is null)
                {
                    failures.Add(t.Name + ": no template");
                }

                w.Close();
            }
            catch (Exception e)
            {
                failures.Add(t.Name + ": " + e.GetType().Name + " " + e.Message);
            }
        }

        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    [AvaloniaFact]
    public void Pseudo_Classes_Map_To_Gtk_States()
    {
        var button = new Button { Content = "B" };
        var toggle = new ToggleButton { Content = "T", IsChecked = true };
        var check = new CheckBox { IsThreeState = true, IsChecked = null };
        var list = new ListBox { ItemsSource = new[] { "a", "b" }, SelectedIndex = 1 };
        var disabled = new Button { Content = "D", IsEnabled = false };
        var panel = new StackPanel();
        panel.Children.Add(button);
        panel.Children.Add(toggle);
        panel.Children.Add(check);
        panel.Children.Add(list);
        panel.Children.Add(disabled);
        var w = Shots.Show(panel);
        CssStates States(Avalonia.StyledElement e) => Css.GetCssNode(e)!.States;

        var pseudo = (IPseudoClasses)button.Classes;
        pseudo.Set(":pointerover", true);
        Dispatcher.UIThread.RunJobs();
        Assert.True(States(button).HasFlag(CssStates.Hover));
        pseudo.Set(":pressed", true);
        Dispatcher.UIThread.RunJobs();
        Assert.True(States(button).HasFlag(CssStates.Active));
        pseudo.Set(":pointerover", false);
        pseudo.Set(":pressed", false);
        Dispatcher.UIThread.RunJobs();
        Assert.False(States(button).HasFlag(CssStates.Hover));

        Assert.True(States(toggle).HasFlag(CssStates.Checked));
        Assert.True(Css.GetCssNode(check)!.Children.Single(n => n.Name == "check").States.HasFlag(CssStates.Indeterminate));
        Assert.True(States(disabled).HasFlag(CssStates.Disabled));
        var rows = list.GetLogicalDescendants().OfType<ListBoxItem>().ToList();
        Assert.True(States(rows[1]).HasFlag(CssStates.Selected));
        Assert.False(States(rows[0]).HasFlag(CssStates.Selected));
        Assert.Equal((0, 2), Css.GetCssNode(rows[0])!.ItemPosition);

        NodeTree.ExtraWindowStates = CssStates.Backdrop;
        NodeTree.RefreshSubtree(Css.GetCssNode(w)!);
        Assert.True(States(button).HasFlag(CssStates.Backdrop));
        NodeTree.ExtraWindowStates = CssStates.None;
        NodeTree.RefreshSubtree(Css.GetCssNode(w)!);
        w.Close();
    }
}
