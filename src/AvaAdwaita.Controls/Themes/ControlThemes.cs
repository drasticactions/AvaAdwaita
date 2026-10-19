using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AvaAdwaita.Controls.Themes;

internal partial class ControlThemes : ResourceDictionary
{
    public ControlThemes()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
