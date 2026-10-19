using System.Linq;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using AvaAdwaita.Controls;
using AvaAdwaita.Controls.Primitives;
using AvaAdwaita.Nodes;
using Xunit;

namespace AvaAdwaita.Theme.Tests;

/// <summary>AvaAdwaita.Controls' ViewStack and ViewSwitcher: the visible page, the buttons and their indicators.</summary>
public class ViewSwitcherTests
{
    private static ViewStack Stack() => new()
    {
        Pages =
        {
            new ViewStackPage { Name = "albums", Title = "Albums", IconName = "view-grid-symbolic", Content = "A" },
            new ViewStackPage { Name = "artists", Title = "Artists", IconName = "view-list-symbolic", Content = "B", BadgeNumber = 3 },
            new ViewStackPage { Name = "songs", Title = "Songs", IconName = "audio-volume-high-symbolic", Content = "C", NeedsAttention = true },
        },
    };

    [AvaloniaFact]
    public void Switcher_Shows_And_Selects_The_Pages()
    {
        var stack = Stack();
        var switcher = new ViewSwitcher { Stack = stack, Policy = ViewSwitcherPolicy.Wide };
        var w = Shots.Show(new StackPanel { Children = { switcher, stack } });
        Assert.Equal("albums", stack.VisiblePageName);
        Assert.True(stack.Pages[0].IsVisible);
        Assert.False(stack.Pages[1].IsVisible);

        var buttons = switcher.Buttons.Cast<ViewSwitcherButton>().ToList();
        Assert.Equal([true, false, false], buttons.Select(b => b.IsChecked == true));
        Assert.Equal(buttons[0].Bounds.Width, buttons[2].Bounds.Width);

        buttons[1].IsChecked = true;
        Assert.Same(stack.Pages[1], stack.VisiblePage);
        Assert.Equal([false, true, false], buttons.Select(b => b.IsChecked == true));

        // A checked button stays checked when clicked again.
        buttons[1].IsChecked = false;
        Assert.True(buttons[1].IsChecked);

        stack.VisiblePageName = "songs";
        Assert.True(buttons[2].IsChecked);
        Assert.True(stack.Pages[2].IsVisible);

        stack.Pages[0].IsPageVisible = false;
        Shots.Pump(w);
        Assert.False(switcher.Buttons[0].IsVisible);
        w.Close();
    }

    [AvaloniaFact]
    public void Bar_Shows_A_Narrow_Switcher_When_Revealed()
    {
        var stack = Stack();
        var bar = new ViewSwitcherBar { Stack = stack };
        var w = Shots.Show(new StackPanel { Children = { stack, bar } });
        AdwaitaEnvironment.SuppressTransitions(w);
        Assert.Equal(0, bar.Bounds.Height);

        bar.Reveal = true;
        Shots.Pump(w);
        var switcher = bar.GetVisualDescendants().OfType<ViewSwitcher>().Single();
        Assert.Same(stack, switcher.Stack);
        Assert.Equal(ViewSwitcherPolicy.Narrow, switcher.Policy);
        Assert.True(bar.Bounds.Height > 0);
        Assert.Equal("viewswitcherbar", Css.GetCssNode(bar)!.Name);
        w.Close();
    }

    [AvaloniaFact]
    public void Inline_Switcher_Toggles_Select_The_Pages()
    {
        var stack = Stack();
        var inline = new InlineViewSwitcher { Stack = stack, DisplayMode = InlineViewSwitcherDisplayMode.Both };
        var w = Shots.Show(new StackPanel { Children = { inline, stack } });
        Assert.Contains("both", Css.GetCssNode(inline.List!)!.Classes);
        Assert.Equal(3, inline.List!.ItemCount);
        Assert.Equal(0, inline.List.SelectedIndex);

        inline.List.SelectedIndex = 2;
        Assert.Equal("songs", stack.VisiblePageName);

        // The pages stay in the stack: the toggles show records of them.
        Assert.All(stack.Pages, p => Assert.Same(stack, p.FindLogicalAncestorOfType<ViewStack>()));
        stack.VisiblePageName = "artists";
        Assert.Equal(1, inline.List.SelectedIndex);
        w.Close();
    }

    [AvaloniaFact]
    public void Stack_Switcher_Buttons_Show_Icons_Or_Titles()
    {
        var stack = Stack();
        stack.Pages[0].IconName = null;
        var switcher = new StackSwitcher { Stack = stack };
        var w = Shots.Show(new StackPanel { Children = { switcher, stack } });
        var buttons = switcher.Buttons.OfType<Avalonia.Controls.Primitives.ToggleButton>().ToList();
        Assert.Equal("Albums", buttons[0].Content);
        Assert.IsType<Presenters.AdwIcon>(buttons[1].Content);
        Assert.Contains("needs-attention", buttons[2].Classes);
        Assert.Contains("linked", Css.GetCssNode(switcher)!.Classes);

        buttons[1].IsChecked = true;
        Assert.Equal("artists", stack.VisiblePageName);
        Assert.False(buttons[0].IsChecked);
        w.Close();
    }

    [AvaloniaFact]
    public void Indicators_Show_Badges_And_Attention()
    {
        var stack = Stack();
        var switcher = new ViewSwitcher { Stack = stack };
        var w = Shots.Show(switcher);
        var bins = switcher.Buttons.Select(b => ((Avalonia.Visual)b).GetVisualDescendants().OfType<IndicatorBin>().First(i => i.IsEffectivelyVisible)).ToList();
        Assert.Equal(["", "badge", "needs-attention"], bins.Select(b => string.Join(".", Css.GetCssNode(b)!.Classes)));
        Assert.Contains("narrow", Css.GetCssNode(switcher)!.Classes);
        w.Close();
    }
}
