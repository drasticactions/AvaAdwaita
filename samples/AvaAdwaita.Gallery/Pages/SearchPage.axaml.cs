using System;
using System.Linq;
using Avalonia.Controls;

namespace AvaAdwaita.Gallery.Pages;

public sealed partial class SearchPage : UserControl
{
    private static readonly string[] s_fruit = ["Apple", "Apricot", "Banana", "Cherry", "Grape", "Lemon", "Mango", "Orange", "Peach", "Pear", "Plum"];

    public SearchPage()
    {
        InitializeComponent();
        Fruit.ItemsSource = s_fruit;
        Bar.KeyCaptureTarget = Results;
        Entry.SearchChanged += (_, _) => EntryResult.Content = $"Searched for “{Entry.Text}”";
        Entry.Activated += (_, _) => EntryResult.Content = $"Activated “{Entry.Text}”";
        BarEntry.SearchChanged += (_, _) =>
            Fruit.ItemsSource = s_fruit.Where(f => f.Contains(BarEntry.Text ?? string.Empty, StringComparison.CurrentCultureIgnoreCase)).ToArray();
    }
}
