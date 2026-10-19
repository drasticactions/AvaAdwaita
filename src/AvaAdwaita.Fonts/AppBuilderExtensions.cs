using System;
using Avalonia;
using Avalonia.Media;

namespace AvaAdwaita.Fonts;

/// <summary>Application builder setup for the Adwaita fonts.</summary>
public static class AppBuilderExtensions
{
    /// <summary>The <see cref="AppContext"/> key the theme reads the fallback families from.</summary>
    public const string FallbackKey = "AvaAdwaita.Fonts.Fallback";

    /// <summary>
    /// Registers Adwaita Sans and Adwaita Mono and makes them the fallback when the desktop's fonts are not
    /// installed, and on other platforms.
    /// </summary>
    public static AppBuilder UseAdwaitaFonts(this AppBuilder builder)
    {
        AppContext.SetData(FallbackKey, AdwaitaFontCollection.Sans + "|" + AdwaitaFontCollection.Mono);
        builder = builder.ConfigureFonts(m =>
        {
            m.AddFontCollection(new AdwaitaFontCollection());
            m.AddFontCollection(new AdwaitaMonoFontCollection());
        });

        // A browser has no fonts of its own, so the default family would be whichever face loaded first.
        return OperatingSystem.IsBrowser()
            ? builder.With(new FontManagerOptions { DefaultFamilyName = AdwaitaFontCollection.Sans })
            : builder;
    }
}
