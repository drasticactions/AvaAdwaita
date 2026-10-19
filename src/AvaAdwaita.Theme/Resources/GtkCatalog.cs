using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace AvaAdwaita.Resources;

/// <summary>
/// A gettext message catalog (<c>.mo</c>) of the installed GTK, which translates the GTK strings the theme shows, as
/// GTK's own widgets are translated.
/// </summary>
internal sealed class GtkCatalog
{
    private readonly Dictionary<string, string> _messages;
    private readonly Dictionary<string, string[]> _plurals;
    private readonly Func<long, long> _plural;

    private GtkCatalog(Dictionary<string, string> messages, Dictionary<string, string[]> plurals)
    {
        _messages = messages;
        _plurals = plurals;

        // The header's Plural-Forms picks a variant for a count; without one, as English.
        var rule = messages.TryGetValue(string.Empty, out var header)
            ? header.Split('\n').FirstOrDefault(l => l.StartsWith("Plural-Forms:", StringComparison.OrdinalIgnoreCase))
            : null;
        _plural = rule is not null && rule.IndexOf("plural=", StringComparison.Ordinal) is var at and >= 0
            ? PluralExpression.Parse(rule[(at + 7)..].TrimEnd().TrimEnd(';')) ?? (n => n == 1 ? 0 : 1)
            : n => n == 1 ? 0 : 1;
    }

    private static GtkCatalog? s_current;
    private static bool s_loaded;

    /// <summary>GTK's catalog for the user's language, or null where there is none.</summary>
    public static GtkCatalog? Current
    {
        get
        {
            if (!s_loaded)
            {
                s_current = Load("gtk40", CultureInfo.CurrentUICulture);
                s_loaded = true;
            }

            return s_current;
        }
    }

    /// <summary>The translation of <paramref name="msgid"/>, or null when the catalog has none.</summary>
    public string? Translate(string msgid) => _messages.TryGetValue(msgid, out var s) && s.Length > 0 ? s : null;

    /// <summary>The translation of <paramref name="msgid"/> in <paramref name="context"/>, as C_() looks it up.</summary>
    public string? Translate(string context, string msgid) => Translate(context + "\u0004" + msgid);

    /// <summary>The translation of <paramref name="msgid"/> for <paramref name="n"/> things (ngettext), or null.</summary>
    public string? Translate(string msgid, string msgidPlural, long n)
    {
        if (!_plurals.TryGetValue(msgid, out var forms))
        {
            return null;
        }

        var index = _plural(n);
        return index >= 0 && index < forms.Length && forms[index].Length > 0 ? forms[index] : null;
    }

    /// <summary><paramref name="msgid"/> in the user's language: GTK's translation, or the English.</summary>
    public static string Gettext(string msgid) => Current?.Translate(msgid) ?? msgid;

    /// <summary><paramref name="msgid"/> in <paramref name="context"/> in the user's language, as C_().</summary>
    public static string Pgettext(string context, string msgid) => Current?.Translate(context, msgid) ?? msgid;

    /// <summary>
    /// The installed catalog of <paramref name="domain"/> for <paramref name="culture"/>, looked up as gettext does
    /// (<c>de_DE</c>, then <c>de</c>) in each <c>$XDG_DATA_DIRS/locale</c>; null where there is none.
    /// </summary>
    public static GtkCatalog? Load(string domain, CultureInfo culture)
    {
        if (culture.Name.Length == 0)
        {
            return null;
        }

        foreach (var dir in DataDirs())
        {
            foreach (var language in Languages(culture))
            {
                var path = Path.Combine(dir, "locale", language, "LC_MESSAGES", domain + ".mo");
                try
                {
                    if (File.Exists(path) && Parse(File.ReadAllBytes(path), out var plurals) is { } messages)
                    {
                        return new GtkCatalog(messages, plurals);
                    }
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }
        }

        return null;
    }

    private static IEnumerable<string> DataDirs()
    {
        var dirs = Environment.GetEnvironmentVariable("XDG_DATA_DIRS");
        if (string.IsNullOrEmpty(dirs))
        {
            dirs = "/usr/local/share:/usr/share";
        }

        return dirs.Split(':', StringSplitOptions.RemoveEmptyEntries);
    }

    // de-DE gives de_DE, then de; zh-Hans-CN gives zh_CN, then zh.
    private static IEnumerable<string> Languages(CultureInfo culture)
    {
        var parts = culture.Name.Split('-');
        if (parts.Length > 1)
        {
            yield return parts[0] + "_" + parts[^1];
        }

        yield return parts[0];
    }

    /// <summary>The messages of a <c>.mo</c> file, keyed by message id; null when it is not one.</summary>
    internal static Dictionary<string, string>? Parse(byte[] data) => Parse(data, out _);

    /// <summary>The messages of a <c>.mo</c> file, and its plural messages' variants keyed by the singular id.</summary>
    internal static Dictionary<string, string>? Parse(byte[] data, out Dictionary<string, string[]> plurals)
    {
        plurals = new Dictionary<string, string[]>(StringComparer.Ordinal);
        if (data.Length < 28)
        {
            return null;
        }

        bool little;
        var magic = BitConverter.ToUInt32(data, 0);
        if (magic == 0x950412de)
        {
            little = BitConverter.IsLittleEndian;
        }
        else if (magic == 0xde120495)
        {
            little = !BitConverter.IsLittleEndian;
        }
        else
        {
            return null;
        }

        uint U32(int offset)
        {
            var v = BitConverter.ToUInt32(data, offset);
            return little ? v : System.Buffers.Binary.BinaryPrimitives.ReverseEndianness(v);
        }

        string? Text(int table, int index)
        {
            var length = (int)U32(table + (index * 8));
            var offset = (int)U32(table + (index * 8) + 4);
            return offset < 0 || length < 0 || offset + length > data.Length ? null : Encoding.UTF8.GetString(data, offset, length);
        }

        var count = (int)U32(8);
        var originals = (int)U32(12);
        var translations = (int)U32(16);
        if (originals + (count * 8) > data.Length || translations + (count * 8) > data.Length)
        {
            return null;
        }

        var messages = new Dictionary<string, string>(count, StringComparer.Ordinal);
        for (var i = 0; i < count; i++)
        {
            if (Text(originals, i) is not { } id || Text(translations, i) is not { } text)
            {
                continue;
            }

            // A plural message's id and translation hold their variants after a NUL.
            if (id.IndexOf('\0') is var nul and >= 0)
            {
                plurals[id[..nul]] = text.Split('\0');
            }
            else
            {
                messages[id] = text;
            }
        }

        return messages;
    }
}
