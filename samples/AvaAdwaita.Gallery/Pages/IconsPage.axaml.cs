using System;
using System.Linq;
using Avalonia.Controls;
using AvaAdwaita.Presenters;

namespace AvaAdwaita.Gallery.Pages;

public sealed partial class IconsPage : UserControl
{
    private const string Prefix = "AvaAdwaita.Icons.";

    public IconsPage()
    {
        InitializeComponent();
        Bundled.ItemsSource = typeof(AdwIcon).Assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith(Prefix, StringComparison.Ordinal) && n.EndsWith("-symbolic.svg", StringComparison.Ordinal))
            .Select(n => n[Prefix.Length..^".svg".Length])
            .Order()
            .ToArray();
    }
}
