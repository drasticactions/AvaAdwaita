using System;
using System.Collections.Generic;
using System.IO;
using AvaAdwaita.Sources.Gvdb;

namespace AvaAdwaita.Settings;

/// <summary>
/// Reads the GSettings keys AvaAdwaita uses without GLib: the dconf user and system databases, and
/// the schema defaults in <c>gschemas.compiled</c>, all with the managed GVDB reader.
/// </summary>
public sealed class DconfSettings : ISettingsSource
{
    private readonly List<GvdbReader> _databases = new();
    private readonly Dictionary<string, GvdbReader> _schemas = new(StringComparer.Ordinal);
    private readonly FileSystemWatcher? _watcher;
    private readonly string? _userDb;
    private SettingsValues _current = SettingsValues.Empty;

    /// <summary>Opens the databases. Missing or unreadable files are skipped.</summary>
    public DconfSettings(bool watch = true)
    {
        _userDb = UserDatabasePath();
        LoadSchemas();
        Reload();
        if (watch && _userDb is not null && Directory.Exists(Path.GetDirectoryName(_userDb)))
        {
            try
            {
                _watcher = new FileSystemWatcher(Path.GetDirectoryName(_userDb)!, Path.GetFileName(_userDb))
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
                };
                _watcher.Changed += (_, _) => OnFileChanged();
                _watcher.Created += (_, _) => OnFileChanged();
                _watcher.Renamed += (_, _) => OnFileChanged();
                _watcher.EnableRaisingEvents = true;
            }
            catch (Exception e) when (e is IOException or ArgumentException or PlatformNotSupportedException)
            {
                _watcher = null;
            }
        }
    }

    /// <inheritdoc/>
    public string Name => "dconf";

    /// <inheritdoc/>
    public SettingsValues Current => _current;

    /// <inheritdoc/>
    public event EventHandler? Changed;

    private void OnFileChanged()
    {
        var old = _current;
        Reload();
        if (old != _current)
        {
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    private static string? UserDatabasePath()
    {
        var config = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        if (string.IsNullOrEmpty(config))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (string.IsNullOrEmpty(home))
            {
                return null;
            }

            config = Path.Combine(home, ".config");
        }

        return Path.Combine(config, "dconf", "user");
    }

    private static GvdbReader? TryOpen(string path)
    {
        try
        {
            return File.Exists(path) ? GvdbReader.Open(File.ReadAllBytes(path)) : null;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private void Reload()
    {
        _databases.Clear();
        if (_userDb is not null && TryOpen(_userDb) is { } user)
        {
            _databases.Add(user);
        }

        // The system databases named by the user profile, as "system-db:local".
        foreach (var line in ReadLines("/etc/dconf/profile/user"))
        {
            var l = line.Trim();
            if (l.StartsWith("system-db:", StringComparison.Ordinal) && TryOpen(Path.Combine("/etc/dconf/db", l[10..])) is { } db)
            {
                _databases.Add(db);
            }
        }

        _current = Build();
    }

    private static IEnumerable<string> ReadLines(string path)
    {
        try
        {
            return File.Exists(path) ? File.ReadAllLines(path) : [];
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    private void LoadSchemas()
    {
        var dirs = Environment.GetEnvironmentVariable("XDG_DATA_DIRS");
        var list = string.IsNullOrEmpty(dirs) ? ["/usr/local/share", "/usr/share"] : dirs.Split(':', StringSplitOptions.RemoveEmptyEntries);
        foreach (var d in list)
        {
            var path = Path.Combine(d, "glib-2.0", "schemas", "gschemas.compiled");
            if (TryOpen(path) is not { } root)
            {
                continue;
            }

            foreach (var schema in new[] { "org.gnome.desktop.interface", "org.gnome.desktop.a11y.interface", "org.gnome.desktop.wm.preferences" })
            {
                if (!_schemas.ContainsKey(schema) && root.GetTable(schema) is { } table)
                {
                    _schemas[schema] = table;
                }
            }
        }
    }

    /// <summary>The value of a key: the user database, then the system databases, then the schema default.</summary>
    public object? Get(string schema, string key)
    {
        var path = "/" + schema.Replace('.', '/') + "/" + key;
        foreach (var db in _databases)
        {
            if (db.GetValue(path) is { } v)
            {
                return v;
            }
        }

        // A schema key is a tuple whose first item is the default value.
        if (_schemas.TryGetValue(schema, out var table) && table.GetValue(key) is object?[] { Length: > 0 } t)
        {
            return t[0];
        }

        return null;
    }

    private SettingsValues Build()
    {
        const string Interface = "org.gnome.desktop.interface";
        SystemColorScheme? scheme = Get(Interface, "color-scheme") switch
        {
            "prefer-dark" => SystemColorScheme.PreferDark,
            "prefer-light" => SystemColorScheme.PreferLight,
            "default" => SystemColorScheme.Default,
            _ => null,
        };

        return new SettingsValues
        {
            ColorScheme = scheme,
            AccentPreset = AccentColors.ParsePreset(Get(Interface, "accent-color") as string),
            HighContrast = Get("org.gnome.desktop.a11y.interface", "high-contrast") as bool?,
            EnableAnimations = Get(Interface, "enable-animations") as bool?,
            FontName = Get(Interface, "font-name") as string,
            DocumentFontName = Get(Interface, "document-font-name") as string,
            MonospaceFontName = Get(Interface, "monospace-font-name") as string,
            IconTheme = Get(Interface, "icon-theme") as string,
            ButtonLayout = Get("org.gnome.desktop.wm.preferences", "button-layout") as string,
            ShowStatusShapes = Get("org.gnome.desktop.a11y.interface", "show-status-shapes") as bool?,
        };
    }

    /// <inheritdoc/>
    public void Dispose() => _watcher?.Dispose();
}
