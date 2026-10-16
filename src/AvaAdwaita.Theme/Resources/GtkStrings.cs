using System.Globalization;
using Avalonia.Controls;

namespace AvaAdwaita.Resources;

/// <summary>
/// The theme's text in the user's language: each string the theme shows is a GTK string, translated by the installed
/// GTK's catalog, as GTK's own widgets are.
/// </summary>
internal static class GtkStrings
{
    // Resource key, GTK message id. The English values are in Strings.axaml.
    private static readonly (string Key, string Id)[] s_strings =
    [
        ("StringTextFlyoutCutText", "Cu_t"),
        ("StringTextFlyoutCopyText", "_Copy"),
        ("StringTextFlyoutPasteText", "_Paste"),
        ("StringManagedFileChooserFileNamePlaceholder", "Name"),
        ("StringManagedFileChooserShowHiddenFilesText", "Show _Hidden Files"),
        ("StringManagedFileChooserOkText", "_Open"),
        ("StringManagedFileChooserCancelText", "_Cancel"),
        ("StringManagedFileChooserNameColumn", "Name"),
        ("StringManagedFileChooserDateModifiedColumn", "Modified"),
        ("StringManagedFileChooserTypeColumn", "Type"),
        ("StringManagedFileChooserSizeColumn", "Size"),
        ("StringManagedFileChooserOverwritePromptConfirmText", "_Replace"),
        ("StringManagedFileChooserOverwritePromptCancelText", "_Cancel"),
        ("AdwStringDelete", "_Delete"),
        ("AdwStringSelectAll", "Select _All"),
        ("AdwStringChangeDirection", "Change di_rection"),
        ("AdwStringCancel", "_Cancel"),
        ("AdwStringSelect", "_Select"),
    ];

    private const string OverwritePromptKey = "StringManagedFileChooserOverwritePromptFileAlreadyExistsText";
    private const string OverwritePromptId = "A file named “%s” already exists.  Do you want to replace it?";

    /// <summary>The translated strings for <paramref name="culture"/>, or an empty dictionary where GTK has none.</summary>
    public static ResourceDictionary For(CultureInfo culture)
    {
        var dictionary = new ResourceDictionary();
        if ((Equals(culture, CultureInfo.CurrentUICulture) ? GtkCatalog.Current : GtkCatalog.Load("gtk40", culture)) is not { } catalog)
        {
            return dictionary;
        }

        foreach (var (key, id) in s_strings)
        {
            if (catalog.Translate(id) is { } text)
            {
                dictionary[key] = text;
            }
        }

        // GTK's printf placeholder becomes the composite format item Avalonia fills in.
        if (catalog.Translate(OverwritePromptId) is { } prompt)
        {
            dictionary[OverwritePromptKey] = prompt.Replace("{", "{{").Replace("}", "}}").Replace("%s", "{0}");
        }

        return dictionary;
    }
}
