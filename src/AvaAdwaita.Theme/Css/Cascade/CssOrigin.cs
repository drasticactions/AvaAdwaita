namespace AvaAdwaita.CssEngine.Cascade;

/// <summary>Where a style sheet comes from. The values are GTK's style provider priorities.</summary>
public enum CssOrigin
{
    /// <summary><c>GTK_STYLE_PROVIDER_PRIORITY_THEME</c>: <c>gtk.css</c> and the runtime providers.</summary>
    Theme = 200,

    /// <summary><c>GTK_STYLE_PROVIDER_PRIORITY_APPLICATION</c>: the application's style sheets.</summary>
    Application = 600,

    /// <summary><c>GTK_STYLE_PROVIDER_PRIORITY_USER</c>: <c>~/.config/gtk-4.0/gtk.css</c>.</summary>
    User = 800,
}
