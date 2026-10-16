using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Runtime.InteropServices;

namespace AvaAdwaita.Presenters;

/// <summary>
/// The C library's abbreviated day names, which GtkCalendar shows (<c>strftime ("%a")</c>). They differ from .NET's
/// in some languages: glibc's Arabic names are one letter where .NET's are whole words.
/// </summary>
internal static class LocaleDayNames
{
    private const int LcTimeMask = 1 << 2;
    private const int AbDay1 = 2 << 16;

    private static readonly ConcurrentDictionary<string, string[]?> s_names = new();
    private static readonly Lazy<bool> s_glibc = new(IsGlibc);

    /// <summary>
    /// The abbreviated day names, Sunday first, of the glibc locale for <paramref name="culture"/>, or null where there
    /// is none: off glibc, or when the locale is not installed.
    /// </summary>
    public static string[]? Abbreviated(CultureInfo culture) =>
        culture.Name.Length == 0 ? null : s_names.GetOrAdd(culture.Name, Load);

    private static bool IsGlibc()
    {
        if (!OperatingSystem.IsLinux())
        {
            return false;
        }

        try
        {
            return gnu_get_libc_version() != IntPtr.Zero;
        }
        catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException)
        {
            return false;
        }
    }

    private static string[]? Load(string name)
    {
        if (!s_glibc.Value)
        {
            return null;
        }

        var localeName = Marshal.StringToCoTaskMemUTF8(name.Replace('-', '_') + ".UTF-8");
        var locale = newlocale(LcTimeMask, localeName, IntPtr.Zero);
        Marshal.FreeCoTaskMem(localeName);
        if (locale == IntPtr.Zero)
        {
            return null;
        }

        try
        {
            var names = new string[7];
            for (var i = 0; i < 7; i++)
            {
                if (Marshal.PtrToStringUTF8(nl_langinfo_l(AbDay1 + i, locale)) is not { Length: > 0 } day)
                {
                    return null;
                }

                names[i] = day;
            }

            return names;
        }
        finally
        {
            freelocale(locale);
        }
    }

    [DllImport("libc")]
    private static extern IntPtr gnu_get_libc_version();

    [DllImport("libc")]
    private static extern IntPtr newlocale(int categoryMask, IntPtr locale, IntPtr baseLocale);

    [DllImport("libc")]
    private static extern IntPtr nl_langinfo_l(int item, IntPtr locale);

    [DllImport("libc")]
    private static extern void freelocale(IntPtr locale);
}
