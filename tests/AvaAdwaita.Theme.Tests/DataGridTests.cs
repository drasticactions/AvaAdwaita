using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using AvaAdwaita.CssEngine.Selectors;
using AvaAdwaita.Nodes;
using Xunit;

namespace AvaAdwaita.Theme.Tests;

/// <summary>AvaAdwaita.DataGrid's themes: the grid as a GTK column view, its separators, positions, groups and editing.</summary>
public class DataGridTests
{
    public sealed class Item
    {
        public string Name { get; set; } = string.Empty;

        public string Kind { get; set; } = string.Empty;

        public bool Done { get; set; }
    }

    private static List<Item> Items() =>
    [
        new() { Name = "Alpha", Kind = "Letter" },
        new() { Name = "Beta", Kind = "Letter", Done = true },
        new() { Name = "One", Kind = "Number" },
    ];

    private static Avalonia.Controls.DataGrid Grid(object items) => new()
    {
        Width = 360,
        Height = 240,
        ItemsSource = (System.Collections.IEnumerable)items,
        Columns =
        {
            new DataGridTextColumn { Header = "Name", Binding = new Avalonia.Data.Binding("Name") },
            new DataGridTextColumn { Header = "Kind", Binding = new Avalonia.Data.Binding("Kind") },
            new DataGridCheckBoxColumn { Header = "Done", Binding = new Avalonia.Data.Binding("Done") },
        },
    };

    private static IEnumerable<CssNode> Nodes(Avalonia.Visual root, string name) =>
        new[] { root }.Concat(root.GetVisualDescendants()).Select(v => v is Avalonia.StyledElement e ? Css.GetCssNode(e) : null)
            .Where(n => n is not null && n.Name == name).Distinct()!;

    [AvaloniaFact]
    public void Grid_Is_A_Column_View()
    {
        var grid = Grid(Items());
        var w = Shots.Show(grid);
        Assert.Equal("columnview", Css.GetCssNode(grid)?.Name);
        var header = Assert.Single(Nodes(grid, "header"));
        Assert.Equal(3, header.Children.Count(c => c.Name == "button" && c.Visible));
        Assert.Single(Nodes(grid, "listview"));
        Assert.Equal(3, Nodes(grid, "row").Count());
        Assert.Equal(9, Nodes(grid, "cell").Count());
        Assert.All(Nodes(grid, "cell").Take(2), c => Assert.Equal("label", c.Children.Single().Name));
        Assert.Contains(Nodes(grid, "checkbutton"), _ => true);
        w.Close();
    }

    [AvaloniaFact]
    public void Grid_Lines_Are_Separators()
    {
        var grid = Grid(Items());
        var w = Shots.Show(grid);
        var view = Css.GetCssNode(grid)!;
        var list = Nodes(grid, "listview").Single();
        Assert.False(view.HasClass("column-separators"));
        Assert.False(list.HasClass("separators"));

        grid.GridLinesVisibility = DataGridGridLinesVisibility.All;
        Shots.Pump(w);
        Assert.True(view.HasClass("column-separators"));
        Assert.True(list.HasClass("separators"));

        // A first cell has no separator before it; the others do.
        var widths = Nodes(grid, "cell").Select(c => (c.PositionFromStart, Width: c.Style!.GetValueText("border-left-width"))).ToList();
        Assert.All(widths, c => Assert.Equal(c.PositionFromStart == 1 ? "0" : "1", c.Width));
        w.Close();
    }

    [AvaloniaFact]
    public void Moved_Columns_Take_Their_Display_Position()
    {
        var grid = Grid(Items());
        var w = Shots.Show(grid);
        grid.Columns[2].DisplayIndex = 0;
        Shots.Pump(w);
        var row = grid.GetVisualDescendants().OfType<DataGridRow>().First();
        var cells = row.GetVisualDescendants().OfType<DataGridCell>().ToList();
        Assert.Equal(1, Css.GetCssNode(cells.Single(c => DataGridColumn.GetColumnContainingElement(c) == grid.Columns[2]))!.PositionFromStart);
        Assert.Equal(2, Css.GetCssNode(cells.Single(c => DataGridColumn.GetColumnContainingElement(c) == grid.Columns[0]))!.PositionFromStart);
        var header = grid.GetVisualDescendants().OfType<DataGridColumnHeader>().Single(h => DataGridColumn.GetColumnContainingElement(h) == grid.Columns[2]);
        Assert.Equal(1, Css.GetCssNode(header)!.PositionFromStart);
        w.Close();
    }

    [AvaloniaFact]
    public void Sorting_Shows_The_Indicator()
    {
        var grid = Grid(Items());
        var w = Shots.Show(grid);
        var indicator = Nodes(grid, "sort-indicator").First();
        Assert.True(indicator.HasClass("unsorted"));
        grid.Columns[0].Sort(ListSortDirection.Ascending);
        Shots.Pump(w);
        Assert.True(indicator.HasClass("ascending"));
        grid.Columns[0].Sort(ListSortDirection.Descending);
        Shots.Pump(w);
        Assert.True(indicator.HasClass("descending"));
        w.Close();
    }

    [AvaloniaFact]
    public void Groups_Have_An_Expander()
    {
        var view = new DataGridCollectionView(Items());
        view.GroupDescriptions.Add(new DataGridPathGroupDescription("Kind"));
        var grid = Grid(view);
        var w = Shots.Show(grid);
        var expanders = Nodes(grid, "expander").ToList();
        Assert.Equal(2, expanders.Count);
        Assert.All(expanders, e => Assert.Equal("treeexpander", e.Parent?.Name));
        Assert.All(expanders, e => Assert.True(e.States.HasFlag(CssStates.Checked)));

        var group = grid.GetVisualDescendants().OfType<DataGridRowGroupHeader>().First();
        grid.CollapseRowGroup(view.Groups.Cast<DataGridCollectionViewGroup>().First(), false);
        Shots.Pump(w);
        Assert.Contains(Nodes(grid, "expander"), e => !e.States.HasFlag(CssStates.Checked));
        Assert.Equal("row", Css.GetCssNode(group)?.Name);
        w.Close();
    }

    [AvaloniaFact]
    public void Read_Only_Checks_Look_Sensitive()
    {
        var grid = Grid(Items());
        var w = Shots.Show(grid);
        var checks = Nodes(grid, "checkbutton").ToList();
        Assert.NotEmpty(checks);
        Assert.Contains(grid.GetVisualDescendants().OfType<CheckBox>(), c => !c.IsEnabled);
        Assert.All(checks, c => Assert.False(c.States.HasFlag(CssStates.Disabled)));

        grid.IsEnabled = false;
        Shots.Pump(w);
        Assert.All(Nodes(grid, "checkbutton"), c => Assert.True(c.States.HasFlag(CssStates.Disabled)));
        w.Close();
    }

    [AvaloniaFact]
    public void Editing_A_Cell_Shows_An_Entry()
    {
        var grid = Grid(Items());
        var w = Shots.Show(grid);
        grid.SelectedIndex = 0;
        grid.CurrentColumn = grid.Columns[0];
        Shots.Pump(w);
        Assert.True(grid.BeginEdit());
        Shots.Pump(w);
        var box = grid.GetVisualDescendants().OfType<TextBox>().Single();
        Assert.Equal("entry", Css.GetCssNode(box)?.Name);
        Assert.Equal("cell", Css.GetCssNode(box)?.Parent?.Name);
        w.Close();
    }
}
