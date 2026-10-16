namespace AvaAdwaita;

/// <summary>Options for <see cref="AppBuilderExtensions.WithAvaAdwaita"/>.</summary>
public sealed class AvaAdwaitaOptions
{
    /// <summary>Which libadwaita stylesheet to run.</summary>
    public AdwCssSource CssSource { get; set; } = AdwCssSource.Auto;

    /// <summary>Also load <c>$XDG_CONFIG_HOME/gtk-4.0/gtk.css</c> at user priority. Off by default.</summary>
    public bool LoadUserStyleSheet { get; set; }

    /// <summary>Read the desktop portal and dconf on Linux. Off makes tests independent of the session.</summary>
    public bool UseSystemSettings { get; set; } = true;
}
