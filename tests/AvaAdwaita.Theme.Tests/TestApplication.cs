using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using AvaAdwaita.Chrome;
using AvaAdwaita.Fonts;

[assembly: AvaloniaTestApplication(typeof(AvaAdwaita.Theme.Tests.TestApplication))]
[assembly: AvaloniaTestIsolation(AvaloniaTestIsolationLevel.PerTest)]

namespace AvaAdwaita.Theme.Tests;

public class TestApplication : Application
{
    static TestApplication()
    {
        AdwaitaEnvironment.Preload(new AvaAdwaitaOptions { UseSystemSettings = false, CssSource = AdwCssSource.Bundled });
        WindowChrome.AssumeClientSide = true;

        // The oracle renders GTK with the GNOME schema defaults: Adwaita Sans 11, Adwaita Sans 12, Adwaita Mono 11.
        AdwaitaEnvironment.State.AddSource(new FixedSettings(new Settings.SettingsValues { FontName = "Adwaita Sans 11", DocumentFontName = "Adwaita Sans 12", MonospaceFontName = "Adwaita Mono 11" }));
    }

    public TestApplication()
    {
        Theme = new AdwaitaTheme();
        Styles.Add(Theme);
        Styles.Add(new DataGrid.AdwaitaDataGridTheme());
    }

    public AdwaitaTheme Theme { get; }

    public static TestApplication Instance => (TestApplication)Current!;

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<TestApplication>()
        .UseSkia()
        .UseHarfBuzz()
        .UseAdwaitaFonts()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}
