using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Browser;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using AvaAdwaita.Fonts;

[assembly: System.Runtime.Versioning.SupportedOSPlatform("browser")]

namespace AvaAdwaita.Gallery.Browser;

internal static class Program
{
    // A browser has no fonts of its own, so Chinese, Japanese and Korean text falls back to Noto Sans CJK.
    private const string Cjk = "fonts:Cjk#Noto Sans CJK JP";

    private static Task Main(string[] args) => AppBuilder.Configure<App>()
        .UseAdwaitaFonts()
        .ConfigureFonts(m => m.AddFontCollection(new EmbeddedFontCollection(new Uri("fonts:Cjk"), new Uri("avares://AvaAdwaita.Gallery.Browser/Fonts"))))
        .With(new FontManagerOptions
        {
            DefaultFamilyName = AdwaitaFontCollection.Sans,
            FontFallbacks = [new FontFallback { FontFamily = new FontFamily(Cjk) }],
        })
        .WithAvaAdwaita(new AvaAdwaitaOptions { CssSource = AdwCssSource.Bundled })
        .StartBrowserAppAsync("out");
}
