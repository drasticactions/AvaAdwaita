using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using AvaAdwaita.Resources;

namespace AvaAdwaita.Gallery.Pages;

public sealed partial class ColorsPage : UserControl
{
    public ColorsPage()
    {
        InitializeComponent();
        AdwaitaEnvironment.Changed += _ => Dispatcher.UIThread.Post(Fill);
        Fill();
    }

    private void Fill()
    {
        var provider = new AdwResourceProvider();
        Swatches.ItemsSource = provider.Keys
            .Where(k => k.StartsWith("Adw.", StringComparison.Ordinal) && !k.StartsWith("Adw.Named.", StringComparison.Ordinal) && k.EndsWith("ColorBrush", StringComparison.Ordinal))
            .Order()
            .Select(key =>
            {
                provider.TryGetResource(key, null, out var brush);
                var name = "--" + string.Concat(key["Adw.".Length..^"Brush".Length].Select((ch, i) => char.IsUpper(ch) ? (i > 0 ? "-" : string.Empty) + char.ToLowerInvariant(ch) : ch.ToString()));
                return new Swatch(name, brush as IBrush);
            })
            .ToArray();
    }
}
