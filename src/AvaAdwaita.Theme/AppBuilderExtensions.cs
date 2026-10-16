using Avalonia;

namespace AvaAdwaita;

/// <summary>Application builder setup for AvaAdwaita.</summary>
public static class AppBuilderExtensions
{
    /// <summary>
    /// Starts loading the libadwaita stylesheet and reading the desktop settings on a background thread,
    /// so the work overlaps the rest of start-up. Optional: <see cref="AdwaitaTheme"/> loads on first use.
    /// </summary>
    public static AppBuilder WithAvaAdwaita(this AppBuilder builder, AvaAdwaitaOptions? options = null)
    {
        AdwaitaEnvironment.Preload(options);
        return builder;
    }
}
