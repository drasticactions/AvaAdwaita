using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using Avalonia.Media.Imaging;

namespace AvaAdwaita.Icons;

/// <summary>
/// Finds icons in freedesktop icon themes: the configured theme, its <c>Inherits</c> chain, then Adwaita
/// and hicolor as GTK appends them. <c>-symbolic</c> SVGs are recolored.
/// </summary>
/// <remarks>Follows the Icon Theme Specification and gtkicontheme.c's lookup order.</remarks>
public sealed class FreedesktopIconProvider : IIconProvider
{
    private readonly List<Theme> _chain = new();
    private readonly ConcurrentDictionary<(string, int, bool), IconData?> _cache = new();

    /// <summary>Uses <paramref name="themeName"/>, or Adwaita when null.</summary>
    public FreedesktopIconProvider(string? themeName)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        AddTheme(themeName ?? "Adwaita", seen);
        AddTheme("Adwaita", seen);
        AddTheme("hicolor", seen);
    }

    /// <summary>The theme names searched, in order.</summary>
    public IEnumerable<string> ThemeChain
    {
        get
        {
            foreach (var t in _chain)
            {
                yield return t.Name;
            }
        }
    }

    /// <summary>The directories icon themes are searched in.</summary>
    public static IEnumerable<string> BaseDirectories()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var dataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
        if (string.IsNullOrEmpty(dataHome) && !string.IsNullOrEmpty(home))
        {
            dataHome = Path.Combine(home, ".local", "share");
        }

        if (!string.IsNullOrEmpty(dataHome))
        {
            yield return Path.Combine(dataHome, "icons");
        }

        if (!string.IsNullOrEmpty(home))
        {
            yield return Path.Combine(home, ".icons");
        }

        var dirs = Environment.GetEnvironmentVariable("XDG_DATA_DIRS");
        foreach (var d in string.IsNullOrEmpty(dirs) ? ["/usr/local/share", "/usr/share"] : dirs.Split(':', StringSplitOptions.RemoveEmptyEntries))
        {
            yield return Path.Combine(d, "icons");
        }
    }

    private void AddTheme(string name, HashSet<string> seen)
    {
        if (!seen.Add(name) || _chain.Count > 16)
        {
            return;
        }

        var roots = new List<string>();
        foreach (var b in BaseDirectories())
        {
            var dir = Path.Combine(b, name);
            if (Directory.Exists(dir))
            {
                roots.Add(dir);
            }
        }

        if (roots.Count == 0)
        {
            return;
        }

        var theme = new Theme(name, roots);
        _chain.Add(theme);
        foreach (var parent in theme.Inherits)
        {
            AddTheme(parent, seen);
        }
    }

    /// <inheritdoc/>
    public IconData? Lookup(string name, int size, bool rtl) => _cache.GetOrAdd((name, size, rtl), k => Find(k.Item1, k.Item2, k.Item3));

    private IconData? Find(string name, int size, bool rtl)
    {
        foreach (var theme in _chain)
        {
            if (rtl && theme.Find(name + "-rtl", size) is { } rp && Load(name + "-rtl", rp) is { } r)
            {
                return r;
            }

            if (theme.Find(name, size) is { } path && Load(name, path) is { } d)
            {
                return d;
            }
        }

        return null;
    }

    private static IconData? Load(string name, string path)
    {
        try
        {
            if (path.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
            {
                var svg = SymbolicSvg.Parse(File.ReadAllBytes(path));
                return svg is null ? null : new IconData(name, svg, null, name.EndsWith("-symbolic", StringComparison.Ordinal), path);
            }

            using var s = File.OpenRead(path);
            return new IconData(name, null, new Bitmap(s), false, path);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException)
        {
            return null;
        }
    }

    private sealed class Theme
    {
        private readonly List<Dir> _dirs = new();
        private readonly List<string> _roots;
        private Dictionary<string, List<(Dir Dir, string Path)>>? _index;

        public Theme(string name, List<string> roots)
        {
            Name = name;
            _roots = roots;
            foreach (var root in roots)
            {
                var index = Path.Combine(root, "index.theme");
                if (File.Exists(index))
                {
                    ParseIndex(index);
                    break;
                }
            }
        }

        public string Name { get; }

        public List<string> Inherits { get; } = new();

        private void ParseIndex(string path)
        {
            string? section = null;
            var values = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
            foreach (var raw in File.ReadLines(path))
            {
                var line = raw.Trim();
                if (line.Length == 0 || line[0] == '#')
                {
                    continue;
                }

                if (line[0] == '[' && line[^1] == ']')
                {
                    section = line[1..^1];
                    values[section] = new Dictionary<string, string>(StringComparer.Ordinal);
                    continue;
                }

                var eq = line.IndexOf('=');
                if (section is not null && eq > 0)
                {
                    values[section][line[..eq].Trim()] = line[(eq + 1)..].Trim();
                }
            }

            if (!values.TryGetValue("Icon Theme", out var main))
            {
                return;
            }

            if (main.TryGetValue("Inherits", out var inh))
            {
                Inherits.AddRange(inh.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            }

            var dirs = new List<string>();
            foreach (var key in new[] { "Directories", "ScaledDirectories" })
            {
                if (main.TryGetValue(key, out var list))
                {
                    dirs.AddRange(list.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                }
            }

            foreach (var d in dirs)
            {
                if (!values.TryGetValue(d, out var v))
                {
                    continue;
                }

                int Int(string k, int def) => v.TryGetValue(k, out var s) && int.TryParse(s, out var i) ? i : def;
                var size = Int("Size", 16);
                var type = v.TryGetValue("Type", out var t) ? t : "Threshold";
                _dirs.Add(new Dir(d, size, Int("Scale", 1), type, Int("MinSize", size), Int("MaxSize", size), Int("Threshold", 2)));
            }
        }

        public string? Find(string name, int size)
        {
            _index ??= BuildIndex();
            if (!_index.TryGetValue(name, out var candidates))
            {
                return null;
            }

            // Icon Theme Specification, LookupIcon: an exact size match, else the closest.
            string? best = null;
            var bestDistance = int.MaxValue;
            foreach (var (dir, path) in candidates)
            {
                if (dir.Scale != 1)
                {
                    continue;
                }

                var distance = dir.Distance(size);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = path;
                }
            }

            return best ?? candidates[0].Path;
        }

        private Dictionary<string, List<(Dir, string)>> BuildIndex()
        {
            var index = new Dictionary<string, List<(Dir, string)>>(StringComparer.Ordinal);
            foreach (var root in _roots)
            {
                foreach (var dir in _dirs)
                {
                    var full = Path.Combine(root, dir.Path);
                    if (!Directory.Exists(full))
                    {
                        continue;
                    }

                    try
                    {
                        foreach (var file in Directory.EnumerateFiles(full))
                        {
                            var ext = Path.GetExtension(file);
                            if (ext is not (".svg" or ".png"))
                            {
                                continue;
                            }

                            var n = Path.GetFileNameWithoutExtension(file);
                            if (!index.TryGetValue(n, out var list))
                            {
                                list = new List<(Dir, string)>();
                                index[n] = list;
                            }

                            list.Add((dir, file));
                        }
                    }
                    catch (Exception e) when (e is IOException or UnauthorizedAccessException)
                    {
                    }
                }
            }

            return index;
        }
    }

    private sealed record Dir(string Path, int Size, int Scale, string Type, int MinSize, int MaxSize, int Threshold)
    {
        public int Distance(int size) => Type switch
        {
            "Scalable" => size < MinSize ? MinSize - size : size > MaxSize ? size - MaxSize : 0,
            "Fixed" => Math.Abs(Size - size),
            _ => size < Size - Threshold ? Size - Threshold - size : size > Size + Threshold ? size - Size - Threshold : 0,
        };
    }
}
