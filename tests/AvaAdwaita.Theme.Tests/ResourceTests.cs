using Avalonia;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using AvaAdwaita.Resources;
using Xunit;

namespace AvaAdwaita.Theme.Tests;

public class ResourceTests
{
    private static object? Get(string key, ThemeVariant? variant = null)
    {
        var provider = new AdwResourceProvider();
        return provider.TryGetResource(key, variant, out var v) ? v : null;
    }

    [AvaloniaFact]
    public void Custom_Properties_And_Named_Colors_Are_Served()
    {
        var theme = TestApplication.Instance.Theme;
        try
        {
            theme.AccentColor = AdwAccentColor.Blue;
            Assert.Equal(Color.Parse("#3584e4"), Get("Adw.AccentBgColor"));
            Assert.IsAssignableFrom<ISolidColorBrush>(Get("Adw.AccentBgColorBrush"));
            Assert.Equal(Color.Parse("#3584e4"), Get("Adw.Named.accent_bg_color"));
            Assert.Equal(Color.Parse("#fafafb"), Get("Adw.WindowBgColor", ThemeVariant.Light));
            Assert.Equal(Color.Parse("#222226"), Get("Adw.WindowBgColor", ThemeVariant.Dark));
            Assert.Equal(Get("Adw.AccentBgColor"), Get("SystemAccentColor"));
            Assert.IsType<FontFamily>(Get("AdwFontFamily"));
            Assert.Equal(14.6667, (double)Get("AdwFontSize")!, 3);

            theme.AccentColor = AdwAccentColor.Purple;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(Color.Parse("#9141ac"), Get("Adw.AccentBgColor"));
        }
        finally
        {
            theme.AccentColor = AdwAccentColor.System;
        }
    }

    [AvaloniaFact]
    public void The_Variant_Follows_The_Color_Scheme_And_The_App()
    {
        var theme = TestApplication.Instance.Theme;
        var app = Application.Current!;
        try
        {
            theme.ColorScheme = AdwColorScheme.ForceDark;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(ThemeVariant.Dark, app.RequestedThemeVariant);

            theme.ColorScheme = AdwColorScheme.Default;
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(ThemeVariant.Light, app.RequestedThemeVariant);

            // An application that asks for Dark itself gets it, as ForceDark.
            app.RequestedThemeVariant = ThemeVariant.Dark;
            Dispatcher.UIThread.RunJobs();
            Assert.True(AdwaitaEnvironment.State.Dark);
        }
        finally
        {
            app.RequestedThemeVariant = ThemeVariant.Default;
            AdwaitaEnvironment.State.RequestedDark = null;
            theme.ColorScheme = AdwColorScheme.Default;
            Dispatcher.UIThread.RunJobs();
        }
    }
}
