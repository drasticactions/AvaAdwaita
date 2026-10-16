using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;

namespace AvaAdwaita.Theme.Tests;

/// <summary>
/// A private session bus (dbus-daemon) with a fake <c>org.freedesktop.portal.Desktop</c> that serves the
/// Settings interface, version 2, from a dictionary and emits SettingChanged when a value is set.
/// </summary>
internal sealed class FakePortal : IPathMethodHandler, IAsyncDisposable
{
    private const string Interface = "org.freedesktop.portal.Settings";
    private readonly Dictionary<(string Ns, string Key), VariantValue> _values = new();
    private Process? _daemon;
    private DBusConnection? _server;

    private FakePortal()
    {
    }

    public string Address { get; private set; } = string.Empty;

    public string Path => "/org/freedesktop/portal/desktop";

    public bool HandlesChildPaths => false;

    /// <summary>Starts the bus and the portal, or returns null when dbus-daemon is not installed.</summary>
    public static async Task<FakePortal?> StartAsync(IEnumerable<((string, string), VariantValue)> values)
    {
        var exe = new[] { "/usr/bin/dbus-daemon", "/bin/dbus-daemon" }.FirstOrDefault(File.Exists);
        if (exe is null)
        {
            return null;
        }

        var portal = new FakePortal();
        foreach (var (k, v) in values)
        {
            portal._values[k] = v;
        }

        var dir = Directory.CreateTempSubdirectory("avaadwaita-bus");
        var psi = new ProcessStartInfo(exe, ["--session", "--nofork", "--print-address=1", $"--address=unix:path={System.IO.Path.Combine(dir.FullName, "bus")}"])
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
        };
        portal._daemon = Process.Start(psi)!;
        portal.Address = (await portal._daemon.StandardOutput.ReadLineAsync())!.Trim();
        portal._server = new DBusConnection(portal.Address);
        await portal._server.ConnectAsync();
        await portal._server.RequestNameAsync("org.freedesktop.portal.Desktop");
        portal._server.AddMethodHandler(portal);
        return portal;
    }

    public void Set(string ns, string key, VariantValue value)
    {
        _values[(ns, key)] = value;
        using var w = _server!.GetMessageWriter();
        w.WriteSignalHeader(null, Path, Interface, "SettingChanged", "ssv");
        w.WriteString(ns);
        w.WriteString(key);
        w.WriteVariant(value);
        _server.TrySendMessage(w.CreateMessage());
    }

    public ValueTask HandleMethodAsync(MethodContext context)
    {
        var request = context.Request;
        var member = request.MemberAsString;
        if (request.InterfaceAsString == "org.freedesktop.DBus.Properties" && member == "Get")
        {
            using var w = context.CreateReplyWriter("v");
            w.WriteVariantUInt32(2);
            context.Reply(w.CreateMessage());
        }
        else if (request.InterfaceAsString == Interface && member == "ReadAll")
        {
            var namespaces = request.GetBodyReader().ReadArrayOfString();
            using var w = context.CreateReplyWriter("a{sa{sv}}");
            var outer = w.WriteDictionaryStart();
            foreach (var group in _values.GroupBy(kv => kv.Key.Ns).Where(g => namespaces.Contains(g.Key)))
            {
                w.WriteDictionaryEntryStart();
                w.WriteString(group.Key);
                var inner = w.WriteDictionaryStart();
                foreach (var kv in group)
                {
                    w.WriteDictionaryEntryStart();
                    w.WriteString(kv.Key.Key);
                    w.WriteVariant(kv.Value);
                }

                w.WriteDictionaryEnd(inner);
            }

            w.WriteDictionaryEnd(outer);
            context.Reply(w.CreateMessage());
        }
        else if (request.InterfaceAsString == Interface && member == "ReadOne")
        {
            var r = request.GetBodyReader();
            var key = (r.ReadString(), r.ReadString());
            if (_values.TryGetValue(key, out var v))
            {
                using var w = context.CreateReplyWriter("v");
                w.WriteVariant(v);
                context.Reply(w.CreateMessage());
            }
            else
            {
                context.ReplyError("org.freedesktop.portal.Error.NotFound", "no such key");
            }
        }
        else
        {
            context.ReplyUnknownMethodError();
        }

        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        _server?.Dispose();
        if (_daemon is { HasExited: false })
        {
            _daemon.Kill();
            await _daemon.WaitForExitAsync();
        }

        _daemon?.Dispose();
    }
}
