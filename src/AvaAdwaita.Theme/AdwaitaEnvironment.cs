using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Threading;
using AvaAdwaita.CssEngine.Cascade;
using AvaAdwaita.Nodes;
using AvaAdwaita.Settings;
using AvaAdwaita.Sources;

namespace AvaAdwaita;

/// <summary>
/// The process-wide state: the stylesheet, the runtime providers, the settings, and the style engine. Most
/// applications use <see cref="AdwaitaTheme"/>; this is for tools and diagnostics.
/// Settings changes arrive on any thread and are applied on the UI thread.
/// </summary>
public static class AdwaitaEnvironment
{
    private static readonly object s_lock = new();
    private static Task<Loaded>? s_preload;
    private static bool s_initialized;
    private static CssStyleSheet? s_accent;
    private static CssStyleSheet? s_fonts;
    private static readonly List<CssStyleSheet> s_appSheets = new();
    private static long s_transitionsResumeAt;

    private sealed record Loaded(StyleSheetSource Source, CssStyleSheet Sheet, List<ISettingsSource> Settings);

    /// <summary>The options the environment was loaded with.</summary>
    public static AvaAdwaitaOptions Options { get; private set; } = new();

    /// <summary>The stylesheets in cascade order: libadwaita's, the runtime providers, the application's.</summary>
    public static CssStyleSet Styles { get; } = new();

    /// <summary>The engine that resolves every node.</summary>
    public static CssStyleEngine Engine { get; } = new(Styles);

    /// <summary>The merged settings and the theme's overrides.</summary>
    public static StyleState State { get; } = new();

    /// <summary>Where libadwaita's stylesheet came from.</summary>
    public static StyleSheetSource Source { get; private set; } = null!;

    /// <summary>True once the stylesheet is loaded and the settings are read.</summary>
    public static bool IsInitialized => s_initialized;

    /// <summary>False keeps <c>:backdrop</c> off, for tests and screenshots of inactive windows.</summary>
    public static bool BackdropEnabled { get; set; } = true;

    /// <summary>Raised on the UI thread after the style state changes and the providers are rebuilt.</summary>
    public static event Action<StyleStateChange>? Changed;

    /// <summary>True while transitions are off: during a color scheme switch, or when animations are disabled.</summary>
    public static bool TransitionsSuppressed => !State.AnimationsEnabled || Environment.TickCount64 < Interlocked.Read(ref s_transitionsResumeAt);

    /// <summary>Ends a scheme-switch pause in transitions now, for tests.</summary>
    internal static void ResumeTransitions() => Interlocked.Exchange(ref s_transitionsResumeAt, 0);

    /// <summary>Shows style changes at once until <see cref="ResumeTransitions()"/>, for tests that compare settled styles.</summary>
    internal static void SuppressTransitions() => Interlocked.Exchange(ref s_transitionsResumeAt, long.MaxValue);

    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Avalonia.Controls.TopLevel, object> s_settledWindows = new();

    /// <summary>
    /// Shows style changes in <paramref name="window"/> and its popups at once until <see cref="ResumeTransitions(Avalonia.Controls.TopLevel)"/>,
    /// for a test comparing settled styles while other tests on the same UI thread turn transitions on and off.
    /// </summary>
    internal static void SuppressTransitions(Avalonia.Controls.TopLevel window) => s_settledWindows.AddOrUpdate(window, window);

    /// <summary>Ends <see cref="SuppressTransitions(Avalonia.Controls.TopLevel)"/>.</summary>
    internal static void ResumeTransitions(Avalonia.Controls.TopLevel window) => s_settledWindows.Remove(window);

    /// <summary>True while transitions are off for <paramref name="visual"/>: everywhere, or in its window.</summary>
    internal static bool TransitionsSuppressedFor(Visual visual)
    {
        if (TransitionsSuppressed)
        {
            return true;
        }

        var top = Avalonia.Controls.TopLevel.GetTopLevel(visual);
        while (top is Avalonia.Controls.Primitives.PopupRoot { ParentTopLevel: var parent } && parent != top)
        {
            top = parent;
        }

        return top is not null && s_settledWindows.TryGetValue(top, out _);
    }

    /// <summary>Starts loading the stylesheet and reading the settings on a background thread.</summary>
    public static void Preload(AvaAdwaitaOptions? options = null)
    {
        lock (s_lock)
        {
            if (s_preload is not null || s_initialized)
            {
                return;
            }

            Options = options ?? new AvaAdwaitaOptions();
            var opts = Options;
            s_preload = Task.Run(() => Load(opts));
        }
    }

    private static Loaded Load(AvaAdwaitaOptions options)
    {
        var source = StyleSheetLoader.Load(options.CssSource, msg => Console.Error.WriteLine(msg));
        var sheet = CssStyleSheet.Parse(source.Css, CssOrigin.Theme, "gtk.css", StyleSheetSource.BaseUri, lazy: source.IsKnownClean);
        var settings = new List<ISettingsSource> { new DebugEnvSettings() };
        if (options.UseSystemSettings && OperatingSystem.IsLinux())
        {
            var portal = PortalSettings.TryCreateAsync();
            if (portal.Wait(TimeSpan.FromSeconds(1)) && portal.Result is { } p)
            {
                settings.Add(p);
            }

            if (!File.Exists("/.flatpak-info"))
            {
                settings.Add(new DconfSettings());
            }
        }

        return new Loaded(source, sheet, settings);
    }

    /// <summary>Finishes initialization; waits for the preload when one is running.</summary>
    public static void EnsureInitialized()
    {
        if (s_initialized)
        {
            return;
        }

        Task<Loaded> task;
        lock (s_lock)
        {
            if (s_initialized)
            {
                return;
            }

            s_preload ??= Task.FromResult(Load(Options));
            task = s_preload;
        }

        var loaded = task.GetAwaiter().GetResult();
        lock (s_lock)
        {
            if (s_initialized)
            {
                return;
            }

            Source = loaded.Source;
            foreach (var s in loaded.Settings)
            {
                State.AddSource(s);
            }

            if (Options.UseSystemSettings && !OperatingSystem.IsLinux() && Application.Current?.PlatformSettings is { } ps)
            {
                State.AddSource(new PlatformSettingsSource(ps));
            }

            Styles.Add(loaded.Sheet);
            s_accent = CssStyleSheet.Parse(State.AccentCss(), CssOrigin.Theme, "accent");
            s_fonts = CssStyleSheet.Parse(State.FontsCss(), CssOrigin.Theme, "fonts");
            Styles.Add(s_accent);
            Styles.Add(s_fonts);
            if (Options.LoadUserStyleSheet)
            {
                LoadUserSheet();
            }

            ApplyStateToEngine();
            Engine.ResolveRequested = () => Dispatcher.UIThread.Post(Engine.ResolveNow, DispatcherPriority.Render);
            State.Changed += (_, change) =>
            {
                if (Dispatcher.UIThread.CheckAccess())
                {
                    OnStateChanged(change);
                }
                else
                {
                    Dispatcher.UIThread.Post(() => OnStateChanged(change));
                }
            };
            s_initialized = true;
        }
    }

    private static void LoadUserSheet()
    {
        if (ReadUserSheet(UserStyleSheetPath()) is { } sheet)
        {
            Styles.Add(sheet);
        }
    }

    /// <summary>GTK's user stylesheet: <c>$XDG_CONFIG_HOME/gtk-4.0/gtk.css</c>, with <c>~/.config</c> when the variable is unset.</summary>
    internal static string UserStyleSheetPath()
    {
        var config = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        if (string.IsNullOrEmpty(config))
        {
            config = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
        }

        return Path.Combine(config, "gtk-4.0", "gtk.css");
    }

    /// <summary>The user stylesheet at user priority, or null when there is none or it cannot be read.</summary>
    internal static CssStyleSheet? ReadUserSheet(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return null;
            }

            var dir = Path.GetDirectoryName(Path.GetFullPath(path))!;
            return CssStyleSheet.Parse(File.ReadAllText(path), CssOrigin.User, path, new Uri(dir + Path.DirectorySeparatorChar).AbsoluteUri);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine($"AvaAdwaita: could not read {path}: {e.Message}");
            return null;
        }
    }

    private static void ApplyStateToEngine()
    {
        var f = State.InterfaceFont;
        Engine.Environment = new CssEngine.Properties.CssEnvironment(f.Family ?? "Sans", f.SizePx ?? (10 * 96.0 / 72.0));
        Engine.Media = State.Media;
    }

    private static void OnStateChanged(StyleStateChange change)
    {
        if ((change & StyleStateChange.ColorScheme) != 0)
        {
            // libadwaita turns transitions off for 250 ms while the scheme switches.
            Interlocked.Exchange(ref s_transitionsResumeAt, Environment.TickCount64 + 250);
        }

        if ((change & StyleStateChange.Accent) != 0)
        {
            var next = CssStyleSheet.Parse(State.AccentCss(), CssOrigin.Theme, "accent");
            Styles.Replace(s_accent, next);
            s_accent = next;
        }

        if ((change & StyleStateChange.Fonts) != 0)
        {
            var next = CssStyleSheet.Parse(State.FontsCss(), CssOrigin.Theme, "fonts");
            Styles.Replace(s_fonts, next);
            s_fonts = next;
        }

        ApplyStateToEngine();
        Changed?.Invoke(change);
    }

    /// <summary>Replaces the application style sheets.</summary>
    public static void SetAppStyleSheets(IEnumerable<CssStyleSheet> sheets)
    {
        EnsureInitialized();
        foreach (var s in s_appSheets)
        {
            Styles.Remove(s);
        }

        s_appSheets.Clear();
        foreach (var s in sheets)
        {
            s_appSheets.Add(s);
            Styles.Add(s);
        }
    }

    /// <summary>Reads an asset the stylesheet refers to, as <c>resource:///org/gnome/Adwaita/styles/assets/check.svg</c>.</summary>
    public static byte[]? ReadAsset(string url)
    {
        const string Prefix = StyleSheetSource.BaseUri + "assets/";
        if (url.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return Source?.Assets.GetValueOrDefault(url[Prefix.Length..]);
        }

        return null;
    }

    /// <summary>Resets everything, for tests.</summary>
    internal static void ResetForTests(AvaAdwaitaOptions options)
    {
        lock (s_lock)
        {
            State.ClearSources();
            foreach (var s in Styles.Sheets)
            {
                Styles.Remove(s);
            }

            foreach (var r in Engine.Roots.ToArrayCopy())
            {
                Engine.RemoveRoot(r);
            }

            s_appSheets.Clear();
            s_preload = null;
            s_initialized = false;
            Options = options;
        }
    }

    private static CssNode[] ToArrayCopy(this IReadOnlyList<CssNode> list)
    {
        var a = new CssNode[list.Count];
        for (var i = 0; i < a.Length; i++)
        {
            a[i] = list[i];
        }

        return a;
    }
}
