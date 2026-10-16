using System;
using System.IO;
using Avalonia;
using Avalonia.Headless;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using AvaAdwaita.Chrome;
using AvaAdwaita.Fonts;

namespace AvaAdwaita.Gallery.Desktop;

/// <summary>
/// <c>--shoot DIR [--dark] [--accent NAME] [--high-contrast]</c>: renders every gallery page headless to
/// <c>DIR/&lt;page&gt;.png</c>, with the bundled stylesheet and no desktop settings.
/// </summary>
internal static class Shooter
{
    public static int Run(string[] args)
    {
        var dir = args.Length > 0 && !args[0].StartsWith("--", StringComparison.Ordinal) ? args[0] : Path.Combine("artifacts", "gallery");
        AppBuilder.Configure<App>()
            .UseSkia()
            .UseHarfBuzz()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
            .UseAdwaitaFonts()
            .WithAvaAdwaita(new AvaAdwaitaOptions { CssSource = AdwCssSource.Bundled, UseSystemSettings = false })
            .SetupWithoutStarting();
        WindowChrome.AssumeClientSide = true;
        var theme = App.Theme;
        theme.ColorScheme = Array.IndexOf(args, "--dark") >= 0 ? AdwColorScheme.ForceDark : AdwColorScheme.ForceLight;
        var accent = Array.IndexOf(args, "--accent");
        if (accent >= 0 && accent + 1 < args.Length && Enum.TryParse<AdwAccentColor>(args[accent + 1], true, out var a))
        {
            theme.AccentColor = a;
        }

        if (Array.IndexOf(args, "--high-contrast") >= 0)
        {
            theme.HighContrast = AdwHighContrast.On;
        }

        Directory.CreateDirectory(dir);
        var window = new MainWindow { Width = 1000, Height = 720 };
        window.Show();
        var view = window.View;
        for (var i = 0; i < view.PageTitles.Count; i++)
        {
            window.Height = 720;
            view.SelectPage(i);
            Pump();

            // Tall enough for the whole page, so a shot shows every section.
            window.Height = Math.Max(720, view.PageExtentHeight + 120);
            Pump();
            using var frame = window.CaptureRenderedFrame();
            if (frame is null)
            {
                Console.Error.WriteLine("no frame");
                return 1;
            }

            var path = Path.Combine(dir, Slug(view.PageTitles[i]) + ".png");
            frame.Save(path, new PngBitmapEncoderOptions());
            Console.WriteLine(path);
        }

        return 0;
    }

    private static void Pump()
    {
        for (var i = 0; i < 4; i++)
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        }
    }

    private static string Slug(string title) =>
        string.Join('-', title.ToLowerInvariant().Split([' ', '&'], StringSplitOptions.RemoveEmptyEntries));
}
