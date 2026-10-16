using System.Text;
using Avalonia.Input;
using AvaAdwaita.Resources;

namespace AvaAdwaita.Markup;

/// <summary>
/// A key gesture as GTK labels an accelerator in a menu (gtk_accelerator_print_label): Shift, Ctrl, Alt and Super in
/// that order, joined by "+", in the user's language, then the key's character in upper case or its name.
/// </summary>
internal static class AcceleratorLabel
{
    private const string Context = "keyboard label";

    public static string Of(KeyGesture gesture)
    {
        var label = new StringBuilder();
        void Modifier(KeyModifiers flag, string name)
        {
            if ((gesture.KeyModifiers & flag) != 0)
            {
                Separate(label);
                label.Append(GtkCatalog.Pgettext(Context, name));
            }
        }

        Modifier(KeyModifiers.Shift, "Shift");
        Modifier(KeyModifiers.Control, "Ctrl");
        Modifier(KeyModifiers.Alt, "Alt");
        Modifier(KeyModifiers.Meta, "Super");

        var key = Key(gesture.Key);
        if (key.Length > 0)
        {
            Separate(label);
            label.Append(key);
        }

        return label.ToString();
    }

    private static void Separate(StringBuilder label)
    {
        if (label.Length > 0)
        {
            label.Append('+');
        }
    }

    // A key with a character shows it in upper case; another shows its GDK name, translated in the "keyboard label"
    // context where GTK's catalog has it, and otherwise with its underscores as spaces.
    private static string Key(Key key)
    {
        if (key is >= Avalonia.Input.Key.A and <= Avalonia.Input.Key.Z)
        {
            return ((char)('A' + (key - Avalonia.Input.Key.A))).ToString();
        }

        if (key is >= Avalonia.Input.Key.D0 and <= Avalonia.Input.Key.D9)
        {
            return ((char)('0' + (key - Avalonia.Input.Key.D0))).ToString();
        }

        var character = key switch
        {
            Avalonia.Input.Key.OemPeriod or Avalonia.Input.Key.Decimal => ".",
            Avalonia.Input.Key.OemComma => ",",
            Avalonia.Input.Key.OemMinus or Avalonia.Input.Key.Subtract => "-",
            Avalonia.Input.Key.OemPlus or Avalonia.Input.Key.Add => "+",
            Avalonia.Input.Key.Multiply => "*",
            Avalonia.Input.Key.Divide or Avalonia.Input.Key.OemQuestion => "/",
            Avalonia.Input.Key.OemSemicolon => ";",
            Avalonia.Input.Key.OemQuotes => "'",
            Avalonia.Input.Key.OemOpenBrackets => "[",
            Avalonia.Input.Key.OemCloseBrackets => "]",
            Avalonia.Input.Key.OemTilde => "`",
            _ => null,
        };
        if (character is not null)
        {
            return character;
        }

        if (key == Avalonia.Input.Key.Space)
        {
            return GtkCatalog.Pgettext(Context, "Space");
        }

        if (key == Avalonia.Input.Key.OemBackslash || key == Avalonia.Input.Key.OemPipe)
        {
            return GtkCatalog.Pgettext(Context, "Backslash");
        }

        var name = key switch
        {
            Avalonia.Input.Key.Back => "BackSpace",
            Avalonia.Input.Key.Enter => "Return",
            Avalonia.Input.Key.Escape => "Escape",
            Avalonia.Input.Key.PageUp => "Page_Up",
            Avalonia.Input.Key.PageDown => "Page_Down",
            Avalonia.Input.Key.None => string.Empty,
            _ => key.ToString(),
        };
        return GtkCatalog.Current?.Translate(Context, name) ?? name.Replace('_', ' ');
    }
}
