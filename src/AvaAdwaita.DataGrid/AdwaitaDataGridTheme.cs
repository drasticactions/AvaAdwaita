using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Styling;
using Avalonia.VisualTree;
using AvaAdwaita.Nodes;

namespace AvaAdwaita.DataGrid;

/// <summary>
/// Styles Avalonia's <c>DataGrid</c> as a GTK column view. Add it to <c>Application.Styles</c> after
/// <see cref="AdwaitaTheme"/>. Add the <c>data-table</c> class to a grid for compact rows; its grid lines
/// become GTK's row and column separators.
/// </summary>
public class AdwaitaDataGridTheme : Styles
{
    static AdwaitaDataGridTheme()
    {
        // The grid keeps its rows, cells and headers in creation order and places them by index, so their nodes
        // take their positions from the grid. A row, cell or header that moves has changed position.
        CssNodeProviders.RegisterPosition<DataGridRow>(RowPosition, Visual.BoundsProperty);
        CssNodeProviders.RegisterPosition<DataGridCell>(ColumnPosition, Visual.BoundsProperty);
        CssNodeProviders.RegisterPosition<DataGridColumnHeader>(ColumnPosition, Visual.BoundsProperty);
        TemplatedControl.TemplateAppliedEvent.AddClassHandler<DataGridColumnHeader>((h, _) => HideFiller(h, "AdwDataGridCornerHeader"));
        TemplatedControl.TemplateAppliedEvent.AddClassHandler<DataGridCell>((c, _) => HideFiller(c, "AdwDataGridFillerCell"));
    }

    /// <summary>Creates the theme.</summary>
    public AdwaitaDataGridTheme()
    {
        Resources.MergedDictionaries.Add(new Themes.ControlThemes());
    }

    private static (int Index, int Count)? RowPosition(DataGridRow row)
    {
        var index = row.Index;
        if (index < 0 || row.FindAncestorOfType<Avalonia.Controls.DataGrid>() is not { } grid)
        {
            return null;
        }

        return (index, System.Math.Max(index + 1, (grid.CollectionView as Avalonia.Collections.DataGridCollectionView)?.Count ?? 0));
    }

    // Among the visible columns in display order.
    private static (int Index, int Count)? ColumnPosition(Control element)
    {
        if (DataGridColumn.GetColumnContainingElement(element) is not { } column || element.FindAncestorOfType<Avalonia.Controls.DataGrid>() is not { } grid)
        {
            return null;
        }

        var visible = grid.Columns.Where(c => c.IsVisible).OrderBy(c => c.DisplayIndex).ToList();
        var index = visible.IndexOf(column);
        return index >= 0 ? (index, visible.Count) : null;
    }

    // The filler column that takes the width after the last is the grid's own and not in its columns; GTK has
    // no such column, so its header and cells are empty space rather than buttons and cells.
    private static void HideFiller(TemplatedControl part, string emptyTheme)
    {
        // A row's filler cell has no column at all.
        var column = DataGridColumn.GetColumnContainingElement(part);
        var filler = column is null ? part is DataGridCell : part.FindAncestorOfType<Avalonia.Controls.DataGrid>() is { } grid && !grid.Columns.Contains(column);
        if (filler && part.TryFindResource(emptyTheme, out var theme) && theme is ControlTheme t && !ReferenceEquals(part.Theme, t))
        {
            part.Theme = t;
        }
    }
}
