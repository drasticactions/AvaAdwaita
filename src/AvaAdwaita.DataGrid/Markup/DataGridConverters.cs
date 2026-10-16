using Avalonia.Controls;
using Avalonia.Data.Converters;

namespace AvaAdwaita.DataGrid.Markup;

/// <summary>Converters the DataGrid themes use to turn grid properties into GTK classes.</summary>
public static class DataGridConverters
{
    /// <summary>The column view's classes: <c>view</c>, and <c>column-separators</c> with vertical grid lines.</summary>
    public static readonly IValueConverter ColumnViewClasses = new FuncValueConverter<DataGridGridLinesVisibility, string>(
        v => (v & DataGridGridLinesVisibility.Vertical) != 0 ? "view column-separators" : "view");

    /// <summary>The list view's classes: <c>view</c>, and <c>separators</c> with horizontal grid lines.</summary>
    public static readonly IValueConverter ListViewClasses = new FuncValueConverter<DataGridGridLinesVisibility, string>(
        v => (v & DataGridGridLinesVisibility.Horizontal) != 0 ? "view separators" : "view");
}
