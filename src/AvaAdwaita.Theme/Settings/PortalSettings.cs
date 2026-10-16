using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;

namespace AvaAdwaita.Settings;

/// <summary>
/// Reads and follows the desktop settings from <c>org.freedesktop.portal.Settings</c>: the
/// appearance keys, and the <c>org.gnome.desktop.*</c> namespaces the GNOME backend exposes.
/// </summary>
/// <remarks>Follows adw-settings-impl-portal.c.</remarks>
public sealed class PortalSettings : ISettingsSource
{
    private const string Destination = "org.freedesktop.portal.Desktop";
    private const string ObjectPath = "/org/freedesktop/portal/desktop";
    private const string Interface = "org.freedesktop.portal.Settings";

    private static readonly string[] s_namespaces =
    [
        "org.freedesktop.appearance",
        "org.gnome.desktop.interface",
        "org.gnome.desktop.a11y.interface",
        "org.gnome.desktop.wm.preferences",
    ];

    private readonly DBusConnection _connection;
    private readonly Dictionary<(string, string), VariantValue> _values = new();
    private readonly object _lock = new();
    private IDisposable? _watch;
    private SettingsValues _current = SettingsValues.Empty;

    private PortalSettings(DBusConnection connection)
    {
        _connection = connection;
    }

    /// <inheritdoc/>
    public string Name => "portal";

    /// <inheritdoc/>
    public SettingsValues Current => _current;

    /// <summary>The portal's Settings interface version.</summary>
    public uint Version { get; private set; }

    /// <summary>The namespaces the portal returned values for.</summary>
    public IReadOnlyCollection<string> Namespaces
    {
        get
        {
            lock (_lock)
            {
                var set = new HashSet<string>();
                foreach (var (ns, _) in _values.Keys)
                {
                    set.Add(ns);
                }

                return set;
            }
        }
    }

    /// <inheritdoc/>
    public event EventHandler? Changed;

    /// <summary>Connects to the session bus and reads the settings, or returns null when there is no portal.</summary>
    public static async Task<PortalSettings?> TryCreateAsync(DBusConnection? connection = null)
    {
        try
        {
            var conn = connection ?? DBusConnection.Session;
            var portal = new PortalSettings(conn);
            await portal.StartAsync().ConfigureAwait(false);
            return portal;
        }
        catch (Exception e) when (e is DBusExceptionBase or System.IO.IOException or System.Net.Sockets.SocketException or InvalidOperationException or ArgumentException)
        {
            return null;
        }
    }

    private async Task StartAsync()
    {
        Version = await GetVersionAsync().ConfigureAwait(false);
        _watch = await _connection.WatchSignalAsync(
            Destination,
            ObjectPath,
            Interface,
            "SettingChanged",
            static (Message m, object? _) =>
            {
                var r = m.GetBodyReader();
                return (r.ReadString(), r.ReadString(), r.ReadVariantValue());
            },
            (Notification<(string Namespace, string Key, VariantValue Value)> n) =>
            {
                if (n.HasValue)
                {
                    var change = n.Value;
                    OnSettingChanged(change.Namespace, change.Key, change.Value);
                }
            },
            ObserverFlags.None,
            emitOnCapturedContext: false).ConfigureAwait(false);

        var all = await ReadAllAsync().ConfigureAwait(false);
        lock (_lock)
        {
            foreach (var (k, v) in all)
            {
                _values[k] = v;
            }

            _current = Build();
        }
    }

    private Task<uint> GetVersionAsync()
    {
        MessageBuffer Create()
        {
            using var w = _connection.GetMessageWriter();
            w.WriteMethodCallHeader(Destination, ObjectPath, "org.freedesktop.DBus.Properties", "Get", "ss");
            w.WriteString(Interface);
            w.WriteString("version");
            return w.CreateMessage();
        }

        return _connection.CallMethodAsync(Create(), static (Message m, object? _) => m.GetBodyReader().ReadVariantValue().GetUInt32());
    }

    private Task<List<((string, string), VariantValue)>> ReadAllAsync()
    {
        MessageBuffer Create()
        {
            using var w = _connection.GetMessageWriter();
            w.WriteMethodCallHeader(Destination, ObjectPath, Interface, "ReadAll", "as");
            w.WriteArray(s_namespaces);
            return w.CreateMessage();
        }

        return _connection.CallMethodAsync(Create(), static (Message m, object? _) =>
        {
            var list = new List<((string, string), VariantValue)>();
            var r = m.GetBodyReader();
            var outer = r.ReadDictionaryStart();
            while (r.HasNext(outer))
            {
                var ns = r.ReadString();
                var inner = r.ReadDictionaryStart();
                while (r.HasNext(inner))
                {
                    var key = r.ReadString();
                    var value = r.ReadVariantValue();
                    list.Add(((ns, key), value));
                }
            }

            return list;
        });
    }

    private void OnSettingChanged(string ns, string key, VariantValue value)
    {
        if (Array.IndexOf(s_namespaces, ns) < 0)
        {
            return;
        }

        SettingsValues next;
        lock (_lock)
        {
            _values[(ns, key)] = value;
            next = Build();
            if (next == _current)
            {
                return;
            }

            _current = next;
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    // A v1 portal wraps Read() replies in an extra variant; ReadAll and SettingChanged may too on some backends.
    private static VariantValue Unwrap(VariantValue v)
    {
        while (v.Type == VariantValueType.Variant)
        {
            v = v.GetVariantValue();
        }

        return v;
    }

    private bool TryGet(string ns, string key, VariantValueType type, out VariantValue value)
    {
        if (_values.TryGetValue((ns, key), out var raw))
        {
            value = Unwrap(raw);
            return value.Type == type;
        }

        value = default;
        return false;
    }

    private string? GetString(string ns, string key) => TryGet(ns, key, VariantValueType.String, out var v) ? v.GetString() : null;

    private bool? GetBool(string ns, string key) => TryGet(ns, key, VariantValueType.Bool, out var v) ? v.GetBool() : null;

    private uint? GetUInt(string ns, string key) => TryGet(ns, key, VariantValueType.UInt32, out var v) ? v.GetUInt32() : null;

    private SettingsValues Build()
    {
        const string Appearance = "org.freedesktop.appearance";
        const string Gnome = "org.gnome.desktop.interface";
        SystemColorScheme? scheme = GetUInt(Appearance, "color-scheme") switch
        {
            null => null,
            1 => SystemColorScheme.PreferDark,
            2 => SystemColorScheme.PreferLight,
            _ => SystemColorScheme.Default,
        };

        (double, double, double)? accent = null;
        if (TryGet(Appearance, "accent-color", VariantValueType.Struct, out var a))
        {
            var r = a.GetItem(0).GetDouble();
            var g = a.GetItem(1).GetDouble();
            var b = a.GetItem(2).GetDouble();
            accent = r < 0 || g < 0 || b < 0 || r > 1 || g > 1 || b > 1 ? (0.2078, 0.5176, 0.8941) : (r, g, b);
        }

        bool? highContrast = GetUInt(Appearance, "contrast") is { } c ? c == 1 : GetBool("org.gnome.desktop.a11y.interface", "high-contrast");
        bool? reducedMotion = GetUInt(Appearance, "reduced-motion") is { } rm ? rm == 1 : null;
        return new SettingsValues
        {
            ColorScheme = scheme,
            AccentRgb = accent,
            AccentPreset = accent is null ? AccentColors.ParsePreset(GetString(Gnome, "accent-color")) : null,
            HighContrast = highContrast,
            ReducedMotion = reducedMotion,
            EnableAnimations = GetBool(Gnome, "enable-animations"),
            FontName = GetString(Gnome, "font-name"),
            DocumentFontName = GetString(Gnome, "document-font-name"),
            MonospaceFontName = GetString(Gnome, "monospace-font-name"),
            IconTheme = GetString(Gnome, "icon-theme"),
            ButtonLayout = GetString("org.gnome.desktop.wm.preferences", "button-layout"),
            ShowStatusShapes = GetBool("org.gnome.desktop.a11y.interface", "show-status-shapes"),
        };
    }

    /// <inheritdoc/>
    public void Dispose() => _watch?.Dispose();
}
