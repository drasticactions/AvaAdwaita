using System;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;

namespace AvaAdwaita.Gallery;

/// <summary>Shows where the stylesheet and settings come from, and switches the color scheme, accent, contrast and stylesheet.</summary>
public sealed class StylePanel : StackPanel
{
    private readonly Label _status = new();

    public StylePanel()
    {
        Spacing = 12;
        Margin = new Avalonia.Thickness(6);
        _status.Classes.Add("dim-label");
        _status.Classes.Add("caption");
        var theme = App.Theme;
        Children.Add(Row("Color scheme", Combo(theme.ColorScheme, v => theme.ColorScheme = v)));
        Children.Add(Row("Accent", Combo(theme.AccentColor, v => theme.AccentColor = v)));
        Children.Add(Row("High contrast", Combo(theme.HighContrast, v => theme.HighContrast = v)));
        var css = Combo(GallerySettings.CssSource, v => GallerySettings.CssSource = v);
        css.IsEnabled = !OperatingSystem.IsBrowser();
        Children.Add(Row("Stylesheet (restart)", css));
        Children.Add(_status);
        AdwaitaEnvironment.Changed += _ => Dispatcher.UIThread.Post(Update);
        Update();
    }

    private void Update()
    {
        var s = AdwaitaEnvironment.State;
        _status.Content = $"Stylesheet: {App.Theme.StyleSheetDescription}\nSettings: {s.SourceName}\n"
            + $"{(s.Dark ? "Dark" : "Light")}, {(s.HighContrast ? "high contrast" : "normal contrast")}, accent {s.Accent}";
    }

    private Control Row(string label, Control value)
    {
        var p = new DockPanel { LastChildFill = true };
        var l = new Label { Content = label, VerticalAlignment = VerticalAlignment.Center, MinWidth = Orientation == Orientation.Horizontal ? 0 : 150, Margin = new Avalonia.Thickness(0, 0, 12, 0) };
        DockPanel.SetDock(l, Dock.Left);
        p.Children.Add(l);
        p.Children.Add(value);
        return p;
    }

    private static ComboBox Combo<T>(T current, Action<T> set)
        where T : struct, Enum
    {
        var values = Enum.GetValues<T>();
        var c = new ComboBox { ItemsSource = values, SelectedItem = current, MinWidth = 140 };
        c.SelectionChanged += (_, _) =>
        {
            if (c.SelectedItem is T v)
            {
                set(v);
            }
        };
        return c;
    }
}
