using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using AvaAdwaita.CssEngine.Cascade;
using AvaAdwaita.CssEngine.Syntax;
using AvaAdwaita.Sources.Elf;
using AvaAdwaita.Sources.Gvdb;

namespace AvaAdwaita.Sources;

/// <summary>
/// Finds the stylesheet: the one in the installed <c>libadwaita-1.so</c>, read as data from its
/// <c>.gresource.adw_stylesheet</c> section, or the bundled libadwaita 1.10.0 copy.
/// </summary>
public static partial class StyleSheetLoader
{
    /// <summary>The libadwaita version of the bundled stylesheet.</summary>
    public const string BundledVersion = "1.10.0";

    /// <summary>The ELF section that holds libadwaita's stylesheet resource.</summary>
    public const string SectionName = ".gresource.adw_stylesheet";

    /// <summary>The path of the stylesheet inside the resource.</summary>
    public const string CssPath = "/org/gnome/Adwaita/styles/gtk.css";

    /// <summary>The asset files the stylesheet refers to.</summary>
    public static readonly string[] AssetNames = ["bullet.svg", "check.svg", "dash.svg", "devel-stripes.svg"];

    private static StyleSheetSource? s_bundled;

    /// <summary>Loads the stylesheet. Logs one line when the installed one is not used.</summary>
    /// <param name="mode">Which stylesheet to use. <c>AVAADWAITA_CSS=bundled</c> or <c>installed</c> overrides <see cref="AdwCssSource.Auto"/>.</param>
    /// <param name="log">Receives the one-line message.</param>
    public static StyleSheetSource Load(AdwCssSource mode = AdwCssSource.Auto, Action<string>? log = null)
    {
        if (mode == AdwCssSource.Auto)
        {
            var env = Environment.GetEnvironmentVariable("AVAADWAITA_CSS")?.ToLowerInvariant();
            mode = env switch
            {
                "bundled" => AdwCssSource.Bundled,
                "installed" => AdwCssSource.Installed,
                _ => mode,
            };
        }

        if (mode == AdwCssSource.Bundled || !OperatingSystem.IsLinux() && mode == AdwCssSource.Auto)
        {
            return Bundled();
        }

        string reason;
        try
        {
            var lib = FindLibrary();
            if (lib is null)
            {
                reason = "libadwaita-1.so.0 not found";
            }
            else
            {
                var installed = ReadInstalled(lib, out reason);
                if (installed is not null)
                {
                    return installed;
                }
            }
        }
        catch (Exception e) when (e is IOException or InvalidDataException or UnauthorizedAccessException)
        {
            reason = e.Message;
        }

        if (mode == AdwCssSource.Installed)
        {
            throw new InvalidOperationException($"AvaAdwaita: installed stylesheet not used: {reason}");
        }

        log?.Invoke($"AvaAdwaita: installed stylesheet not used: {reason}");
        return Bundled() with { FallbackReason = reason };
    }

    /// <summary>The bundled libadwaita 1.10.0 stylesheet.</summary>
    public static StyleSheetSource Bundled()
    {
        if (s_bundled is not null)
        {
            return s_bundled;
        }

        var asm = typeof(StyleSheetLoader).Assembly;
        string css;
        using (var s = asm.GetManifestResourceStream("AvaAdwaita.Bundled.gtk.css")!)
        using (var r = new StreamReader(s, Encoding.UTF8))
        {
            css = r.ReadToEnd();
        }

        var assets = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        foreach (var name in AssetNames)
        {
            using var s = asm.GetManifestResourceStream("AvaAdwaita.Bundled.assets." + name);
            if (s is not null)
            {
                using var m = new MemoryStream();
                s.CopyTo(m);
                assets[name] = m.ToArray();
            }
        }

        // The bundled copy is checked by the tests, so it is parsed lazily.
        s_bundled = new StyleSheetSource(false, null, BundledVersion, css, assets, true, null);
        return s_bundled;
    }

    /// <summary>The directories searched for <c>libadwaita-1.so.0</c>, in order.</summary>
    public static IEnumerable<string> SearchDirectories()
    {
        var ld = Environment.GetEnvironmentVariable("LD_LIBRARY_PATH");
        if (!string.IsNullOrEmpty(ld))
        {
            foreach (var d in ld.Split(':', StringSplitOptions.RemoveEmptyEntries))
            {
                yield return d;
            }
        }

        yield return "/usr/lib64";
        yield return "/usr/lib";
        yield return "/usr/lib/x86_64-linux-gnu";
        yield return "/usr/lib/aarch64-linux-gnu";
        if (File.Exists("/.flatpak-info"))
        {
            yield return "/app/lib";
        }
    }

    /// <summary>The real path of the installed library, or null.</summary>
    public static string? FindLibrary()
    {
        foreach (var dir in SearchDirectories())
        {
            var p = Path.Combine(dir, "libadwaita-1.so.0");
            if (File.Exists(p))
            {
                var info = new FileInfo(p);
                var target = info.ResolveLinkTarget(returnFinalTarget: true);
                return target?.FullName ?? info.FullName;
            }
        }

        return null;
    }

    [GeneratedRegex(@"libadwaita-1\.so\.0\.(\d+)\.(\d+)$")]
    private static partial Regex SonameVersion();

    /// <summary>The version from the real file name, as <c>libadwaita-1.so.0.1000.0</c> → 1.10.0, or null.</summary>
    public static string? VersionFromPath(string path)
    {
        var m = SonameVersion().Match(path);
        if (!m.Success)
        {
            return null;
        }

        var minor = int.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
        var micro = int.Parse(m.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);
        return $"1.{minor / 100}.{micro}";
    }

    /// <summary>
    /// The version in <c>pkgconfig/libadwaita-1.pc</c> beside the library, for distributions that install it as
    /// <c>libadwaita-1.so.0</c> with no version in the name.
    /// </summary>
    internal static string? VersionFromPkgConfig(string libraryPath)
    {
        try
        {
            var pc = Path.Combine(Path.GetDirectoryName(libraryPath) ?? string.Empty, "pkgconfig", "libadwaita-1.pc");
            if (!File.Exists(pc))
            {
                return null;
            }

            foreach (var line in File.ReadLines(pc))
            {
                if (line.StartsWith("Version:", StringComparison.Ordinal))
                {
                    return line["Version:".Length..].Trim();
                }
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }

        return null;
    }

    /// <summary>Reads the resource bundle in the library's stylesheet section.</summary>
    internal static GvdbReader? ReadBundle(string libraryPath)
    {
        var elf = ElfReader.Open(libraryPath);
        var section = elf.Find(SectionName);
        if (section is null)
        {
            return null;
        }

        return GvdbReader.Open(ElfReader.ReadSection(libraryPath, section));
    }

    private static StyleSheetSource? ReadInstalled(string lib, out string reason)
    {
        var bundle = ReadBundle(lib);
        if (bundle is null)
        {
            reason = $"no {SectionName} section in {lib}";
            return null;
        }

        var bytes = bundle.ReadResource(CssPath);
        if (bytes is null)
        {
            reason = $"no {CssPath} in {lib}";
            return null;
        }

        var css = Encoding.UTF8.GetString(bytes);
        var assets = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        foreach (var name in AssetNames)
        {
            if (bundle.ReadResource("/org/gnome/Adwaita/styles/assets/" + name) is { } a)
            {
                assets[name] = a;
            }
        }

        var version = VersionFromPath(lib) ?? VersionFromPkgConfig(lib) ?? (css == Bundled().Css ? BundledVersion : null);
        var clean = IsKnownClean(css, lib);
        if (!clean)
        {
            var sheet = CssStyleSheet.Parse(css, CssOrigin.Theme, "gtk.css", StyleSheetSource.BaseUri);
            var problem = sheet.Errors.FirstOrDefault(e => e.Kind != CssErrorKind.Warning);
            if (problem is not null)
            {
                reason = $"{lib}: {sheet.Errors.Count(e => e.Kind != CssErrorKind.Warning)} problems, first at {problem}";
                return null;
            }

            RememberClean(css, lib);
        }

        reason = string.Empty;
        return new StyleSheetSource(true, lib, version, css, assets, true, null);
    }

    // The verdict is cached by content, so a library that parsed cleanly once is parsed lazily after.
    private static string Hash(string css) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(css)));

    private static bool IsKnownClean(string css, string lib)
    {
        var hash = Hash(css);
        if (hash == Hash(Bundled().Css))
        {
            return true;
        }

        var file = VerdictFile(hash);
        return file is not null && File.Exists(file);
    }

    private static void RememberClean(string css, string lib)
    {
        try
        {
            var file = VerdictFile(Hash(css));
            if (file is not null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(file)!);
                File.WriteAllText(file, lib + "\n");
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
        }
    }

    private static string? VerdictFile(string hash)
    {
        var cache = Environment.GetEnvironmentVariable("XDG_CACHE_HOME");
        if (string.IsNullOrEmpty(cache))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (string.IsNullOrEmpty(home))
            {
                return null;
            }

            cache = Path.Combine(home, ".cache");
        }

        var engine = typeof(StyleSheetLoader).Assembly.GetName().Version?.ToString() ?? "0";
        return Path.Combine(cache, "avaadwaita", $"clean-{engine}-{hash[..32]}");
    }
}
