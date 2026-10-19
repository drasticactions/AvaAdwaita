using System;
using System.Collections.Generic;
using System.Text;
using AvaAdwaita.Resources;

namespace AvaAdwaita.Controls.Primitives;

/// <summary>GTK's accelerator syntax: <c>&lt;Control&gt;&lt;Shift&gt;q</c>, as gtk_accelerator_parse reads it.</summary>
internal static class Accelerators
{
    private static readonly Dictionary<string, char> s_characters = new(StringComparer.Ordinal)
    {
        ["space"] = ' ', ["exclam"] = '!', ["quotedbl"] = '"', ["numbersign"] = '#', ["dollar"] = '$', ["percent"] = '%',
        ["ampersand"] = '&', ["apostrophe"] = '\'', ["parenleft"] = '(', ["parenright"] = ')', ["asterisk"] = '*',
        ["plus"] = '+', ["comma"] = ',', ["minus"] = '-', ["period"] = '.', ["slash"] = '/', ["colon"] = ':',
        ["semicolon"] = ';', ["less"] = '<', ["equal"] = '=', ["greater"] = '>', ["question"] = '?', ["at"] = '@',
        ["bracketleft"] = '[', ["backslash"] = '\\', ["bracketright"] = ']', ["asciicircum"] = '^', ["underscore"] = '_',
        ["grave"] = '`', ["braceleft"] = '{', ["bar"] = '|', ["braceright"] = '}', ["asciitilde"] = '~',
        ["KP_Space"] = ' ', ["KP_Multiply"] = '*', ["KP_Add"] = '+', ["KP_Separator"] = ',', ["KP_Subtract"] = '-',
        ["KP_Decimal"] = '.', ["KP_Divide"] = '/', ["KP_Equal"] = '=',
    };

    /// <summary>The key's name and modifiers, or null when <paramref name="accelerator"/> is not one.</summary>
    public static (string Key, GdkModifiers Modifiers)? Parse(string accelerator)
    {
        var modifiers = GdkModifiers.None;
        var rest = accelerator.AsSpan();
        while (rest.Length > 0 && rest[0] == '<')
        {
            var close = rest.IndexOf('>');
            if (close < 0)
            {
                return null;
            }

            switch (rest[1..close].ToString().ToLowerInvariant())
            {
                case "primary" or "control" or "ctrl" or "ctl":
                    modifiers |= GdkModifiers.Control;
                    break;
                case "shift" or "shft":
                    modifiers |= GdkModifiers.Shift;
                    break;
                case "alt" or "mod1":
                    modifiers |= GdkModifiers.Alt;
                    break;
                case "super":
                    modifiers |= GdkModifiers.Super;
                    break;
                case "hyper":
                    modifiers |= GdkModifiers.Hyper;
                    break;
                case "meta":
                    modifiers |= GdkModifiers.Meta;
                    break;
                case "release" or "mod2" or "mod3" or "mod4" or "mod5":
                    break;
                default:
                    return null;
            }

            rest = rest[(close + 1)..];
        }

        var key = rest.ToString();
        if (key.Length == 0 || key.Contains('<') || key.Contains('>'))
        {
            return null;
        }

        return (key.Length == 1 ? char.ToLowerInvariant(key[0]).ToString() : key, modifiers);
    }

    /// <summary>The character a key types, as gdk_keyval_to_unicode, or null.</summary>
    public static char? Character(string key)
    {
        if (key.Length == 1)
        {
            return key[0];
        }

        if (key.Length == 4 && key.StartsWith("KP_", StringComparison.Ordinal) && char.IsAsciiDigit(key[3]))
        {
            return key[3];
        }

        return s_characters.TryGetValue(key, out var c) ? c : null;
    }

    /// <summary>The shortcut's label for people (gtk_accelerator_get_label): the modifiers and the key joined by "+".</summary>
    public static string Label(string key, GdkModifiers modifiers)
    {
        var label = new StringBuilder();
        void Add(string text)
        {
            if (label.Length > 0)
            {
                label.Append('+');
            }

            label.Append(text);
        }

        if (modifiers.HasFlag(GdkModifiers.Shift))
        {
            Add(GtkCatalog.Pgettext("keyboard label", "Shift"));
        }

        if (modifiers.HasFlag(GdkModifiers.Control))
        {
            Add(GtkCatalog.Pgettext("keyboard label", "Ctrl"));
        }

        if (modifiers.HasFlag(GdkModifiers.Alt))
        {
            Add(GtkCatalog.Pgettext("keyboard label", "Alt"));
        }

        if (modifiers.HasFlag(GdkModifiers.Super))
        {
            Add(GtkCatalog.Pgettext("keyboard label", "Super"));
        }

        if (modifiers.HasFlag(GdkModifiers.Hyper))
        {
            Add(GtkCatalog.Pgettext("keyboard label", "Hyper"));
        }

        if (modifiers.HasFlag(GdkModifiers.Meta))
        {
            Add(GtkCatalog.Pgettext("keyboard label", "Meta"));
        }

        Add(key == "space" ? GtkCatalog.Pgettext("keyboard label", "Space")
            : Character(key) is { } c && c > ' ' && c < 0x7f ? char.ToUpperInvariant(c).ToString()
            : GtkCatalog.Current?.Translate("keyboard label", key) ?? key.Replace('_', ' '));
        return label.ToString();
    }
}
