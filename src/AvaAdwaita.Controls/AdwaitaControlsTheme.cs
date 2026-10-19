using Avalonia.Styling;

namespace AvaAdwaita.Controls;

/// <summary>
/// The ControlThemes of the controls in this library: libadwaita's and GTK's widgets that Avalonia has no control
/// for. Add it to <c>Application.Styles</c> after <see cref="AdwaitaTheme"/>.
/// </summary>
public class AdwaitaControlsTheme : Styles
{
    /// <summary>Creates the theme.</summary>
    public AdwaitaControlsTheme()
    {
        Resources.MergedDictionaries.Add(new Themes.ControlThemes());
        Resources.MergedDictionaries.Add(Controls.Resources.AdwStrings.For(System.Globalization.CultureInfo.CurrentUICulture));
    }
}
