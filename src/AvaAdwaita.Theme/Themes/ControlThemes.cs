using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AvaAdwaita.Themes;

internal partial class ControlThemes : ResourceDictionary
{
    public ControlThemes()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
