using System.Globalization;
using System.Linq;
using Avalonia.Controls;
using AvaAdwaita.Resources;

namespace AvaAdwaita.Controls.Resources;

/// <summary>
/// The controls' text in the user's language: each string is libadwaita's, translated by the installed libadwaita's
/// catalog, as libadwaita's own widgets are.
/// </summary>
internal static class AdwStrings
{
    /// <summary>Resource key, libadwaita message id; the id is the English text.</summary>
    internal static readonly (string Key, string Id)[] Strings =
    [
        ("AdwStringApply", "Apply"),
        ("AdwStringShowPassword", "Show Password"),
        ("AdwStringHidePassword", "Hide Password"),
        ("AdwStringBack", "Back"),
        ("AdwStringPreferences", "Preferences"),
        ("AdwStringSearch", "Search"),
        ("AdwStringSearchPreferences", "Search preferences"),
        ("AdwStringNoResultsFound", "No Results Found"),
        ("AdwStringTryADifferentSearch", "Try a different search"),
        ("AdwStringUntitledPage", "Untitled page"),
        ("AdwStringAbout", "About"),
        ("AdwStringWhatsNewRow", "What’s _New"),
        ("AdwStringDetailsRow", "_Details"),
        ("AdwStringWebsiteRow", "_Website"),
        ("AdwStringSupportRow", "_Support Questions"),
        ("AdwStringIssueRow", "_Report an Issue"),
        ("AdwStringTroubleshootingRow", "_Troubleshooting"),
        ("AdwStringCreditsRow", "_Credits"),
        ("AdwStringLegalRow", "_Legal"),
        ("AdwStringAcknowledgementsRow", "_Acknowledgements"),
        ("AdwStringWhatsNew", "What’s New"),
        ("AdwStringDetails", "Details"),
        ("AdwStringTroubleshooting", "Troubleshooting"),
        ("AdwStringTroubleshootingText", "To assist in troubleshooting, you can view your debugging information. Providing this information to the application developers can help diagnose any problems you encounter when you report an issue."),
        ("AdwStringDebuggingInformationRow", "_Debugging Information"),
        ("AdwStringDebuggingInformation", "Debugging Information"),
        ("AdwStringCopyText", "_Copy Text"),
        ("AdwStringSaveAs", "_Save As…"),
        ("AdwStringCredits", "Credits"),
        ("AdwStringLegal", "Legal"),
        ("AdwStringAcknowledgements", "Acknowledgements"),
        ("AdwStringCodeBy", "Code by"),
        ("AdwStringDesignBy", "Design by"),
        ("AdwStringArtworkBy", "Artwork by"),
        ("AdwStringDocumentationBy", "Documentation by"),
        ("AdwStringTranslatedBy", "Translated by"),
        ("AdwStringThisApplication", "This Application"),
        ("AdwStringSaveDebuggingInformation", "Save debugging information"),
        ("AdwStringUnableToSaveDebuggingInformation", "Unable to save debugging information"),
        ("AdwStringClose", "Close"),
        ("AdwStringCopiedToClipboard", "Copied to clipboard"),
        ("AdwStringCloseTab", "Close Tab"),
        ("AdwStringViewOpenTabs", "View Open Tabs"),
        ("AdwStringSearchTabs", "Search Tabs"),
        ("AdwStringSearchTabsPlaceholder", "Search tabs"),
        ("AdwStringNoOpenTabs", "No Open Tabs"),
        ("AdwStringNoTabsFound", "No Tabs Found"),
        ("AdwStringNewTab", "New _Tab"),
        ("AdwStringKeyboardShortcuts", "Keyboard Shortcuts"),
        ("AdwStringSearchShortcuts", "Search shortcuts"),
        ("AdwStringNoShortcut", "No Shortcut"),
        ("AdwStringLicenseGpl20", "GNU General Public License, version 2 or later"),
        ("AdwStringLicenseGpl30", "GNU General Public License, version 3 or later"),
        ("AdwStringLicenseLgpl21", "GNU Lesser General Public License, version 2.1 or later"),
        ("AdwStringLicenseLgpl30", "GNU Lesser General Public License, version 3 or later"),
        ("AdwStringLicenseBsd", "BSD 2-Clause License"),
        ("AdwStringLicenseMitX11", "The MIT License (MIT)"),
        ("AdwStringLicenseArtistic", "Artistic License 2.0"),
        ("AdwStringLicenseGpl20Only", "GNU General Public License, version 2 only"),
        ("AdwStringLicenseGpl30Only", "GNU General Public License, version 3 only"),
        ("AdwStringLicenseLgpl21Only", "GNU Lesser General Public License, version 2.1 only"),
        ("AdwStringLicenseLgpl30Only", "GNU Lesser General Public License, version 3 only"),
        ("AdwStringLicenseAgpl30", "GNU Affero General Public License, version 3 or later"),
        ("AdwStringLicenseAgpl30Only", "GNU Affero General Public License, version 3 only"),
        ("AdwStringLicenseBsd3", "BSD 3-Clause License"),
        ("AdwStringLicenseApache20", "Apache License, Version 2.0"),
        ("AdwStringLicenseMpl20", "Mozilla Public License 2.0"),
        ("AdwStringLicense0Bsd", "BSD Zero-Clause License"),
    ];

    /// <summary>Resource key, GTK message id, for the GTK widgets the controls rebuild.</summary>
    internal static readonly (string Key, string Id)[] GtkStrings =
    [
        ("AdwStringClearEntry", "Clear Entry"),
    ];

    /// <summary>Resource key, libadwaita message id with a printf %s, which becomes a composite format item.</summary>
    internal static readonly (string Key, string Id)[] Formats =
    [
        ("AdwStringAvatarOf", "Avatar of %s"),
        ("AdwStringVersion", "Version %s"),
        ("AdwStringLicensePreamble", "This application comes with absolutely no warranty. See the <a href=\"%s\">%s</a> for details."),
    ];

    /// <summary>The strings for <paramref name="culture"/>: translated where libadwaita has them, English elsewhere.</summary>
    public static ResourceDictionary For(CultureInfo culture)
    {
        var catalog = GtkCatalog.Load("libadwaita", culture);
        var dictionary = new ResourceDictionary();
        foreach (var (key, id) in Strings)
        {
            dictionary[key] = catalog?.Translate(id) ?? id;
        }

        var gtk = GtkCatalog.Load("gtk40", culture);
        foreach (var (key, id) in GtkStrings)
        {
            dictionary[key] = gtk?.Translate(id) ?? id;
        }

        foreach (var (key, id) in Formats)
        {
            var text = catalog?.Translate(id) ?? id;
            dictionary[key] = Composite(text);
        }

        return dictionary;
    }

    private static GtkCatalog? s_catalog;
    private static bool s_catalogLoaded;

    /// <summary>
    /// libadwaita's text for <paramref name="n"/> things (ngettext) in the user's language, with its <c>%u</c> or
    /// <c>%d</c> filled in.
    /// </summary>
    public static string Plural(string singular, string plural, long n)
    {
        if (!s_catalogLoaded)
        {
            s_catalog = GtkCatalog.Load("libadwaita", CultureInfo.CurrentUICulture);
            s_catalogLoaded = true;
        }

        var text = s_catalog?.Translate(singular, plural, n) ?? (n == 1 ? singular : plural);
        var count = n.ToString(CultureInfo.CurrentCulture);
        return text.Replace("%u", count).Replace("%d", count);
    }

    /// <summary>The English text for <paramref name="key"/>, for a control not yet in a tree with the resources.</summary>
    public static string English(string key) =>
        Strings.Concat(GtkStrings).FirstOrDefault(s => s.Key == key).Id is { } id ? id
        : Formats.FirstOrDefault(s => s.Key == key).Id is { } format ? Composite(format) : key;

    // Each printf %s becomes the next composite format item, in order.
    private static string Composite(string text)
    {
        var escaped = text.Replace("{", "{{").Replace("}", "}}");
        var parts = escaped.Split("%s");
        var builder = new System.Text.StringBuilder(parts[0]);
        for (var i = 1; i < parts.Length; i++)
        {
            builder.Append('{').Append(i - 1).Append('}').Append(parts[i]);
        }

        return builder.ToString();
    }
}
