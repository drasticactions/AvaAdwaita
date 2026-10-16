using System.Collections.Generic;
using Avalonia.Collections;
using Avalonia.Controls;

namespace AvaAdwaita.Gallery.Pages;

public sealed partial class DataGridPage : UserControl
{
    public DataGridPage()
    {
        InitializeComponent();
        Plain.ItemsSource = new List<FileRow>(Files());
        Table.ItemsSource = new List<FileRow>(Files());
        var grouped = new DataGridCollectionView(Files());
        grouped.GroupDescriptions.Add(new DataGridPathGroupDescription(nameof(FileRow.Kind)));
        Grouped.ItemsSource = grouped;
    }

    private static FileRow[] Files() =>
    [
        new() { Name = "Report.odt", Kind = "Document", Size = "24 KB", Starred = true },
        new() { Name = "Budget.ods", Kind = "Document", Size = "11 KB" },
        new() { Name = "Holiday.jpg", Kind = "Picture", Size = "3.1 MB" },
        new() { Name = "Portrait.png", Kind = "Picture", Size = "820 KB", Starred = true },
        new() { Name = "Ideas.md", Kind = "Text", Size = "2 KB" },
        new() { Name = "Notes.txt", Kind = "Text", Size = "1 KB" },
    ];
}
