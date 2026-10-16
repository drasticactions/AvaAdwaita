using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Layout;

namespace AvaAdwaita.Markup;

/// <summary>Small converters the control themes use.</summary>
public static class CssConverters
{
    /// <summary>
    /// A node's color as GtkTextView draws its text: through Pango's 16-bit color attributes, which truncate the
    /// alpha (half of white is 127, where a label's float color rounds to 128). Bind it to <c>Css.Style</c>.
    /// </summary>
    public static readonly IValueConverter PangoForeground = new FuncValueConverter<CssEngine.Computed.ComputedStyle?, Avalonia.Media.IBrush?>(s =>
    {
        if (s is null)
        {
            return null;
        }

        var c = s.Core.Color;
        var (r, g, b, _) = c.ToRgba8();
        var alpha = System.Math.Floor(System.Math.Clamp(c.Alpha, 0, 1) * 65535) / 65535;
        return new Avalonia.Media.Immutable.ImmutableSolidColorBrush(Avalonia.Media.Color.FromRgb(r, g, b), alpha);
    });

    /// <summary>
    /// Selected text's color, as GtkText draws it: the text, then the selection's background over it, then the text
    /// again in the selection's color (transparent in libadwaita, so nothing). Avalonia draws the selection behind the
    /// text and the selected text in one brush, so that brush is the composite. Bind the text's foreground, then the
    /// selection node's <c>Css.Style</c>.
    /// </summary>
    public static readonly IMultiValueConverter SelectedText = new FuncMultiValueConverter<object?, Avalonia.Media.IBrush?>(values =>
    {
        var list = System.Linq.Enumerable.ToList(values);
        if (list.Count < 2 || list[0] is not Avalonia.Media.ISolidColorBrush text || list[1] is not CssEngine.Computed.ComputedStyle selection)
        {
            return null;
        }

        var foreground = text.Color;
        var under = Avalonia.Media.Color.FromArgb((byte)System.Math.Round(foreground.A * text.Opacity), foreground.R, foreground.G, foreground.B);
        var tinted = Over(CssConvert.Color(selection.Drawn(selection.Background.Color)), under);
        return new Avalonia.Media.Immutable.ImmutableSolidColorBrush(Over(CssConvert.Color(selection.Drawn(selection.Core.Color)), tinted));
    });

    // Source over destination, in sRGB as GTK's cairo and GSK renderers blend.
    private static Avalonia.Media.Color Over(Avalonia.Media.Color source, Avalonia.Media.Color destination)
    {
        double sa = source.A / 255.0, da = destination.A / 255.0;
        var a = sa + (da * (1 - sa));
        if (a <= 0)
        {
            return default;
        }

        byte Channel(byte s, byte d) => (byte)System.Math.Round(((s * sa) + (d * da * (1 - sa))) / a);
        return Avalonia.Media.Color.FromArgb((byte)System.Math.Round(a * 255), Channel(source.R, destination.R), Channel(source.G, destination.G), Channel(source.B, destination.B));
    }

    /// <summary>GTK's orientation class: <c>horizontal</c> or <c>vertical</c>.</summary>
    public static readonly IValueConverter OrientationClass = new FuncValueConverter<Orientation, string>(o => o == Orientation.Vertical ? "vertical" : "horizontal");

    /// <summary>True for <see cref="Orientation.Vertical"/>.</summary>
    public static readonly IValueConverter IsVertical = new FuncValueConverter<Orientation, bool>(o => o == Orientation.Vertical);

    /// <summary>True for a check box menu item.</summary>
    public static readonly IValueConverter IsCheckBoxToggle = new FuncValueConverter<MenuItemToggleType, bool>(t => t == MenuItemToggleType.CheckBox);

    /// <summary>True for a radio menu item.</summary>
    public static readonly IValueConverter IsRadioToggle = new FuncValueConverter<MenuItemToggleType, bool>(t => t == MenuItemToggleType.Radio);

    /// <summary>
    /// A progress bar trough's classes from value, minimum and maximum: <c>empty</c> at the minimum and
    /// <c>full</c> at the maximum, as gtkprogressbar.c's update_node_classes.
    /// </summary>
    public static readonly IMultiValueConverter ProgressTroughClasses = new FuncMultiValueConverter<double, string?>(v =>
    {
        var a = System.Linq.Enumerable.ToArray(v);
        if (a.Length < 3 || a[2] <= a[1])
        {
            return null;
        }

        var fraction = (a[0] - a[1]) / (a[2] - a[1]);
        return fraction <= 0 ? "empty" : fraction >= 1 ? "full" : null;
    });

    /// <summary>A menu button's inner button classes, with <c>.text-button</c> or <c>.image-button</c> from the content, as GtkMenuButton.</summary>
    public static readonly IValueConverter MenuButtonClasses = new FuncValueConverter<object?, string>(c => c switch
    {
        string => "arrow-button toggle text-button",
        AvaAdwaita.Presenters.AdwIcon or PathIcon or Avalonia.Controls.Image => "arrow-button toggle image-button",
        _ => "arrow-button toggle",
    });

    /// <summary>A notebook header's classes from the tab placement: <c>top horizontal</c>, <c>left vertical</c>…</summary>
    public static readonly IValueConverter NotebookHeaderClasses = new FuncValueConverter<Dock, string>(d => d switch
    {
        Dock.Bottom => "bottom horizontal",
        Dock.Left => "left vertical",
        Dock.Right => "right vertical",
        _ => "top horizontal",
    });

    /// <summary>One item per tree level, for a tree expander's <c>indent</c> nodes.</summary>
    public static readonly IValueConverter IndentLevels = new FuncValueConverter<int, int[]>(level => new int[System.Math.Max(0, level)]);

    /// <summary>Stretch for left alignment: a GTK label fills its cell and keeps its text at the start.</summary>
    public static readonly IValueConverter LeftToStretch = new FuncValueConverter<HorizontalAlignment, HorizontalAlignment>(a => a == HorizontalAlignment.Left ? HorizontalAlignment.Stretch : a);

    /// <summary>True for a number above zero.</summary>
    public static readonly IValueConverter IsPositive = new FuncValueConverter<int, bool>(n => n > 0);

    /// <summary>The symbolic icon for a file chooser item's icon key.</summary>
    public static readonly IValueConverter FileIcon = new FuncValueConverter<string?, string>(k => k switch
    {
        "Icon_Folder" => "folder-symbolic",
        "Icon_Volume" => "drive-harddisk-symbolic",
        _ => "text-x-generic-symbolic",
    });

    /// <summary>A key gesture as GTK shows accelerators, as <c>Ctrl+S</c>.</summary>
    public static readonly IValueConverter GestureText = new FuncValueConverter<KeyGesture?, string?>(g => g is null ? null : AcceleratorLabel.Of(g));
}
