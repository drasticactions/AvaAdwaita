using System;
using Avalonia;
using AvaAdwaita.Fonts;

namespace AvaAdwaita.Gallery.Desktop;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        if (args.Length >= 1 && args[0] == "--shoot")
        {
            return Shooter.Run(args.AsSpan(1).ToArray());
        }

        MainWindow.NoExtend = Array.IndexOf(args, "--no-extend") >= 0;
        return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UsePlatformDetect()
        .UseWaylandWithFallback()
        .UseAdwaitaFonts()
        .WithAvaAdwaita(new AvaAdwaitaOptions { CssSource = GallerySettings.CssSource })
        .LogToTrace();
}
